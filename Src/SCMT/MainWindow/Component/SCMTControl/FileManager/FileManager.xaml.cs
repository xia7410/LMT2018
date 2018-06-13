﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
//用于显示文件属性对话框
using System.Runtime.InteropServices;
using CommonUility;
using FileManager;
using LogManager;
using SCMTOperationCore.Message.SI;


namespace SCMTMainWindow.Component.SCMTControl.FileManager
{
	/// <summary>
	/// FileManager.xaml 的交互逻辑
	/// </summary>
	public partial class FileManager : UserControl
	{
		/// <summary>
		/// ListView  显示 文件列表，全局变量 
		/// </summary>
		ListView lvFileInfo = new ListView();

		/// <summary>
		/// 当前被选中的文件夹， 全局变量
		/// </summary>
		DirectoryTreeViewItem selectedDTVI;

		/// <summary>
		/// 构造函数，添加 控件  TreeView  ListView  绑定属性，注册事件等
		/// </summary>
		public FileManager()
		{
			InitializeComponent();

			Width = 800;
			Height = 600;

			//定义文件夹树
			DirectoryTreeView mainTree = new DirectoryTreeView();
			mainTree.SelectedItemChanged += MainTree_SelectedItemChanged;
			theGrid.Children.Add(mainTree);
			Grid.SetColumn(mainTree, 0);

			//分隔条
			GridSplitter splite = new GridSplitter();
			splite.Width = 2;
			splite.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
			theGrid.Children.Add(splite);
			Grid.SetColumn(splite, 1);

			//全局ListView
			lvFileInfo = new ListView();
			theGrid.Children.Add(lvFileInfo);
			Grid.SetColumn(lvFileInfo, 2);

			//定义字段，绑定到文件信息类中
			GridView myview = new GridView();
			lvFileInfo.View = myview;
			GridViewColumn mycolun = new GridViewColumn();
			mycolun.Header = "文件名";
			mycolun.Width = 120;

			//文件名是带图标的文件名，需要使用模板
			DataTemplate template = new DataTemplate();

			//模板是一个  包含  Image  控件  和  TextBlock 控件  的 StackPanel
			FrameworkElementFactory fileStackPanel = new FrameworkElementFactory(typeof(StackPanel));

			//定义图标，并绑定
			FrameworkElementFactory fileIcon = new FrameworkElementFactory(typeof(Image));
			fileIcon.SetValue(Image.WidthProperty, 16.0);
			fileIcon.SetValue(Image.HeightProperty, 16.0);
			fileIcon.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Center);
			fileIcon.SetValue(Image.MarginProperty, new Thickness(0, 0, 2, 0));
			fileIcon.SetBinding(Image.SourceProperty, new Binding("ImgSource"));

			//定义文件名并绑定
			FrameworkElementFactory fileText = new FrameworkElementFactory(typeof(TextBlock));
			fileText.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Center);
			fileText.SetBinding(TextBlock.TextProperty, new Binding("FileName"));
			fileStackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

			//设置  stackpanel
			fileStackPanel.AppendChild(fileIcon);
			fileStackPanel.AppendChild(fileText);
			template.VisualTree = fileStackPanel;

			//该列的模板就是刚刚定义的stackpanel
			mycolun.CellTemplate = template;
			myview.Columns.Add(mycolun);

			mycolun = new GridViewColumn();
			mycolun.Header = "修改日期";
			mycolun.Width = 120;
			mycolun.DisplayMemberBinding = new Binding("LastModifyTime");
			myview.Columns.Add(mycolun);

			mycolun = new GridViewColumn();
			mycolun.Header = "大小";
			mycolun.Width = 80;
			mycolun.DisplayMemberBinding = new Binding("Size");
			myview.Columns.Add(mycolun);

			mycolun = new GridViewColumn();
			mycolun.Header = "类型";
			mycolun.Width = 40;
			mycolun.DisplayMemberBinding = new Binding("FileType");
			myview.Columns.Add(mycolun);

			//mycolun = new GridViewColumn();
			//mycolun.Header = "路径";
			//mycolun.Width = 80;
			//mycolun.DisplayMemberBinding = new Binding("FilePath");
			//myview.Columns.Add(mycolun);

			//右键菜单添加测试
			ContextMenu myContext = new ContextMenu();

			MenuItem myMUItem = new MenuItem();
			myMUItem.Header = "下载至基站";
			myMUItem.Name = "Menu01";
			myContext.Items.Add(myMUItem);

