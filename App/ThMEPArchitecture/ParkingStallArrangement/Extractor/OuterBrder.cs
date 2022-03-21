using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using Linq2Acad;
using ThMEPEngineCore;
using ThMEPArchitecture.ViewModel;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public class OuterBrder
    {
        public DBObjectCollection OuterLineObjs = new DBObjectCollection();
        public DBObjectCollection BuildingObjs = new DBObjectCollection();
        public DBObjectCollection BuildingWithoutRampObjs = new DBObjectCollection();

        public List<Line> SegLines = new List<Line>();//分割线
        private List<Line> VaildSegLines;//有效的分割线
        public List<BlockReference> Buildings = new List<BlockReference>();//建筑物block
        public Polyline WallLine = new Polyline();//外框线
        public List<BlockReference> Ramps = new List<BlockReference>();//坡道块
        public List<BlockReference> LonelyRamps = new List<BlockReference>();//未合并的坡道
        

        public ThCADCoreNTSSpatialIndex BuildingSpatialIndex = null;//建筑物SpatialIndex
        public ThCADCoreNTSSpatialIndex LonelyRampSpatialIndex = null;//孤立在地库中间的坡道
        public ThCADCoreNTSSpatialIndex AttachedRampSpatialIndex = null;//依附在地库边界上的坡道
        public ThCADCoreNTSSpatialIndex BuildingWithoutRampSpatialIndex = null;//不包含坡道的建筑物
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex;// 全部障碍物（包含坡道）polyline的spatial index
        public ThCADCoreNTSSpatialIndex WallSpatialIndex;//墙的spatial index
        public List<Ramps> RampLists = new List<Ramps>();//孤立坡道块
        private Serilog.Core.Logger Logger;
        public bool Extract(BlockReference basement)
        {
            Explode(basement);
            BuildingWithoutRampSpatialIndex = new ThCADCoreNTSSpatialIndex(BuildingWithoutRampObjs);
            BuildingSpatialIndex = new ThCADCoreNTSSpatialIndex(BuildingObjs);
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(BuildingObjs.ExplodeBlocks());
            
            foreach (var obj in OuterLineObjs)
            {
                var pline = obj as Polyline;
                if (pline.Length > 0.0)
                {
                    pline = pline.DPSimplify(1.0);
                    pline = pline.MakeValid().OfType<Polyline>().OrderByDescending(p => p.Area).First(); // 处理自交
                }
                WallLine = pline;
                break;
            }
            if (WallLine.GetPoints().Count() == 0)//外框线不存在
            {
                Active.Editor.WriteMessage("地库边界不存在！");
                return false;
            }

            if (Buildings.Count == 0)
            {
                Active.Editor.WriteMessage("障碍物不存在！");
                return false;
            }

            if (Ramps.Count == 0)
            {
                WallSpatialIndex = new ThCADCoreNTSSpatialIndex(WallLine.ToLines().ToCollection());
                return true;
            }

            if (Ramps.Count > 0)//合并坡道至墙线
            {
                Ramps.ForEach(ramp => LonelyRamps.Add(ramp));
                var rampSpatialIndex = new ThCADCoreNTSSpatialIndex(Ramps.ToCollection());
                var rstRamps = rampSpatialIndex.SelectFence(WallLine);
                AttachedRampSpatialIndex = new ThCADCoreNTSSpatialIndex(rstRamps);
                foreach (var ramp in rstRamps)
                {
                    LonelyRamps.Remove(ramp as BlockReference);
                }
                LonelyRampSpatialIndex = new ThCADCoreNTSSpatialIndex(LonelyRamps.ToCollection());
                var splitArea = WallLine.SplitByRamp(rstRamps);
                splitArea = splitArea.DPSimplify(1.0);
                splitArea = splitArea.MakeValid().OfType<Polyline>().OrderByDescending(p => p.Area).First(); // 处理自交
                WallLine = splitArea;
            }
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(WallLine.ToLines().ToCollection());

            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                var rst = LonelyRampSpatialIndex.SelectFence(segLine);
                if (rst.Count > 0)
                {
                    var blk = rst[0] as BlockReference;
                    var rect = blk.GetRect();
                    RampLists.Add(new Ramps(segLine.Intersect(rect, 0).First(), blk));

                    SegLines.RemoveAt(i);
                }
            }
            return true;
        }

        private bool IsOuterLayer(string layer)
        {
            return layer.ToUpper() == "地库边界";
        }

        private bool IsBuildingLayer(string layer)
        {
            return layer.ToUpper().Contains("障碍物");
        }

        private bool IsRampLayer(string layer)
        {
            return layer.ToUpper() == "坡道";
        }

        private bool IsSegLayer(string layer)
        {
            return layer.ToUpper() == "分割线";
        }

        private void Explode(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);

            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                AddObjs(ent);
            }
        }

        private void AddObjs(Entity ent)
        {
            if (IsOuterLayer(ent.Layer))
            {
                if (ent is Polyline pline)
                {
                    OuterLineObjs.Add(pline);
                }
            }
            if (IsBuildingLayer(ent.Layer))
            {
                if (ent is BlockReference br)
                {
                    var l = new Line();
                    l.Intersect(br, Intersect.OnBothOperands);
                    Buildings.Add(br);
                    BuildingObjs.Add(br);
                    BuildingWithoutRampObjs.Add(br);
                }
            }
            if (IsRampLayer(ent.Layer))
            {
                if (ent is BlockReference br)
                {
                    Ramps.Add(br);
                    Buildings.Add(br);
                    BuildingObjs.Add(br);
                }
            }
            if (IsSegLayer(ent.Layer))
            {
                if (ent is Line line)
                {
                    SegLines.Add(line);
                }
            }
        }
        #region 分割线检查
        public bool SegLineVaild(Serilog.Core.Logger logger)
        {
            // 标记圆半径5000
            Logger = logger;
            // 判断正交（中点标记）
            if(!IsOrthogonal()) return false;
            //分割线不可在区域外相交（区域外交点标记）
            if (!AllIntSecPointInside()) return false;
            // 判断每根分割线至少有两个交点(端点标记）
            if (!HaveAtLeastTwoIntsecPoints()) return false ;
            //获取有效分割线
            VaildSegLines = SegLines.GetVaildSegLines(WallLine);
            // 判断分割线净宽（中点标记）
            if (!LaneWidthSatisfied()) return false;
            // 判断孤立车道（两个以上标记剩余中点，以下标记自己）
            if (!Allconnected()) return false;
            // 分割线穿块检查（被穿块的bounding box，外扩）
            if (!NoneCrossBlock()) return false;
            return true;
        }

        // 判断正交
        private bool IsOrthogonal()
        {
            double tol = 0.02;// arctan 0.02 （1.146°）以下的交会自动归正
            for (int i = 0; i < SegLines.Count; i++)
            {

                var line = SegLines[i];
                var spt = line.StartPoint;

                var ept = line.EndPoint;
                //1. check parallel, perpendicular
                var X_dif = Math.Abs(spt.X - ept.X);
                var Y_dif = Math.Abs(spt.Y - ept.Y);
                if (Y_dif > X_dif)// 垂直线
                {
                    if (X_dif / Y_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        //Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        //Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        line.GetCenter().MarkPoint();
                        return false;
                    }
                    var newX = (spt.X + ept.X) / 2;
                    spt = new Point3d(newX, spt.Y, 0);
                    ept = new Point3d(newX, ept.Y, 0);
                }
                if (X_dif > Y_dif)// 水平线
                {
                    if (Y_dif / X_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        //Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        //Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        line.GetCenter().MarkPoint();
                        return false;
                    }
                    var newY = (spt.Y + ept.Y) / 2;
                    spt = new Point3d(spt.X, newY, 0);
                    ept = new Point3d(ept.X, newY, 0);
                }
                SegLines[i] = new Line(spt, ept);
            }
            return true;
        }
        //分割线不可在区域外相交
        private bool AllIntSecPointInside()
        {
            var IntSecPoints = SegLines.GetIntSecPts();
            foreach(var pt in IntSecPoints)
            {
                if (!WallLine.Contains(pt))
                {
                    Logger?.Information("发现区域外交点 ！\n");
                    Logger?.Information(pt.ToString() + "的交点不符合要求\n");
                    Active.Editor.WriteMessage("发现区域外交点 ！\n");
                    pt.MarkPoint();
                    return false;
                }  
            }
            return true;
        }
        // 判断每根分割线至少有两个交点
        private bool HaveAtLeastTwoIntsecPoints()
        {
            //double tol = 1e-4;
            for (int i = 0; i < SegLines.Count; i++)
            {
                var pts = new List<Point3d>();
                var line = SegLines[i];
                var spt = line.StartPoint;

                // check intersection points
                pts.AddRange(line.Intersect(WallLine, 0));//求与边界的交点
                for (int j = 0; j < SegLines.Count; j++)
                {
                    if (i == j) continue;
                    pts.AddRange(line.Intersect(SegLines[j], 0));//求与其他分割线的交点
                }
                var orderPts = pts.OrderBy(p => p.DistanceTo(line.StartPoint)).ToList();
                if (orderPts.Count < 2)
                {
                    Logger?.Information("该分割线只有" + orderPts.Count.ToString() + "个交点" + "\n");
                    Logger?.Information("起始点：" + line.StartPoint.ToString() + "终点：" + line.EndPoint.ToString() + "的分割线不符合要求" + "\n");
                    Active.Editor.WriteMessage("该分割线只有" + orderPts.Count.ToString() + "个交点" + "\n");
                    //Active.Editor.WriteMessage("起始点：" + line.StartPoint.ToString() + "\n");
                    //Active.Editor.WriteMessage("终点：" + line.EndPoint.ToString() + "\n");
                    line.StartPoint.MarkPoint();
                    line.EndPoint.MarkPoint();
                    return false;
                }
                // Check if two intersection points on the segline are the same
                //for(int k = 0;k < orderPts.Count-1; k++)
                //{
                //    //logger.
                //    if (orderPts[k].Equals(orderPts[k + 1]))
                //    {
                //        Logger?.Information("最多两条线（分割线或者边界线）相交与同一点 \n");
                //        Logger?.Information("交点：" + orderPts[k].ToString());
                //        Active.Editor.WriteMessage("最多两条线相交于同一点");
                //        Active.Editor.WriteMessage("交点：" + orderPts[k].ToString());

                //        return false;
                //    }
                //}
            }
            return true;
        }
        // 判断分割线净宽
        private bool LaneWidthSatisfied()
        {
            double tol = (ParameterStock.RoadWidth / 2)-0.1;// 2749.9
            for (int i = 0; i < VaildSegLines.Count; i++)
            {
                var segline = VaildSegLines[i];
                var rect = segline.Buffer(tol);
                var rst1 = ObstacleSpatialIndex.SelectCrossingPolygon(rect);
                var rst2 = WallSpatialIndex.SelectCrossingPolygon(rect);
                if (rst1.Count > 0|| rst2.Count > 0)
                {
                    var spt = SegLines[i].StartPoint;
                    var ept = SegLines[i].EndPoint;
                    Logger?.Information("发现分割线净宽不够 ！\n");
                    Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                    Active.Editor.WriteMessage("发现分割线净宽不够 \n");
                    SegLines[i].GetCenter().MarkPoint();
                    return false;
                }
            }
            //var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            //segAreasCnt = rstAreas.Count;
            return true;
        }
        // 判断孤立车道
        private bool Allconnected()
        {
            var CheckedLines = new DBObjectCollection();
            CheckedLines.Add(SegLines[0]);
            var rest_idx = new List<int>();
            for (int i = 1; i < SegLines.Count; ++i) rest_idx.Add(i);

            while (rest_idx .Count != 0)
            {
                var curCount = rest_idx.Count;// 记录列表个数
                for (int j = 0; j < curCount; ++j)
                {
                    var idx = rest_idx[j];
                    var line = SegLines[idx];
                    if (line.ConnectWithAny(CheckedLines))
                    {
                        CheckedLines.Add(line);
                        rest_idx.RemoveAt(j);
                        break;
                    }
                    if (j == curCount - 1)
                    {
                        Logger?.Information("分割线未互相连接 ！\n");
                        Active.Editor.WriteMessage("分割线未互相连接！ \n");
                        if(CheckedLines.Count < 3)
                        {
                            foreach(Line linetomark in CheckedLines)
                            {
                                linetomark.GetCenter().MarkPoint();
                            }
                        }
                        else
                        {
                            foreach(int idxtomark in rest_idx)
                            {
                                SegLines[idxtomark].GetCenter().MarkPoint();
                            }
                        }
                        return false;// 当前线不与任何线相交
                    }
                        
                }
            }
            return true;
        }
        // 分割线穿块检查
        private bool NoneCrossBlock()
        {
            foreach(var line in VaildSegLines)
            {
                foreach(BlockReference block in BuildingObjs)
                {
                    if ( line.Intersect(block.GetRect(), Intersect.OnBothOperands).Count != 0)
                    {
                        Logger?.Information("分割线穿块 ！\n");
                        Active.Editor.WriteMessage("分割线穿块！ \n");
                        block.MarkBlock();
                        return false;
                    }
                }
                //if(BuildingSpatialIndex.SelectFence(line).Count !=0) return false;
            }
            return true;
        }
        #endregion
    }

    public static class HelperEX
    {
        public static bool ConnectWithAny(this Line line,DBObjectCollection objs)
        {
            foreach (Entity ent in objs)
            {
                if (line.Intersect(ent, Intersect.OnBothOperands).Count != 0) return true;
            }
            return false;
        }
        public static List<Point3d> GetIntSecPts(this List<Line> segline) // 获取全部分割线的交点
        {
            var IntSecPoints = new List<Point3d>();//交点列表
            for (int i = 0; i < segline.Count; ++i)
            {
                var line1 = segline[i];
                for (int j = i+1; j < segline.Count; ++j)
                {
                    var line2 = segline[j];
                    var templ = line1.Intersect(line2, Intersect.OnBothOperands);
                    if (templ.Count != 0) IntSecPoints.Add(templ.First());
                }
            }
            return IntSecPoints;
        }
        public static List<Line> GetVaildSegLines(this List<Line> segline,Polyline wallLine)
        {
            var vaildSegLines = new List<Line>();
            for(int i = 0; i < segline.Count; ++i)
            {
                var pts = GetAllIntSecPoints(i, segline, wallLine);
                vaildSegLines.Add(new Line(pts.First(), pts.Last()));
            }
            return vaildSegLines;
        }
        private static List<Point3d> GetAllIntSecPoints(int idx, List<Line> segline, Polyline wallLine)//获取segline中某一跟全部的交点，跟外边框的交点选取最外的交点-车道宽
        {
            double tol = ParameterStock.VerticalSpotWidth *2;// 边界剪掉两个车位宽
            var IntSecPoints = new List<Point3d>();//断点列表
            var line1 = segline[idx];
            var VerticalDirection = line1.IsVertical();
            var templ = line1.Intersect(wallLine, Intersect.OnBothOperands);
            if (templ.Count != 0)//初始线和外包框有交点
            {
                Point3d pt1;
                Point3d pt2;
                if (templ.Count < 2)
                {
                    var pt = templ.First();
                    if (VerticalDirection)
                    {
                        pt1 = new Point3d(pt.X, pt.Y + tol, 0);
                        pt2 = new Point3d(pt.X, pt.Y - tol, 0);
                    }
                    else
                    {
                        pt1 = new Point3d(pt.X + tol, pt.Y, 0);
                        pt2 = new Point3d(pt.X - tol, pt.Y, 0);
                    }
                    if (wallLine.Contains(pt1)) IntSecPoints.Add(pt1);
                    else IntSecPoints.Add(pt2);
                }
                else
                {
                    if (VerticalDirection)
                    {
                        templ = templ.OrderBy(i => i.Y).ToList();// 垂直order by Y
                        pt1 = templ.First();
                        pt1 = new Point3d(pt1.X, pt1.Y + tol, 0);//取不到两个车位的阈值
                        pt2 = templ.Last();
                        pt2 = new Point3d(pt2.X, pt2.Y - tol, 0);
                    }
                    else
                    {
                        templ = templ.OrderBy(i => i.X).ToList();//水平orderby X
                        pt1 = templ.First();
                        pt1 = new Point3d(pt1.X + tol, pt1.Y, 0);
                        pt2 = templ.Last();
                        pt2 = new Point3d(pt2.X - tol, pt2.Y, 0);
                    }
                    IntSecPoints.Add(pt1);
                    IntSecPoints.Add(pt2);
                }
            }
            for (int i = 0; i < segline.Count; ++i)
            {
                if (i == idx) continue;
                var line2 = segline[i];
                templ = line1.Intersect(line2, Intersect.OnBothOperands);
                if (templ.Count != 0) IntSecPoints.Add(templ.First());
            }
            if (VerticalDirection) return IntSecPoints.OrderBy(i => i.Y).ToList();
            else return IntSecPoints.OrderBy(i => i.X).ToList();
        }

        public static DBObjectCollection ExplodeBlocks(this DBObjectCollection blocks)
        {
            var dbObjs = new DBObjectCollection();
            foreach( BlockReference block in blocks)
            {
                block.Explode(dbObjs);
            }
            return dbObjs;
        }

        public static void MarkBlock(this BlockReference block,double scaleValue = 1.1, string LayerName = "AI-提示")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, LayerName, 0);
                var rect = block.GetRect();
                rect.Scale(rect.GetCenter(), scaleValue);
                rect.Layer = LayerName;
                rect.ColorIndex = 10;
                rect.AddToCurrentSpace();
            }

        }
        public static void MarkPoint(this Point3d Pt, double Radius = 5000, string LayerName = "AI-提示")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, LayerName, 1);
                var circle = CreateCircle(Pt, Radius);
                circle.Layer = LayerName;
                circle.ColorIndex = 1;
                circle.AddToCurrentSpace();
            }
        }
        public static Circle CreateCircle(Point3d Center, double Radius)
        {
            var circle = new Circle();
            circle.Center = Center;
            circle.Radius = Radius;
            return circle;
        }
    }
}
