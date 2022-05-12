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
       public static class ThFireComputeService
        {
            public static int[,] CreateZerosArray(int m, int n)
            {
                var result = new int[m, n];
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        result[i, j] = 0;
                    }
                }
                return result;
            }
            public static int[] CreateZerosArray(int m)
            {
                var result = new int[m];
                for (int i = 0; i < m; i++)
                {
                    result[i] = 0;
                }
                return result;
            }
            public static double InnerProduct(int[] Array1, double[] Array2)
            {
                double result = 0;
                for (int i = 0; i < Array1.Length; i++)
                {
                    result += Array1[i] * Array2[i];
                }
                return result;
            }
            public static List<int[]> CountKitchenNums(List<List<Point3dCollection>> floorAreaList, Point3dCollection selectArea, List<List<int>> floorList, int FloorNumbers)
            {
                using var acadDatabase = AcadDatabase.Active();
                var markExtractEngine = new ThFireMarkExtractionEngine();
                markExtractEngine.ExtractFromMS(acadDatabase.Database);
                var markRecognizeEngine = new ThAIRoomMarkRecognitionEngine();
                markRecognizeEngine.Recognize(markExtractEngine.Results, selectArea);
                var ele = markRecognizeEngine.Elements;
                var floorKitchenNumList = CreateZerosArray(FloorNumbers, floorAreaList[0].Count);
                var fHouseNumList = new List<int[]>();
                for (int i = 0; i < floorKitchenNumList.Length / floorAreaList[0].Count; i++)
                {
                    var house = new int[floorAreaList[0].Count];
                    for (int j = 0; j < floorAreaList[0].Count; j++)
                    {
                        house[j] = floorKitchenNumList[i, j];
                    }
                    fHouseNumList.Add(house);
                }
                return fHouseNumList;
            }
            public static List<int[]> CountKitchenNums(ThFireSysInfo sysIn)
            {
                var floorAreaList = sysIn.FloorAreaList;
                var floorNumList = sysIn.FloorNumList;
                var FloorNumbers = sysIn.FloorNumbers;
                using var acadDatabase = AcadDatabase.Active();
                var markExtractEngine = new ThFireMarkExtractionEngine();
                markExtractEngine.ExtractFromMS(acadDatabase.Database);
                var markRecognizeEngine = new ThAIRoomMarkRecognitionEngine();
                markRecognizeEngine.Recognize(markExtractEngine.Results, sysIn.SelectedArea);
                var ele = markRecognizeEngine.Elements;
                var households = new int[floorAreaList.Count, floorAreaList[0].Count];
                var floorKitchenNumList = CreateZerosArray(FloorNumbers, floorAreaList[0].Count);
                for (int i = 0; i < floorNumList.Count; i++)
                {
                    foreach (var f in floorNumList[i])
                    {
                        for (int j = 0; j < floorAreaList[0].Count; j++)
                        {
                            floorKitchenNumList[f - 1, j] = households[i, j];
                        }
                    }
                }
                var fHouseNumList = new List<int[]>();
                for (int i = 0; i < floorKitchenNumList.Length / floorAreaList[0].Count; i++)
                {
                    var house = new int[floorAreaList[0].Count];
                    for (int j = 0; j < floorAreaList[0].Count; j++)
                    {
                        house[j] = floorKitchenNumList[i, j];
                    }
                    fHouseNumList.Add(house);
                }
                return fHouseNumList;
            }
            public static List<int[]> CountToiletNums(List<List<Point3dCollection>> floorAreaList,
                Point3dCollection selectArea, List<List<int>> floorList, int FloorNumbers)
            {
                using var acadDatabase = AcadDatabase.Active();
                var engineKitchen = new ThDB3RoomMarkRecognitionEngine();
                engineKitchen.Recognize(acadDatabase.Database, selectArea);
                var ele = engineKitchen.Elements;
                var households = new int[floorAreaList.Count, floorAreaList[0].Count];
                var floorKitchenNumList = CreateZerosArray(FloorNumbers, floorAreaList[0].Count);
                for (int i = 0; i < floorList.Count; i++)
                {
                    foreach (var f in floorList[i])
                    {
                        for (int j = 0; j < floorAreaList[0].Count; j++)
                        {
                            floorKitchenNumList[f - 1, j] = households[i, j];
                        }
                    }
                }
                var fHouseNumList = new List<int[]>();
                for (int i = 0; i < floorKitchenNumList.Length / floorAreaList[0].Count; i++)
                {
                    var house = new int[floorAreaList[0].Count];
                    for (int j = 0; j < floorAreaList[0].Count; j++)
                    {
                        house[j] = floorKitchenNumList[i, j];
                    }
                    fHouseNumList.Add(house);
                }
                return fHouseNumList;
            }
            public static List<List<ThFireSystemService>> CountCleanToolNums(List<List<Point3dCollection>> floorAreaList,
                List<int[]> households, List<List<int>> floorList, Point3dCollection selectArea, List<int> notExistFloor,
                Dictionary<string, List<string>> blockConfig, bool cleanToolFlag)
            {
                using (var acadDatabase = AcadDatabase.Active())
                {
                    if (!cleanToolFlag)
                    {
                        var CleanToolList2 = new List<List<ThFireSystemService>>();
                        for (int i = 0; i < floorAreaList.Count; i++)
                        {
                            foreach (var f in floorList[i])
                            {
                                var CleanTools = new List<ThFireSystemService>();
                                for (int j = 0; j < floorAreaList[0].Count; j++)
                                {
                                    var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                    cleanTools[0] = households[f - 1][j] * 2;
                                    cleanTools[1] = households[f - 1][j] * 2;
                                    cleanTools[2] = 0;
                                    cleanTools[3] = households[f - 1][j];
                                    cleanTools[4] = households[f - 1][j] * 2;
                                    cleanTools[5] = households[f - 1][j] * 2;
                                    cleanTools[6] = households[f - 1][j];
                                    cleanTools[7] = 0;
                                    var CleanTool = new ThFireSystemService(f, j, households[f - 1][j], cleanTools);
                                    CleanTools.Add(CleanTool);
                                }
                                CleanToolList2.Add(CleanTools);
                            }
                        }
                        CleanToolList2 = CleanToolList2.OrderBy(l => l.First().GetFloorNumber()).ToList();
                        return CleanToolList2;
                    }
                    var engine = new ThWCleanToolsRecongnitionEngine(blockConfig);
                    engine.Recognize(acadDatabase.Database, selectArea);
                    var allCleanToolsInSelectedArea = engine.Datas.Select(d => d.Geometry).ToCollection();
                    var CleanToolList = new List<List<ThFireSystemService>>();
                    var cleanToolsManager = new ThCleanToolsManager(blockConfig);
                    for (int i = 0; i < floorAreaList.Count; i++)
                    {
                        foreach (var f in floorList[i])
                        {
                            var CleanTools = new List<ThFireSystemService>();
                            CleanToolList.Add(CleanTools);
                        }
                    }
                    foreach (var nf in notExistFloor)
                    {
                        var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        var CleanTools = new List<ThFireSystemService>();
                        for (int j = 0; j < floorAreaList[0].Count; j++)
                        {
                            CleanTools.Add(new ThFireSystemService(nf, j, 0, cleanTools));
                        }
                        CleanToolList.Add(CleanTools);
                    }
                    CleanToolList = CleanToolList.OrderBy(l => l.First().GetFloorNumber()).ToList();
                    return CleanToolList;
                }
            }
            public static List<List<ThFireSystemService>> CountCleanToolNums(ThFireSysInfo sysIn, List<int[]> households)
            {
                var cleanToolFlag = sysIn.CleanToolFlag;
                var floorAreaList = sysIn.FloorAreaList;
                var floorList = sysIn.FloorNumList;
                var blockConfig = sysIn.BlockConfig;
                var selectArea = sysIn.SelectedArea;
                var notExistFloor = sysIn.NotExistFloor;
                using (var acadDatabase = AcadDatabase.Active())
                {
                    if (!cleanToolFlag)
                    {
                        var CleanToolList2 = new List<List<ThFireSystemService>>();
                        for (int i = 0; i < floorAreaList.Count; i++)
                        {
                            foreach (var f in floorList[i])
                            {
                                var CleanTools = new List<ThFireSystemService>();
                                for (int j = 0; j < floorAreaList[0].Count; j++)
                                {
                                    var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                    cleanTools[0] = households[f - 1][j] * 2;
                                    cleanTools[1] = households[f - 1][j] * 2;
                                    cleanTools[2] = 0;
                                    cleanTools[3] = households[f - 1][j];
                                    cleanTools[4] = households[f - 1][j] * 2;
                                    cleanTools[5] = households[f - 1][j] * 2;
                                    cleanTools[6] = households[f - 1][j];
                                    cleanTools[7] = 0;
                                    var CleanTool = new ThFireSystemService(f, j, households[f - 1][j], cleanTools);
                                    CleanTools.Add(CleanTool);
                                }
                                CleanToolList2.Add(CleanTools);
                            }
                        }
                        CleanToolList2 = CleanToolList2.OrderBy(l => l.First().GetFloorNumber()).ToList();
                        return CleanToolList2;
                    }
                    var engine = new ThWCleanToolsRecongnitionEngine(blockConfig);
                    engine.Recognize(acadDatabase.Database, selectArea);
                    var allCleanToolsInSelectedArea = engine.Datas.Select(d => d.Geometry).ToCollection();
                    var CleanToolList = new List<List<ThFireSystemService>>();
                    var cleanToolsManager = new ThCleanToolsManager(blockConfig);
                    for (int i = 0; i < floorAreaList.Count; i++)
                    {
                        foreach (var f in floorList[i])
                        {
                            var CleanTools = new List<ThFireSystemService>();
                            CleanToolList.Add(CleanTools);
                        }
                    }
                    foreach (var nf in notExistFloor)
                    {
                        var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        var CleanTools = new List<ThFireSystemService>();
                        for (int j = 0; j < floorAreaList[0].Count; j++)
                        {
                            CleanTools.Add(new ThFireSystemService(nf, j, 0, cleanTools));
                        }
                        CleanToolList.Add(CleanTools);
                    }
                    CleanToolList = CleanToolList.OrderBy(l => l.First().GetFloorNumber()).ToList();
                    return CleanToolList;
                }
            }
            public static List<ThFireStoreyInfo> CreateStoreysList(int FloorNumbers, double FloorHeight, List<int> FlushFaucet, List<int> NoPRValve, List<int[]> households)
            {
                var StoreyList = new List<ThFireStoreyInfo>();
                for (int i = 0; i < FloorNumbers; i++)
                {
                    bool hasFlushFaucet = FlushFaucet.Contains(i + 1);
                    bool noValve = NoPRValve.Contains(i + 1);
                    var storey = new ThFireStoreyInfo(i + 1, FloorHeight, hasFlushFaucet, noValve, households[i]);
                    StoreyList.Add(storey);
                }
                var zeroHouse = CreateZerosArray(households[0].Length);
                StoreyList.Add(new ThFireStoreyInfo(FloorNumbers + 1, FloorHeight, false, false, zeroHouse));
                return StoreyList;
            }
            public static List<ThFireStoreyInfo> CreateStoreysList(ThFireSysInfo sysIn, List<int[]> households)
            {
                var FloorNumbers = sysIn.FloorNumbers;
                var FlushFaucet = sysIn.FlushFaucet;
                var NoPRValve = sysIn.NoPRValve;
                var FloorHeight = sysIn.FloorHeight;
                var StoreyList = new List<ThFireStoreyInfo>();
                for (int i = 0; i < FloorNumbers; i++)
                {
                    bool hasFlushFaucet = FlushFaucet.Contains(i + 1);
                    bool noValve = NoPRValve.Contains(i + 1);
                    var storey = new ThFireStoreyInfo(i + 1, FloorHeight, hasFlushFaucet, noValve, households[i]);
                    StoreyList.Add(storey);
                }
                var zeroHouse = CreateZerosArray(households[0].Length);
                StoreyList.Add(new ThFireStoreyInfo(FloorNumbers + 1, FloorHeight, false, false, zeroHouse));
                return StoreyList;
            }
            public static List<ThFireSystemDiagram> CreatePipeSystem(ref List<double[]> NGLIST, ref List<double[]> U0LIST, List<int> lowestStorey,
                List<int> highestStorey, double PipeOffset_X, List<List<ThFireSystemService>> floorCleanToolList, int areaIndex, double PipeGap,
                double[] WaterEquivalent, ThFirePipeInfo setViewModel, double T, int maxHouseholdNums, List<string> pipeNumber)
            {
                var QL = setViewModel.MaxDayQuota;
                var Kh = setViewModel.MaxDayHourCoefficient;
                var m = setViewModel.NumberOfHouseholds;
                var PipeSystem = new List<ThFireSystemDiagram>();
                for (int i = 0; i < lowestStorey.Count; i++)
                {
                    PipeSystem.Add(new ThFireSystemDiagram(pipeNumber[i], lowestStorey[i], highestStorey[i], PipeOffset_X + i * PipeGap, highestStorey));
                    double[] NgList = new double[highestStorey[i]];
                    double[] NgTotalList = new double[highestStorey[i]];
                    double[] U0List = new double[highestStorey[i]];
                    double[] U0aveList = new double[highestStorey[i]];
                    for (int j = highestStorey[i] - 1; j >= lowestStorey[i] - 1; j--)
                    {
                        var toolNums = floorCleanToolList[j][areaIndex].GetCleaningTools();
                        var householdNum = floorCleanToolList[j][areaIndex].GetHouseholdNums();
                        if (householdNum == 0)
                        {
                            householdNum = maxHouseholdNums;
                        }
                        NgList[j] = InnerProduct(toolNums, WaterEquivalent);
                        if (Math.Abs(NgList[j]) < 1e-6)
                        {
                            U0List[j] = 0;
                        }
                        else
                        {
                            U0List[j] = 100 * QL * m * Kh / (0.2 * (NgList[j] / householdNum) * T * 3600);
                        }
                    }
                    for (int j = lowestStorey[i] - 2; j >= 0; j--)
                    {
                        NgList[j] = 0;
                        U0List[j] = 0;
                    }
                    for (int j = lowestStorey[i] - 1; j < highestStorey[i]; j++)
                    {
                        U0aveList[j] = 0;
                        NgTotalList[j] = 0;
                        for (int k = j; k < highestStorey[i]; k++)
                        {
                            U0aveList[j] += U0List[k] * NgList[k];
                            NgTotalList[j] += NgList[k];
                        }
                        CreatePipeSystem(new ThFireSysInfo(), new ThFireSysProcess());
                        if (Math.Abs(NgTotalList[j]) > 1e-6)
                        {
                            U0aveList[j] /= NgTotalList[j];
                        }
                    }
                    for (int j = 0; j < lowestStorey[i] - 1; j++)
                    {
                        U0aveList[j] = U0aveList[lowestStorey[i] - 1];
                        NgTotalList[j] = NgTotalList[lowestStorey[i] - 1];
                    }
                    for (int j = 0; j < highestStorey[i]; j++)
                    {
                        var pipeCompute = new ThPipeComputeService(U0aveList[j], NgTotalList[j]);
                        var DN = pipeCompute.PipeDiameterCompute();
                        PipeSystem[i].PipeUnits.Add(new ThFirePipeUnit(DN, j));
                    }
                    NGLIST.Add(NgTotalList);
                    U0LIST.Add(U0aveList);
                }
                return PipeSystem;
            }
            public static List<ThFireSystemDiagram> CreatePipeSystem(ThFireSysInfo sysIn, ThFireSysProcess sysProcess)
            {
                var QL = sysIn.MaxDayQuota;
                var Kh = sysIn.MaxDayHourCoefficient;
                var m = sysIn.NumberOfHouseholds;
                var T = sysIn.T;
                var lowestStorey = sysIn.LowestStorey;
                var highestStorey = sysIn.HighestStorey;
                var PipeOffset_X = sysIn.PipeOffset_X;
                var floorCleanToolList = sysProcess.FloorCleanToolList;
                var pipeNumber = sysIn.PipeNumber;
                var areaIndex = sysIn.AreaIndex;
                var maxHouseholdNums = sysProcess.MaxHouseholdNums;
                var PipeSystem = new List<ThFireSystemDiagram>();
                for (int i = 0; i < lowestStorey.Count; i++)
                {
                    PipeSystem.Add(new ThFireSystemDiagram(pipeNumber[i], lowestStorey[i], highestStorey[i], PipeOffset_X + i * sysIn.PipeGap, highestStorey));
                    double[] NgList = new double[highestStorey[i]];
                    double[] NgTotalList = new double[highestStorey[i]];
                    double[] U0List = new double[highestStorey[i]];
                    double[] U0aveList = new double[highestStorey[i]];
                    for (int j = highestStorey[i] - 1; j >= lowestStorey[i] - 1; j--)
                    {
                        var toolNums = floorCleanToolList[j][areaIndex].GetCleaningTools();
                        var householdNum = floorCleanToolList[j][areaIndex].GetHouseholdNums();
                        if (householdNum == 0)
                        {
                            householdNum = maxHouseholdNums;
                        }
                        NgList[j] = InnerProduct(toolNums, sysIn.WaterEquivalent);
                        if (Math.Abs(NgList[j]) < 1e-6)
                        {
                            U0List[j] = 0;
                        }
                        else
                        {
                            U0List[j] = 100 * QL * m * Kh / (0.2 * (NgList[j] / householdNum) * T * 3600);
                        }
                    }
                    for (int j = lowestStorey[i] - 2; j >= 0; j--)
                    {
                        NgList[j] = 0;
                        U0List[j] = 0;
                    }
                    for (int j = lowestStorey[i] - 1; j < highestStorey[i]; j++)
                    {
                        U0aveList[j] = 0;
                        NgTotalList[j] = 0;
                        for (int k = j; k < highestStorey[i]; k++)
                        {
                            U0aveList[j] += U0List[k] * NgList[k];
                            NgTotalList[j] += NgList[k];
                        }
                        if (Math.Abs(NgTotalList[j]) > 1e-6)
                        {
                            U0aveList[j] /= NgTotalList[j];
                        }
                    }
                    for (int j = 0; j < lowestStorey[i] - 1; j++)
                    {
                        U0aveList[j] = U0aveList[lowestStorey[i] - 1];
                        NgTotalList[j] = NgTotalList[lowestStorey[i] - 1];
                    }
                    for (int j = 0; j < highestStorey[i]; j++)
                    {
                        var pipeCompute = new ThPipeComputeService(U0aveList[j], NgTotalList[j]);
                        var DN = pipeCompute.PipeDiameterCompute();
                        PipeSystem[i].PipeUnits.Add(new ThFirePipeUnit(DN, j));
                    }
                    sysProcess.NGLIST.Add(NgTotalList);
                    sysProcess.U0LIST.Add(U0aveList);
                }
                return PipeSystem;
            }
            public static double[] GetBlockSize(BlockTable bt, string BlockValue)
            {
                if (bt.Has(BlockValue))
                {
                    var objId = bt[BlockValue];
                    BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);
                    var extent = br.GeometricExtents;
                    var Length = extent.MaxPoint.X - extent.MinPoint.X;
                    var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                    var Size = new double[] { Length, Hight };
                    return Size;
                }
                return new double[] { 0, 0 };
            }
            public static List<double[]> CreateBlockSizeList(BlockTable bt)
            {
                var BlockSize = new List<double[]>();
                BlockSize.Add(GetBlockSize(bt, PressureReducingValve));
                BlockSize.Add(GetBlockSize(bt, CheckValve));
                BlockSize.Add(GetBlockSize(bt, WaterMeter));
                BlockSize.Add(GetBlockSize(bt, AutoExhaustValve));
                return BlockSize;
            }
            public static Point3dCollection CreatePolyLine(Point3d pt1, Point3d pt2)
            {
                var ptls = new Point3d[5];
                ptls[0] = pt1;
                ptls[1] = new Point3d(pt2.X, pt1.Y, 0);
                ptls[2] = pt2;
                ptls[3] = new Point3d(pt1.X, pt2.Y, 0);
                ptls[4] = pt1;
                var SelectedArea = new Point3dCollection(ptls);
                return SelectedArea;
            }
            public static List<Point3dCollection> CreateRectList(ThStoreys sobj)
            {
                var spt = sobj.ObjectId.GetBlockPosition();
                var eptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("宽度"));
                var eptY = spt.Y - Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("高度"));
                var LineXList = new List<double>();
                for (int i = 0; i < sobj.ObjectId.GetDynProperties().Count; i++)
                {
                    if (sobj.ObjectId.GetDynProperties()[i].PropertyName.Contains("分割") &&
                        sobj.ObjectId.GetDynProperties()[i].PropertyName.Contains(" X"))
                    {
                        var index = int.Parse(sobj.ObjectId.GetDynProperties()[i].PropertyName[2].ToString());
                        var SplitX = Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X"));
                        if (SplitX < 0)
                        {
                            continue;
                        }
                        if (SplitX > eptX - spt.X)
                        {
                            continue;
                        }
                        LineXList.Add(spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X")));
                    }
                }
                var floorZone = new ThFireFloorZone(spt, new Point3d(eptX, eptY, 0), LineXList);
                var rectList = floorZone.CreateRectList();
                return rectList;
            }
            public static Point3d CreateFloorPt(ThStoreys sobj)
            {
                var spt = sobj.ObjectId.GetBlockPosition();
                var ptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("基点 X"));
                var ptY = spt.Y + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("基点 Y"));
                return new Point3d(ptX, ptY, 0);
            }
            public static Polyline CreateFloorRect(ThStoreys sobj)
            {
                var pt = new Point3d[5];
                var spt = sobj.ObjectId.GetBlockPosition();
                var height = Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("高度"));
                var width = Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("宽度"));
                var offset1 = new Point3d(width, 0, 0);
                var offset2 = new Point3d(width, -height, 0);
                var offset3 = new Point3d(0, -height, 0);
                pt[0] = spt;
                pt[1] = offset1.TransformBy(sobj.Data.BlockTransform);
                pt[2] = offset2.TransformBy(sobj.Data.BlockTransform);
                pt[3] = offset3.TransformBy(sobj.Data.BlockTransform);
                pt[4] = spt;
                var pline = new Polyline();
                for (int i = 0; i < 5; i++)
                {
                    pline.AddVertexAt(i, pt[i].ToPoint2D(), 0, 0, 0);
                }
                return pline;
            }
            public static List<List<Point3dCollection>> CreateFloorAreaList(List<ThIfcSpatialElement> elements)
            {
                using var acadDatabase = AcadDatabase.Active();
                var FloorAreaList = new List<List<Point3dCollection>>();
                foreach (var obj in elements)
                {
                    if (obj is ThStoreys)
                    {
                        var sobj = obj as ThStoreys;
                        var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        if (!br.IsDynamicBlock) continue;
                        if (sobj.StoreyType.ToString().Contains("StandardStorey"))
                        {
                            if (!sobj.StoreyNumber.Trim().StartsWith("-") && !sobj.StoreyNumber.Trim().StartsWith("B"))
                            {
                                var rectList = CreateRectList(sobj);
                                FloorAreaList.Add(rectList);
                            }
                        }
                    }
                }
                return FloorAreaList;
            }
            public static Polyline CreateFloorAreaList(ThIfcSpatialElement element)
            {
                using var acadDatabase = AcadDatabase.Active();
                var FloorArea = new Polyline();
                var obj = element;
                {
                    if (obj is ThStoreys)
                    {
                        var sobj = obj as ThStoreys;
                        var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        if (br.IsDynamicBlock)
                        {
                            if (sobj.StoreyType.ToString().Contains("StandardStorey"))
                            {
                                FloorArea = CreateFloorRect(sobj);
                            }
                        }
                    }
                }
                return FloorArea;
            }
            public static Point3d CreateFloorPt(ThIfcSpatialElement element)
            {
                using var acadDatabase = AcadDatabase.Active();
                var FloorPt = new Point3d();
                var obj = element;
                {
                    if (obj is ThStoreys)
                    {
                        var sobj = obj as ThStoreys;
                        var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        if (br.IsDynamicBlock)
                        {
                            if (sobj.StoreyType.ToString().Contains("StandardStorey"))
                            {
                                FloorPt = CreateFloorPt(sobj);
                            }
                        }
                    }
                }
                return FloorPt;
            }
            public static List<List<int>> CreateFloorNumList(List<string> FloorNum)
            {
                var FNumSplit = new List<string[]>();
                foreach (var f in FloorNum)
                {
                    FNumSplit.Add(f.Split(','));
                }
                var FloorNumList = new List<List<int>>();
                foreach (var f in FNumSplit)
                {
                    var fiNum = new List<int>();
                    for (int i = 0; i < f.Length; i++)
                    {
                        if (f[i].Trim().StartsWith("-"))
                        {
                            continue;
                        }
                        if (f[i].Contains('-'))
                        {
                            var start = Convert.ToInt32(f[i].Split('-')[0]);
                            var end = Convert.ToInt32(f[i].Split('-')[1]);
                            for (int j = start; j <= end; j++)
                            {
                                var hasNum = false;
                                foreach (var fi in FNumSplit)
                                {
                                    if (fi.Contains(Convert.ToString(j)))
                                    {
                                        hasNum = true;
                                        break;
                                    }
                                }
                                if (!hasNum)
                                {
                                    fiNum.Add(j);
                                }
                            }
                        }
                        else
                        {
                            fiNum.Add(Convert.ToInt32(f[i].Trim('B')));
                        }
                    }
                    if (fiNum.Count != 0)
                    {
                        FloorNumList.Add(fiNum);
                    }
                }
                return FloorNumList;
            }
            public static List<int> ExtractData(string floorls, string dataName)
            {
                var FlushFaucet = new List<int>();
                if (floorls != "")
                {
                    foreach (var f in floorls.Split(','))
                    {
                        if (f.Contains('-'))
                        {
                            var f1 = f.Split('-')[0];
                            var f2 = f.Split('-').Last();
                            if (f1 != "" && f2 != "")
                            {
                                if (Regex.IsMatch(f1, @"^[+-]?\d*$") && Regex.IsMatch(f2, @"^[+-]?\d*$"))
                                {
                                    if (Convert.ToInt32(f1) < Convert.ToInt32(f2))
                                    {
                                        for (int i = Convert.ToInt32(f1); i <= Convert.ToInt32(f2); i++)
                                        {
                                            if (!FlushFaucet.Contains(i))
                                            {
                                                FlushFaucet.Add(i);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show(dataName + "输入有误，\"-\"左边数字必须小于右边");
                                        return new List<int>();
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(dataName + "输入有误，\"-\"左右只能是数字");
                                return new List<int>();
                            }
                        }
                        else
                        {
                            if (f != "" && !FlushFaucet.Contains(Convert.ToInt32(f)))
                            {
                                FlushFaucet.Add(Convert.ToInt32(f));
                            }
                        }
                    }
                }
                return FlushFaucet;
            }
            public static int GetMaxHouseholds(List<int[]> households, List<int> FlushFaucet)
            {
                var maxHouse = 0;
                for (int i = 0; i < households.Count; i++)
                {
                    var house = households[i];
                    maxHouse = Math.Max(maxHouse, house.Max());
                    if (FlushFaucet.Contains(i + 1))
                    {
                        maxHouse += 1;
                    }
                }
                return maxHouse;
            }
        }
}