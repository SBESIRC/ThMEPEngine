using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.BlindAreaReminder
{
    public class ClearHatch
    {
        private List<PolygonInfo> m_polygonInfos;

        public static void MakeClearHatch(List<PolygonInfo> polygonInfos)
        {
            var clearHatch = new ClearHatch(polygonInfos);
            clearHatch.DoClearHatch();
        }

        public ClearHatch(List<PolygonInfo> polygonInfos)
        {
            m_polygonInfos = polygonInfos;
        }

        public void DoClearHatch()
        {
            using (var db = AcadDatabase.Active())
            {
                var hatches = db.ModelSpace.OfType<Hatch>().Where(p => p.Layer.Equals(ThMEPCommon.BLINDAREA_HATCH_LAYER_NAME)).ToList();
                if (hatches.Count > 0)
                {
                    foreach (var hatch in hatches)
                    {
                        if (IsInvalidHatch(hatch))
                        {
                            var hatchEntity = db.ModelSpace.Element(hatch.Id, true);
                            hatchEntity.Erase();
                        }
                    }
                }
            }
        }

        private bool IsInvalidHatch(Hatch hatch)
        {
            if (!hatch.Bounds.HasValue)
                return false;

            var extents = hatch.Bounds.Value;
            var nearPos = extents.CenterPoint();

            foreach (PolygonInfo polygon in m_polygonInfos)
            {
                if (GeomUtils.PtInLoop(polygon.ExternalProfile, nearPos))
                    return true;
            }

            return false;
        }
    }
}
