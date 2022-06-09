using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPArchitecture.ViewModel
{
    public enum CommandMode { WithUI, WithoutUI}
    public enum CommandTypeEnum {RunWithoutIteration, RunWithIteration, RunWithIterationAutomatically}//directly, with splitters, without splitters
    public enum CommandRunModeEnum { Auto, Horizental, Vertical }
    public enum CommandRunSpeedEnum { Fast, General, Slow, Advanced }
    public enum CommandColumnSizeEnum { Large, LargeAndSmall, Small}
    public class ParkingStallArrangementViewModel : NotifyPropertyChangedBase
    {
        private bool _usePolylineAsObstacle = true;

        public bool UsePolylineAsObstacle
        {
            get { return _usePolylineAsObstacle; }
            set
            {
                _usePolylineAsObstacle = value;
                RaisePropertyChanged("UsePolylineAsObstacle");
            }
        }

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
        private bool _UseMultiProcess = true;

        public bool UseMultiProcess
        {
            get { return _UseMultiProcess; }
            set
            {
                _UseMultiProcess = value;
                RaisePropertyChanged("UseMultiProcess");
            }
        }
        private bool _UseMultiSelection = false;//是否多选

        public bool UseMultiSelection
        {
            get { return _UseMultiSelection; }
            set
            {
                _UseMultiSelection = value;
                RaisePropertyChanged("UseMultiSelection");
            }
        }

        //只生成分割线
        private bool _JustCreateSplittersChecked = true;

        public bool JustCreateSplittersChecked
        {
            get { return _JustCreateSplittersChecked; }
            set 
            { 
                _JustCreateSplittersChecked = value; 
                RaisePropertyChanged("JustCreateSplittersChecked"); 
            }
        }

        //分割线打断调整
        private bool _OptmizeThenBreakSeg = false;

        public bool OptmizeThenBreakSeg
        {
            get { return _OptmizeThenBreakSeg; }
            set
            {
                _OptmizeThenBreakSeg = value;
                RaisePropertyChanged("OptmizeThenBreakSeg");
            }
        }

        //方案个数
        private int _LayoutCount = 1;

        public int LayoutCount
        {
            get { return _LayoutCount; }
            set 
            { 
                _LayoutCount = value; 
                RaisePropertyChanged("LayoutCount"); 
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
        private int _VerticalSpotLength = 5300; //mm

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
        // 建筑物判断容差。将所有建筑物外扩3000做并集，在内缩3000即为建筑外包框
        private int _BuildingTolerance = 3000;
        public int BuildingTolerance
        {
            get
            {
                return _BuildingTolerance;
            }
            set
            {
                _BuildingTolerance = value;
                RaisePropertyChanged("BuildingTolerance");
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

        //柱子完成面是否影响车道净宽
        private bool _ColumnAdditionalInfluenceLaneWidth = true;

        public bool ColumnAdditionalInfluenceLaneWidth
        {
            get { return _ColumnAdditionalInfluenceLaneWidth; }
            set
            {
                _ColumnAdditionalInfluenceLaneWidth = value;
                RaisePropertyChanged("ColumnAdditionalInfluenceLaneWidth");
            }
        }

        private CommandColumnSizeEnum _CommandColumnSize = CommandColumnSizeEnum.LargeAndSmall;
        public CommandColumnSizeEnum CommandColumnSize
        {
            get
            { return _CommandColumnSize; }
            set
            {
                _CommandColumnSize = value;
                if (value == CommandColumnSizeEnum.Large)
                {
                    ColumnWidth = 7800;
                    ColumnShiftDistanceOfDoubleRowModular = 1050;
                    MidColumnInDoubleRowModular = false;
                    ColumnShiftDistanceOfSingleRowModular = 550;
                }
                else if (value == CommandColumnSizeEnum.LargeAndSmall)
                {
                    ColumnWidth = 7800;
                    ColumnShiftDistanceOfDoubleRowModular = 550;
                    MidColumnInDoubleRowModular = true;
                    ColumnShiftDistanceOfSingleRowModular = 550;
                }
                else if (value == CommandColumnSizeEnum.Small)
                {
                    ColumnWidth = 5400;
                    ColumnShiftDistanceOfDoubleRowModular = 550;
                    MidColumnInDoubleRowModular = true;
                    ColumnShiftDistanceOfSingleRowModular = 550;
                }
                else
                {
                    ColumnWidth = 7800;
                    ColumnShiftDistanceOfDoubleRowModular = 550;
                    MidColumnInDoubleRowModular = true;
                    ColumnShiftDistanceOfSingleRowModular = 550;
                }
                RaisePropertyChanged("CommandColumnSize");
            }
        }
        //最大柱间距,需要改成柱间距
        private int _ColumnWidth = 7800; //mm

        public int ColumnWidth
        {
            get
            {
                return _ColumnWidth;
            }
            set
            {
                _ColumnWidth = value;
                RaisePropertyChanged("ColumnWidth");
            }
        }

        //背靠背模块：柱子沿车道法向偏移距离
        private int _ColumnShiftDistanceOfDoubleRowModular = 550; //mm

        public int ColumnShiftDistanceOfDoubleRowModular
        {
            get
            {
                return _ColumnShiftDistanceOfDoubleRowModular;
            }
            set
            {
                _ColumnShiftDistanceOfDoubleRowModular = value;
                RaisePropertyChanged("ColumnShiftDistanceOfDoubleRowModular");
            }
        }

        //背靠背模块是否使用中柱
        private bool _MidColumnInDoubleRowModular = true;

        public bool MidColumnInDoubleRowModular
        {
            get { return _MidColumnInDoubleRowModular; }
            set
            {
                _MidColumnInDoubleRowModular = value;
                RaisePropertyChanged("MidColumnInDoubleRowModular");
            }
        }

        //单排模块：柱子沿车道法向偏移距离
        private int _ColumnShiftDistanceOfSingleRowModular = 550; //mm

        public int ColumnShiftDistanceOfSingleRowModular
        {
            get
            {
                return _ColumnShiftDistanceOfSingleRowModular;
            }
            set
            {
                _ColumnShiftDistanceOfSingleRowModular = value;
                RaisePropertyChanged("ColumnShiftDistanceOfSingleRowModular");
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
                    PopulationCount = 50;
                    MaxTimespan = 30;
                }
                else if(value == CommandRunSpeedEnum.Slow)//slow
                {
                    IterationCount = 60;
                    PopulationCount = 80;
                    MaxTimespan = 60;
                }
                else
                {
                    IterationCount = 50;
                    PopulationCount = 200;
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
        private int _ProcessCount = -1; //自动，设置为核心数量
        public int ProcessCount
        {
            get
            { return _ProcessCount; }
            set
            {
                _ProcessCount = value;
                RaisePropertyChanged("ProcessCount");
            }
        }
        private int _ThreadCount = 3; //默认3
        public int ThreadCount
        {
            get
            { return _ThreadCount; }
            set
            {
                _ThreadCount = value;
                RaisePropertyChanged("ThreadCount");
            }
        }

        private bool _ShowLogs = false;//显示日志，默认为false

        public bool ShowLogs
        {
            get { return _ShowLogs; }
            set
            {
                _ShowLogs = value;
                RaisePropertyChanged("ShowLogs");
            }
        }
        //横向优先_纵向车道计算长度调整_背靠背模块
        public double LayoutScareFactor_Intergral = 0.7;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public double LayoutScareFactor_Adjacent = 0.7;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public double LayoutScareFactor_betweenBuilds = 0.7;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public double LayoutScareFactor_SingleVert = 0.7;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public double SingleVertModulePlacementFactor = 1.0;
        public void Set(HiddenParameter hp)
        {
            LayoutScareFactor_Intergral = hp.LayoutScareFactor_Intergral;
            LayoutScareFactor_Adjacent = hp.LayoutScareFactor_Adjacent;
            LayoutScareFactor_betweenBuilds = hp.LayoutScareFactor_betweenBuilds;
            LayoutScareFactor_SingleVert = hp.LayoutScareFactor_SingleVert;
            SingleVertModulePlacementFactor = hp.SingleVertModulePlacementFactor;
        }
    }

    public static class ParameterStock
    {
        private static int _RoadWidth = 5500;
        public static int RoadWidth
        {
            get 
            {
                if (Setted) return _RoadWidth;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        //平行车位尺寸,长度
        private static int _ParallelSpotLength = 6000; //mm

        public static int ParallelSpotLength
        {
            get 
            { 
                if (Setted) return _ParallelSpotLength;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        //平行车位尺寸,宽度
        private static int _ParallelSpotWidth = 2400; //mm

        public static int ParallelSpotWidth
        {
            get
            {
                if (Setted) return _ParallelSpotWidth;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        //垂直车位尺寸, 长度
        private static int _VerticalSpotLength = 5100; //mm

        public static int VerticalSpotLength
        {
            get
            {
                if (Setted) return _VerticalSpotLength;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        //垂直车位尺寸, 宽度
        private static int _VerticalSpotWidth = 2400; //mm

        public static int VerticalSpotWidth
        {
            get
            {
                if(Setted)
                return _VerticalSpotWidth;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }
        // 车位碰撞参数 D2
        private static int _D2 = 200;
        public static int D2
        {
            get
            {
                return _D2;
            }
        }

        private static int _BuildingTolerance = 3000;
        public static int BuildingTolerance
        {
            get
            {
                if (Setted)return _BuildingTolerance;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        public static bool LogMainProcess = true;
        public static bool LogSubProcess = false;

        private static int _ProcessCount = -1;
        public static int ProcessCount
        {
            get
            {
                if (Setted) return _ProcessCount;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        private static int _ThreadCount = 3;
        public static int ThreadCount
        {
            get
            {
                if (Setted) return _ThreadCount;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }

        



        private static bool _UseMultiSelection = false;//是否多选

        public static bool UseMultiSelection
        {
            get
            {
                if (Setted) return _UseMultiSelection;
                else throw new ArgumentException("ParameterStock Unsetted");
            }
        }
        public static double ObstacleArea;
        public static double TotalArea;
        public static bool ReadHiddenParameter = false;
        private static bool Setted = false;
        public static void Set(ParkingStallArrangementViewModel vm)
        {
            _RoadWidth = vm.RoadWidth;
            _ParallelSpotLength = vm.ParallelSpotLength;
            _ParallelSpotWidth = vm.ParallelSpotWidth;
            _VerticalSpotLength = vm.VerticalSpotLength;
            _VerticalSpotWidth = vm.VerticalSpotWidth;
            _BuildingTolerance = vm.BuildingTolerance;
            _ProcessCount = vm.ProcessCount;
            _ThreadCount = vm.ThreadCount;
            _UseMultiSelection = vm.UseMultiSelection;
            Setted = true;
        }
    }

    public class HiddenParameter
    {
        public bool LogMainProcess = true;
        public bool LogSubProcess = false;
        //横向优先_纵向车道计算长度调整_背靠背模块
        public double LayoutScareFactor_Intergral = 0.7;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public double LayoutScareFactor_Adjacent = 0.7;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public double LayoutScareFactor_betweenBuilds = 0.7;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public double LayoutScareFactor_SingleVert = 0.7;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public double SingleVertModulePlacementFactor = 1.0;
        private void Save()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(this);
                var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var filePath = System.IO.Path.Combine(currentDllPath, "ThParkingStallConfig.json");
                writer = new StreamWriter(filePath, false);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        public static HiddenParameter ReadOrCreateDefault() 
        {
            TextReader reader = null;
            var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = System.IO.Path.Combine(currentDllPath, "ThParkingStallConfig.json");
            HiddenParameter hp;
            try
            {
                if (ParameterStock.ReadHiddenParameter &&File.Exists(filePath))
                {
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    hp = JsonConvert.DeserializeObject<HiddenParameter>(fileContents);
                }
                else
                {
                    hp = new HiddenParameter();
                    if (!File.Exists(filePath))hp.Save();
                }
                ParameterStock.LogMainProcess = hp.LogMainProcess;
                ParameterStock.LogSubProcess = hp.LogSubProcess;
                return hp;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
    }
}
