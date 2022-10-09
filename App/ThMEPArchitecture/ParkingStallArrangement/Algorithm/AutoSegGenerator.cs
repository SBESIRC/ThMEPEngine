using System;
using AcHelper;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using DotNetARX;
using ThMEPArchitecture.ViewModel;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore;
using ThParkingStall.Core.Tools;
using NetTopologySuite.Geometries;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    class AutoSegGenerator : IDisposable
    {
        // 输入数据以及参数
        private ThCADCoreNTSSpatialIndex BuildingSpatialIndex;
        Serilog.Core.Logger Logger;
        private Polyline WallLine;
        private int RoadWidth;
        private double CutProp;
        private double StepSize;
        private double CutTol;
        private int Tol;
        private int SubPlanCounts;
        private bool LastPlanOnly;//是否只保留最终分割方案（分割到不能继续分割）
        private List<Polyline> Buildings = new List<Polyline>();
        private BisectionArea BisectAreaTree;//二分树状区域
        private Dictionary<Tuple<int?, int?, int?, int?>, BisectionArea> BisectAreaDic;//记录全部计算过的区域
        private Dictionary<Tuple<int, int, int, bool>,BisectionSegLine> BisectSegLineDic;// 记录全部计算过的segline
        private HashSet<int> VertSegLineValues = new HashSet<int>();// 所有合理垂直分区线的值
        private HashSet<int> HorzSegLineValues = new HashSet<int>();// 所有合理水平分区线的值
        public HashSet<BisectionPlan> AllSegPlans = new HashSet<BisectionPlan>();
        private bool ShowSegLineOnly = true;
        public AutoSegGenerator(LayoutData layoutData, Serilog.Core.Logger logger,int cutol =1000)
            : this(layoutData.WallLine, layoutData.Buildings, logger, cutol)
        {
            
        }
        public AutoSegGenerator(OLayoutData oLayoutData, Serilog.Core.Logger logger, int cutol = 1000)
            : this(oLayoutData.WallLine, oLayoutData.Buildings, logger, cutol)
        {

        }
        public AutoSegGenerator(Polygon wallLine,List<Polygon> buildings, Serilog.Core.Logger logger, int cutol = 1000)
        {
            WallLine = wallLine.Shell.ToDbPolyline();//初始墙线
            Buildings = buildings.Select(b => b.Shell.ToDbPolyline()).ToList();
            BuildingSpatialIndex = new ThCADCoreNTSSpatialIndex(Buildings.ToCollection());//建筑物pline索引
            Logger = logger;
            RoadWidth = ParameterStock.RoadWidth;
            CutProp = 1.0;// 切割阈值比例，默认一倍车道宽（1.0）
            CutTol = cutol;//比正常道路宽的容差，至少为3
            StepSize = CutProp * RoadWidth + CutTol;//切割宽度,默认6500距离才尝试切割
            Tol = 2; //出头距离
            var initspliters = new List<BisectionSegLine>() { null, null, null, null };
            BisectAreaTree = new BisectionArea(WallLine, initspliters);//动态N叉树
            BisectAreaDic = new Dictionary<Tuple<int?, int?, int?, int?>, BisectionArea>();
            BisectSegLineDic = new Dictionary<Tuple<int, int, int, bool>, BisectionSegLine>();
        }

        private void GetBuildings(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            foreach (var obj in dbObjs)
            {
                var pline = obj as Polyline;
                Buildings.Add(pline);
            }
        }
        public void Run(bool lastPlanOnly = true,bool showSegLineOnly = true)
        {
            ShowSegLineOnly = showSegLineOnly;
            LastPlanOnly = lastPlanOnly;
            Logger?.Information("二分分区线穷举");
            var initAreaKey = new Tuple<int?, int?, int?, int?>(null, null, null, null);
            BisectAreaDic.Add(initAreaKey, BisectAreaTree);
            List<BisectionArea> areas = new List<BisectionArea> { BisectAreaTree };
            List<BisectionArea> nextLevelAreas;
            while(areas.Count != 0)//当前需要分割的区域
            {
                //Logger?.Information("\n" + areas.GetNewAreaCount().ToString());
                nextLevelAreas = new List<BisectionArea>();
                foreach (var bisectArea in areas)
                {
                    nextLevelAreas.AddRange(GetSubAreas(bisectArea));
                }
                areas = nextLevelAreas;
            }
            Logger?.Information(" 二分分区线总数：" + BisectSegLineDic.Count.ToString() +"");
            //if(ShowSegLineOnly) GetGrid();
            //else
            if (!ShowSegLineOnly)
            {
                GetAllSegLinePlan();
                //var maxCount = AllSegPlans.Max(p => p.Count);
                //var LastLayer = AllSegPlans.Where(p => p.Count == maxCount);
                Logger?.Information("\n 总方案数");
                Logger?.Information("\n" + AllSegPlans.Count().ToString());
                Active.Editor.WriteMessage("\n 总方案数");
                Active.Editor.WriteMessage("\n" + AllSegPlans.Count().ToString());
                var LastLayer = GetLastLayerPlan();
                Logger?.Information("\n 最后一层方案数");
                Logger?.Information("\n" + LastLayer.Count().ToString());
                Active.Editor.WriteMessage("\n 最后一层方案数");
                Active.Editor.WriteMessage("\n" + LastLayer.Count().ToString());
                int i = 0;
                foreach (var plan in LastLayer)
                {
                    plan.Draw(i, BisectSegLineDic);
                    i += 1;
                }
                ReclaimMemory();
            }
        }
        public List<BisectionSegLine> GetGrid()
        {
            var girdLines = new List<BisectionSegLine>();
            var allLines = BisectSegLineDic.Values.ToList();
            //allLines.ForEach(l => { l.DrawSegLine(); l.ShowLowerUpperBound(); });
            for (int i = 0; i < allLines.Count(); i++)
            {
                bool donminated = false;
                for (int j = 0;j < allLines.Count(); j++)
                {
                    if (i == j) continue;
                    if(allLines[i].IsDominatedBy(allLines[j]))
                    {
                        donminated = true;
                        break;
                    }
                }
                if (!donminated) girdLines.Add(allLines[i]);
            }
            //girdLines.ForEach(l => { l.DrawSegLine();/*l.ShowLowerUpperBound();*/ });
            return girdLines;
        }
        //获取所有切到不能再切的方案
        private List<BisectionPlan> GetLastLayerPlan()
        {
            var LastLayerPlan = new List<BisectionPlan>();
            foreach (var plan in AllSegPlans)
            {
                if (plan.AllAreas.All(key => BisectAreaDic[key].SegLines.Count == 0))
                {
                    LastLayerPlan.Add(plan);
                }
            }
            return LastLayerPlan;
        }
        // 获取全部plan
        private void GetAllSegLinePlan()
        {
            var planList = new List<Tuple<int, int, int, bool>>();
            var initAreaKey = new Tuple<int?, int?, int?, int?>(null, null, null, null);
            var initAreaKeys = new List<Tuple<int?, int?, int?, int?>> { initAreaKey };
            var initPlan = new BisectionPlan(planList, initAreaKeys);
            var Set = new HashSet<BisectionPlan> { initPlan };
            int SegLineCount = 0;
            Logger?.Information("\n #####################################");
            Logger?.Information("\n 分区线个数：0");
            Logger?.Information("\n 方案个数（包含重复）：1" );
            Logger?.Information("\n 方案个数（不包含重复）：1");
            if(!LastPlanOnly) AllSegPlans.Add(initPlan);
            bool end;
            while (true)
            {
                SegLineCount += 1;
                SubPlanCounts = 0;
                end = UpdatePlans(ref Set);
                if (end) break;
                if (!LastPlanOnly) AllSegPlans.UnionWith(Set);
                ReclaimMemory();
                Logger?.Information("\n #####################################");
                Logger?.Information("\n 分区线个数："+ SegLineCount.ToString());
                Logger?.Information("\n 方案个数（包含重复）：" + SubPlanCounts.ToString());
                Logger?.Information("\n 方案个数（不包含重复）：" + Set.Count.ToString());
            }
            if (LastPlanOnly) AllSegPlans = Set;//任何情况，二分极限数量都相等
        }
        // 获取一个set全部子plan
        private bool UpdatePlans(ref HashSet<BisectionPlan> orgPlanSet)
        {
            var subPlans = new HashSet<BisectionPlan>();
            foreach(var plan in orgPlanSet)
            {
                foreach (var areaKey in plan.AllAreas)// 此方案的所有子区域
                {
                    BisectionArea bisecArea = BisectAreaDic[areaKey];
                    SubPlanCounts += bisecArea.SegLines.Count;//记录所有方案数
                    for (int i = 0; i < bisecArea.SegLines.Count; ++i)//子区域全部的可能分区线
                    {
                        var SegLines = bisecArea.SegLines[i];
                        var newPlan = plan.GetSubPlan(SegLines,
                            (bisecArea.SubAreaKeys[2 * i], bisecArea.SubAreaKeys[2 * i + 1]), areaKey);//新方案
                        subPlans.Add(newPlan);
                    }
                }
            }
            if (subPlans.Count == 0) return true;
            else 
            {
                orgPlanSet = subPlans;
                return false;
            }
        }
        private List<BisectionArea> GetSubAreas(BisectionArea bisectArea)// 更新当前区域，并且检查是否为结束（子区域个数为0）
        {
            var subAreas = new List<BisectionArea>();
            if (bisectArea.AllSearched == true) return subAreas;// 已经探索过则不探索，因为其子区域都已加过探索列表
            UpdateAllVertSeg(bisectArea);// 更新垂直分区线子区域
            UpdateAllHorzSeg(bisectArea);// 更新水平分区线子区域
            bisectArea.AllSearched = true;
            bisectArea.SubAreaKeys.ForEach(k => subAreas.Add(BisectAreaDic[k]));
            //bisectArea.ShowSegLines();// 展示分区线
            return subAreas;
        }
        private void UpdateAllVertSeg(BisectionArea bisectArea)//区域探索完添加标记
        {
            //假设当前区域外部没有墙线，或者分区线穿插不到外部的墙线
            var area = bisectArea.Area.Clone() as Polyline;
            var pts = area.GetPoints();
            var left = pts.Min(pt => pt.X);
            var right = pts.Max(pt => pt.X);
            var bottom = (int)pts.Min(pt => pt.Y) - Tol;
            var top = (int)pts.Max(pt => pt.Y) + Tol;
            // 竖向起始分区线，从left开始，步长;cutdistance
            bool CrossBuilding = false;// 判断车道buffer是否穿障碍物,上一个buffer包含障碍物才考虑生成新的分区线
            var startX = left;//上一个位置
            var endX = left + StepSize;//尝试位置，新位置
            while (endX < right)
            {
                var spt = new Point3d(startX, top, 0);
                var ept = new Point3d(endX, bottom, 0);
                var upPt = new Point3d(endX, top, 0);
                var buttomPt = ept;
                var segLine = new Line(upPt, buttomPt);
                var areaBuffer = AutoFunctions.GetRect(spt,ept);
                if (!CrossBuilding)// 之前区域未包含建筑
                {
                    var segRectInBuild = BuildingSpatialIndex.SelectCrossingPolygon(areaBuffer);
                    if (segRectInBuild.Count > 0)//当前区域包含建筑
                    {
                        CrossBuilding = true;
                    }
                    else// 不包含，扩大区域
                    {
                        endX += StepSize;
                    }
                }
                else// 之前区域包含建筑
                {
                    var segRectInBuild = BuildingSpatialIndex.SelectFence(segLine);//判断分区线是否穿墙
                    if (segRectInBuild.Count > 0)// 当前分区线穿墙
                    {
                        endX += StepSize;// 扩大区域
                    }
                    else// 不穿墙，判断是否vaild
                    {
                        var segFlag = IsCorrectSegLines(segLine, area, out double maxVal, out double minVal, out double moveDist);
                        if (segFlag)// 为合理分区线
                        {
                            // 移动分区线到中点
                            var foundMid = GetMidVal(endX, minVal, maxVal, true, out int midX);// 获取中点坐标，优先在所有已有的线中找
                            segLine.StartPoint = new Point3d(midX, top, 0);
                            segLine.EndPoint = new Point3d(midX, bottom, 0);
                            var segAreas = segLine.Split(area);
                            if (segAreas.Count == 2)//当前中线合理
                            {
                                segAreas.SortByOrder(segLine);// 对分区排序，上下，左右
                                if (!foundMid) VertSegLineValues.Add(midX);// 没有已有的线，记录中线的值
                                // 更新搜索区域
                                CrossBuilding = false;
                                startX = endX;
                                var vals = GetStartEndValue(area, segLine);
                                // 将当前分区线转换为BisecSegLine 
                                var BisecSegLine = new BisectionSegLine(segLine, midX, vals.Item1, vals.Item2, maxVal, minVal);
                                var lineKey = BisecSegLine.Key;
                                if (BisectSegLineDic.ContainsKey(lineKey)) BisecSegLine = BisectSegLineDic[lineKey];
                                else//新的分区线
                                {
                                    BisectSegLineDic.Add(lineKey, BisecSegLine);
                                    //if (ShowSegLineOnly) BisecSegLine.DrawSegLine();//画出来
                                }
                                //为节点添加分支
                                bisectArea.AddBranch(BisecSegLine, out List<BisectionSegLine> curSpliters1, out List<BisectionSegLine> curSpliters2);

                                var leftkey = curSpliters1.GetKey();
                                if (!BisectAreaDic.ContainsKey(leftkey)) 
                                {
                                    var AreaLeft = new BisectionArea(segAreas[0], curSpliters1);// 垂直线的左区域
                                    BisectAreaDic.Add(leftkey, AreaLeft);//字典中添加元素
                                }
                                var rightkey = curSpliters2.GetKey();
                                if (!BisectAreaDic.ContainsKey(rightkey))
                                {
                                    var AreaRight = new BisectionArea(segAreas[1], curSpliters2);// 垂直线的右区域
                                    BisectAreaDic.Add(rightkey, AreaRight);//字典中添加元素
                                }
                            }
                        }
                        else//不合理分区线，无操作，扩大范围
                        {
                            
                        }
                        endX += moveDist;
                    }
                }
            }
        }

        private void UpdateAllHorzSeg(BisectionArea bisectArea)//区域探索完添加标记
        {
            //假设当前区域外部没有墙线，或者分区线穿插不到外部的墙线
            var area = bisectArea.Area.Clone() as Polyline;
            var pts = area.GetPoints();
            var left = (int)pts.Min(pt => pt.X) - Tol;
            var right = (int)pts.Max(pt => pt.X) + Tol;
            var buttom = pts.Min(pt => pt.Y);
            var top = pts.Max(pt => pt.Y) ;
            // 竖向起始分区线，从left开始，步长;cutdistance
            bool CrossBuilding = false;// 判断车道buffer是否穿障碍物,上一个buffer包含障碍物才考虑生成新的分区线

            var startY = buttom;//上一个位置
            var endY = startY + StepSize;//尝试位置，新位置
            while (endY < top)
            {
                var spt = new Point3d(left, startY, 0);
                var ept = new Point3d(right, endY, 0);
                var leftPt = new Point3d(left, endY, 0);
                var rightPt = ept;
                var segLine = new Line(leftPt, rightPt);
                var areaBuffer = AutoFunctions.GetRect(spt, ept);
                if (!CrossBuilding)// 之前区域未包含建筑
                {
                    var segRectInBuild = BuildingSpatialIndex.SelectCrossingPolygon(areaBuffer);
                    if (segRectInBuild.Count > 0)//当前区域包含建筑
                    {
                        CrossBuilding = true;
                    }
                    else// 不包含，扩大区域
                    {
                        endY += StepSize;
                    }
                }
                else// 之前区域包含建筑
                {
                    var segRectInBuild = BuildingSpatialIndex.SelectFence(segLine);
                    if (segRectInBuild.Count > 0)// 当前分区线穿墙
                    {
                        endY += StepSize;// 扩大区域
                    }
                    else// 不穿墙，判断是否vaild
                    {
                        var segFlag = IsCorrectSegLines(segLine, area, out double maxVal, out double minVal, out double moveDist);

                        if (segFlag)// 为合理分区线
                        {
                            // 移动分区线到中点
                            var foundMid = GetMidVal(endY, minVal, maxVal, false, out int midY);// 获取中点坐标，优先在所有已有的线中找
                            segLine.StartPoint = new Point3d(left, midY, 0);
                            segLine.EndPoint = new Point3d(right, midY, 0);
                            var segAreas = segLine.Split(area);
                            if (segAreas.Count == 2)//当前中线合理
                            {
                                segAreas.SortByOrder(segLine);// 对分区排序，上下，左右
                                if (!foundMid) HorzSegLineValues.Add(midY);// 没有已有的线，记录中线的值
                                // 更新搜索区域
                                CrossBuilding = false;
                                startY = endY;
                                var vals = GetStartEndValue(area, segLine);
                                // 将当前分区线转换为BisecSegLine 
                                var BisecSegLine = new BisectionSegLine(segLine, midY, vals.Item1, vals.Item2, maxVal, minVal);
                                var lineKey = BisecSegLine.Key;
                                if (BisectSegLineDic.ContainsKey(lineKey)) BisecSegLine = BisectSegLineDic[lineKey];
                                else// 新的分区线
                                {
                                    BisectSegLineDic.Add(lineKey, BisecSegLine);
                                    //if (ShowSegLineOnly) BisecSegLine.DrawSegLine();//画出来
                                }
                                //为节点添加分支
                                bisectArea.AddBranch(BisecSegLine, out List<BisectionSegLine> curSpliters1, out List<BisectionSegLine> curSpliters2);

                                var topkey = curSpliters1.GetKey();
                                if (!BisectAreaDic.ContainsKey(topkey)) 
                                {
                                    var AreaTop = new BisectionArea(segAreas[0], curSpliters1);// 垂直线的左区域
                                    BisectAreaDic.Add(topkey, AreaTop);//字典中添加元素
                                }
                                var bottomkey = curSpliters2.GetKey();
                                if (!BisectAreaDic.ContainsKey(bottomkey))
                                {
                                    var AreaBottom = new BisectionArea(segAreas[1], curSpliters2);// 垂直线的右区域
                                    BisectAreaDic.Add(bottomkey, AreaBottom);//字典中添加元素
                                }
                            }
                        }
                        else//不合理分区线，无操作，扩大范围
                        {
                        }
                        endY += moveDist;
                    }
                }
            }
        }
        private bool GetMidVal(double value,double minVal ,double maxVal,bool vertical,out int midVal)
        {
            var mid = (int)((minVal + maxVal) / 2 + value);// 当前中点横坐标
            midVal = mid;
            var mid1 = mid - 1;
            var mid2 = mid + 1;
            var lis = new List<int> { mid1, mid, mid2 };
            if (vertical)
            {
                foreach(var val in lis)
                {
                    if (VertSegLineValues.Contains(val))
                    {
                        midVal = val;
                        return true;//当前有记录
                    }
                }
            }
            else
            {
                foreach (var val in lis)
                {
                    if (HorzSegLineValues.Contains(val))
                    {
                        midVal = val;
                        return true;//当前有记录
                    }
                }
            }
            return false;//当前无记录
        }

        private (int,int) GetStartEndValue(Polyline area, Line SegLine)//利用分区线有效的部分
        {
            int minVal;
            int maxVal;
            var vaildLine = GetVaildSegLineAtLine(area, SegLine);
            if(SegLine.IsVertical())
            {
                minVal =(int) Math.Min(vaildLine.StartPoint.Y, vaildLine.EndPoint.Y);//向下取整
                maxVal = (int) Math.Ceiling(Math.Max(vaildLine.StartPoint.Y, vaildLine.EndPoint.Y)) ;//向上取整
            }
            else
            {
                minVal = (int)Math.Min(vaildLine.StartPoint.X, vaildLine.EndPoint.X);
                maxVal = (int)Math.Ceiling(Math.Max(vaildLine.StartPoint.X, vaildLine.EndPoint.X));
            }
            return (minVal, maxVal);
        }
        private Line GetVaildSegLineAtLine(Polyline area, Line SegLine)// 获取当前有效分区线,切掉超出边界的部分
        {
            var templ = SegLine.Intersect(area, Intersect.OnBothOperands);
            if (templ.Count != 2)
            {
                return null;
            }
            return new Line(templ[0], templ[1]);
        }
        private bool IsCorrectSegLines(Line segLine, Polyline area,out double maxVal, out double minVal,out double moveDist)
        {
            maxVal = 0;
            minVal = 0;
            var segAreas = segLine.Split(area);
            if (segAreas.Count != 2)//不是两个区域直接退出
            {
                moveDist = StepSize/10;
                return false;
            }
            foreach (var segArea in segAreas)
            {
                var buildLines = BuildingSpatialIndex.SelectCrossingPolygon(segArea);
                if (buildLines.Count == 0)
                {
                    moveDist = StepSize;
                    return false;//子区域没有建筑物直接退出
                }
                var dist = Tol + (RoadWidth / 2);
                var midpt = segArea.GetCenter();
                if (segLine.GetValueType(midpt))
                {
                    maxVal = segLine.GetMinDist(buildLines) - dist;
                }
                else
                {
                    minVal = -segLine.GetMinDist(buildLines) + dist;
                }
            }
            moveDist = Math.Max(StepSize, maxVal + StepSize);//跳出当前区域，避免重复计算

            if (maxVal <= (minVal + CutTol - Tol))// 最大最小值之间要留有容差距离
            {
                return false;
            }
            return true;
        }
        private List<Line> GetAllSegLineAtLine(Polyline area,Line SegLine)// 获取一个区域在当前线的位置的全部分区线
        {
            // 假设分区线穿过区域被截断成多根
            var segLineList = new List<Line>();
            var templ = SegLine.Intersect(area, Intersect.OnBothOperands);
            if (templ.Count < 2)
            {
                return segLineList;
            }
            var Vertical = SegLine.IsVertical();
            if (Vertical) templ = templ.OrderBy(i => i.Y).ToList();// 垂直order by Y
            else templ = templ.OrderBy(i => i.X).ToList();//水平orderby X
            for(int i =0;i < templ.Count -1; ++i)
            {
                Point3d MidPoint;
                if (Vertical)
                {
                    MidPoint = new Point3d(templ[i].X, (templ[i].Y + templ[i + 1].Y) / 2, 0);
                }
                else
                {
                    MidPoint = new Point3d((templ[i].X + templ[i + 1].X)/2, templ[i].Y , 0);
                }
                if (area.Contains(MidPoint)) segLineList.Add(new Line(templ[i], templ[i + 1]));//中点在区域内则该线为可行线
            }
            return segLineList;
        }
        private void Clear()//清空非托管资源
        {
        }
        public void Dispose()
        {
            Clear();
        }
        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
    }

    class BisectionSegLine
    {
        public readonly int Value;
        public readonly int StartVal;
        public readonly int EndVal;
        public readonly bool Vertical;
        public readonly double MaxVal;
        public readonly double MinVal;
        public readonly Line SegLine;
        public readonly Tuple<int, int, int, bool> Key;
        public BisectionSegLine(Line segLine,int value,int startVal,int endVal, double maxVal,double minVal)
        {
            MaxVal = maxVal;
            MinVal = minVal;
            Vertical = segLine.IsVertical();
            SegLine = segLine.Clone() as Line;
            Value = value;
            StartVal = startVal;
            EndVal = endVal;
            Key = new Tuple<int, int, int, bool>(Value, StartVal, EndVal, Vertical);
        }
        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
        public bool IsVertical()
        {
            return Vertical;
        }
        public void DrawSegLine(int layer = 0)
        {
            string LayerName = "AI-自动分区线" + layer.ToString();
            var segLine_C = SegLine.Clone() as Line;
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                if (!currentDb.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(currentDb.Database, LayerName, 3);
                segLine_C.Layer = LayerName;
                segLine_C.ColorIndex = 3;
                segLine_C.AddToCurrentSpace();
            }
        }
    }
    class BisectionArea// 二分区域，树状结构
    {
        public List<BisectionSegLine> Spliters;// 分割出当前区域的分区线,分别在区域的上下左右
        public Polyline Area;
        public List<BisectionSegLine> SegLines;

        public List<Tuple<int?, int?, int?, int?>> SubAreaKeys;//所有子区域的key
        public bool AllSearched;// 是否完成探索
        public BisectionArea(Polyline area,List<BisectionSegLine> spliters)
        {
            if(spliters.Count != 4) throw new ArgumentException("分区线数量不对");
            Spliters = spliters;// 构造区域的ID的数据结构
            Area = area.Clone() as Polyline;
            SegLines = new List<BisectionSegLine>();
            SubAreaKeys = new List<Tuple<int?, int?, int?, int?>>();
        }

        public Tuple<int?,int?,int?,int?> GetKey()// 获取当前区域的key，区域ID
        {
            return Spliters.GetKey();
        }
        // 添加分支，一根分区线可以确定一个分支
        public void AddBranch(BisectionSegLine BisecSegLine,out List<BisectionSegLine> curSpliters1, out List<BisectionSegLine> curSpliters2)
        {
            curSpliters1 = new List<BisectionSegLine>();
            curSpliters2 = new List<BisectionSegLine>();
            foreach(var s in Spliters)
            {
                curSpliters1.Add(s);
                curSpliters2.Add(s);
            }
            if (BisecSegLine.IsVertical())// 垂直线
            {
                curSpliters1[3] = BisecSegLine; //垂直线，位于其左区域的右边
                curSpliters2[2] = BisecSegLine; //垂直线，位于其右区域的左边
            }
            else// 水平线
            {
                curSpliters1[1] = BisecSegLine; //水平线，位于其上区域的下边
                curSpliters2[0] = BisecSegLine; //水平线，位于其下区域的上边
            }
            SegLines.Add(BisecSegLine);
            SubAreaKeys.Add(curSpliters1.GetKey());
            SubAreaKeys.Add(curSpliters2.GetKey());
        }

        public void ShowSegLines()
        {
            string LayerName = "AI-自动分区线";
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                if (!currentDb.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(currentDb.Database, LayerName, 3);
                foreach (var bisecSegline in SegLines)
                {
                    var segline = bisecSegline.SegLine;
                    segline.Layer = LayerName;
                    segline.ColorIndex = 3;
                    segline.AddToCurrentSpace();
                }
            }
        }
    }
    class BisectionPlan: IEquatable<BisectionPlan>//分区线组合
    {
        public HashSet<Tuple<int, int, int, bool>> PlanKey;// 分割方案的key
        public HashSet<Tuple<int?, int?, int?, int?>> AllAreas;//所有子区域 key的合集
        public BisectionPlan(List<Tuple<int, int, int, bool>> planKey,List<Tuple<int?, int?, int?, int?>> allAreas)
        {
            AllAreas = allAreas.ToHashSet();
            PlanKey = planKey.ToHashSet();

        }
        public override int GetHashCode()
        {
            var hashCodeToReturn = 1;
            PlanKey.ForEach(key => hashCodeToReturn ^= key.GetHashCode());
            return hashCodeToReturn;
        }
        // 获取基于新分区线的子方案。 subAreaKeys 为新分割出的区域的key（两个）
        // orgAreaKey 为原始区域
        public BisectionPlan GetSubPlan(BisectionSegLine segLine, (Tuple<int?, int?, int?, int?>, Tuple<int?, int?, int?, int?>)
            subAreaKeys, Tuple<int?, int?, int?, int?> orgAreaKey)
        {
            if (PlanKey.Contains(segLine.Key)) throw new ArgumentException("Key Already in Hash Set");
            var newPlanKey = PlanKey.ToList();
            newPlanKey.Add(segLine.Key);

            var newAreas = AllAreas.ToList();
            newAreas.Remove(orgAreaKey);//移除旧区域
            newAreas.Add(subAreaKeys.Item1);
            newAreas.Add(subAreaKeys.Item2);
            return new BisectionPlan(newPlanKey, newAreas);
        }

        public bool Equals(BisectionPlan other)
        {
            //return this.PlanKey.SetEquals(other.PlanKey);
            if (this.PlanKey.Count != other.PlanKey.Count) return false;

            var exceptResult = this.PlanKey.Except(other.PlanKey);

            return exceptResult.Count() == 0;
        }
        public void Draw(int layer, Dictionary<Tuple<int, int, int, bool>, BisectionSegLine> BisectSegLineDic)
        {

            PlanKey.ForEach(key => BisectSegLineDic[key].DrawSegLine(layer));
        }
    }
    static class AutoFunctions
    {
        private static List<bool> Directions = new List<bool>() { false, false, true, true };//Spliters 默认方向，上下左右
        public static Polyline GetBuffer(this Line BufferLine, double distance)
        {
            // 获取bufferline往正或负方向的buffer
            // distance 为正，则正向buffer，为负则负向buffer
            // positive 坐标增加方向buffer

            var pline = new Polyline();
            if (BufferLine.IsVertical())
            {
                var points = new List<Point2d> {new Point2d(BufferLine.StartPoint.X , BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.StartPoint.X + distance, BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.EndPoint.X + distance, BufferLine.EndPoint.Y),
                                                new Point2d(BufferLine.EndPoint.X , BufferLine.EndPoint.Y)};
                pline.CreatePolyline(points.ToArray());
            }
            else
            {
                var points = new List<Point2d> {new Point2d(BufferLine.StartPoint.X , BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.StartPoint.X , BufferLine.StartPoint.Y + distance) ,
                                                new Point2d(BufferLine.EndPoint.X , BufferLine.EndPoint.Y + distance),
                                                new Point2d(BufferLine.EndPoint.X , BufferLine.EndPoint.Y)};
                pline.CreatePolyline(points.ToArray());
            }
            pline.Closed = true;
            return pline;
        }

        public static Polyline GetRect(Point3d pt1,Point3d pt2)
        {
            var pline = new Polyline();
            pline.CreateRectangle(new Point2d(pt1.X, pt1.Y), new Point2d(pt2.X, pt2.Y));
            return pline;
        }
        public static void SortByOrder(this List<Polyline> segAreas, Line segLine)
        {
            if (segAreas.Count != 2) throw new ArgumentException("二分区域必须为2");
            var center1 = segAreas.First().GetCenter();
            if (segLine.IsVertical())// 垂直线，先左后右
            {
                if(center1.X > segLine.StartPoint.X) // 转换顺序
                {
                    segAreas.Reverse();
                }
            }
            else//水平线，先上后下
            {
                if (center1.Y < segLine.StartPoint.Y) // 转换顺序
                {
                    segAreas.Reverse();
                }
            }
        }

        public static bool GetValueType(this Line line, Point3d pt)
        {
            //判断点是直线的上限还是下限
            //var dir = line.IsVertical();
            if (line.IsVertical())
            {
                return pt.X > line.StartPoint.X;
            }
            else
            {
                return pt.Y > line.StartPoint.Y;
            }
        }
        public static double GetMinDist(this Line line, DBObjectCollection buildLines)// 返回线到建筑点集的最短距离
        {
            //var minDists = new List<double>();
            var geos = new List<Geometry>();
            foreach (var build in buildLines)
            {
                 if(build is Polyline pline)
                {
                    geos.Add(pline.ToNTSLineString());
                    //minDists.Add(line.Distance(curve));
                }
            }
            return line.ToNTSLineString().Distance(new GeometryCollection(geos.ToArray()));
            //return minDists.Min();
        }
        public static Tuple<int?, int?, int?, int?> GetKey(this List<BisectionSegLine> Spliters)// 获取当前区域的key，区域ID
        {
            if (Spliters.Count != 4) throw new ArgumentException("分区线数量不对");
            //4个值分别代表分割当前区域上下左右位置的分区线,每个值都可以为空
            var result = new List<int?>();
            for (int i = 0; i < Spliters.Count; ++i)
            {
                var spliter = Spliters[i];
                if (spliter != null)
                {
                    var Vertical = spliter.Vertical;
                    var value = spliter.Value;
                    if (Vertical != Directions[i]) throw new ArgumentException("当前区域分区线方向错误");
                    result.Add(value);
                }
                else result.Add(null);
            }
            return new Tuple<int?, int?, int?, int?>(result[0], result[1], result[2], result[3]);
        }

        public static int GetValue(this Line line)
        {
            if (line.IsVertical())
            {
                return (int)line.StartPoint.X;
            }
            else
            {
                return (int)line.StartPoint.Y;
            }
        }

        public static int GetNewAreaCount(this List<BisectionArea> Areas)
        {
            int count = 0;
            foreach(var area in Areas)
            {
                if (!area.AllSearched) count += 1;
            }
            return count;
        }

        public static List<Polyline> Split(this Line line,Polyline pline)
        {
            var lstrs = new List<LineString>();
            lstrs.Add(pline.ToNTSLineString());
            lstrs.Add(line.ToNTSLineString());
            return lstrs.GetPolygons().Select(p => p.Shell.ToDbPolyline()).ToList();
        }

        public static bool IsDominatedBy(this BisectionSegLine l_this, BisectionSegLine l_other)
        {
            if(l_this.Vertical != l_other.Vertical) return false;
            var this_abs = (l_this.MaxVal - l_this.MinVal)/2;
            var other_abs = (l_other.MaxVal - l_other.MinVal)/2;
            var this_lb =   l_this.Value - this_abs;
            var other_lb =  l_other.Value - other_abs;
            var this_ub =  l_this.Value + this_abs;
            var other_ub = l_other.Value + other_abs;
            var tol = 5;
            bool bool1 = false;
            if(this_lb <= other_lb + tol && this_ub + tol >= other_lb )
            {
                bool1 = true;
            }
            if(this_ub + tol >= other_ub  && this_lb <= other_ub + tol)
            {
                bool1 = true;
            }
            //var bool1 = (this_lb <= other_ub + tol) || (this_ub + tol>= other_lb);

            var this_SE = l_this.SegLine.GetStartEndValue();
            var other_SE = l_other.SegLine.GetStartEndValue();
            var bool2 = ((this_SE.Item1  >= other_SE.Item1)  && (this_SE.Item2 < other_SE.Item2 )) ||
                        ((this_SE.Item1 > other_SE.Item1) && (this_SE.Item2 <= other_SE.Item2));
            //var bool2 = l_this.StartVal+tol >= l_other.StartVal && l_this.EndVal <= l_other.EndVal + tol;
            return bool1 && bool2;
        }
        public static bool DominatedByAny(this BisectionSegLine l_this,IEnumerable<BisectionSegLine> others)
        {
            foreach(var l in others)
            {
                if (l_this.IsDominatedBy(l)) return true;
            }
            return false;
        }

        private static (int, int) GetStartEndValue(this Line SegLine)//利用分区线有效的部分
        {
            int minVal;
            int maxVal;
            if (SegLine.IsVertical())
            {
                minVal = (int)Math.Min(SegLine.StartPoint.Y, SegLine.EndPoint.Y);//向下取整
                maxVal = (int)Math.Ceiling(Math.Max(SegLine.StartPoint.Y, SegLine.EndPoint.Y));//向上取整
            }
            else
            {
                minVal = (int)Math.Min(SegLine.StartPoint.X, SegLine.EndPoint.X);
                maxVal = (int)Math.Ceiling(Math.Max(SegLine.StartPoint.X, SegLine.EndPoint.X));
            }
            return (minVal, maxVal);
        }

        public static void ShowLowerUpperBound(this BisectionSegLine bisectionSeg, string layer = "最大最小值")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 3);
            }


            LineSegment SegLine = bisectionSeg.SegLine.ToNTSLineSegment();
            var abs = (bisectionSeg.MaxVal - bisectionSeg.MinVal)/2;
            var lb =  bisectionSeg.Value + abs;
            var ub =  bisectionSeg.Value -abs;
            LinearRing shell;
            if (SegLine.IsVertical())
            {
                var origion = new Coordinate(lb, SegLine.P0.Y);
                var coors = new Coordinate[] { origion,
                                                new Coordinate(lb, SegLine.P1.Y),
                                                new Coordinate(ub, SegLine.P1.Y),
                                                new Coordinate(ub, SegLine.P0.Y),origion};
                shell = new LinearRing(coors);

            }
            else
            {
                var origion = new Coordinate(SegLine.P0.X, lb);
                var coors = new Coordinate[] { origion,
                                                new Coordinate(SegLine.P1.X, lb),
                                                new Coordinate(SegLine.P1.X, ub),
                                                new Coordinate(SegLine.P0.X, ub),origion};
                shell = new LinearRing(coors);

            }
            var poly = shell.ToDbPolyline();
            poly.Layer = layer;
            poly.ColorIndex = 3;
            poly.AddToCurrentSpace();
        }

    }
}
