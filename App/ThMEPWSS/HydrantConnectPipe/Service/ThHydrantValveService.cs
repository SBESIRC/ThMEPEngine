using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantValveService
    {
        public List<BlockReference> GetHydrantValve(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var hydrantPipe = acadDb.ModelSpace.OfType<BlockReference>().Where(o => IsHydranLayer(o.Layer) && IsBlockReference(o.GetEffectiveName())).ToList();
                return hydrantPipe;
            }
        }

        private bool IsHydranLayer(string layer)
        {
            if (layer.ToUpper().Contains("W-FRPT-HYDT-EQPM"))
            {
                return true;
            }
            return false;
        }

        private bool IsBlockReference(string layer)
        {
            if(layer.Contains("蝶阀"))
            {
                return true;
            }
            return false;
        }
    }
}
