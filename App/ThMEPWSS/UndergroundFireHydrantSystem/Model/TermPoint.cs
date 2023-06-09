﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
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
        public int Type { get; set; }//1 消火栓; 2 其他区域; 3 同时供消火栓与其他区域; 4 水泵接合器
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
            var textHeight = 350;
            double leftX;
            double rightX;
            double leftY;
            double rightY;
            if (TextLine.StartPoint.X < TextLine.EndPoint.X)
            {
                leftX = TextLine.StartPoint.X;
                rightX = TextLine.EndPoint.X;
                leftY = TextLine.StartPoint.Y;
                rightY = TextLine.EndPoint.Y;
            }
            else
            {
                leftX = TextLine.EndPoint.X;
                rightX = TextLine.StartPoint.X;
                leftY = TextLine.EndPoint.Y;
                rightY = TextLine.StartPoint.Y;
            }

            var pt1 = new Point3d(leftX, leftY + textHeight, 0);
            var pt2 = new Point3d(rightX, rightY, 0);
            string str = ExtractText(spatialIndex, pt1, pt2);
            PipeNumber = str;
            pt1 = new Point3d(leftX, leftY - 150, 0);
            pt2 = new Point3d(rightX, rightY - textHeight, 0);
            var str2 = ExtractText(spatialIndex, pt1, pt2);
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

        private string ExtractText(ThCADCoreNTSSpatialIndex spatialIndex, Point3d pt1, Point3d pt2)
        {
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//文字范围
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            var pipeNumber = "";
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    pipeNumber = br.TextString;
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
            var xRange = 250;
            var yRange = 250;
            var pt1 = new Point3d(PtEx._pt.X - xRange, PtEx._pt.Y + yRange, 0);
            var pt2 = new Point3d(PtEx._pt.X + xRange, PtEx._pt.Y - yRange, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//消火栓范围
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            //var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
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
