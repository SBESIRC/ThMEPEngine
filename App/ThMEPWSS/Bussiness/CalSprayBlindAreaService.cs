using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.Utils;
using ThWSS;

namespace ThMEPWSS.Bussiness
{
    public class CalSprayBlindAreaService
    {
        readonly double length = 3400;
        public void CalSprayBlindArea(List<BlockReference> sprays, Polyline polyline)
        {
            Vector3d vDir = Vector3d.XAxis;
            Vector3d tDir = Vector3d.YAxis;
            var sprayPts = sprays.Select(x => x.Position).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(sprayPts, vDir, tDir, length);
            var blindArea = GetBlindArea(sprayData, polyline);

            //打印盲区
            InsertBlindArea(blindArea);
        }

        /// <summary>
        /// 获取盲区
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> GetBlindArea(List<SprayLayoutData> sprays, Polyline polyline)
        {
            var sprayArea = SprayLayoutDataUtils.Radii(sprays);
            return polyline.Difference(sprayArea).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        private void InsertBlindArea(List<Polyline> blindArea)
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

