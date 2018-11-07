﻿using System;
using System.Collections.Generic;
using System.Linq;
using LogManager;
using MAP_DEVTYPE_DEVATTRI = System.Collections.Generic.Dictionary<NetPlan.EnumDevType, System.Collections.Generic.List<NetPlan.DevAttributeInfo>>;

namespace NetPlan
{
	/// <summary>
	/// 网规中连接的管理
	/// </summary>
	internal class NetPlanLinkMgr
	{
		#region 公共接口

		internal void Clear()
		{
			lock (m_syncObj)
			{
				m_listWaitPrevRru.Clear();
				m_mapLinks.Clear();
				m_listParsedRru.Clear();
			}
		}

		/// <summary>
		/// 从MIB信息中解析出所有的连接信息，保存在本地
		/// </summary>
		/// <param name="mapMibInfo"></param>
		/// <returns></returns>
		internal bool ParseLinksFromMibInfo(MAP_DEVTYPE_DEVATTRI mapMibInfo)
		{
			// 解析新的信息，删除旧的信息
			Clear();

			if (null == mapMibInfo || 0 == mapMibInfo.Count)
			{
				Log.Error("传入MIB信息为空");
				return false;
			}

			if (!mapMibInfo.ContainsKey(EnumDevType.board))
			{
				Log.Error($"传入的MIB信息中不包含board信息");
				return false;
			}

			if (!mapMibInfo.ContainsKey(EnumDevType.rru))
			{
				Log.Error("传入的MIB信息中不包含rru信息");
				return false;
			}

			var boardList = mapMibInfo[EnumDevType.board];
			var rruList = mapMibInfo[EnumDevType.rru];
			if (null == boardList || null == rruList || boardList.Count == 0 || rruList.Count == 0)
			{
				Log.Error("传入的MIB信息中board信息或rru信息缺失");
				return false;
			}

			// 解析board与rru之间的连接
			if (!ParseBoardToRruLink(rruList, boardList))
			{
				Log.Error("解析board与rru之间的连接信息失败");
				return false;
			}

			if (!mapMibInfo.ContainsKey(EnumDevType.rru_ant))
			{
				return true;
			}

			var rruAntCfgList = mapMibInfo[EnumDevType.rru_ant];
			if (0 == rruAntCfgList.Count)
			{
				return true;
			}

			if (0 != rruAntCfgList.Count && !mapMibInfo.ContainsKey(EnumDevType.ant))
			{
				Log.Error($"天线安装配置信息不为空，但天线信息为空");
				return false;
			}

			var antList = mapMibInfo[EnumDevType.ant];

			// 解析rru与ant之间的连接
			if (!ParseRruToAntLinks(rruAntCfgList, rruList, antList))
			{
				Log.Error("解析rru到天线阵的连接失败");
				return false;
			}

			// 解析board与rhub之间的连接
			if (!mapMibInfo.ContainsKey(EnumDevType.rhub))
			{
				Log.Debug("未找到RHUB设备类型信息");
				return true;
			}

			var rhubList = mapMibInfo[EnumDevType.rhub];
			if (rhubList.Count == 0)
			{
				Log.Debug("未找到RHUB设备类型信息");
				return true;
			}

			if (!ParseBoardToRhubLink(rhubList, boardList))
			{
				
			}

			// 解析rhub与pico之间的连接


			return true;
		}

		/// <summary>
		/// 获取所有的连接
		/// </summary>
		/// <returns></returns>
		internal Dictionary<EnumDevType, List<WholeLink>> GetAllLink()
		{
			lock (m_syncObj)
			{
				return m_mapLinks;
			}
		}

		internal NetPlanLinkMgr()
		{
			m_mapLinks = new Dictionary<EnumDevType, List<WholeLink>>();
			m_listParsedRru = new List<ParsedRruInfo>();
			m_listWaitPrevRru = new List<ParsedRruInfo>();
		}

