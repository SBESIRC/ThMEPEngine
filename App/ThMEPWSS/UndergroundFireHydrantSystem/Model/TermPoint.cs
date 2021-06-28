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
            Tolerance = 200;
        }

        public void SetLines(List<Line> labelLine)
        {
            foreach(var l in labelLine)
            {
                var spt = new Point3dEx(l.StartPoint);
                var ept = new Point3dEx(l.EndPoint);
                if (PtEx._pt.DistanceTo(spt._pt) < Tolerance || PtEx._pt.DistanceTo(ept._pt) < Tolerance)
                {
                    StartLine = l;
                }
            }
            foreach (var l in labelLine)
            {
                var spt = new Point3dEx(l.StartPoint);
                var ept = new Point3dEx(l.EndPoint);
                var spt1 = new Point3dEx(StartLine.StartPoint);
                var ept1 = new Point3dEx(StartLine.EndPoint);
                if(!l.Equals(StartLine))
                {
                    if (spt.Equals(spt1) || spt.Equals(ept1) || ept.Equals(spt1) || ept.Equals(ept1))
                    {
                        TextLine = l;
                    }
                }
            }
        }

        public void SetPipeNumber(List<DBText> Results)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
            var leftX = 0.0;
            var rightX = 0.0;
            var downY = TextLine.StartPoint.Y;
            var textHeight = 1000;
            if (TextLine.StartPoint.X < TextLine.EndPoint.X)
            {
                leftX = TextLine.StartPoint.X;
                rightX = TextLine.EndPoint.X;
            }
            else
            {
                leftX = TextLine.EndPoint.X;
                rightX = TextLine.StartPoint.X;
            }

            var pt1 = new Point3d(leftX, downY + textHeight, 0);
            var pt2 = new Point3d(rightX, downY, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//文字范围
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            foreach(var obj in DBObjs)
            {
                var br = obj as DBText;
                PipeNumber = br.TextString;
            }
        }

        public void SetType(List<BlockReference> Results)
        {
            var xRange = 1200;
            var yRange = 250;
            var pt1 = new Point3d(PtEx._pt.X - xRange, PtEx._pt.Y + yRange, 0);
            var pt2 = new Point3d(PtEx._pt.X + xRange, PtEx._pt.Y - yRange, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//消火栓范围
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            if(DBObjs.Count == 0)
            {
                Type = 2;//只供给其他区域
            }
            else
            {
                if(PipeNumber[0] == 'X')
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
