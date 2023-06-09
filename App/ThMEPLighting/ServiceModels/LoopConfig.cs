﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPLighting.ServiceModels
{
    public class LoopConfig 
    {
        /// <summary>
        /// 系统类型
        /// </summary>
        public string systemType
        {
            get; set;
        }

        /// <summary>
        /// 配置内容
        /// </summary>
        public ObservableCollection<ConfigModel> configModels { get; set; }
    }

    public class ConfigModel : NotifyPropertyChangedBase
    {
        public static string loopTypeColumn = "连线内容";
        public static string layerTypeColumn = "图层";
        public static string pointNumColumn = "点位上限";
        /// <summary>
        /// 连线内容
        /// </summary>
        public string loopType
        {
            get; set;
        }

        /// <summary>
        /// 图层
        /// </summary>
        public string layerType
        {
            get; set;
        }

        /// <summary>
        /// 点位上限
        /// </summary>
        public string pointNum
        {
            get; set;
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        private bool check = true;
        public bool isCheck
        {
            get {
                return check;
            }
            set
            {
                check = value;
                
                this.RaisePropertyChanged();
            }
        }
    }
}