		/// <summary>
		/// 增加连接到列表中
		/// </summary>
		/// <param name="srcEndpoint"></param>
		/// <param name="dstEndpoint"></param>
		/// <param name="linkType"></param>
		internal void AddLinkToList(LinkEndpoint srcEndpoint, LinkEndpoint dstEndpoint, EnumDevType linkType)
		{
			var link = new WholeLink
			{
				m_srcEndPoint = srcEndpoint,
				m_dstEndPoint = dstEndpoint,
				m_linkType = linkType
			};

			lock (m_syncObj)
			{
				if (m_mapLinks.ContainsKey(linkType))
				{
					m_mapLinks[linkType].Add(link);
				}
				else
				{
					var listLink = new List<WholeLink> { link };
					m_mapLinks[linkType] = listLink;
				}
			}
		}

		#endregion


		#region 私有方法

		/// <summary>
		/// 解析board与rru之间的连接、rru与rru之间的连接
		/// </summary>
		/// <param name="rruDevList">rru设备列表</param>
		/// <param name="boardDevList">板卡设备列表</param>
		/// <returns></returns>
		private bool ParseBoardToRruLink(IEnumerable<DevAttributeInfo> rruDevList, List<DevAttributeInfo> boardDevList)
		{
			foreach (var rru in rruDevList)
			{
				if (null == rru)
				{
					continue;
				}

				// RRU最多有个4个光口连接板卡获取RRU
				for (var i = 1; i < 5; i++)
				{
					if (!HandleRruOfpLinkInfo(i, rru, boardDevList))
						return false;
				}
			}

			return HandleWaitPrevRruList();
		}

		/// <summary>
		/// 生成一个板卡到rru的连接信息
		/// </summary>
		/// <param name="strBoardIndex"></param>
		/// <param name="nBoardIrPort"></param>
		/// <param name="strRruIndex"></param>
		/// <param name="nRruIrPort"></param>
		/// <returns></returns>
		private bool GenerateBoardToRruLink(string strBoardIndex, int nBoardIrPort, string strRruIndex, int nRruIrPort)
		{
			var boardEndpoint = new LinkEndpoint
			{
				devType = EnumDevType.board,
				strDevIndex = strBoardIndex,
				nPortNo = nBoardIrPort,
				portType = EnumPortType.bbu_to_rru
			};

			var rruEndPoint = new LinkEndpoint
			{
				devType = EnumDevType.rru,
				strDevIndex = strRruIndex,
				nPortNo = nRruIrPort,
				portType = EnumPortType.rru_to_bbu
			};

			AddLinkToList(boardEndpoint, rruEndPoint, EnumDevType.board_rru);

			return true;
		}

		/// <summary>
		/// 生成一条board到rhub设备的连接
		/// </summary>
		/// <param name="strBoardIndex"></param>
		/// <param name="nBoardIrPort"></param>
		/// <param name="strRhubIndex"></param>
		/// <param name="nRhubIrPort"></param>
		/// <returns></returns>
		private bool GenerateBoardToRhubLink(string strBoardIndex, int nBoardIrPort, string strRhubIndex, int nRhubIrPort)
		{
			var boardEndpoint = new LinkEndpoint
			{
				devType = EnumDevType.board,
				strDevIndex = strBoardIndex,
				nPortNo = nBoardIrPort,
				portType = EnumPortType.bbu_to_rhub
			};

			var rruEndPoint = new LinkEndpoint
			{
				devType = EnumDevType.rhub,
				strDevIndex = strRhubIndex,
				nPortNo = nRhubIrPort,
				portType = EnumPortType.rhub_to_bbu
			};

			// todo 此处board到rhub设备的连接类型设置为board_rru，校验功能是否会检查
			AddLinkToList(boardEndpoint, rruEndPoint, EnumDevType.board_rru);
			return true;
		}

