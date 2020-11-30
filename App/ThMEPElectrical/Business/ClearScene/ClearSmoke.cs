using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;

namespace ThMEPElectrical.Business.ClearScene
{
    public class ClearSmoke
    {
        private List<Polyline> m_polylines;

        public static void MakeClearSmoke(List<Polyline> polylines)
        {
            var clearSmoke = new ClearSmoke(polylines);
            clearSmoke.DoClear();
        }

        public ClearSmoke(List<Polyline> polylines)
        {
            m_polylines = polylines;
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
        }

        private bool IsInvalid(Point3d point)
        {
            foreach (var poly in m_polylines)
            {
                if (GeomUtils.PtInLoop(poly, point))
                    return true;
            }

            return false;
        }
    }
}
