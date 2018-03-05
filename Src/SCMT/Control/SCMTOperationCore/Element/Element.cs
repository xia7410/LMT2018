﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections;

namespace SCMTOperationCore
{
    public abstract class Element
    {
        public IPAddress m_IPAddress { get; set; }                    // 对端IP地址;
        public ConnectionState m_ConnectionState { get; set; }        // 连接状态;

        public Element()
        {
        }
        
    }
}