		/// <summary>
		/// 生成一个rru到天线阵的连接
		/// </summary>
		/// <param name="strRruIndex"></param>
		/// <param name="nRruPort"></param>
		/// <param name="strAntIndex"></param>
		/// <param name="nAntPathNo"></param>
		/// <returns></returns>
		private bool GenerateRruToAntLink(string strRruIndex, int nRruPort, string strAntIndex, int nAntPathNo)
		{
			var rruEndpoint = new LinkEndpoint
			{
				devType = EnumDevType.rru,
				strDevIndex = strRruIndex,
				nPortNo = nRruPort,
				portType = EnumPortType.rru_to_ant
			};

			var antEndPoint = new LinkEndpoint
			{
				devType = EnumDevType.ant,
				strDevIndex = strAntIndex,
				nPortNo = nAntPathNo,
				portType = EnumPortType.ant_to_rru
			};

			AddLinkToList(rruEndpoint, antEndPoint, EnumDevType.rru_ant);

			return true;
		}

		/// 生成一个rru到rru的连接
		private bool GenerateRruToRruLink(ParsedRruInfo prevRruInfo, ParsedRruInfo curRruInfo)
		{
			if (null == prevRruInfo || null == curRruInfo)
			{
				Log.Error("传入无效的参数");
				return false;
			}

			// todo 上一级rru的连接板卡的光口号与rru之间的级联的光口号的关系如何确定
			var prevRruDownLinePort = -1;	// 上级rru的下联口
			if (1 == prevRruInfo.nRruIrPort)
			{
				prevRruDownLinePort = 2;	// rru在级联模式下，必然区分上联口和下联口。上联口只能是1。TODO rru是否有4个光口的？
				// todo 当前5GIII的AAU设备是4个光口，1主3辅，尚不支持级联模式，后来人要注意(2018.11.2,houshangling)
			}

			var nextRruUpLinePort = curRruInfo.nRruIrPort;		// 上联口
																// rru级联必然是上级rru的下联口连接到下级rru的上联口

			var prevEndpoint = new LinkEndpoint
			{
				devType = EnumDevType.rru,
				strDevIndex = prevRruInfo.strRruIndex,
				nPortNo = prevRruDownLinePort,
				portType = EnumPortType.rru_to_rru
			};

			var curEndPoint = new LinkEndpoint
			{
				devType = EnumDevType.rru,
				strDevIndex = curRruInfo.strRruIndex,
				nPortNo = nextRruUpLinePort,
				portType = EnumPortType.rru_to_rru
			};

			// todo 级联模式，设备类型设定为board to rru
			AddLinkToList(prevEndpoint, curEndPoint, EnumDevType.board_rru);

			return true;
		}

		/// <summary>
		/// 生成一条rhub到rhub的连接
		/// </summary>
		/// <param name="prevRhubInfo"></param>
		/// <param name="curRhubInfo"></param>
		/// <returns></returns>
		private bool GenerateRhubToRhubLink(ParsedRruInfo prevRhubInfo, ParsedRruInfo curRhubInfo)
		{
			throw new NotImplementedException("rhub到rhub的级联模式尚未支持");
		}

		// 处理没有找到上级rru的信息
		private bool HandleWaitPrevRruList()
		{
			var listDel = new List<ParsedRruInfo>();
			// 处理未找到上级rru信息的rru。
			lock (m_syncObj)
			{
				while (m_listWaitPrevRru.Count > 0)
				{
					listDel.Clear();
					for (int i = m_listWaitPrevRru.Count - 1; i >= 0; i--)
					{
						var waitPrevRru = m_listWaitPrevRru.ElementAt(i);
						foreach (var existedRru in m_listParsedRru)
						{
							if (!waitPrevRru.IsPrevRru(existedRru)) continue;

							if (!GenerateRruToRruLink(existedRru, waitPrevRru))
							{
								Log.Error("");
								return false;
							}
						}

						// 把waitPrevRru加入到m_listParsedRru队列
						AddElementToParsedRruList(waitPrevRru);
						listDel.Add(waitPrevRru);
					}

					foreach (var item in listDel)
					{
						m_listWaitPrevRru.Remove(item);
					}
				}
			}
			return true;
		}

		private bool HandleWaitPrevRhubList()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 从已经解析的rru中找到当前rru上一级rru
		/// </summary>
		/// <param name="curRruInfo"></param>
		/// <returns></returns>
		private ParsedRruInfo GetPrevRruInfo(ParsedRruInfo curRruInfo)
		{
			if (null == curRruInfo)
			{
				return null;
			}

			lock (m_syncObj)
			{
				return m_listParsedRru.FirstOrDefault(curRruInfo.IsPrevRru);
			}
		}

