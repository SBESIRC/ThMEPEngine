using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class TermPoint
    {
        public Point3dEx PtEx { get; set; }//端点
        public Line StartLine { get; set; }//标注起始线
        public Line TextLine { get; set; }//标注水平线
        public string PipeNumber { get; set; }//标注

        public int Type { get; set; }//1 消火栓; 2 其他区域; 3 同时供消火栓与其他区域; 

        private double Tolerance { get; set; }//容差

        public TermPoint(Point3dEx ptEx)
        {
            PtEx = ptEx;
            Tolerance = 100;
        }

        public void SetLines(List<Line> labelLine)
        {
            
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
                    StartLine = l;
                }
            }
            if(StartLine is null)
            {
                return;
            }
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
                var tolerance = 100;
                if(!l.Equals(StartLine))
                {
                    if (spt.DistanceTo(spt1) < tolerance || spt.DistanceTo(ept1) < tolerance || ept.DistanceTo(spt1) < tolerance || ept.DistanceTo(ept1) < tolerance)
                    {
                        TextLine = l;
                    }
                }
            }
        }

        public void SetPipeNumber(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var leftX = 0.0;
            var rightX = 0.0;
            var leftY = 0.0;
            var rightY = 0.0;
            var textHeight = 500;
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
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//文字范围
            
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            foreach(var obj in DBObjs)
            {
                //var br = obj as DBText;
                //PipeNumber = br.TextString;
                if (obj is DBText)
                {
                    var br = obj as DBText;
                    PipeNumber = br.TextString;
                }
                else
                {

                    var ad = (obj as Entity).AcadObject;
                    dynamic o = ad;
                    if ((o.ObjectName as string).Equals("TDbText"))
                    {
                        PipeNumber = o.Text;
                    }
                }
            }
        }

        public void SetType(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var xRange = 1200;
            var yRange = 250;
            var pt1 = new Point3d(PtEx._pt.X - xRange, PtEx._pt.Y + yRange, 0);
            var pt2 = new Point3d(PtEx._pt.X + xRange, PtEx._pt.Y - yRange, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//消火栓范围
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            if(DBObjs.Count == 0)
            {
                Type = 2;//只供给其他区域
            }
            else
            {
                if(PipeNumber[0] == 'X' || PipeNumber[0] == 'B')
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
