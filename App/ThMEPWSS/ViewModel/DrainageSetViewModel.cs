using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class DrainageSetViewModel : NotifyPropertyChangedBase
    {
        public DrainageSetViewModel()
        {
            FloorLineSpace = 1800;
            FaucetFloor = "1";
            NoCheckValve = "";
            MaxDayQuota = 250;
            MaxDayHourCoefficient = 2.5;
            NumberOfHouseholds = 3.5;

            PartitionDatas = new ObservableCollection<PartitionData>();
            var pipeNumber = new string[] { "JGL", "J1L1", "J2L1", "J3L1"};
            foreach(var number in pipeNumber)
            {
                var partitionData = new PartitionData();
                partitionData.RiserNumber = number;
                if(number == "JGL")
                {
                    partitionData.MinimumFloorNumber = "1";
                    partitionData.HighestFloorNumber = "1";
                }
                PartitionDatas.Add(partitionData);
            }
            


            DynamicRadios = new ObservableCollection<DynamicRadioButton>();
            DynamicRadios.Add(new DynamicRadioButton() { Content = "穿梁", GroupName = "type", IsChecked = true });
            DynamicRadios.Add(new DynamicRadioButton() { Content = "埋地", GroupName = "type", IsChecked = false });
        }
        private double floorLineSpace { get; set; }
        /// <summary>
        /// 楼层线间距
        /// </summary>
        public double FloorLineSpace
        {
            get { return floorLineSpace; }
            set
            {
                floorLineSpace = value;
                this.RaisePropertyChanged();
            }
        }
        private string faucetFloor { get; set; }
        /// <summary>
        /// 冲洗龙头
        /// </summary>
        [RegularExpression(@"\d+", ErrorMessage = ("Invalid format"))]
        public string FaucetFloor
        {
            get { return faucetFloor; }
            set
            {
                faucetFloor = value;
                this.RaisePropertyChanged();
            }
        }

        private string noCheckValve { get; set; }
        /// <summary>
        /// 无减压阀
        /// </summary>
        public string NoCheckValve
        {
            get { return noCheckValve; }
            set
            {
                noCheckValve = value;
                this.RaisePropertyChanged();
            }
        }


        private double maxDayQuota { get; set; }
        /// <summary>
        /// 最高日用水定额
        /// </summary>
        public double MaxDayQuota
        {
            get { return maxDayQuota; }
            set
            {
                maxDayQuota = value;
                this.RaisePropertyChanged();
            }
        }
        private double maxDayHourCoefficient { get; set; }
        /// <summary>
        /// 最高日小时变化系数
        /// </summary>
        public double MaxDayHourCoefficient
        {
            get { return maxDayHourCoefficient; }
            set
            {
                maxDayHourCoefficient = value;
                this.RaisePropertyChanged();
            }
        }

        private double numberOfHouseholds { get; set; }
        public double NumberOfHouseholds
        {
            get { return numberOfHouseholds; }
            set
            {
                numberOfHouseholds = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<PartitionData> partitionDatas { get; set; }
        /// <summary>
        /// 分区设置的列表数据
        /// </summary>
        public ObservableCollection<PartitionData> PartitionDatas
        {
            get { return partitionDatas; }
            set
            {
                partitionDatas = value;
                this.RaisePropertyChanged();
            }
        }
        private PartitionData selectPartition { get; set; }
        /// <summary>
        /// 列表数据选中项
        /// </summary>
        public PartitionData SelectPartition 
        {
            get { return selectPartition; }
            set 
            {
                selectPartition = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<DynamicRadioButton> dynamicRadios { get; set; }
        /// <summary>
        /// 敷设方式数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> DynamicRadios 
        {
            get { return dynamicRadios; }
            set 
            {
                this.dynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }
        RelayCommand addPartitionRow;
        public RelayCommand AddPartitionRow
        {
            get
            {
                if (addPartitionRow == null)
                    addPartitionRow = new RelayCommand(() => AddPartitionRowToData());

                return addPartitionRow;
            }
            set { addPartitionRow = value; }
        }
        private void AddPartitionRowToData()
        {
            if (PartitionDatas == null)
                PartitionDatas = new ObservableCollection<PartitionData>();
            PartitionDatas.Add(new PartitionData());
            //将选中项定位到新增
            SelectPartition = PartitionDatas.Last();
        }
        RelayCommand deletePartitionRow;
        public RelayCommand DeletePartitionRow
        {
            get
            {
                if (deletePartitionRow == null)
                    deletePartitionRow = new RelayCommand(() => DeletePartitionRowToData());

                return deletePartitionRow;
            }
            set { deletePartitionRow = value; }
        }
        private void DeletePartitionRowToData()
        {
            if (null == SelectPartition)
                return;
            PartitionDatas.Remove(SelectPartition);
            SelectPartition = PartitionDatas.FirstOrDefault();
            
        }

        //todo:
         //实现输入参数的 clone
        public DrainageSetViewModel Clone()
        {
            var cloned = new DrainageSetViewModel();
            cloned.FloorLineSpace = this.floorLineSpace;
            cloned.FaucetFloor = FaucetFloor;
            cloned.NoCheckValve = NoCheckValve;
            cloned.MaxDayQuota = MaxDayQuota;
            cloned.MaxDayHourCoefficient = MaxDayHourCoefficient;
            cloned.NumberOfHouseholds = NumberOfHouseholds;

            cloned.PartitionDatas.Clear();
            foreach(var pd in PartitionDatas)
            {
                cloned.PartitionDatas.Add(pd.Clone());
            }
            {
                //cloned.PartitionDatas = PartitionDatas.cl
                //var pipeNumber = new string[] { "JGL", "J1L1", "J2L1", "J3L1" };
                //foreach (var number in pipeNumber)
                //{
                //    //var partitionData = new PartitionData();
                //    //partitionData.RiserNumber = number;
                //    //if (number == "JGL")
                //    //{
                //    //    partitionData.MinimumFloorNumber = "1";
                //    //    partitionData.HighestFloorNumber = "1";
                //    //}
                //    cloned.PartitionDatas.Add(partitionData);
                //}
            }

            return cloned;
        }
    }
    public class PartitionData : NotifyPropertyChangedBase//,IDataErrorInfo
    {
        public string RiserNumber { get; set; }
        //public string MinimumFloorNumber { get; set; }

        private string minimumFloorNumber;
        public string MinimumFloorNumber 
        {
            get
            {
                return minimumFloorNumber;
            } 
            set
            {
                if (value != null && Regex.IsMatch(value, @"^[+-]?\d*$") && value != "")
                {
                    if(Convert.ToInt32(value) != 0)
                    {
                        minimumFloorNumber = Convert.ToString((Convert.ToInt32(value)));
                        RaisePropertyChanged("MinimumFloorNumber");
                    }
                   
                }
            }
        }

        //public string HighestFloorNumber { get; set; }
        private string highestFloorNumber;
        public string HighestFloorNumber
        {
            get
            {
                return highestFloorNumber;
            }
            set
            {
                if (value != null && Regex.IsMatch(value, @"^[+-]?\d*$") && value != "")
                {
                    if(Convert.ToInt32(value) != 0)
                    {
                        highestFloorNumber = Convert.ToString((Convert.ToInt32(value)));
                        RaisePropertyChanged("HighestFloorNumber");
                    }
                   
                }
                    
            }
        }
        public PartitionData Clone()
        {
            var cloned = new PartitionData();
            cloned.RiserNumber = RiserNumber;
            cloned.MinimumFloorNumber = MinimumFloorNumber;
            cloned.HighestFloorNumber = HighestFloorNumber;
            return cloned;
        }
        //public string Error => throw new NotImplementedException();

        //public string this[string columnName]
        //{
        //    get
        //    {
        //        if(columnName.Equals("MinimumFloorNumber"))
        //        {
        //            if (minimumFloorNumber <= 0)
        //                return "Invalid value";
        //        }

        //        return "";
        //    }
        //}
    }
}
