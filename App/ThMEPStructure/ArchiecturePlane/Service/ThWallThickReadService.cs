using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.ArchiecturePlane.Service
{
    internal class ThWallThickReadService
    {
        private double PointSearchRange { get; set; } = 5.0;
        private ThCADCoreNTSSpatialIndex WallSpatialIndex { get; set; }
        public ThWallThickReadService(DBObjectCollection wallLines)
        {
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(wallLines);
        }
        public void SetPointSearchRange(double pointSearchRange)
        {
            if (pointSearchRange > 0.0)
            {
                PointSearchRange = pointSearchRange;
            }
        }
        public double GetWallThick(Point3d sp, Point3d ep)
        {
            /*
             *      |                          |
             *                 
             *      |--------------------------|  (中心)  
             *                 
             *      |                          |
             */
            var thick = 0.0;
            var dir = sp.GetVectorTo(ep);
            var spEnvelop = ThDrawTool.CreateSquare(sp, PointSearchRange);
            var spLines = Query(spEnvelop).FilterVertical(dir);

            var epEnvelop = ThDrawTool.CreateSquare(ep, PointSearchRange);
            var epLines = Query(epEnvelop).FilterVertical(dir);

            if (spLines.Count > 0 && epLines.Count > 0)
            {
                foreach (Line first in spLines.OfType<Line>())
                {
                    foreach (Line second in epLines.OfType<Line>())
                    {
                        if (Math.Abs(first.Length - second.Length) <= 5.0)
                        {
                            if (Math.Max(first.Length, second.Length) > thick)
                            {
                                thick = Math.Max(first.Length, second.Length);
                            }
                            break;
                        }
                    }
                }
            }
            spEnvelop.Dispose();
            epEnvelop.Dispose();
            return thick;
        }
        private DBObjectCollection Query(Polyline outline)
        {
            return WallSpatialIndex.SelectCrossingPolygon(outline);
        }
    }
}
