using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Catel.Collections;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPHVAC.FanLayout.ViewModel;

namespace ThMEPHVAC.FanLayout.Engine
{
    public abstract class ThFanRecognitionEngine
    {
        protected string FanLayer { set; get; }
        protected string EffectiveName { set; get; }
        public abstract List<ThFanConfigInfo> GetFanConfigInfo(Point3dCollection area);
        public List<BlockReference> GetFanBlockReference(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var fan = acadDb.ModelSpace.OfType<BlockReference>().Where(o => IsFanLayer(o.Layer) && IsBlockReference(o.GetEffectiveName())).ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(fan.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(selectArea);
                var rst = dbObjects.Cast<BlockReference>().ToList();
                return rst;
            }
        }

        private bool IsFanLayer(string layer)
        {
            if (layer.ToUpper().Contains(FanLayer))
            {
                return true;
            }
            return false;
        }

        private bool IsBlockReference(string layer)
        {
            if (layer.Contains(EffectiveName))
            {
                return true;
            }
            return false;
        }
    }
}
