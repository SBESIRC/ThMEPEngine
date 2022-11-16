using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    /// <summary>
    /// 门洞处看线过滤器
    /// </summary>
    internal class ThDoorHiddenKanxianFilter
    {
        private List<ThComponentInfo> _doors;
        private List<ThGeometry> _kanxians;
        private ThCADCoreNTSSpatialIndex _kanXianSpatialIndex;

        private const double DoorOpeningBufferLength = 5.0;

        public ThDoorHiddenKanxianFilter(List<ThComponentInfo> doors,List<ThGeometry> hiddenKanxians)
        {
            _doors = doors!=null?doors:new List<ThComponentInfo>();
            _kanxians = hiddenKanxians != null ? hiddenKanxians:new List<ThGeometry>();
            _kanXianSpatialIndex = new ThCADCoreNTSSpatialIndex(_kanxians.Select(o => o.Boundary).ToCollection());
        }

        public List<ThGeometry> Filter()
        {
            var collector = new DBObjectCollection();
            _doors.ForEach(o =>
            {
                var startPtV = o.Start;
                var endPtV = o.End;

                if(!startPtV.HasValue || !endPtV.HasValue)
                {
                    return;
                }
                var startPt = startPtV.Value;
                var endPt = endPtV.Value;
                if (startPt.DistanceTo(endPt) <= 10.0)
                {
                    return;
                }
                var holeWidth = o.HoleWidth.ToDouble();
                if(holeWidth<=10.0)
                {
                    return;
                }

                var direction = startPt.GetVectorTo(endPt).GetNormal();
                var newStartPt = startPt - direction.MultiplyBy(DoorOpeningBufferLength);
                var newEndPt = endPt + direction.MultiplyBy(DoorOpeningBufferLength);
                var searchArea = ThDrawTool.ToRectangle(newStartPt, newEndPt, holeWidth+2* DoorOpeningBufferLength);
                collector = collector.Union(SelectWindowPolygon(searchArea));
            });

            return _kanxians.Where(o => collector.Contains(o.Boundary)).ToList();
        }

        private DBObjectCollection SelectWindowPolygon(Polyline area)
        {
            return _kanXianSpatialIndex.SelectWindowPolygon(area);
        }
    }
}
