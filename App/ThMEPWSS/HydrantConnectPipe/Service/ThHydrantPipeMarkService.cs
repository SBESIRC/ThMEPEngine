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
    public class ThHydrantPipeMarkService
    {
        public List<BlockReference> GetHydrantPipeMark(Point3dCollection selectArea)
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
