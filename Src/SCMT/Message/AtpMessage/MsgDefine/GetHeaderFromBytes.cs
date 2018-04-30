﻿namespace AtpMessage.MsgDefine
{
	public class GetHeaderFromBytes
	{
		public static GtsMsgHeader GetHeader(byte[] bytes)
		{
			GtsMsgHeader header = new GtsMsgHeader();
			header.DeserializeToStruct(bytes, 0);
			return header;
		}
	}
}