		// 处理rru光口连接的信息。todo 应该能从board_rru中读到ir信息
		private bool HandleRruOfpLinkInfo(int nOfpIndex, DevAttributeInfo rru, List<DevAttributeInfo> boardDevList)
		{
			// 取出rru编号、接入板卡的信息
			var rruNo = rru.GetFieldOriginValue("netRRUNo");

			// 级联模式的rru，连接的板卡的信息和第1级rru连接板卡信息一样
			var rackNo = rru.GetFieldOriginValue("netRRUAccessRackNo");
			var shelfNo = rru.GetFieldOriginValue("netRRUAccessShelfNo");
			var boardTypeInRru = rru.GetFieldOriginValue("netRRUAccessBoardType");
			var rruIndex = $".{rruNo}";

			// 级联模式才会有接入级数>1的情况
			var workMode = rru.GetFieldOriginValue("netRRUOfpWorkMode");

			var slotNoMib = "netRRUAccessSlotNo";
			
			if (nOfpIndex > 1)
			{
				slotNoMib = $"netRRUOfp{nOfpIndex}SlotNo";
			}

			// 获取光口n连接的槽位号
			var slotNo = rru.GetFieldOriginValue(slotNoMib);
			if ("-1" == slotNo)
			{
				Log.Debug($"索引为{rruIndex}的RRU光口{nOfpIndex}未连接设备");
				return true;
			}

			var boardIndex = $".{rackNo}.{shelfNo}.{slotNo}";

			// 根据索引获取板卡信息
			var boardInfo = boardDevList.FirstOrDefault(item => item.m_strOidIndex == boardIndex);
			if (null == boardInfo)
			{
				Log.Error($"索引为{rruIndex}的RRU连接板卡索引{boardIndex}，未找到对应的板卡");
				return false;
			}

			// 判断板卡的类型是否相同
			var boardTypeInBoard = boardInfo.GetFieldOriginValue("netBoardType");
			if (boardTypeInBoard != boardTypeInRru)
			{
				Log.Error($"索引为{rruIndex}的RRU连接板卡类型{boardTypeInRru}，而相同索引{boardIndex}的板卡类型为{boardTypeInBoard}，二者不一致");
				return false;
			}

			// 获取每个光口连接的对端光口号和接入级数
			var ofpPortMibName = $"netRRUOfp{nOfpIndex}AccessOfpPortNo";
			var accessPosMibName = $"netRRUOfp{nOfpIndex}AccessLinePosition";

			var remoteOfPort = rru.GetFieldOriginValue(ofpPortMibName);
			if ("-1" == remoteOfPort)
			{
				Log.Debug($"索引为{rruIndex}的RRU光口{nOfpIndex}未连接任何设备");
				return true;
			}

			var accessPos = rru.GetFieldOriginValue(accessPosMibName);
			if ("-1" == accessPos) // todo 需要注意非级联模式设置为默认值-1的情况
			{
				Log.Error($"索引为{rruIndex}的RRU光口{nOfpIndex}连接对端光口{remoteOfPort}，但接入级数为-1，无效信息");
				return false;
			}

			var parsedRru = new ParsedRruInfo
			{
				nBoardIrPort = int.Parse(remoteOfPort),
				nCurLinePos = int.Parse(accessPos),
				nRruIrPort = nOfpIndex,
				strBoardIndex = boardIndex,
				strRruIndex = rruIndex
			};

			// rru连接板卡时，接入级数为1；正常模式，接入级数为1
			if ("1" == accessPos)
			{
				// todo 在netIROptPlanEntry中查找是否存在这条连接。2级级联的设备不存在于这张表中

				if (!GenerateBoardToRruLink(boardIndex, int.Parse(remoteOfPort), rruIndex, nOfpIndex))
				{
					Log.Error("生成板卡到rru的连接失败");
					return false;
				}

				if ("2" == workMode || "5" == workMode)
				{
					AddElementToParsedRruList(parsedRru);
				}
				return true;
			}

			if ("2" != workMode && "5" != workMode)
			{
				Log.Error($"索引为{rruIndex}的RRU工作模式为{workMode}，光口级联级数错误");
				return false;
			}

			// 接入级数大于1，则要找接入级数小1级的设备组成连接信息
			var prevRru = GetPrevRruInfo(parsedRru);
			if (null == prevRru)
			{
				// 没有找到上级rru，放入等待队列
				AddElementToWaitPrevList(parsedRru);
				return true;
			}

			if (!GenerateRruToRruLink(prevRru, parsedRru))
			{
				Log.Error("生成rru到rru的连接失败");
				return false;
			}

			AddElementToParsedRruList(parsedRru);

			return true;
		}

