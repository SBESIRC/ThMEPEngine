using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.Model;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDamperSearcher
    {
        public double Tolerance = 500.0;
        public List<ThBlockReferenceData> Results { get; set; }
        public ThBConvertDamperSearcher()
        {
            Results = new List<ThBlockReferenceData>();
        }

        public void Search(List<ThRawIfcDistributionElementData> damperBlocks, List<Curve> ductCenterLine, List<ThBConvertFanPoints> fanPoints)
        {
            var geometries = damperBlocks.Select(o => o.Geometry).ToCollection();
            var blockIndex = new ThCADCoreNTSSpatialIndex(geometries);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ductCenterLine.ToCollection());
            fanPoints.ForEach(o =>
            {
                var rectangle = o.Inlet.CreateSquare(Tolerance);
                var filterCurve = spatialIndex.SelectCrossingPolygon(rectangle)
                    .OfType<Curve>()
                    .Where(c => !o.OutletExist || c.DistanceTo(o.Outlet, false) > Tolerance / 10.0)
                    .OrderBy(c => c.DistanceTo(o.Inlet, false))
                    .FirstOrDefault();
                if (filterCurve != null)
                {
                    var isStartPoint = filterCurve.StartPoint.DistanceTo(o.Inlet) < filterCurve.StartPoint.DistanceTo(o.Inlet);
                    var startPoint = isStartPoint ? filterCurve.StartPoint : filterCurve.EndPoint;
                    var frame = new DBObjectCollection { filterCurve }
                        .Buffer(Tolerance / 10.0)
                        .OfType<Polyline>()
                        .OrderByDescending(p => p.Area)
                        .First();
                    var block = blockIndex.SelectCrossingPolygon(frame)
                        .OfType<Polyline>()
                        .OrderBy(p => p.DistanceTo(startPoint, false))
                        .FirstOrDefault();
                    if(block != null)
                    {
                        Results.Add(
                            damperBlocks.Where(rawBlock => rawBlock.Geometry.Equals(block)).First().Data as ThBlockReferenceData);
                    }
                    else
                    {
                        // 一根风管连接多根风管情况
                    }
                }
            });
        }

        private void SearchPath(Curve srcCurve, Curve thisCurve, bool isStartPoint)
        {

        }
    }
}
