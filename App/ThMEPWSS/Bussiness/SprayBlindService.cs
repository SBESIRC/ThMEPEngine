﻿using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.Uitl.ShadowIn2D;
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
            foreach (var spray in sprays)
            {
                //保护类型
                var sprayRadii = spray.Radii as Polyline;
                if (!(ThWSSUIService.Instance.Parameter.blindAreaType == BlindAreaType.Rectangle))
                {
                    sprayRadii = spray.ArcRadii.ToNTSPolygon().ToDbPolylines()[0];
                }
                
                var intersectPolys = holes.Where(x => sprayRadii.Intersects(x)).ToList();

                //计算真实的保护区域
                var area = sprayRadii.Intersection(new DBObjectCollection() { polyline })
                    .Cast<Polyline>()
                    .Where(y => y.Contains(spray.Position))
                    .FirstOrDefault();
                if (area != null)
                {
                    area = area.Difference(intersectPolys.ToCollection())
                    .Cast<Polyline>()
                    .Where(y => y.Contains(spray.Position))
                    .SelectMany(z => z.Buffer(1).Cast<Polyline>())
                    .FirstOrDefault();
                }
                if (area != null)
                {
                    var obstacle = holes.Where(x => area.Contains(x)).ToList();
                    protectAreas.AddRange(shadowService.CreateShadow(spray.Position, area, obstacle).SelectMany(x => x.Buffer(1).Cast<Polyline>()));
                }
            }
            ThCADCoreNTSService.Instance.ArcTessellationLength = 1000;
            var sprayArea = SprayLayoutDataUtils.Radii(protectAreas).Cast<Polyline>().ToList();
            CalHolesService calHolesService = new CalHolesService();
            var holeDic = calHolesService.CalHoles(sprayArea);
            var frames = holeDic.Keys.ToList();
            frames.AddRange(holes);

            //计算边界盲区
            List<Polyline> blindAreas = new List<Polyline>();
            blindAreas.AddRange(polyline.Difference(frames.ToCollection()).Cast<Polyline>().ToList());

            //计算洞口盲区
            var dBObjects = holes.ToCollection();
            foreach (var holeArea in holeDic.SelectMany(x => x.Value))
            {
                blindAreas.AddRange(holeArea.Difference(dBObjects).Cast<Polyline>().ToList());
            }
            //using (AcadDatabase ad = AcadDatabase.Active())
            //{
            //    foreach (var item in protectAreas)
            //    {
            //        item.ColorIndex = 4;
            //        ad.ModelSpace.Add(item);
            //    }
            //}
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
                var layerId = LayerTools.AddLayer(db.Database, ThWSSCommon.BlindArea_LayerName);
                db.Database.UnFrozenLayer(ThWSSCommon.BlindArea_LayerName);

                foreach (var area in blindArea.Where(x => x.Area > 1))
                {
                    area.Layer = ThWSSCommon.BlindArea_LayerName;
                    area.ColorIndex = 10;
                    area.ConstantWidth = 50;
                    db.ModelSpace.Add(area);

                    //// 外圈轮廓
                    //ObjectIdCollection objIdColl = new ObjectIdCollection();
                    //objIdColl.Add(area.Id);

                    //// 填充面积框线
                    //Hatch hatch = new Hatch();
                    //hatch.LayerId = layerId;
                    //db.ModelSpace.Add(hatch);
                    //hatch.ColorIndex = 10;
                    //hatch.SetHatchPattern(HatchPatternType.PreDefined, "Solid");
                    //hatch.Associative = true;
                    //hatch.AppendLoop(HatchLoopTypes.Default, objIdColl);
                    //// 重新生成Hatch纹理
                    //hatch.EvaluateHatch(true);

                    //ObjectIdList ids = new ObjectIdList(new ObjectId[2] { area.Id, hatch.Id });
                    //db.Database.CreateGroup(area.Id.ToString(), ids);
                }
            }
        }
    }
}