		/// <summary>
		/// 解析rru与天线的连接
		/// </summary>
		/// <param name="rruAntCfgList">天线阵安装规划表记录列表</param>
		/// <param name="rruDevList">rru设备列表</param>
		/// <param name="antDevList">天线阵设备列表</param>
		/// <returns></returns>
		private bool ParseRruToAntLinks(IReadOnlyCollection<DevAttributeInfo> rruAntCfgList, IReadOnlyCollection<DevAttributeInfo> rruDevList,
			IReadOnlyCollection<DevAttributeInfo> antDevList)
		{
			// 遍历天线阵安装规划表
			if (null == rruAntCfgList || 0 == rruAntCfgList.Count)
			{
				return true;
			}

			if (null == rruDevList || 0 == rruDevList.Count)
			{
				Log.Error("天线阵安装规划表信息不为空，但RRU信息为空");
				return false;
			}

			if (null == antDevList || 0 == antDevList.Count)
			{
				Log.Error("天线阵安装规划表信息不为空，但天线信息为空");
				return false;
			}

			foreach (var ra in rruAntCfgList)
			{
				var rruNo = ra.GetFieldOriginValue("netSetRRUNo");
				var rruIndex = $".{rruNo}";

				// 根据rru索引查找rru设备，如果未找到就认为存在错误
				var rru = rruDevList.FirstOrDefault(item => item.m_strOidIndex == rruIndex);
				if (null == rru)
				{
					Log.Error($"根据rru索引{rruIndex}未找到对应的rru设备");
					continue;
				}

				var rruPathNo = ra.GetFieldOriginValue("netSetRRUPortNo");

				var antNo = ra.GetFieldOriginValue("netSetRRUPortAntArrayNo");
				if ("-1" == antNo)
				{
					Log.Debug($"编号为{rruNo}的rru通道{rruPathNo}未连接天线");
					continue;
				}

				var antIndex = $".{antNo}";

				// 根据ant索引查找ant设备
				var ant = antDevList.FirstOrDefault(item => item.m_strOidIndex == antIndex);
				if (null == ant)
				{
					Log.Error($"根据ant索引{antIndex}未找到对应的ant设备");
					continue;
				}

				var antPathNo = ra.GetFieldOriginValue("netSetRRUPortAntArrayPathNo");
				if ("-1" == antPathNo)
				{
					Log.Error($"编号为{rruNo}的rru通道{rruPathNo}连接天线阵{antNo}的通道未设置");
					continue;
				}

				if (!GenerateRruToAntLink(rruIndex, int.Parse(rruPathNo), antIndex, int.Parse(antPathNo)))
				{
					Log.Error($"生成索引为{rruIndex}的rru到索引为{antIndex}的ant连接失败");
					continue;
				}
				Log.Debug($"生成索引为{rruIndex}的rru通道{rruPathNo}到索引为{antIndex}的ant通道{antPathNo}连接成功");
			}

			return true;		// 如果中间有break调用，这里就会返回false
		}

