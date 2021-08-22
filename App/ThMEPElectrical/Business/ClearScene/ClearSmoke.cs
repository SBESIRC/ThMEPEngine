using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Business.BlindAreaReminder;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business.ClearScene
{
    public class ClearSmoke
    {
        private List<Polyline> m_polylines;
        private List<PolygonInfo> m_polygonInfos = new List<PolygonInfo>();
        private ThMEPOriginTransformer m_originTransformer;

        public static void MakeClearSmoke(List<Polyline> polylines, ThMEPOriginTransformer originTransformer)
        {
            var clearSmoke = new ClearSmoke(polylines, originTransformer);
            clearSmoke.DoClear();
        }

        public ClearSmoke(List<Polyline> polylines, ThMEPOriginTransformer originTransformer)
        {
            m_polylines = polylines;
            m_originTransformer = originTransformer;
            foreach (var poly in m_polylines)
                m_polygonInfos.Add(new PolygonInfo(poly));
        }

        public void DoClear()
        {
            using (var db = AcadDatabase.Active())
            {
                var protectCircles = db.ModelSpace.OfType<Circle>().Where(p => p.Layer.Equals(ThMEPCommon.PROTECTAREA_LAYER_NAME)).ToList();
                if (protectCircles.Count > 0)
                {
                    foreach (var circle in protectCircles)
                    {
                        if (IsInvalid(circle.Center))
                        {
                            var entity = db.ModelSpace.Element(circle.Id, true);
                            entity.Erase();
                        }
                    }
                }

                var protectBlockRefs = db.ModelSpace.OfType<BlockReference>().Where(p => p.Name.Equals(ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME)).ToList();
                if (protectBlockRefs.Count > 0)
                {
                    foreach (var blockRef in protectBlockRefs)
                    {
                        if (IsInvalid(blockRef.Position))
                        {
                            var entity = db.ModelSpace.Element(blockRef.Id, true);
                            entity.Erase();
                        }
                    }
                }
            }

            ClearHatch.MakeClearHatch(m_polygonInfos,m_originTransformer);
        }

        private bool IsInvalid(Point3d point)
        {
            var copyPoint = new Point3d(point.X, point.Y, point.Z);
            if (null != m_originTransformer)
                copyPoint = m_originTransformer.Transform(copyPoint);
            foreach (var poly in m_polylines)
            {
                if (GeomUtils.PtInLoop(poly, copyPoint))
                    return true;
            }

            return false;
        }
    }
}
