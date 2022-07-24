using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterSupplyPipeSystem.Method;

namespace ThMEPWSS.WaterSupplyPipeSystem.Data
{
    public class SysIn
    {
        public double PipeOffset_X { get; set; }//第一根竖管相对于楼板起始 X 的偏移量
        public double PipeGap { get; set; }//竖管间的偏移量
        public double T { get; set; }//24h
        public double FloorLength { get; set; }//楼板线长度
        public double[] WaterEquivalent { get; set; }//用水当量数
        public Point3d InsertPt { get; set; }//插入点
        public int AreaIndex { get; set; }//当前分区索引
        public int LayingMethod { get; set; }//敷设方式
        public double FloorHeight { get; set; }//楼层线间距 mm
        public Dictionary<string, List<string>> BlockConfig { get; set; }//图块配置
        public List<int> FlushFaucet { get; set; }//冲洗龙头层
        public List<int> NoPRValve { get; set; }//无减压阀层
        public Point3dCollection SelectedArea { get; set; }
        public List<List<Point3dCollection>> FloorAreaList { get; set; }
        public List<List<int>> FloorNumList { get; set; }
        public bool CleanToolFlag { get; set; }//卫生洁具读取类型
        public int FloorNumbers { get; set; }
        public Dictionary<string, string> FloorHeightDic { get; set; }
        public double MaxDayQuota { get; set; }//最高日用水定额 QL   
        public double MaxDayHourCoefficient { get; set; }//最高日小时变化系数  Kh
        public double NumberOfHouseholds { get; set; }//每户人数  m
        public List<string> PipeNumber { get; set; } //立管编号
        public List<int> LowestStorey { get; set; } //最低楼层
        public List<int> HighestStorey { get; set; } //最高楼层
        public List<int> FloorExist { get; set; } //存在的楼层号
        public List<int> PipeFloorList { get; set; }//
        public List<int> NotExistFloor { get; set; }//不存在楼层
        public List<double[]> BlockSize { get; set; }//块尺寸


        public SysIn()
        {
            PipeOffset_X = 1e4;
            PipeGap = -600;
            T = 24;
            FloorLength = 20000;
            WaterEquivalent = new double[] { 0.5, 0.75, 1, 0.75, 1, 0.5, 1, 1.2 };
        }


        public bool Set(AcadDatabase acadDatabase, WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            var setViewModel = uiConfigs.SetViewModel;
            InsertPt = uiConfigs.InsertPt;
            AreaIndex = Tool.GetAreaIndex(uiConfigs);
            LayingMethod = Convert.ToInt32(setViewModel.LayingDynamicRadios[1].IsChecked);
            FloorHeight = setViewModel.FloorLineSpace;
            BlockConfig = blockConfig;
            FlushFaucet = Tool.GetFlushFaucet(setViewModel, out bool rstFlush);
            if (!rstFlush) return false;
            NoPRValve = Tool.GetNoPRValve(setViewModel, out bool rstNoPRValve);
            if (!rstNoPRValve) return false;
            SelectedArea = uiConfigs.SelectedArea;
            FloorAreaList = uiConfigs.FloorAreaList;
            FloorNumList = uiConfigs.FloorNumList;
            CleanToolFlag = setViewModel.CleanToolDynamicRadios[0].IsChecked;
            FloorNumbers = Tool.GetFloorNumbers(FloorNumList);
            FloorHeightDic = FloorHeightsViewModel.Instance.GetSpecialFloorHeightsDict(FloorNumbers);
            MaxDayQuota = setViewModel.MaxDayQuota;
            MaxDayHourCoefficient = Convert.ToDouble(setViewModel.MaxDayHourCoefficient.ToString("0.0"));
            NumberOfHouseholds = Convert.ToDouble(setViewModel.NumberOfHouseholds.ToString("0.0"));
            var rstLowHighStorey = Tool.GetLowHighStorey(setViewModel, FloorNumbers, out List<string> pipeNumber,
            out List<int> lowestStorey, out List<int> highestStorey);
            if (!rstLowHighStorey) return false;
            PipeNumber = pipeNumber;
            LowestStorey = lowestStorey;
            HighestStorey = highestStorey;
            var rstGetFloorExist = Tool.GetFloorExist(highestStorey, lowestStorey, pipeNumber, out List<int> floorExist, out List<int> pipeFloorList);
            if (!rstGetFloorExist) return false;
            FloorExist = floorExist;
            PipeFloorList = pipeFloorList;
            NotExistFloor = Tool.GetNotExistFloor(FloorNumbers, FloorNumList);

            var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);//创建BlockTable
            BlockSize = ThWCompute.CreateBlockSizeList(bt);//获取并添加 block 尺寸

            return true;
        }

        public bool TankSet(AcadDatabase acadDatabase, WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            var setViewModel = uiConfigs.SetViewModel;
            InsertPt = uiConfigs.InsertPt;
            AreaIndex = Tool.GetAreaIndex(uiConfigs);
            FloorHeight = setViewModel.FloorLineSpace;
            BlockConfig = blockConfig;
            FlushFaucet = Tool.GetFlushFaucet(setViewModel, out bool rstFlush);
            if (!rstFlush) return false;
            SelectedArea = uiConfigs.SelectedArea;
            FloorAreaList = uiConfigs.FloorAreaList;
            FloorNumList = uiConfigs.FloorNumList;
            CleanToolFlag = setViewModel.CleanToolDynamicRadios[0].IsChecked;
            FloorNumbers = Tool.GetFloorNumbers(FloorNumList);
            FloorHeightDic = FloorHeightsViewModel.Instance.GetSpecialFloorHeightsDict(FloorNumbers);
            MaxDayQuota = setViewModel.MaxDayQuota;
            MaxDayHourCoefficient = Convert.ToDouble(setViewModel.MaxDayHourCoefficient.ToString("0.0"));
            NumberOfHouseholds = Convert.ToDouble(setViewModel.NumberOfHouseholds.ToString("0.0"));

            var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);//创建BlockTable
            BlockSize = ThWCompute.CreateBlockSizeList(bt);//获取并添加 block 尺寸

            return true;
        }
    }
}