		/// <summary>
		/// 解析板卡到rhub之间的连接
		/// netIROptPlanEntry
		/// </summary>
		/// <param name="rhubList"></param>
		/// <param name="boardList"></param>
		/// <returns></returns>
		private bool ParseBoardToRhubLink(IReadOnlyCollection<DevAttributeInfo> rhubList,
			IReadOnlyCollection<DevAttributeInfo> boardList)
		{
			// rhub设备可能是4个光口连接，也可能是2个光口连接
			foreach (var rhubDai in rhubList)
			{
				// 取出rhub设备，读取连接板卡的架框槽
				if (null == rhubDai)
				{
					continue;
				}

				for (var i = 1; i < 5; i++)
				{
					if (!HandleRhubOfpLinkInfo(i, rhubDai, boardList))
					{
						return false;
					}
				}
			}

			// 5G的pico设备只有一个网口，通过网口与rhub的电口相连
			// 在mib中，netRRUOfp1AccessEthernetPort表示连接的是对端Hub eth口
			return HandleWaitPrevRhubList();
		}

		/// <summary>
		/// 处理rhub设备的光口号信息
		/// </summary>
		/// <param name="nOfpIndex">光口索引，rhub2.0才有4个光口</param>
		/// <param name="rhub">rhub设备属性信息</param>
		/// <param name="boardDevList">板卡信息列表</param>
		/// <returns></returns>
		private bool HandleRhubOfpLinkInfo(int nOfpIndex, DevAttributeInfo rhub, IEnumerable<DevAttributeInfo> boardDevList)
		{
			// 取出rru编号、接入板卡的信息
			var rhubNo = rhub.GetFieldOriginValue("netRHUBNo");

			// 级联模式的rru，连接的板卡的信息和第1级rru连接板卡信息一样
			var rackNo = rhub.GetFieldOriginValue("netRHUBAccessRackNo");
			var shelfNo = rhub.GetFieldOriginValue("netRHUBAccessShelfNo");
			var boardTypeInRhub = rhub.GetFieldOriginValue("netRHUBAccessBoardType");
			var hubIndex = $".{rhubNo}";

			// 级联模式才会有接入级数>1的情况
			var workMode = rhub.GetFieldOriginValue("netRHUBOfpWorkMode");

			var slotNoMib = "netRHUBAccessSlotNo";		// 光口接入的槽位号
			if (nOfpIndex > 1)
			{
				slotNoMib = $"netRHUBOfp{nOfpIndex}SlotNo";
			}

			// 获取光口n连接的槽位号
			var slotNo = rhub.GetFieldOriginValue(slotNoMib);
			if (null == slotNo)
			{
				Log.Debug($"当前版本MIB中不包含{slotNoMib}字段");
				return true;
			}

			if ("-1" == slotNo)
			{
				Log.Debug($"索引为{hubIndex}的RHUB光口{nOfpIndex}未连接设备");
				return true;
			}

			var boardIndex = $".{rackNo}.{shelfNo}.{slotNo}";

			// 根据索引获取板卡信息
			var boardInfo = boardDevList.FirstOrDefault(item => item.m_strOidIndex == boardIndex);
			if (null == boardInfo)
			{
				Log.Error($"索引为{hubIndex}的RHUB连接板卡索引{boardIndex}，未找到对应的板卡");
				return false;
			}

			// 判断板卡的类型是否相同
			var boardTypeInBoard = boardInfo.GetFieldOriginValue("netBoardType");
			if (boardTypeInBoard != boardTypeInRhub)
			{
				Log.Error($"索引为{hubIndex}的RHUB连接板卡类型{boardTypeInRhub}，而相同索引{boardIndex}的板卡类型为{boardTypeInBoard}，二者不一致");
				return false;
			}

			// 获取每个光口连接的对端光口号和接入级数
			var ofpPortMibName = $"netRHUBOfp{nOfpIndex}AccessOfpPortNo";
			var accessPosMibName = $"netRHUBOfp{nOfpIndex}AccessLinePosition";

			var remoteOfPort = rhub.GetFieldOriginValue(ofpPortMibName);
			if (null == remoteOfPort)
			{
				Log.Debug($"当前版本MIB中不包含{ofpPortMibName}字段");
				return true;
			}

			if ("-1" == remoteOfPort)
			{
				Log.Debug($"索引为{hubIndex}的RHUB光口{nOfpIndex}未连接任何设备");
				return true;
			}

			var accessPos = rhub.GetFieldOriginValue(accessPosMibName);
			if (null == accessPos)
			{
				Log.Debug($"当前版本MIB中不包含{accessPosMibName}字段");
				return true;
			}

			if ("-1" == accessPos) // todo 需要注意非级联模式设置为默认值-1的情况
			{
				Log.Error($"索引为{hubIndex}的RRU光口{nOfpIndex}连接对端光口{remoteOfPort}，但接入级数为-1，无效信息");
				return false;
			}

			var parsedRhub = new ParsedRruInfo
			{
				nBoardIrPort = int.Parse(remoteOfPort),
				nCurLinePos = int.Parse(accessPos),
				nRruIrPort = nOfpIndex,
				strBoardIndex = boardIndex,
				strRruIndex = hubIndex
			};

			// rhub连接板卡时，接入级数为1；正常模式，接入级数为1
			if ("1" == accessPos)
			{
				// todo 在netIROptPlanEntry中查找是否存在这条连接。2级级联的设备不存在于这张表中

				if (!GenerateBoardToRhubLink(boardIndex, int.Parse(remoteOfPort), hubIndex, nOfpIndex))
				{
					Log.Error($"生成板卡到rhub{hubIndex}的连接失败");
					return false;
				}

				if ("2" == workMode || "5" == workMode)		// 级联；负荷分担 + 级联
				{
					AddElementToParsedRruList(parsedRhub);
				}
				return true;
			}

			if ("2" != workMode && "5" != workMode)		// 级联；负荷分担+级联
			{
				Log.Error($"索引为{hubIndex}的RHUB工作模式为{workMode}，光口级联级数错误");
				return false;
			}

			// 接入级数大于1，则要找接入级数小1级的设备组成连接信息
			var prevRhub = GetPrevRruInfo(parsedRhub);
			if (null == prevRhub)
			{
				// 没有找到上级rru，放入等待队列
				AddElementToWaitPrevList(parsedRhub);
				return true;
			}

			if (!GenerateRhubToRhubLink(prevRhub, parsedRhub))
			{
				Log.Error("生成rru到rru的连接失败");
				return false;
			}

			AddElementToParsedRruList(parsedRhub);      // 解析完成的就加入到列表中

			return true;
		}