			myMUItem = new MenuItem();
			myMUItem.Header = "查看";
			myMUItem.Name = "FileLook";
			myMUItem.Click += FileLook_Click;
			myContext.Items.Add(myMUItem);

			myMUItem = new MenuItem();
			myMUItem.Header = "刷新";
			myMUItem.Name = "Refresh";
			myMUItem.Click += Refresh_Click;
			myContext.Items.Add(myMUItem);

			myMUItem = new MenuItem();
			myMUItem.Header = "重命名";
			myMUItem.Name = "Rename";
			myMUItem.Click += Rename_Click;
			myContext.Items.Add(myMUItem);

			myMUItem = new MenuItem();
			myMUItem.Header = "新建文件夹";
			myMUItem.Name = "NewFolder";
			myMUItem.Click += NewFolder_Click;
			myContext.Items.Add(myMUItem);

			lvFileInfo.ContextMenu = myContext;

			//设置文件列表的  鼠标拖拽事件
			lvFileInfo.AllowDrop = true;
			lvFileInfo.Drop += LvFileInfo_Drop;

			//设置文件 list 的 移动事件
			lvFileInfo.MouseMove += LvFileInfo_MouseMove;

			InitMember();

			InitTargetFileTreeInfo();
		}

		// 私有成员初始化
		private void InitMember()
		{
			string boardIp = "172.27.245.92";		// TODO 这里需要使用实际的IP地址
			_fileHandler = new FileMgrFileHandler(boardIp);
			_fileHandler.GetFileInfoRspArrived += GetFileInfoCallBack;

			_boardIp = boardIp;
		}

		// 调用接口，查询板卡上的目录信息，并填入到控件中
		private void InitTargetFileTreeInfo()
		{
			if ( !_fileHandler.GetBoardFileInfo("") )
			{
				Log.Error($"获取板卡{_boardIp}路径信息失败");
				// TODO 前台错误信息提示
				return;
			}

			// 等待si消息回复
		}

		private void GetFileInfoCallBack(byte[] rspBytes)
		{
			var rsp = new SI_SILMTENB_GetFileInfoRspMsg();
			if (-1 == rsp.DeserializeToStruct(rspBytes, 0))
			{
				Log.Error("查询文件信息结果转换失败");
				return;
			}

			if (1 == rsp.s8GetResult)	// 获取结果： 0：成功 1：失败
			{
				Log.Error("基站上报数据失败");
				return;
			}

			var fileNumber = rsp.u16FileNum;
			var fileCount = fileNumber & 0x00FF;
			var fileVersion = (fileNumber & 0xFF00) >> 8;
			Log.Debug($"文件数量：{fileCount}，文件版本：{fileVersion}");

			if (fileVersion == 0)
			{
				ShowFileInfoToView(rsp);
			}
			else if (fileVersion == 1)
			{
				var rspv2 = new SI_SILMTENB_GetFileInfoRspMsg_v2();
				if (-1 == rspv2.DeserializeToStruct(rspBytes, 0))
				{
					Log.Error("查询文件信息结果转换失败");
					return;
				}

				ShowFileInfoToView(rspv2);
			}
		}

		// TODO 把文件数据写入到控件，分为两个版本
		private void ShowFileInfoToView(SI_SILMTENB_GetFileInfoRspMsg rsp)
		{
			
		}

		private void ShowFileInfoToView(SI_SILMTENB_GetFileInfoRspMsg_v2 rsp)
		{

		}

		/// <summary>
		/// 文件 list 的鼠标移动事件，用来执行拖拽
		/// TODO 需要判断方向，是从本地往板卡还是从板卡往本地
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LvFileInfo_MouseMove(object sender, MouseEventArgs e)
		{
			//只有鼠标左键处于按下状态时，才启用鼠标移动事件执行拖拽
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (lvFileInfo.SelectedItem != null)
				{
					FileInfoDemo dragFileInfo = (FileInfoDemo)lvFileInfo.SelectedItem;

					try
					{
						var dragInfo = DragDrop.DoDragDrop(lvFileInfo, dragFileInfo, DragDropEffects.Copy);
						_fileHandler.SendFileToRemote(dragFileInfo.FilePath + dragFileInfo.FileName, "");	// TODO 需要处理成实际的路径
					}
					catch (Exception)
					{

					}

				}
			}
		}
			

		//线程等待
		public static void Waite(int nTime)
		{
			ExecuteWaite(() => Thread.Sleep(nTime));
		}

		public static void ExecuteWaite(Action act)
		{
			var waiteFrame = new DispatcherFrame();


			IAsyncResult ret = act.BeginInvoke(dummy => waiteFrame.Continue = false, null);

			Dispatcher.PushFrame(waiteFrame);

			act.EndInvoke(ret);
		}

		//这个应该是最快的拷贝方式，但是无法和进度条一起显示。。。
		private void CopyFile(object obj)
		{
			string strFileName = (string)obj;

			string[] strInfos = strFileName.Split('|');

			string strSourceFile = strInfos[0];
			string strDestFile = strInfos[1];

			try
			{
				File.Copy(strSourceFile, strDestFile);
			}
			catch (Exception)
			{

			}
		}

		/// <summary>
		/// 开始进行文件的复制，通过流方式进行拷贝，并且显示拷贝进度
		/// </summary>
		/// <param name="strSourceFileName">源文件</param>
		/// <param name="strDestFileName">目标文件</param>
		private void DoFileCopy(string strSourceFileName, string strDestFileName)
		{
			try
			{
				//创建  源文件的  文件流  以只读方式打开，其他程序也可以同时以只读打开
				FileStream fsFileSource = new FileStream(strSourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				//创建  目标文件 的流，以创建新文件打开，如果已存在，抛出异常，其他程序可以只读打开
				FileStream fsFileDest = new FileStream(strDestFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);

				FileInfo fSourceInfo = new FileInfo(strSourceFileName);
				long nTotalSize = fSourceInfo.Length;                                       //文件的总长度
				byte[] bArry = new byte[4096];                                                  //读写文件的缓冲数组
				long nCurrentSize = 0;                                                                //每次读取的大小
				
				while (nCurrentSize < nTotalSize)
				{
					int n = fsFileSource.Read(bArry, 0, 4096);                              //每次读取1024个字节

					nCurrentSize += n;

					fsFileDest.Write(bArry, 0, n);

					long nPercent = (nCurrentSize * 100) / nTotalSize;
					
					Waite(1);
				}

				if (nCurrentSize == nTotalSize)
				{
				}

				fsFileDest.Close();
				fsFileSource.Close();

				RefreshFileList();
			}
			catch (FileNotFoundException ex)
			{
				MessageBox.Show("文件不存在，请确认");
			}
			catch (IOException ex)
			{
				MessageBox.Show("文件已存在");
			}

		}

		/// <summary>
		/// 文件列表  的拖拽事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LvFileInfo_Drop(object sender, DragEventArgs e)
		{
			if (selectedDTVI != null)
			{
				//获取被拖拽过来的文件信息
				FileInfoDemo dropFileInfo = e.Data.GetData(typeof(FileInfoDemo)) as FileInfoDemo;

				//根据文件路径进行文件的复制
				if (dropFileInfo != null)
				{
					try
					{
						//执行文件的复制
						DoFileCopy(dropFileInfo.FilePath + "\\" + dropFileInfo.FileName, selectedDTVI.DirInfo.FullName + "\\" + dropFileInfo.FileName);

					}
					catch (Exception)
					{
					}

				}//end if(null)

			}//end if(selected = null)

		}

		/// <summary>
		/// 右键菜单  新建文件夹  事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NewFolder_Click(object sender, RoutedEventArgs e)
		{
			if (selectedDTVI != null)
			{
				FileRename renameFolder = new FileRename("新建文件夹", null);
				renameFolder.ShowDialog();

				//如果  点击  确定，则创建文件夹，否则取消
				if (renameFolder.bOK)
				{
					string strPath = selectedDTVI.DirInfo.FullName + "//" + renameFolder.strNewName;

					int i = 0;
					string strExitePath = strPath;

					//最多创建到文件夹1000
					while (Directory.Exists(strExitePath) && i < 1000)
					{
						i++;
						strExitePath = strPath + i.ToString();
					}

					try
					{
						Directory.CreateDirectory(strExitePath);
					}
					catch (Exception)
					{
						MessageBox.Show("创建文件夹失败");
					}
				}
				else
				{
					return;
				}//end bOK

			}
			else
			{
				MessageBox.Show("请选择需要创建文件夹的父目录");
			}
		}

		/// <summary>
		/// 右键菜单  重命名  点击事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Rename_Click(object sender, RoutedEventArgs e)
		{
			if (lvFileInfo.SelectedItem != null)
			{
				FileInfoDemo selectedFile = (FileInfoDemo)lvFileInfo.SelectedItem;
				string fileFullName = selectedFile.FilePath + "\\" + selectedFile.FileName;
				FileInfo thisFile = new FileInfo(fileFullName);

				//打开  重命名  界面，获取输入的新的文件名
				FileRename renameDlg = new FileRename(selectedFile.FileName, selectedFile.FileType);
				renameDlg.ShowDialog();
				string strNewName = renameDlg.strNewName;
				strNewName = selectedFile.FilePath + "\\" + strNewName;

				//如果 按下  确认  按键，则执行修改
				if (renameDlg.bOK)
				{
					try
					{
						thisFile.MoveTo(strNewName);
						RefreshFileList();
					}
					catch (Exception)
					{
						MessageBox.Show("重命名失败");
					}
				}

				//fileFullName = selectedFile.FilePath + "\\newName" + selectedFile.FileType;

				//thisFile.MoveTo(fileFullName);

				//RefreshFileList();           

			}
		}

		/// <summary>
		/// 刷新当前选中的路径下的文件信息，可以被多个函数调用的，避免添加到刷新事件中无法被其他函数调用
		/// </summary>
		private void RefreshFileList()
		{
			//清除文件列表信息，重新加载
			lvFileInfo.Items.Clear();

			FileInfo[] fileInfos;
			DirectoryInfo[] dirInfos;

			try
			{
				dirInfos = selectedDTVI.DirInfo.GetDirectories();
				fileInfos = selectedDTVI.DirInfo.GetFiles();
			}
			catch
			{
				return;
			}

			foreach (DirectoryInfo dirInfo in dirInfos)
			{
				FileInfoDemo myDir = new FileInfoDemo();
				myDir.FileName = dirInfo.Name;
				myDir.Size = null;
				myDir.LastModifyTime = dirInfo.LastAccessTime.ToString();
				myDir.FilePath = dirInfo.Parent.Name;
				myDir.FileType = "文件夹";
				myDir.ImgSource = FileInfoGet.GetIcon(dirInfo.Name, true, true);

				lvFileInfo.Items.Add(myDir);
			}

			foreach (FileInfo info in fileInfos)
			{
				FileInfoDemo myFile = new FileInfoDemo();
				myFile.FileName = info.Name;
				//将 long 类型的长度  转换为  千位分隔符表示
				//具体的做法是，如果文件为空，长度为 0KB，否则，就算不够1024也算1KB
				myFile.Size = string.Format("{0:N0}KB", (info.Length + 1023) / 1024);
				myFile.LastModifyTime = info.LastAccessTime.ToString();
				myFile.FilePath = info.DirectoryName;
				myFile.FileType = info.Extension;
				myFile.ImgSource = FileInfoGet.GetIcon(info.Name, true, false);

				lvFileInfo.Items.Add(myFile);
			}
		}

		/// <summary>
		/// 右键菜单  刷新按钮  点击事件  刷新当前文件信息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			RefreshFileList();
		}

		/// <summary>
		/// 右键  查看  菜单  点击事件，查看文件属性
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FileLook_Click(object sender, RoutedEventArgs e)
		{
			if (lvFileInfo.SelectedItem != null)
			{
				FileInfoDemo selectedFile = (FileInfoDemo)lvFileInfo.SelectedItem;

				string FileFullPath = selectedFile.FilePath + "\\" + selectedFile.FileName;

				//结构体信息填充，打开文件属性对话框
				FileInfoGet.SHELLEXECUTEINFO FileInfo = new FileInfoGet.SHELLEXECUTEINFO();
				FileInfo.cbSize = Marshal.SizeOf(FileInfo);
				FileInfo.lpVerb = "properties";
				FileInfo.lpFile = FileFullPath;
				FileInfo.nShow = FileInfoGet.SW_SHOW;
				FileInfo.fMask = FileInfoGet.SW_MASK_INVOKEIDLIST;

				FileInfo.lpDirectory = null;
				FileInfo.lpParameters = null;
				FileInfo.lpIDList = IntPtr.Zero;

				FileInfoGet.ShellExecuteEx(ref FileInfo);
			}
		}

		/// <summary>
		/// 文件夹树改变时，查找文件夹下是否存在文件，如果存在，则显示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			selectedDTVI = e.NewValue as DirectoryTreeViewItem;

			RefreshFileList();
		}

		#region 私有属性、成员

		private FileMgrFileHandler _fileHandler;
		private string _boardIp;

		#endregion
	}
}
