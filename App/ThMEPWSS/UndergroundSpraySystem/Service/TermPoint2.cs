﻿using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

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
            var adjs = sprayIn.LeadLineDic[StartLine];
            if (adjs.Count > 1)
            {
                return;
            }
            double minDist = 100;
            foreach (var l in sprayIn.LeadLines)
            {
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
            if(TextLine is null)
            {
                var line = new Line(PtEx._pt.OffsetX(-500), PtEx._pt.OffsetX(400));
                string str1 = ExtractText(spatialIndex, line.GetRect());
                PipeNumber = str1;
                ;
                if(PipeNumber is null)
                {
                    ;
                }
                return;
            }
            string str = ExtractText(spatialIndex, TextLine.GetRect());
            PipeNumber = str;
            var str2 = ExtractText(spatialIndex, TextLine.GetRect(false));
            PipeNumber2 = str2;
            if (PipeNumber2 is null)
            {
                return;
            }

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
        public void SetType()
        {
            if(PipeNumber.Contains("防火分区"))
            {
                Type = 1;
                return;
            }
            if(PipeNumber.Trim().StartsWith("ZP"))
            {
                Type = 2;
                return;
            }
            if(PipeNumber.Contains("水泵接合器"))
            {
                Type = 3;
                return;
            }
            Type = 4;
        }



    }
}
