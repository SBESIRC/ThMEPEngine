using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPArchitecture.ViewModel
{
    public enum CommandMode { WithUI, WithoutUI}
    public enum CommandTypeEnum {RunWithoutIteration, RunWithIteration, RunWithIterationAutomatically}//directly, with splitters, without splitters
    public enum CommandRunModeEnum { Auto, Horizental, Vertical }
    public enum CommandRunSpeedEnum { Fast, General, Slow, Advanced }

    public class ParkingStallArrangementViewModel : NotifyPropertyChangedBase
    {
        private CommandTypeEnum _CommandType = CommandTypeEnum.RunWithoutIteration;
        public CommandTypeEnum CommandType
        {
            get
            { return _CommandType; }
            set
            {
                _CommandType = value;
                RaisePropertyChanged("CommandType");
                RaisePropertyChanged("IsComputationParaSetupEnabled");
            }
        }

        public bool IsComputationParaSetupEnabled
        {
            get
            {
                return CommandType != CommandTypeEnum.RunWithoutIteration;
            }
        }

        //平行车位尺寸,长度
        private int _ParallelSpotLength = 6000; //mm

        public int ParallelSpotLength
        {
            get 
            { 
                return _ParallelSpotLength; 
            }
            set 
            { 
                _ParallelSpotLength = value;
                RaisePropertyChanged("ParallelSpotLength");
            }
        }

        //平行车位尺寸,宽度
        private int _ParallelSpotWidth = 2400; //mm

        public int ParallelSpotWidth
        {
            get
            {
                return _ParallelSpotWidth;
            }
            set
            {
                _ParallelSpotWidth = value;
                RaisePropertyChanged("ParallelSpotWidth");
            }
        }

        //垂直车位尺寸, 长度
        private int _VerticalSpotLength = 5100; //mm

        public int VerticalSpotLength
        {
            get
            {
                return _VerticalSpotLength;
            }
            set
            {
                _VerticalSpotLength = value;
                RaisePropertyChanged("VerticalSpotLength");
            }
        }

        //垂直车位尺寸, 宽度
        private int _VerticalSpotWidth = 2400; //mm

        public int VerticalSpotWidth
        {
            get
            {
                return _VerticalSpotWidth;
            }
            set
            {
                _VerticalSpotWidth = value;
                RaisePropertyChanged("VerticalSpotWidth");
            }
        }

        private int _RoadWidth  = 5500; //mm

        public int RoadWidth
        {
            get
            {
                return _RoadWidth;
            }
            set
            {
                _RoadWidth = value;
                RaisePropertyChanged("RoadWidth");
            }
        }


        //最大柱间距
        private int _MaxColumnWidth = 7800; //mm

        public int MaxColumnWidth
        {
            get
            {
                return _MaxColumnWidth;
            }
            set
            {
                _MaxColumnWidth = value;
                RaisePropertyChanged("MaxColumnWidth");
            }
        }
        //平行于车道方向柱子尺寸
        private int _ColumnSizeOfParalleToRoad = 500; //mm

        public int ColumnSizeOfParalleToRoad
        {
            get
            {
                return _ColumnSizeOfParalleToRoad;
            }
            set
            {
                _ColumnSizeOfParalleToRoad = value;
                RaisePropertyChanged("ColumnSizeOfParalleToRoad");
            }
        }
        //垂直于车道方向柱子尺寸
        private int _ColumnSizeOfPerpendicularToRoad = 500; //mm

        public int ColumnSizeOfPerpendicularToRoad
        {
            get
            {
                return _ColumnSizeOfPerpendicularToRoad;
            }
            set
            {
                _ColumnSizeOfPerpendicularToRoad = value;
                RaisePropertyChanged("ColumnSizeOfPerpendicularToRoad");
            }
        }
        //柱子完成面尺寸
        private int _ColumnAdditionalSize = 50; //mm

        public int ColumnAdditionalSize
        {
            get
            {
                return _ColumnAdditionalSize;
            }
            set
            {
                _ColumnAdditionalSize = value;
                RaisePropertyChanged("ColumnAdditionalSize");
            }
        }

        private CommandRunModeEnum _RunMode = CommandRunModeEnum.Auto;
        public CommandRunModeEnum RunMode
        {
            get
            { return _RunMode; }
            set
            {
                _RunMode = value;
                RaisePropertyChanged("RunMode");
            }
        }

        public bool IsAdvancedSettingEnabled
        {
            get
            {
                return CommandRunSpeed.Equals(CommandRunSpeedEnum.Advanced);
            }
        }

        private CommandRunSpeedEnum _CommandRunSpeed = CommandRunSpeedEnum.Fast;
        public CommandRunSpeedEnum CommandRunSpeed
        {
            get
            { return _CommandRunSpeed; }
            set
            {
                _CommandRunSpeed = value;
                if(value == CommandRunSpeedEnum.Fast)
                {
                    IterationCount = 5;
                    PopulationCount = 10;
                    MaxTimespan = 5;
                }
                else if(value == CommandRunSpeedEnum.General)
                {
                    IterationCount = 20;
                    PopulationCount = 40;
                    MaxTimespan = 30;
                }
                else if(value == CommandRunSpeedEnum.Slow)//slow
                {
                    IterationCount = 30;
                    PopulationCount = 100;
                    MaxTimespan = 60;
                }
                else
                {
                    IterationCount = 30;
                    PopulationCount = 100;
                    MaxTimespan = 300;
                }
                RaisePropertyChanged("CommandRunSpeed");
                RaisePropertyChanged("IsAdvancedSettingEnabled");
            }
        }
        //迭代次数
        private int _IterationCount = 5; //fast mode
        public int IterationCount
        {
            get
            { return _IterationCount; }
            set
            {
                _IterationCount = value;
                RaisePropertyChanged("IterationCount");
            }
        }
        //种群数量
        private int _PopulationCount = 10; //fast mode
        public int PopulationCount
        {
            get
            { return _PopulationCount; }
            set
            {
                _PopulationCount = value;
                RaisePropertyChanged("PopulationCount");
            }
        }
        //最长时间
        private double _MaxTimespan = 5; //fast mode
        public double MaxTimespan
        {
            get
            { return _MaxTimespan; }
            set
            {
                _MaxTimespan = value;
                RaisePropertyChanged("MaxTimespan");
            }
        }
    }
}
