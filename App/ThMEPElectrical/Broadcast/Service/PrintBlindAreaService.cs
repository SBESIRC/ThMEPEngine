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

namespace ThMEPElectrical.Broadcast.Service
{
    public class PrintBlindAreaService
    {
        public void PrintBlindArea(List<Point3d> layoutPts, Polyline roomPoly, double protectRange)
        {
            var blindAreas = CalProtectBlindArea(layoutPts, roomPoly, protectRange);
            InsertBlindArea(blindAreas);
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        public void InsertBlindArea(List<Polyline> blindArea)
        {
            using (var db = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(db.Database, ThMEPCommon.BlindAreaLayer);
                db.Database.UnLockLayer(ThMEPCommon.BlindAreaLayer);
                db.Database.UnFrozenLayer(ThMEPCommon.BlindAreaLayer);
                db.Database.UnPrintLayer(ThMEPCommon.BlindAreaLayer);

                foreach (var area in blindArea.Where(x => x.Area > 1))
                {
                    area.Layer = ThMEPCommon.BlindAreaLayer;
                    area.ColorIndex = 5;
                    area.ConstantWidth = 50;
                    db.ModelSpace.Add(area);
                }
            }
        }

        /// <summary>
        /// 计算保护盲区
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="roomPoly"></param>
        /// <returns></returns>
        public List<Polyline> CalProtectBlindArea(List<Point3d> layoutPts, Polyline roomPoly, double protectRange)
        {
            var objs = new DBObjectCollection();
            foreach (var pt in layoutPts)
            {
                var circle = new Circle(pt, Vector3d.ZAxis, protectRange);
                foreach (var poly in circle.ToNTSPolygon(20).ToDbPolylines())
                {
                    objs.Add(poly);
                }
            }

            var blindAreas = roomPoly.Difference(objs).Cast<Polyline>().ToList();
            return blindAreas;
        }
    }
}
