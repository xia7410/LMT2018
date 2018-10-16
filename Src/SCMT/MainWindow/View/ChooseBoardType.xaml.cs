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

using NetPlan;

namespace SCMTMainWindow.View
{
    /// <summary>
    /// ChooseBoardType.xaml 的交互逻辑
    /// </summary>
    public partial class ChooseBoardType : Window
    {
        public string strBoardName;
        public bool bOK = false;
        public ChooseBoardType(List<BoardEquipment> boardInfo)
        {
            InitializeComponent();

            if(boardInfo != null)
            {
                foreach(BoardEquipment item in boardInfo)
                {
                    this.BoardType.Items.Add(item.boardTypeName);
                }
            }

            this.BoardType.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            strBoardName = this.BoardType.SelectedItem.ToString();
            bOK = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            bOK = false;
            this.Close();
        }
    }
}