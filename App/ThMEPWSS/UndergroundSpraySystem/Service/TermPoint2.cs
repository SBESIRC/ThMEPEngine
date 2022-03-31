using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using DotNetARX;
using Linq2Acad;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class TermPoint2
    {
        public Point3dEx PtEx { get; set; }//端点
        public Line StartLine { get; set; }//标注起始线
        public Line TextLine { get; set; }//标注水平线
        public string PipeNumber { get; set; }//标注
        public string PipeNumber2 { get; set; }//标注
        public int Type { get; set; }//1 防火分区; 2 立管; 3 水泵接合器; 4 其他
        public bool HasSignalValve { get; set; }//存在信号阀
        public bool HasFlow { get; set; }//存在水流指示器
        private double Tolerance { get; set; }//容差
        public TermPoint2(Point3dEx ptEx)
        {
            PtEx = ptEx;
            Tolerance = 100;
        }
        public void SetLines(SprayIn sprayIn)
        {
            var distDic = new Dictionary<Line, double>();//线的距离字典
            foreach(var l in sprayIn.LeadLines)
            {
                var spt = l.StartPoint;
                var ept = l.EndPoint;
                if(PtEx._pt.DistanceTo(spt) < Tolerance || PtEx._pt.DistanceTo(ept) < Tolerance) 
                {
                    distDic.Add(l, Math.Min(PtEx._pt.DistanceTo(spt), PtEx._pt.DistanceTo(ept)));
                }
            }
            if(distDic.Count > 0)
            {
                distDic.OrderBy(o => o.Value);
                StartLine = distDic.Keys.First();
            }
            if(StartLine is null)
            {
                return;
            }
            if(!sprayIn.LeadLineDic.ContainsKey(StartLine))
            {
                return;
            }
            var adjs = sprayIn.LeadLineDic[StartLine];
            TextLine = adjs[0] as Line;
            //double minDist = 100;
            //foreach (var l in adjs)
            //{
            //    var spt = l.StartPoint;
            //    var ept = l.EndPoint;
            //    var spt1 = StartLine.StartPoint;
            //    var ept1 = StartLine.EndPoint;
            //    if(!l.Equals(StartLine))
            //    {
            //        if (StartLine.GetLinesDist(l)< minDist)
            //        {
            //            TextLine = l;
            //            minDist = StartLine.GetLinesDist(l);
            //        }
            //    }
            //}
        }

        public void SetPipeNumber(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            if(TextLine is null)
            {
                var line = new Line(PtEx._pt.OffsetX(-500), PtEx._pt.OffsetX(400));
                string str1 = ExtractText(spatialIndex, line.GetRect());
                PipeNumber = str1;

                return;
            }
            string str = ExtractText(spatialIndex, CreateLineHalfBuffer(TextLine, 300));
            PipeNumber = str;
            var str2 = ExtractText(spatialIndex, CreateLineHalfBuffer(TextLine, 300));
            PipeNumber2 = str2;
            if (PipeNumber2 is null)
            {
                return;
            }

        }

        private static Polyline CreateLineHalfBuffer(Line line, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            var spt = line.StartPoint;
            var ept = line.EndPoint;
            pts.Add(spt.ToPoint2D()); // low left
            pts.Add(spt.OffsetY(tolerance).ToPoint2D()); // high left
            pts.Add(ept.OffsetY(tolerance).ToPoint2D()); // low right
            pts.Add(ept.ToPoint2D()); // high right
            pts.Add(spt.ToPoint2D()); // low left
            pl.CreatePolyline(pts);
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                currentDb.CurrentSpace.Add(pl);
            }
            return pl;
        }


        private string ExtractText(ThCADCoreNTSSpatialIndex spatialIndex, Tuple<Point3d, Point3d> tuplePoint)
        {
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            var pipeNumber = "";
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    pipeNumber = br.TextString;
                }
            }
            return pipeNumber;
        }

        private string ExtractText(ThCADCoreNTSSpatialIndex spatialIndex, Polyline selectArea)
        {
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            var pipeNumber = "";
            double dist = 1000;
            if(DBObjs.Count == 1)
            {
                pipeNumber = (DBObjs[0] as DBText).TextString;
            }
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    var curDist = Math.Min(br.Position.DistanceTo(selectArea.StartPoint), 
                                           br.Position.DistanceTo(selectArea.GetPoint3dAt(3)));
                    if (curDist < dist)
                    {
                        pipeNumber = br.TextString;
                        dist = curDist;
                    }
                }
            }
            return pipeNumber;
        }

        public void SetType(SprayIn sprayIn, bool acrossFloor = false)
        {
            if(PipeNumber.Contains("防火分区"))
            {
                Type = 1;
                return;
            }
            if(sprayIn.ThroughPt.Contains(PtEx))
            {
                Type = 2;
                return;
            }
            //if(PipeNumber.Trim().StartsWith("ZP") && !acrossFloor)
            //{
            //    Type = 2;
            //    return;
            //}
            if(PipeNumber.Contains("水泵接合器"))
            {
                Type = 3;
                return;
            }
            Type = 4;
        }
    }
}
