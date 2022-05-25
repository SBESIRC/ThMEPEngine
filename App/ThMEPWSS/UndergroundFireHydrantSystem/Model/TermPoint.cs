using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class TermPoint
    {
        public Point3dEx PtEx { get; set; }//端点
        public Line StartLine { get; set; }//标注起始线
        public Line TextLine { get; set; }//标注水平线
        public string PipeNumber { get; set; }//标注
        public string PipeNumber2 { get; set; }//标注
        public int Type { get; set; }//1 消火栓; 2 其他区域; 3 同时供消火栓与其他区域; 4 水泵接合器; 5 跨层点
        private double Tolerance { get; set; }//容差
        public TermPoint(Point3dEx ptEx)
        {
            PtEx = ptEx;
            Tolerance = 100;
        }

        public void SetLines(FireHydrantSystemIn fireHydrantSysIn, List<Line> labelLine)
        {
            var distDic = new Dictionary<Line, double>();//线的距离字典
            foreach(var l in labelLine)
            {
                if(l is null)
                {
                    continue;
                }
                
                var spt = new Point3dEx(l.StartPoint);
                var ept = new Point3dEx(l.EndPoint);
                
                if(PtEx._pt.DistanceTo(spt._pt) < Tolerance || PtEx._pt.DistanceTo(ept._pt) < Tolerance) 
                {
                    distDic.Add(l, Math.Min(PtEx._pt.DistanceTo(spt._pt), PtEx._pt.DistanceTo(ept._pt)));
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
            var adjs = fireHydrantSysIn.LeadLineDic[StartLine];
            if (adjs.Count > 1)
            {
                return;
            }
            double minDist = 100;
            foreach (var l in labelLine)
            {
                if (l is null)
                {
                    continue;
                }
                var spt = l.StartPoint;
                var ept = l.EndPoint;
                var spt1 = StartLine.StartPoint;
                var ept1 = StartLine.EndPoint;
                if(!l.Equals(StartLine))
                {
                    if (StartLine.GetLinesDist(l)< minDist)
                    {
                        TextLine = l;
                        minDist = StartLine.GetLinesDist(l);
                    }
                }
            }
        }

        public void SetPipeNumber(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            string str = ExtractText(spatialIndex);
            PipeNumber = str;
            var str2 = ExtractText(spatialIndex);
            PipeNumber2 = str2;
            if (PipeNumber2 is null)
            {
                return;
            }
            if(PipeNumber2.Contains("X") || PipeNumber2.Contains("-"))
            {
                PipeNumber2 = "";
            }
        }

        private string ExtractText(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            double offset = 200;
            var pt1 = TextLine.StartPoint.OffsetY(offset);
            var pt2 = TextLine.EndPoint.OffsetY(offset);
            var line = new Line(pt1,pt2);
            var midPt = General.GetMidPt(pt1,pt2);
            double tor = 1000;
            var DBObjs = spatialIndex.SelectFence(line);
            var pipeNumber = "";
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    var centerPt = General.GetMidPt(br);
                    var dist = centerPt.DistanceTo(midPt);
                    if(dist<tor)
                    {
                        tor = dist;
                        pipeNumber = br.TextString;
                    }
                }
                else
                {
                    var ad = (obj as Entity).AcadObject;
                    dynamic o = ad;
                    if ((o.ObjectName as string).Equals("TDbText"))
                    {
                        pipeNumber = o.Text;
                    }
                }
            }
            return pipeNumber;
        }
        public void SetType(bool verticalHasHydrant)
        {
            if(PipeNumber?.Contains("水泵接合器") == true)
            {
                Type = 4;
                return;
            }
            if(!verticalHasHydrant)
            {
                Type = 2;//只供给其他区域
            }
            else
            {
                if (PipeNumber.IsCurrentFloor())
                {
                    Type = 1;//只供给消火栓
                }
                else
                {
                    Type = 3;//同时供消火栓与其他区域
                }
            }
        }
    }
}
