using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPArchitecture.ViewModel
{
    public enum CommandMode { WithUI, WithoutUI}
    public enum CommandTypeEnum {RunWithoutIteration, RunWithIteration, RunWithIterationAutomatically}//directly, with splitters, without splitters
    //public enum CommandRunModeEnum { Auto, Horizental, Vertical }
    public enum CommandRunSpeedEnum { Fast, General, Slow, Advanced }
    public enum CommandColumnSizeEnum { Large, LargeAndSmall, Small}
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
        private bool _UseMultiProcess = true;

        private Visibility _AdvancedSettingVisibility = Visibility.Collapsed;
        public Visibility AdvancedSettingVisibility 
        { 
            get
            {
                return _AdvancedSettingVisibility;
            }
            set
            {
                _AdvancedSettingVisibility = value;
                RaisePropertyChanged("AdvancedSettingVisibility");
            }
        } 

        public bool UseMultiProcess
        {
            get { return _UseMultiProcess; }
            set
            {
                _UseMultiProcess = value;
                RaisePropertyChanged("UseMultiProcess");
            }
        }
        //补充分区线
        private bool _AddBoundSegLines = true;
        public bool AddBoundSegLines
        {
            get { return _AddBoundSegLines; }
            set
            {
                _AddBoundSegLines = value;
                RaisePropertyChanged("AddBoundSegLines");
            }
        }
        //只生成分区线
        private bool _JustCreateSplittersChecked = false;

        public bool JustCreateSplittersChecked
        {
            get { return _JustCreateSplittersChecked; }
            set 
            { 
                _JustCreateSplittersChecked = value; 
                RaisePropertyChanged("JustCreateSplittersChecked"); 
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

        private bool _DoubleRowModularDecrease200 = true;

        public bool DoubleRowModularDecrease200
        {
            get { return _DoubleRowModularDecrease200; }
            set
            {
                _DoubleRowModularDecrease200 = value;
                RaisePropertyChanged("BackToBackDecrease200");
            }
        }
        //尽端环通
        private bool _AllowLoopThroughEnd = false;
        public bool AllowLoopThroughEnd
        {
            get { return _AllowLoopThroughEnd; }
            set
            {
                _AllowLoopThroughEnd = value;
                RaisePropertyChanged("AllowLoopThroughEnd");
            }
        }
        //背靠背长度限制
        private int _DisAllowMaxLaneLength = 50000;
        public int DisAllowMaxLaneLength
        {
            get { return _DisAllowMaxLaneLength; }
            set
            {
                _DisAllowMaxLaneLength = value;
                RaisePropertyChanged("DisAllowMaxLaneLength");
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
        private int _WallLineThickness = 300;
        public int WallLineThickness
        {
            get { return _WallLineThickness; }
            set 
            { 
                _WallLineThickness = value;
                RaisePropertyChanged("WallLineThickness");
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
        private bool _AutoSolution = true;

        public bool AutoSolution//排布方向：自动（多方案）
        {
            get { return _AutoSolution; }
            set
            {
                _AutoSolution = value;
                RaisePropertyChanged("AutoSolution");
            }
        }

        private bool _HorizentalSolution = false;

        public bool HorizontalSolution//排布方向：横向优先（多方案）
        {
            get { return _HorizentalSolution; }
            set
            {
                _HorizentalSolution = value;
                RaisePropertyChanged("HorizontalSolution");
            }
        }
        private bool _VerticalSolution = false;

        public bool VerticalSolution//排布方向：纵向优先（多方案）
        {
            get { return _VerticalSolution; }
            set
            {
                _VerticalSolution = value;
                RaisePropertyChanged("VerticalSolution");
            }
        }
        private bool _SpeedUpMode = false;
        public bool SpeedUpMode
        {
            get { return _SpeedUpMode; }
            set 
            {
                _SpeedUpMode = value;
                RaisePropertyChanged("SpeedUpMode");
            }
        }

        private CommandRunSpeedEnum _CommandRunSpeed = CommandRunSpeedEnum.General;
        public CommandRunSpeedEnum CommandRunSpeed
        {
            get
            { return _CommandRunSpeed; }
            set
            {
                _CommandRunSpeed = value;
                if(value == CommandRunSpeedEnum.Fast)
                {
                    IterationCount = 30;
                    PopulationCount = 30;
                    MaxTimespan = 10;
                }
                else if(value == CommandRunSpeedEnum.General)
                {
                    IterationCount = 60;
                    PopulationCount = 80;
                    MaxTimespan = 30;
                }
                else if(value == CommandRunSpeedEnum.Slow)//slow
                {
                    IterationCount = 200;
                    PopulationCount = 200;
                    MaxTimespan = 60;
                }
                else
                {
                    IterationCount = 1;
                    PopulationCount = 3;
                    MaxTimespan = 10;
                }
                RaisePropertyChanged("CommandRunSpeed");
                RaisePropertyChanged("IsAdvancedSettingEnabled");
            }
        }
        //迭代次数
        private int _IterationCount = 60; //General mode
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
        private int _PopulationCount = 80; //Generalmode
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
        private double _MaxTimespan = 30; //fast mode
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
        private int _BorderlineMoveRange = 0;
        public int BorderlineMoveRange
        {
            get
            { return _BorderlineMoveRange; }
            set
            {
                _BorderlineMoveRange = value;
                RaisePropertyChanged("BorderlineMoveRange");
            }
        }
        private int _TargetParkingCntMin = 1;
        public int TargetParkingCntMin
        {
            get
            { return _TargetParkingCntMin; }
            set
            {
                _TargetParkingCntMin = value;
                RaisePropertyChanged("TargetParkingCntMin");
            }
        }

        private int _TargetParkingCntMax = 1;
        public int TargetParkingCntMax
        {
            get
            { return _TargetParkingCntMax; }
            set
            {
                _TargetParkingCntMax = value;
                RaisePropertyChanged("TargetParkingCntMax");
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

        private bool _ShowLogs = true;//显示日志，默认为true
        public bool ShowLogs
        {
            get { return _ShowLogs; }
            set
            {
                _ShowLogs = value;
                RaisePropertyChanged("ShowLogs");
            }
        }

        private bool _ShowTitle = true;//显示标题（总指标）
        public bool ShowTitle
        {
            get { return _ShowTitle; }
            set
            {
                _ShowTitle = value;
                RaisePropertyChanged("ShowTitle");
            }
        }

        private bool _ShowSubAreaTitle = false;//显示SubArea标题（分区指标）
        public bool ShowSubAreaTitle
        {
            get { return _ShowSubAreaTitle; }
            set
            {
                _ShowSubAreaTitle = value;
                RaisePropertyChanged("ShowSubAreaTitle");
            }
        }

        private bool _ShowTable = false;//显示标题（总指标）
        public bool ShowTable
        {
            get { return _ShowTable; }
            set
            {
                _ShowTable = value;
                RaisePropertyChanged("ShowTable");
            }
        }
        public List<int> GetMultiSolutionList()
        {
            var list = new List<int>();
            if(AutoSolution) list.Add(0);
            if(HorizontalSolution) list.Add(1);
            if(VerticalSolution) list.Add(2);
            return list;
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
        private static double _WallLineThickness =300;
        public static double WallLineThickness
        {
            get 
            { 
                if(Setted) return _WallLineThickness;
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

        private static int _RunMode = 0; //fast mode
        public static int RunMode
        {
            get
            {
                return _RunMode;
            }
            set
            {
                _RunMode = value;
                if (!ReadHiddenParameter)
                {
                    if (value == 0)//自动
                    {
                        LayoutScareFactor_Intergral = 1.0;
                        LayoutScareFactor_Adjacent = 1.0;
                        LayoutScareFactor_betweenBuilds = 1.0;
                    }
                    else if (value == 1)//横向
                    {
                        LayoutScareFactor_Intergral = 0.5;
                        LayoutScareFactor_Adjacent = 0.5;
                        LayoutScareFactor_betweenBuilds = 0.5;
                    }
                    else if (value == 2)//纵向
                    {
                        LayoutScareFactor_Intergral = 0.5;
                        LayoutScareFactor_Adjacent = 0.5;
                        LayoutScareFactor_betweenBuilds = 0.5;
                    }
                }
            }
        }

        public static bool AddBoundSegLines;
        public static double BuildingArea;//建筑面积（m^2)
        public static double TotalArea;//地库面积（m^2)
        public static double AreaMax;//最大地库面积
        public static int BorderlineMoveRange;
        public static bool ReadHiddenParameter = false;
        public static int CutTol = 995;//全自动分区线比车道多出的额外距离
        private static bool Setted = false;

        //横向优先_纵向车道计算长度调整_背靠背模块
        public static double LayoutScareFactor_Intergral = 1.0;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public static double LayoutScareFactor_Adjacent = 1.0;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public static double LayoutScareFactor_betweenBuilds = 1.0;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public static double LayoutScareFactor_SingleVert = 1.0;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public static double SingleVertModulePlacementFactor = 1.0;

        public static void Set(ParkingStallArrangementViewModel vm)
        {
            _RoadWidth = vm.RoadWidth;
            _WallLineThickness = vm.WallLineThickness;
            _ParallelSpotLength = vm.ParallelSpotLength;
            _ParallelSpotWidth = vm.ParallelSpotWidth;
            _VerticalSpotLength = vm.VerticalSpotLength;
            _VerticalSpotWidth = vm.VerticalSpotWidth;
            _BuildingTolerance = vm.BuildingTolerance;
            _ProcessCount = vm.ProcessCount;
            _ThreadCount = vm.ThreadCount;
            AddBoundSegLines = vm.AddBoundSegLines;
            BorderlineMoveRange = vm.BorderlineMoveRange;
            var hp = HiddenParameter.ReadOrCreateDefault();
            if (ReadHiddenParameter)
            {
                LayoutScareFactor_Intergral = hp.LayoutScareFactor_Intergral;
                LayoutScareFactor_Adjacent = hp.LayoutScareFactor_Adjacent;
                LayoutScareFactor_betweenBuilds = hp.LayoutScareFactor_betweenBuilds;
                LayoutScareFactor_SingleVert = hp.LayoutScareFactor_SingleVert;
                SingleVertModulePlacementFactor = hp.SingleVertModulePlacementFactor;
            }
            Setted = true;
        }
    }

    public class HiddenParameter
    {
        public bool LogMainProcess = true;
        public bool LogSubProcess = false;
        //横向优先_纵向车道计算长度调整_背靠背模块
        public double LayoutScareFactor_Intergral = 1.0;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public double LayoutScareFactor_Adjacent = 1.0;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public double LayoutScareFactor_betweenBuilds = 1.0;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public double LayoutScareFactor_SingleVert = 1.0;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public double SingleVertModulePlacementFactor = 1.0;
        //全自动分区线比车道多出的额外距离
        public int CutTol = 995;
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
            HiddenParameter hp = new HiddenParameter();
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
                    if (!File.Exists(filePath))hp.Save();
                }
                ParameterStock.LogMainProcess = hp.LogMainProcess;
                ParameterStock.LogSubProcess = hp.LogSubProcess;
                ParameterStock.CutTol = hp.CutTol;
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
