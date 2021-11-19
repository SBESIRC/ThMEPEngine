using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantPipeMarkService
    {
        public List<BlockReference> GetHydrantPipeMark(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var blks = acadDb.ModelSpace.OfType<BlockReference>().Where(o => IsHydranLayer(o.Layer) && IsBlockReference(o.GetEffectiveName())).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(blks.ToCollection());
                var filterObjs = spatialIndex.SelectCrossingPolygon(selectArea);
                var hydrantPipes = new List<BlockReference>();
                if (filterObjs.Count != 0)
                {
                    var blk = filterObjs[0] as BlockReference;
                    hydrantPipes.Add(blk);
                }
                return hydrantPipes;
            }
        }
        private bool IsHydranLayer(string layer)
        {
            if (layer.ToUpper().Contains("W-FRPT-HYDT-DIMS"))
            {
                return true;
            }
            return false;
        }

        private bool IsBlockReference(string layer)
        {
            if (layer.Contains("消火栓管线管径") || layer.Contains("消火栓管径150"))
            {
                return true;
            }
            return false;
        }
    }
}
