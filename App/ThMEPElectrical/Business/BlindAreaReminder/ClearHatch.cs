using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business.BlindAreaReminder
{
    public class ClearHatch
    {
        private List<PolygonInfo> m_polygonInfos;
        private ThMEPOriginTransformer m_originTransformer;

        public static void MakeClearHatch(List<PolygonInfo> polygonInfos, ThMEPOriginTransformer originTransformer=null)
        {
            var clearHatch = new ClearHatch(polygonInfos, originTransformer);
            clearHatch.DoClearHatch();
        }

        public ClearHatch(List<PolygonInfo> polygonInfos, ThMEPOriginTransformer originTransformer)
        {
            m_polygonInfos = polygonInfos;
            m_originTransformer = originTransformer;
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

            var copyPoint = nearPos;
            if (null != m_originTransformer)
                copyPoint = m_originTransformer.Transform(copyPoint);
            foreach (PolygonInfo polygon in m_polygonInfos)
            {
                if (GeomUtils.PtInLoop(polygon.ExternalProfile, copyPoint))
                    return true;
            }

            return false;
        }
    }
}
