using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel
{
    public class ParamSettingViewModel : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 是否生成污废水排管
        /// </summary>
        private bool? _wasteWaterChecked = false;
        public bool? WasteWaterChecked
        {
            get
            {
                return _wasteWaterChecked;
            }
            set
            {
                _wasteWaterChecked = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 污废水
        /// </summary>
        SewageWasteWaterEnum _sewageWasteWater;
        public SewageWasteWaterEnum SewageWasteWater
        {
            get
            {
                return _sewageWasteWater;
            }     
            set
            {
                _sewageWasteWater = value;
                OnPropertyChanged(nameof(SewageWasteWater));
            }
        }

        /// <summary>
        /// 是否生成雨水排管
        /// </summary>
        private bool? _rainWaterChecked = false;
        public bool? RainWaterChecked
        {
            get
            {
                return _rainWaterChecked;
            }
            set
            {
                _rainWaterChecked = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否生成冷凝水排管
        /// </summary>
        private bool? _condensateChecked = false;
        public bool? CondensateChecked
        {
            get
            {
                return _condensateChecked;
            }
            set
            {
                _condensateChecked = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 间接排水设置
        /// </summary>
        DrainageSettingEnum _indirectDrainageSetting;
        public DrainageSettingEnum IndirectDrainageSetting
        {
            get
            {
                return _indirectDrainageSetting;
            }
            set
            {
                _indirectDrainageSetting = value;
                OnPropertyChanged(nameof(IndirectDrainageSetting));
            }
        }

        /// <summary>
        /// 一层单排设置
        /// </summary>
        SingleRowSettingEnum _singleRowSetting;
        public SingleRowSettingEnum SingleRowSetting
        {
            get
            {
                return _singleRowSetting;
            }
            set
            {
                _singleRowSetting = value;
                OnPropertyChanged(nameof(SingleRowSetting));
            }
        }
    }

    public enum SewageWasteWaterEnum
    {
        /// <summary>
        /// 污废合流
        /// </summary>
        Confluence = 0,

        /// <summary>
        /// 污废分流
        /// </summary>
        Diversion = 1,
    }

    public enum DrainageSettingEnum
    {
        /// <summary>
        /// 标注至雨水接口
        /// </summary>
        Tagging = 0,

        /// <summary>
        /// 13#雨水口
        /// </summary>
        RainwaterInlet13 = 1,

        /// <summary>
        /// 室外水封井
        /// </summary>
        OutdoorWell = 2,

        /// <summary>
        /// 不考虑间接排水
        /// </summary>
        NotConsidered = 3,
    }

    public enum SingleRowSettingEnum
    {
        /// <summary>
        /// 预留堵头
        /// </summary>
        ReservedPlug = 0,

        /// <summary>
        /// 绘制详图
        /// </summary>
        DrawDetail = 1,

        /// <summary>
        /// 不考虑单排
        /// </summary>
        NotConsidered = 2,
    }
}