		private void AddElementToParsedRruList(ParsedRruInfo element)
		{
			if (null == element)
			{
				return;
			}

			lock (m_syncObj)
			{
				m_listParsedRru.Add(element);
			}
		}

		private void AddElementToWaitPrevList(ParsedRruInfo element)
		{
			if (null == element)
			{
				return;
			}

			lock (m_syncObj)
			{
				m_listWaitPrevRru.Add(element);
			}
		}

		#endregion


		#region 私有数据区

		private Dictionary<EnumDevType, List<WholeLink>> m_mapLinks;

		private List<ParsedRruInfo> m_listParsedRru;		// 所有的工作模式为级联的rru信息

		private List<ParsedRruInfo> m_listWaitPrevRru;		// 等待上一级的rru

		private readonly object m_syncObj = new object();

		#endregion
	}

	internal class ParsedRruInfo
	{
		internal string strBoardIndex;
		internal int nBoardIrPort;
		internal string strRruIndex;
		internal int nRruIrPort;
		internal int nCurLinePos;           // 这个RRU级联级数

		/// <summary>
		/// 验证传入的rru是否是当前rru的上级rru
		/// </summary>
		/// <param name="wanntedRruInfo"></param>
		internal bool IsPrevRru(ParsedRruInfo wanntedRruInfo)
		{
			if (null == wanntedRruInfo)
			{
				return false;
			}

			if (strBoardIndex != wanntedRruInfo.strBoardIndex)		// 先比对所连板卡是否相同
			{
				return false;
			}

			if (nBoardIrPort != wanntedRruInfo.nBoardIrPort)		// 比对连接板卡的光口号是否相同
			{
				return false;
			}

			return nCurLinePos == (wanntedRruInfo.nCurLinePos + 1);		// 比对级数是否差1
		}
	}
}