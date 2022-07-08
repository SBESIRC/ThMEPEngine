using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace ThMEPElectrical.Model
{
    public class ThBlockConvertModel : INotifyPropertyChanged
    {        
        public ThBlockConvertModel()
        {
            blkScale = "1:100";
            blkFrame = "标注带边框";
            equipOps = CapitalOP.All;
            manualActuatorOps = false;
            havcOps = true;
            wssOps = true;
            BlockConvertInfos = new ObservableCollection<BlockConvertInfo>();
            BlkScales = new ObservableCollection<string>(new List<string> { "1:100", "1:150" });
            BlkFrames = new ObservableCollection<string>(new List<string> { "标注带边框", "标注无边框" });

            // test data
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000000", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000001", EquipmentType = "排水泵", CompareResult = "一致"});
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000002", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000003", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000004", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000005", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000006", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000007", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000008", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000009", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000010", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000011", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000012", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000013", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000014", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000015", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000016", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000017", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000018", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000019", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000020", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000021", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000022", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000023", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000024", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000025", EquipmentType = "排水泵", CompareResult = "一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000026", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000027", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000028", EquipmentType = "排水泵", CompareResult = "不一致" });
            BlockConvertInfos.Add(new BlockConvertInfo { Major = "0000029", EquipmentType = "排水泵", CompareResult = "不一致" });
        }
        public ObservableCollection<string> BlkScales { get; set; }
        public ObservableCollection<string> BlkFrames { get; set; }
        public ObservableCollection<BlockConvertInfo> BlockConvertInfos { get; set; }

        public void IgnoreBlockConvertInfos(List<string> ids)
        {
            var newBlkInfos = BlockConvertInfos
                .OfType<BlockConvertInfo>()
                .Where(o => !ids.Contains(o.Id))
                .ToList();
            BlockConvertInfos = new ObservableCollection<BlockConvertInfo>(newBlkInfos);
        }


        private bool havcOps;
        public bool HavcOps
        {
            get
            {
                return havcOps;
            }
            set
            {
                havcOps = value;
                RaisePropertyChanged("HavcOps");
            }
        }

        private bool wssOps;
        public bool WssOps
        {
            get
            {
                return wssOps;
            }
            set
            {
                wssOps = value;
                RaisePropertyChanged("WssOps");
            }
        }

        private CapitalOP equipOps;
        public CapitalOP EquipOps
        {
            get
            {
                return equipOps;
            }
            set
            {
                equipOps = value;
                RaisePropertyChanged("EquipOps");
            }
        }

        private bool manualActuatorOps;
        public bool ManualActuatorOps
        {
            get
            {
                return manualActuatorOps;
            }
            set
            {
                manualActuatorOps = value;
                RaisePropertyChanged("ManualActuatorOps");
            }
        }

        private string blkScale = "";
        public string BlkScale
        {
            get
            {
                return blkScale;
            }
            set
            {
                blkScale = value;
                RaisePropertyChanged("BlkScale");
            }
        }
        private double compareTolerance;
        public double CompareTolerance
        {
            get
            {
                return compareTolerance;
            }
            set
            {
                compareTolerance = value;
                RaisePropertyChanged("CompareTolerance");
            }
        }
        public double BlkScaleValue
        {
            get
            {
                string[] values = blkScale.Split(':');
                return double.Parse(values[1]);
            }
        }

        private string blkFrame = "";
        public string BlkFrame
        {
            get
            {
                return blkFrame;
            }
            set
            {
                blkFrame = value;
                RaisePropertyChanged("BlkFrame");
            }
        }
        public string BlkFrameValue
        {
            get
            {
                return blkFrame;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public enum CapitalOP
    {
        Strong,
        Weak,
        All,
    }
    public class BlockConvertInfo
    {
        public string Id { get; private set; }
        /// <summary>
        /// 来源专业
        /// </summary>
        public string Major { get; set; } = "";
        /// <summary>
        /// 设备类型
        /// </summary>
        public string EquipmentType { get; set; } = "";
        /// <summary>
        /// 对比结果
        /// </summary>
        public string CompareResult { get; set; } = "";
        public BlockConvertInfo()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
