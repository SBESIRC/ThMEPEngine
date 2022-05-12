using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Assistant;
using ThMEPWSS.ViewModel;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using static ThMEPWSS.Assistant.DrawUtils;
using ThMEPEngineCore.Model.Common;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Exception = System.Exception;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Model;
using static ThMEPWSS.Hydrant.Service.Common;
namespace ThMEPWSS.Hydrant.Service
{
        public class ThFireSysInfo
        {
            public double PipeOffset_X { get; set; }
            public double PipeGap { get; set; }
            public double T { get; set; }
            public double FloorLength { get; set; }
            public double[] WaterEquivalent { get; set; }
            public Point3d InsertPt { get; set; }
            public int AreaIndex { get; set; }
            public int LayingMethod { get; set; }
            public double FloorHeight { get; set; }
            public Dictionary<string, List<string>> BlockConfig { get; set; }
            public List<int> FlushFaucet { get; set; }
            public List<int> NoPRValve { get; set; }
            public Point3dCollection SelectedArea { get; set; }
            public List<List<Point3dCollection>> FloorAreaList { get; set; }
            public List<List<int>> FloorNumList { get; set; }
            public bool CleanToolFlag { get; set; }
            public int FloorNumbers { get; set; }
            public Dictionary<string, string> FloorHeightDic { get; set; }
            public double MaxDayQuota { get; set; }
            public double MaxDayHourCoefficient { get; set; }
            public double NumberOfHouseholds { get; set; }
            public List<string> PipeNumber { get; set; }
            public List<int> LowestStorey { get; set; }
            public List<int> HighestStorey { get; set; }
            public List<int> FloorExist { get; set; }
            public List<int> PipeFloorList { get; set; }
            public List<int> NotExistFloor { get; set; }
            public List<double[]> BlockSize { get; set; }
            public ThFireSysInfo()
            {
                PipeOffset_X = 1e4;
                PipeGap = -600;
                T = 24;
                FloorLength = 20000;
                WaterEquivalent = new double[] { 0.5, 0.75, 1, 0.75, 1, 0.5, 1, 1.2 };
            }
            ThPipeComputeService PipeComputeService;
            public bool Set(AcadDatabase acadDatabase, ThFireDiagramInfo uiConfigs, Dictionary<string, List<string>> blockConfig)
            {
                var setViewModel = new ThFirePipeInfo();
                InsertPt = uiConfigs.InsertPt;
                AreaIndex = ThFireService.GetAreaIndex(uiConfigs);
                LayingMethod = Convert.ToInt32(setViewModel.LayingDynamicRadios[1].IsChecked);
                FloorHeight = setViewModel.FloorLineSpace;
                BlockConfig = blockConfig;
                FlushFaucet = ThFireService.GetFlushFaucet(setViewModel, out bool rstFlush);
                if (!rstFlush) return false;
                NoPRValve = ThFireService.GetNoPRValve(setViewModel, out bool rstNoPRValve);
                if (!rstNoPRValve) return false;
                SelectedArea = uiConfigs.SelectedArea;
                FloorAreaList = uiConfigs.FloorAreaList;
                FloorNumList = uiConfigs.FloorNumList;
                CleanToolFlag = setViewModel.CleanToolDynamicRadios[0].IsChecked;
                FloorNumbers = ThFireService.GetFloorNumbers(FloorNumList);
                FloorHeightDic = FloorHeightsViewModel.Instance.GetSpecialFloorHeightsDict(FloorNumbers);
                MaxDayQuota = setViewModel.MaxDayQuota;
                MaxDayHourCoefficient = Convert.ToDouble(setViewModel.MaxDayHourCoefficient.ToString("0.0"));
                NumberOfHouseholds = Convert.ToDouble(setViewModel.NumberOfHouseholds.ToString("0.0"));
                var rstLowHighStorey = ThFireService.GetLowHighStorey(setViewModel, FloorNumbers, out List<string> pipeNumber,
                out List<int> lowestStorey, out List<int> highestStorey);
                if (!rstLowHighStorey) return false;
                PipeNumber = pipeNumber;
                LowestStorey = lowestStorey;
                HighestStorey = highestStorey;
                var rstGetFloorExist = ThFireService.GetFloorExist(highestStorey, lowestStorey, pipeNumber, out List<int> floorExist, out List<int> pipeFloorList);
                if (!rstGetFloorExist) return false;
                FloorExist = floorExist;
                PipeFloorList = pipeFloorList;
                NotExistFloor = ThFireService.GetNotExistFloor(FloorNumbers, FloorNumList);
                var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);
                BlockSize = ThFireComputeService.CreateBlockSizeList(bt);
                return true;
            }
        }
}