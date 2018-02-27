﻿/*----------------------------------------------------------------
// Copyright (C) 2017 大唐移动通信设备有限公司 版权所有;
//
// 文件名：MainWindowVM.cs
// 文件功能描述：主界面控制类;
// 创建人：郭亮;
// 版本：V1.0
// 创建时间：2017-12-12
//----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SCMTMainWindow
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private string _title = "DTMobile Station Combine Maintain Tool";

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        private bool _btnEnabled = true;
        public bool BtnEnabled
        {
            get { return _btnEnabled; }
            set
            {
                _btnEnabled = value;
                OnPropertyChanged();
            }
        }

        private ICommand _cmdSample;
        public ICommand CmdSample => _cmdSample ?? (_cmdSample = new AsyncCommand(async () =>
        {
            Title = "Busy...";
            BtnEnabled = false;
            //do something
            await Task.Delay(2000);
            Title = "Arthas.Demo";
            BtnEnabled = true;
        }));

        private ICommand _cmdSampleWithParam;
        public ICommand CmdSampleWithParam => _cmdSampleWithParam ?? (_cmdSampleWithParam = new AsyncCommand<string>(async str =>
        {
            Title = $"Hello I'm {str} currently";
            BtnEnabled = false;
            //do something
            await Task.Delay(2000);
            Title = "Arthas.Demo";
            BtnEnabled = true;
        }));
    }

    #region Command

    public class AsyncCommand : ICommand
    {
        protected readonly Predicate<object> _canExecute;
        protected Func<Task> _asyncExecute;

        public AsyncCommand(Func<Task> asyncExecute, Predicate<object> canExecute = null)
        {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public async void Execute(object parameter)
        {
            await _asyncExecute();
        }
    }

    public class AsyncCommand<T> : ICommand
    {
        protected readonly Predicate<T> _canExecute;
        protected Func<T, Task> _asyncExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public AsyncCommand(Func<T, Task> asyncExecute, Predicate<T> canExecute = null)
        {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public async void Execute(object parameter)
        {
            await _asyncExecute((T)parameter);
        }
    }
    #endregion


    public enum OrderStatus { None, New, Processing, Shipped, Received };

    public class AlarmGrid
    {
        public string AlarmNo { get; set; }
        public string AlarmName { get; set; }
        public string AlarmTime { get; set; }

        public AlarmGrid(string AlmNo, string AlmName, string AlmTime)
        {
            AlarmNo = AlmNo;
            AlarmName = AlmName;
            AlarmTime = AlmTime;
        }

        public static ObservableCollection<AlarmGrid> GetData()
        {
            ObservableCollection<AlarmGrid> ret = new ObservableCollection<AlarmGrid>();
            AlarmGrid a = new AlarmGrid("7001", "无线链路建立失败", DateTime.Now.ToString());
            AlarmGrid b = new AlarmGrid("1003", "设备进入不稳定状态", DateTime.Now.ToString());
            AlarmGrid c = new AlarmGrid("1004", "单板软件启动失败", DateTime.Now.ToString());
            AlarmGrid d = new AlarmGrid("3201", "BBU板卡温度异常", DateTime.Now.ToString());
            AlarmGrid e = new AlarmGrid("3201", "BBU板卡温度异常", DateTime.Now.ToString());
            AlarmGrid f = new AlarmGrid("3201", "BBU板卡温度异常", DateTime.Now.ToString());

            ret.Add(a);
            ret.Add(b);
            ret.Add(c);
            ret.Add(d);
            ret.Add(e);
            ret.Add(f);

            return ret;
        }

    }

    public class DataGrid
    {
        public string Data1 { get; set; }
        public string Data2 { get; set; }
        public string Data3 { get; set; }
        public string Data4 { get; set; }

        public DataGrid(string data1, string data2, string data3, string data4)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
        }

        public static ObservableCollection<DataGrid> GetData()
        {
            ObservableCollection<DataGrid> ret = new ObservableCollection<DataGrid>();
            DataGrid a = new DataGrid("1", "FDD", DateTime.Now.ToString(), "91500");
            DataGrid b = new DataGrid("2", "NBIOT", DateTime.Now.ToString(), "91500");
            DataGrid c = new DataGrid("3", "5G II", DateTime.Now.ToString(), "91500");
            DataGrid d = new DataGrid("3201", "TDD-3DMIMO", DateTime.Now.ToString(), "91500");

            ret.Add(a);
            ret.Add(b);
            ret.Add(c);
            ret.Add(d);

            return ret;
        }

    }

}