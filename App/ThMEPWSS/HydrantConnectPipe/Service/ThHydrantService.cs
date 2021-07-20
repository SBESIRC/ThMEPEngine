using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.HydrantConnectPipe.Engine;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantService
    {
        public List<ThHydrant> GetFireHydrant(Point3dCollection selectArea)
        {
            List<ThHydrant> fireHydrants = new List<ThHydrant>();
            using (var database = AcadDatabase.Active())
            using (var fireHydrantEngine = new ThHydrantRecognitionEngine())
            {
                fireHydrantEngine.Recognize(database.Database, selectArea);//从块里取数据
                fireHydrantEngine.RecognizeMS(database.Database, selectArea);//从本图上取数据
                foreach (var element in fireHydrantEngine.Datas)
                {
                    ThHydrant fireHydrant = ThHydrant.Create(element);
                    fireHydrants.Add(fireHydrant);
                }
            }
            return fireHydrants;
        }
    }
}
