using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThWSS;

namespace ThMEPWSS.Bussiness
{
    public class SprayBlindService
    {
        /// <summary>
        /// 获取盲区
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Polyline> GetBlindArea(List<SprayLayoutData> sprays, Polyline polyline)
        {
            var sprayArea = SprayLayoutDataUtils.Radii(sprays);
            return polyline.Difference(sprayArea).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        public void InsertBlindArea(List<Polyline> blindArea)
        {
            using (var db = AcadDatabase.Active())
            {
                LayerTools.AddLayer(db.Database, ThWSSCommon.BlindArea_LayerName);
                foreach (var area in blindArea)
                {
                    area.Layer = ThWSSCommon.BlindArea_LayerName;
                    area.ColorIndex = 211;
                    db.ModelSpace.Add(area);
                }
            }
        }
    }
}
