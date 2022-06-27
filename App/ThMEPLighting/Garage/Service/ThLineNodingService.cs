using NFox.Cad;
using ThCADCore.NTS;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThLineNodingService
    {
        public List<Line> DxLines { get; private set; } // 输出
        public List<Line> FdxLines { get; private set; } // 输出
        public List<Line> SingleRowLines { get; private set; } // 输出
        private double PointTolerance { get; set; }
        private ThCADCoreNTSSpatialIndex DxSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FdxSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex SingleRowSpatialIndex { get; set; }
        public ThLineNodingService(List<Line> dxLines, List<Line> fdxLines, List<Line> singleRowLines)
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
            SingleRowLines = new List<Line>();
            PointTolerance = ThGarageLightCommon.RepeatedPointDistance;
            DxSpatialIndex = new ThCADCoreNTSSpatialIndex(dxLines.ToCollection());
            FdxSpatialIndex = new ThCADCoreNTSSpatialIndex(fdxLines.ToCollection());
            SingleRowSpatialIndex = new ThCADCoreNTSSpatialIndex(singleRowLines.ToCollection());
        }

        public void Noding()
        {
            // 非灯线和灯线不能重叠
            var totalLines = new List<Line>();
            var results = ThGarageUtils.CleanNoding(DxSpatialIndex.SelectAll().OfType<Line>().ToList(),
                FdxSpatialIndex.SelectAll().OfType<Line>().ToList(), SingleRowSpatialIndex.SelectAll().OfType<Line>().ToList());
            results.ForEach(o =>
            {
                var midPt = o.StartPoint.GetMidPt(o.EndPoint);
                var outline = midPt.CreateSquare(PointTolerance);
                var dxs = DxSpatialIndex.SelectCrossingPolygon(outline);
                if (dxs.OfType<Line>().Where(e => IsCollinear(o, e)).Count() >= 1)
                {
                    DxLines.Add(o);
                }
                else
                {
                    dxs = FdxSpatialIndex.SelectCrossingPolygon(outline);
                    if (dxs.OfType<Line>().Where(e => IsCollinear(o, e)).Count() >= 1)
                    {
                        FdxLines.Add(o);
                    }
                    else
                    {
                        dxs = SingleRowSpatialIndex.SelectCrossingPolygon(outline);
                        if (dxs.OfType<Line>().Where(e => IsCollinear(o, e)).Count() >= 1)
                        {
                            SingleRowLines.Add(o);
                        }
                    }
                }
                outline.Dispose();
            });
        }

        public void SetPointTolerance(double pointTolerance)
        {
            if (pointTolerance > 0.0)
            {
                PointTolerance = pointTolerance;
            }
        }

        private bool IsCollinear(Line first, Line second)
        {
            return ThGeometryTool.IsCollinearEx(first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint);
        }
    }
}
