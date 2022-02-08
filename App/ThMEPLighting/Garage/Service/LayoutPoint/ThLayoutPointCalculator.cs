using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    internal class ThLayoutPointCalculator
    {
        private double Margin { get; set; }
        private double Interval { get; set; }
        /// <summary>
        /// 是连续的一段，
        /// 线是有顺序的，线之间是相连的
        /// </summary>
        private List<Line> DxLines { get; set; }
        private List<Line> UnLayoutLines { get; set; }
        public List<Point3d> Results { get; set; }
        private double Step = 5.0;
        private int MaxGeneration = 150;

        public ThLayoutPointCalculator(List<Line> dxLines,List<Line> unLayoutLines,double interval,double margin)
        {
            DxLines = dxLines;
            UnLayoutLines = unLayoutLines;
            Interval = interval;
            Margin= margin;
            Results = new List<Point3d>();
        }
        public void Layout()
        {
            var pts = Distribute(DxLines, Margin, Interval);
            if(IsValid(pts))
            {
                Results = pts;
                return;
            }
            int i = 1;
            while (true)
            {
                var pts1 = Distribute(DxLines, Margin, Interval - i * Step);
                if (IsValid(pts1))
                {
                    Results = pts1;
                    break;
                }
                var pts2 = Distribute(DxLines, Margin, Interval + i * Step);
                if (IsValid(pts2))
                {
                    Results = pts2;
                    break;
                }
                if (++i > MaxGeneration)
                {
                    break;
                }
            }
        }

        private bool IsValid(List<Point3d> pts)
        {
            // 布置的点在不可布区域就是不合理的
            var dbPoints = ToDBPoints(pts); // 把点转成DBPoints
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var isIn = IsIn(spatialIndex); // 判断点是否在不可布区域内
            dbPoints.ThDispose(); // 释放资源
            return isIn ? false : true;
        }

        private bool IsIn(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return UnLayoutLines.Where(o =>
            {
                var outline = ThDrawTool.ToOutline(o.StartPoint, o.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
                var objs = spatialIndex.SelectCrossingPolygon(outline);
                outline.Dispose();
                return objs.Count > 0;
            }).Any();
        }

        private DBObjectCollection ToDBPoints(List<Point3d> pts)
        {
            return pts.Select(p => new DBPoint(p)).ToCollection();
        }

        private List<Point3d> Distribute(List<Line> lines, double margin, double interval)
        {
            if(interval>=0)
            {
                var polyline = lines.ToPolyline();
                var pts = GetPoints(polyline);
                var lineParameter = new ThLineSplitParameter
                {
                    Margin = margin,
                    Interval = interval,
                    Segment = pts,
                };
                return lineParameter.Distribute();
            }
            else
            {
                return new List<Point3d>();
            }
        }
        private List<Point3d> GetPoints(Polyline pLine)
        {
            var results = new List<Point3d>();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                results.Add(pLine.GetPoint3dAt(i));
            }
            return results;
        }
    }
}
