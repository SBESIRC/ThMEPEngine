using System;
using NFox.Cad;
using Linq2Acad;
using System.Data;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThMEPWSS.Service;
using ThMEPWSS.Uitl.ShadowIn2D;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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
        public List<Polyline> GetBlindArea(List<SprayLayoutData> sprays, Polyline polyline, List<Polyline> holes)
        {
            //计算喷淋实际保护区域
            List<Polyline> protectArea = sprays.SelectMany(x =>
            {
                var objs = new DBObjectCollection();
                objs.Add(x.Radii);
                var interPoly = polyline.Intersection(objs).Cast<Polyline>().ToList();
                foreach (var hole in holes)
                {
                    interPoly = interPoly.SelectMany(m => m.Difference(new DBObjectCollection() { hole }).Cast<Polyline>()).ToList();
                }
                return interPoly.Where(y => y.Contains(x.Position)).SelectMany(z => z.Buffer(1).Cast<Polyline>());
            }).ToList();

            var sprayArea = SprayLayoutDataUtils.Radii(protectArea).Cast<Polyline>().OrderByDescending(x => x.Area).ToList();
            Polyline frame = sprayArea.First();
            DBObjectCollection dBObjects = new DBObjectCollection();
            dBObjects.Add(frame);
            //计算边界盲区
            List<Polyline> blindAreas = new List<Polyline>();
            blindAreas.AddRange(polyline.Difference(dBObjects).Cast<Polyline>().ToList());

            //计算洞口盲区
            sprayArea.Remove(frame);
            dBObjects.Clear();
            holes.ForEach(x => dBObjects.Add(x));
            foreach (var holeArea in sprayArea)
            {
                blindAreas.AddRange(holeArea.Difference(dBObjects).Cast<Polyline>().ToList());
            }

            return blindAreas;
        }

        /// <summary>
        /// 获取盲区
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Polyline> GetRealBlindArea(List<SprayLayoutData> sprays, Polyline polyline, List<Polyline> holes)
        {
            //计算喷淋实际保护区域
            ShadowService shadowService = new ShadowService();
            List<Polyline> protectAreas = new List<Polyline>();
            ThCADCoreNTSService.Instance.ArcTessellationLength = (int)ThWSSUIService.Instance.Parameter.blindAreaType;
            holes = holes.ToCollection().UnionPolygons().OfType<Polyline>().ToList();
            foreach (var spray in sprays)
            {
                //保护类型
                var sprayRadii = spray.Radii as Polyline;
                //sprayRadii = spray.ArcRadii.TessellateCircleWithArc(ThCADCoreNTSService.Instance.ArcTessellationLength);
                var intersectPolys = holes.Where(x => sprayRadii.Intersects(x)).ToList();

                //计算真实的保护区域
                var area = sprayRadii.Intersection(new DBObjectCollection() { polyline })
                    .OfType<Polyline>()
                    .Where(y => y.Contains(spray.Position))
                    .FirstOrDefault();
                if (area != null)
                {
                    area = area.Difference(intersectPolys.ToCollection())
                    .OfType<Polyline>()
                    .Where(y => y.Contains(spray.Position))
                    .SelectMany(z => z.Buffer(1).OfType<Polyline>())
                    .FirstOrDefault();
                }
                if (area != null)
                {
                    var obstacle = holes.Where(x => area.Contains(x)).ToList();
                    protectAreas.AddRange(shadowService.CreateShadow(spray.Position, area, obstacle).SelectMany(x => x.Buffer(1).Cast<Polyline>()));
                }
            }
            ThCADCoreNTSService.Instance.ArcTessellationLength = 1000;
            var sprayArea = SprayLayoutDataUtils.Radii(protectAreas).OfType<Polyline>().ToList();
            CalHolesService calHolesService = new CalHolesService();
            var holeDic = calHolesService.CalHoles(sprayArea);
            var frames = holeDic.Keys.ToList();
            frames.AddRange(holes);

            //计算边界盲区
            List<Polyline> blindAreas = new List<Polyline>();
            blindAreas.AddRange(polyline.Difference(frames.ToCollection()).OfType<Polyline>().ToList());

            //计算洞口盲区
            var dBObjects = holes.ToCollection();
            foreach (var holeArea in holeDic.SelectMany(x => x.Value))
            {
                blindAreas.AddRange(holeArea.Difference(dBObjects).OfType<Polyline>().ToList());
            }

            return blindAreas;
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        public void InsertBlindArea(List<Polyline> blindArea)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.CreateAILayer(ThWSSCommon.Blind_Zone_LayerName, 1);
                foreach (var area in blindArea.Where(x => x.Area > 1))
                {
                    area.ConstantWidth = 50;
                    area.ColorIndex = (int)ColorIndex.BYLAYER;
                    area.Layer = ThWSSCommon.Blind_Zone_LayerName;
                    db.ModelSpace.Add(area);
                }
            }
        }
    }
}
