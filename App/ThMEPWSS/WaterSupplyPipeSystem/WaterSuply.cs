using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPEngineCore.Engine;
using System.Text.RegularExpressions;
using System.Windows;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.tool;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.WaterSupplyPipeSystem.Data;

namespace ThMEPWSS.WaterSupplyPipeSystem
{
    public class WaterSuplyBlockNames
    {
        public const string CheckValve = "截止阀";
        public const string AutoExhaustValve = "自动排气阀系统1";
        public const string PressureReducingValve = "减压阀";
        public const string VacuumBreaker = "真空破坏器";
        public const string WaterMeter = "水表1";
        public const string WaterPipeInterrupted = "水管中断";
        public const string WaterTap = "水龙头1";
        public const string Elevation = "标高";
        public const string PipeDiameter = "给水管径100";
        public const string PRValveDetail = "减压阀详图-AI-2";
        public const string FloorFraming = "楼层框定";
        public const string Casing = "套管系统";
        public const string ButterflyValve = "蝶阀";
        public const string GateValve = "闸阀";
        public const string LoopMark = "消火栓环管标记";
        public const string LoopNodeMark = "消火栓环管节点标记";
        public const string FireHydrant = "室内消火栓系统1";
    }
    public class WaterSuplyUtils
    {
        public static string WaterSuplyBlockFilePath
        {
            get
            {
                return ThCADCommon.WSSDwgPath();
            }
        }
        public static void ImportNecessaryBlocks()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterSuplyBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.CheckValve));//截止阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.AutoExhaustValve));//自动排气阀系统1
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PressureReducingValve));//减压阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.VacuumBreaker));//真空破坏器
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterMeter));//水表1
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterPipeInterrupted));//水管中断
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterTap));//水龙头
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Elevation));//标高
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PipeDiameter));//给水管经100
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PRValveDetail));//减压阀详图
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Casing));//套管系统
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.ButterflyValve));//蝶阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.GateValve));//闸阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.LoopMark));//
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.LoopNodeMark));//
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.FireHydrant));//
            }
        }
    }
    public enum LayingMethod //敷设方式
    {
        Piercing,  //穿梁
        Buried     //埋地
    }
    //常用的计算工具
    public static class ThWCompute
    {
        public static int[,] CreateZerosArray(int m, int n)//创建全0的二维数组
        {
            var result = new int[m, n];
            for (int i = 0; i < m; i++)//遍历楼层
            {
                for (int j = 0; j < n; j++)//遍历分区
                {
                    result[i, j] = 0;
                }
            }
            return result;
        }

        public static int[] CreateZerosArray(int m)//创建全0的二维数组
        {
            var result = new int[m];
            for (int i = 0; i < m; i++)//遍历楼层
            {
                result[i] = 0;
            }
            return result;
        }

        public static double InnerProduct(int[] Array1, double[] Array2)//求两个数组的內积
        {
            double result = 0;
            for (int i = 0; i < Array1.Length; i++)
            {
                result += Array1[i] * Array2[i];
            }
            return result;
        }

        public static List<int[]> CountKitchenNums(SysIn sysIn)
        {
            var floorAreaList = sysIn.FloorAreaList;
            var floorNumList = sysIn.FloorNumList;
            var FloorNumbers = sysIn.FloorNumbers;

            using var acadDatabase = AcadDatabase.Active();
            //统计厨房数
            //创建厨房识别引擎

            //提取本地块内的房间名称
            var markExtractEngine = new ThWaterRoomMarkExtractionEngine();
            markExtractEngine.ExtractFromMS(acadDatabase.Database);

            var markRecognizeEngine = new ThAIRoomMarkRecognitionEngine();
            markRecognizeEngine.Recognize(markExtractEngine.Results, sysIn.SelectedArea);
            var ele = markRecognizeEngine.Elements;

            var rooms = ele.Where(e => (e as ThIfcTextNote).Text.Equals("厨房")).Select(e => (e as ThIfcTextNote).Geometry);

            var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
            var households = new int[floorAreaList.Count, floorAreaList[0].Count];
            for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层
            {
                for (int j = 0; j < floorAreaList[0].Count; j++)
                {
                    var overlapHouse = kitchenIndex.SelectCrossingPolygon(floorAreaList[i][j]);
                    households[i, j] = GetDeduplicationHouseCnt( overlapHouse);
                }
            }

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

        public static int GetDeduplicationHouseCnt(DBObjectCollection overlapHouse)
        {
            double tor = 200;
            var overlapList = new List<int>();
            if(overlapHouse.Count > 1)
            {
                for(int i =0; i < overlapHouse.Count - 1;i++)
                {
                    var centerPti = (overlapHouse[i] as Polyline).GetCentroidPoint();
                    for(int j = i+1; j < overlapHouse.Count;j++)
                    {
                        var centerPtj = (overlapHouse[j] as Polyline).GetCentroidPoint();
                        if (centerPti.DistanceTo(centerPtj) < tor)
                        {
                           if(!overlapList.Contains(j))
                            {
                                overlapList.Add(j);
                            }
                        }
                    }
                }
                return overlapHouse.Count - overlapList.Count;
            }
            return overlapHouse.Count;
        }

        public static List<List<CleaningToolsSystem>> CountCleanToolNums(SysIn sysIn, List<int[]> households)
        {
            var cleanToolFlag = sysIn.CleanToolFlag;
            var floorAreaList = sysIn.FloorAreaList;
            var floorList = sysIn.FloorNumList;
            var blockConfig = sysIn.BlockConfig;
            var selectArea = sysIn.SelectedArea;
            var notExistFloor = sysIn.NotExistFloor;

            using (var acadDatabase = AcadDatabase.Active())
            {
                if (!cleanToolFlag)//缺省值
                {
                    var CleanToolList2 = new List<List<CleaningToolsSystem>>();
                    //统计卫生间数目，弃用
                    //var toiletNums = CountToiletNums(floorAreaList, selectArea, floorList, FloorNumbers);
                    for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层块
                    {
                        foreach (var f in floorList[i])//遍历每个楼层
                        {
                            var CleanTools = new List<CleaningToolsSystem>();
                            for (int j = 0; j < floorAreaList[0].Count; j++)//遍历楼层的每个区域
                            {
                                var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                cleanTools[0] = households[f - 1][j] * 2;//坐便器
                                cleanTools[1] = households[f - 1][j] * 2;//洗手台
                                cleanTools[2] = 0;//双洗手台
                                cleanTools[3] = households[f - 1][j];//洗涤盆
                                cleanTools[4] = households[f - 1][j] * 2;//淋浴器
                                cleanTools[5] = households[f - 1][j] * 2;//洗衣机
                                cleanTools[6] = households[f - 1][j];//阳台洗手盆
                                cleanTools[7] = 0;//浴缸

                                var CleanTool = new CleaningToolsSystem(f, j, households[f - 1][j], cleanTools);
                                CleanTools.Add(CleanTool);
                            }
                            CleanToolList2.Add(CleanTools);
                        }
                    }
                    CleanToolList2 = CleanToolList2.OrderBy(l => l.First().GetFloorNumber()).ToList();
                    return CleanToolList2;
                }

                //统计卫生洁具数
                var engine = new ThWCleanToolsRecongnitionEngine(blockConfig);
                engine.Recognize(acadDatabase.Database, selectArea);
                var allCleanToolsInSelectedArea = engine.Datas.Select(d => d.Geometry).ToCollection();
                var allCleanToolsSpatialIndex = new ThCADCoreNTSSpatialIndex(allCleanToolsInSelectedArea);

                var CleanToolList = new List<List<CleaningToolsSystem>>();
                var cleanToolsManager = new ThCleanToolsManager(blockConfig);
                for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层块
                {
                    foreach (var f in floorList[i])//遍历每个楼层
                    {
                        var CleanTools = new List<CleaningToolsSystem>();
                        for (int j = 0; j < floorAreaList[0].Count; j++)//遍历楼层的每个区域
                        {
                            try
                            {
                                var cleanToolsInSubArea = allCleanToolsSpatialIndex.SelectCrossingPolygon(floorAreaList[i][j]);
                                var allBlockNames = engine.Datas.Select(ct => ct.Data as string);
                                var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                if (cleanToolsInSubArea.Count != 0)
                                {
                                    foreach (var ct in cleanToolsInSubArea)
                                    {
                                        try
                                        {
                                            if (ct is BlockReference ctBr)
                                            {
                                                var index = cleanToolsManager.CleanToolIndex(ctBr.Name);
                                                if (index > -1) cleanTools[index] += 1;
                                            }
                                        }
                                        catch{}
                                    }
                                }
                                if(households[f - 1][j]>0)
                                {
                                    for(int k =0; k < cleanTools.Length;k++)
                                    {
                                        cleanTools[k] = cleanTools[k] / households[f - 1][j];
                                    }
                                }
                                var CleanTool = new CleaningToolsSystem(f, j, households[f - 1][j], cleanTools);
                                CleanTools.Add(CleanTool);
                            }
                            catch
                            {
                                ;
                            }
                        }
                        CleanToolList.Add(CleanTools);
                    }
                }
                if(!notExistFloor.IsNull())
                {
                    foreach (var nf in notExistFloor)
                    {
                        var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        var CleanTools = new List<CleaningToolsSystem>();
                        for (int j = 0; j < floorAreaList[0].Count; j++)//遍历楼层的每个区域
                        {
                            CleanTools.Add(new CleaningToolsSystem(nf, j, 0, cleanTools));
                        }
                        CleanToolList.Add(CleanTools);
                    }
                }
                
                CleanToolList = CleanToolList.OrderBy(l => l.First().GetFloorNumber()).ToList();
                return CleanToolList;
            }
        }

        public static List<ThWSSDStorey> CreateStoreysList(SysIn sysIn, List<int[]> households)
        {
            var FloorNumbers = sysIn.FloorNumbers;
            var FlushFaucet = sysIn.FlushFaucet;
            var NoPRValve = sysIn.NoPRValve;
            var FloorHeight = sysIn.FloorHeight;
            var StoreyList = new List<ThWSSDStorey>();
            for (int i = 0; i < FloorNumbers; i++)
            {
                bool hasFlushFaucet = FlushFaucet.Contains(i + 1);  //有冲洗龙头为true
                bool noValve = NoPRValve.Contains(i + 1);  //无减压阀为true                      
                //楼层初始化
                var storey = new ThWSSDStorey(i + 1, FloorHeight, hasFlushFaucet, noValve, households[i]);
                StoreyList.Add(storey);
            }
            var zeroHouse = CreateZerosArray(households[0].Length);
            StoreyList.Add(new ThWSSDStorey(FloorNumbers + 1, FloorHeight, false, false, zeroHouse));

            return StoreyList;
        }


        public static List<ThWSuplySystemDiagram> CreatePipeSystem(SysIn sysIn, SysProcess sysProcess)
        {
            var QL = sysIn.MaxDayQuota;  //最高日用水定额 QL
            var Kh = sysIn.MaxDayHourCoefficient;  //最高日小时变化系数  Kh
            var m = sysIn.NumberOfHouseholds;   //每户人数  m
            var T = sysIn.T;

            var lowestStorey = sysIn.LowestStorey;
            var highestStorey = sysIn.HighestStorey;
            var PipeOffset_X = sysIn.PipeOffset_X;
            var floorCleanToolList = sysProcess.FloorCleanToolList;
            var pipeNumber = sysIn.PipeNumber;
            var areaIndex = sysIn.AreaIndex;
            var maxHouseholdNums = sysProcess.MaxHouseholdNums;


            var PipeSystem = new List<ThWSuplySystemDiagram>();// 创建竖管系统列表

            for (int i = 0; i < lowestStorey.Count; i++)  //对于每一根竖管 i 
            {
                //生成竖管对象并添加至竖管系统列表
                PipeSystem.Add(new ThWSuplySystemDiagram(pipeNumber[i], lowestStorey[i], highestStorey[i], PipeOffset_X + i * sysIn.PipeGap, highestStorey));
                double[] NgList = new double[highestStorey[i]];//每层楼的当量总数
                double[] NgTotalList = new double[highestStorey[i]];//每层楼的当量总数
                double[] U0List = new double[highestStorey[i]];//每层楼的出流概率
                double[] U0aveList = new double[highestStorey[i]];//每层楼的平均出流概率，用于立管计算

                for (int j = highestStorey[i] - 1; j >= lowestStorey[i] - 1; j--)
                {
                    var toolNums = floorCleanToolList[j][areaIndex].GetCleaningTools();//当前层的卫生洁具数
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
                for (int j = lowestStorey[i] - 1; j < highestStorey[i]; j++)// 对于竖管 i 的第 j 个竖管单元(即第 j 层)
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

                for (int j = 0; j < lowestStorey[i] - 1; j++)// 对于竖管 i 的第 j 个竖管单元(即第 j 层)
                {
                    U0aveList[j] = U0aveList[lowestStorey[i] - 1];
                    NgTotalList[j] = NgTotalList[lowestStorey[i] - 1];
                }
                for (int j = 0; j < highestStorey[i]; j++)
                {
                    var pipeCompute = new PipeCompute(U0aveList[j], NgTotalList[j]);
                    var DN = pipeCompute.PipeDiameterCompute();
                    PipeSystem[i].PipeUnits.Add(new ThWSSDPipeUnit(DN, j));
                }
                sysProcess.NGLIST.Add(NgTotalList);
                sysProcess.U0LIST.Add(U0aveList);
            }

            return PipeSystem;
        }


        //水箱立管管径计算
        public static void U0NgCompute(int areaNums, int highestFloor, int NumberofPressurizedFloors, SysIn sysIn, SysProcess sysProcess)
        {
            var QL = sysIn.MaxDayQuota;  //最高日用水定额 QL
            var Kh = sysIn.MaxDayHourCoefficient;  //最高日小时变化系数  Kh
            var m = sysIn.NumberOfHouseholds;   //每户人数  m
            var T = sysIn.T;

            var floorCleanToolList = sysProcess.FloorCleanToolList;
            var maxHouseholdNums = sysProcess.MaxHouseholdNums;

            var minPrFloor = highestFloor - NumberofPressurizedFloors + 1;//最低压力层

            for (int areaIndex = 1; areaIndex < areaNums + 1; areaIndex++)//分区遍历
            {
                for (int i = 0; i < 2; i++) //只存在两根立管
                {
                    //生成竖管对象并添加至竖管系统列表
                    double[] NgList = new double[highestFloor];//每层楼的当量总数
                    double[] NgTotalList = new double[highestFloor];//每层楼的当量总数
                    double[] U0List = new double[highestFloor];//每层楼的出流概率
                    double[] U0aveList = new double[highestFloor];//每层楼的平均出流概率，用于立管计算
                    for(int j = 0; j< highestFloor; j++)//每层统计
                    {
                        var toolNums = floorCleanToolList[j][areaIndex].GetCleaningTools();//当前层的卫生洁具数
                        var householdNum = floorCleanToolList[j][areaIndex].GetHouseholdNums();
                        if (householdNum == 0) householdNum = maxHouseholdNums;

                        NgList[j] = InnerProduct(toolNums, sysIn.WaterEquivalent);
                        if (Math.Abs(NgList[j]) < 1e-6)  U0List[j] = 0;
                        else U0List[j] = 100 * QL * m * Kh / (0.2 * (NgList[j] / householdNum) * T * 3600);
                    }

                    if(i==0)
                    {
                        for(int j = 0; j < minPrFloor - 1; j++)
                        {
                            U0aveList[j] = 0;
                            NgTotalList[j] = 0;
                            for (int k = 0; k < j + 1; k++)
                            {
                                U0aveList[j] += U0List[k] * NgList[k];
                                NgTotalList[j] += NgList[k];
                            }
                            if (Math.Abs(NgTotalList[j]) > 1e-6)
                            {
                                U0aveList[j] /= NgTotalList[j];
                            }
                        }
                        for(int j = minPrFloor - 1; j < highestFloor; j++)
                        {
                            U0aveList[j] = U0aveList[j - 1];
                            NgTotalList[j] = NgTotalList[j - 1];
                        }
                    }
                    else
                    {
                        U0aveList[minPrFloor - 2] = 0;
                        NgTotalList[minPrFloor - 2] = 0;
                        for (int j = minPrFloor - 1; j < highestFloor; j++)
                        {
                            U0aveList[j] = 0;
                            NgTotalList[j] = 0;
                            for (int k = minPrFloor - 1; k < j + 1; k++)
                            {
                                U0aveList[j] += U0List[k] * NgList[k];
                                NgTotalList[j] += NgList[k];
                            }
                            if (Math.Abs(NgTotalList[j]) > 1e-6)
                            {
                                U0aveList[j] /= NgTotalList[j];
                            }
                        }
                    }
                    sysProcess.NGLIST.Add(NgTotalList);
                    sysProcess.U0LIST.Add(U0aveList);
                }
            }
        }


        public static double[] GetBlockSize(BlockTable bt, string BlockValue)//获取block尺寸
        {
            if (bt.Has(BlockValue))
            {
                var objId = bt[BlockValue];//获取objectID
                BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);//?
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
            var BlockSize = new List<double[]>();//减压阀 截止阀 水表 自动排气阀 尺寸
            //获取并添加 block 尺寸
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.PressureReducingValve));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.CheckValve));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.WaterMeter));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.AutoExhaustValve));
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
            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
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

            var floorZone = new FloorZone(spt, new Point3d(eptX, eptY, 0), LineXList);
            var rectList = floorZone.CreateRectList();//创建楼层分区的多段线

            return rectList;
        }

        public static Point3d CreateFloorPt(ThStoreys sobj)
        {
            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
            var ptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("基点 X"));
            var ptY = spt.Y + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("基点 Y"));

            return new Point3d(ptX, ptY, 0);
        }

        public static Polyline CreateFloorRect(ThStoreys sobj)
        {
            var pt = new Point3d[5];

            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
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
            for(int i = 0; i < 5; i++)
            {
                pline.AddVertexAt(i, pt[i].ToPoint2D(), 0 ,0, 0);
            }
            return pline;
        }

        public static List<List<Point3dCollection>> CreateFloorAreaList(List<ThIfcSpatialElement> elements)//创建所有楼层的分区列表
        {
            using var acadDatabase = AcadDatabase.Active();
            var FloorAreaList = new List<List<Point3dCollection>>();
            foreach (var obj in elements)//遍历楼层
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
                            FloorAreaList.Add(rectList);//分区的多段线添加
                        }

                    }
                }
            }

            return FloorAreaList;
        }

        public static Polyline CreateFloorAreaList(ThIfcSpatialElement element)//创建当前楼层的框选
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
                            {
                                FloorArea = CreateFloorRect(sobj);
                            }

                        }
                    }
                    
                }
            }

            return FloorArea;
        }

        public static Point3d CreateFloorPt(ThIfcSpatialElement element)//创建当前楼层的框选
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
                            {
                                FloorPt = CreateFloorPt(sobj);
                            }

                        }
                    }

                }
            }

            return FloorPt;
        }

        public static List<List<int>> CreateFloorNumList(List<string> FloorNum, ref int maxFloor) //提取每张图纸的楼层号
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
                    var maxFi = fiNum.Max();
                    if (maxFi > maxFloor) maxFloor = maxFi;
                }
            }

            return FloorNumList;
        }

        public static List<List<int>> CreateFloorNumList(List<string> FloorNum) //提取每张图纸的楼层号
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

        //统计分区数
        public static int CountAreaNums(List<List<Point3dCollection>> FloorAreaList, ThCADCoreNTSSpatialIndex kitchenIndex,
            ref int StartNum)
        {
            StartNum = 100;
            int AreaNums = 0;
            var areaNum = 1;
            for (int i = 0; i < FloorAreaList.Count; i++)
            {
                if (FloorAreaList[i].Count() > areaNum)
                {
                    areaNum = FloorAreaList[i].Count();
                }
            }
            var households = new int[FloorAreaList.Count, areaNum];
            for (int i = 0; i < FloorAreaList.Count; i++)
            {
                var areaNums = 0;
                for (int j = 1; j < FloorAreaList[i].Count; j++)
                {
                    households[i, j] = Convert.ToInt32(kitchenIndex.SelectCrossingPolygon(FloorAreaList[i][j]).Count > 0);
                    if (households[i, j] > 0)
                    {
                        areaNums += households[i, j];
                        if (StartNum > j)
                        {
                            StartNum = j;
                        }
                    }

                }
                if (AreaNums < areaNums)
                {
                    AreaNums = areaNums;
                }
            }

            return AreaNums;
        }

        public static List<int> ExtractData(string floorls, string dataName)
        {
            var FlushFaucet = new List<int>();//冲洗龙头层
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

