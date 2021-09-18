using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThClearPreviouResultService
    {
        private ThRacewayParameter RacewayParameter { get; set; }
        public ThClearPreviouResultService()
        {
            RacewayParameter = new ThRacewayParameter();
        }
        public void Clear(Database db, List<Polyline> regions)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                if(regions.Count==0)
                {
                    return;
                }
                // 收集
                var lightBlks = CollectLightBlks(db);
                var marks = CollectMarks(db);
                var centers = CollectCableTrayCenterLine(db);
                var sides = CollectCableTraySideLine(db);

                // 合并
                var totalObjs = new DBObjectCollection();
                Merge(totalObjs, lightBlks);
                Merge(totalObjs, marks);
                Merge(totalObjs, centers);
                Merge(totalObjs, sides);

                // 移动到原点
                var transformer = new ThMEPOriginTransformer(regions.ToCollection());
                totalObjs.Cast<Entity>().ForEach(e =>
                {
                    e.UpgradeOpen();
                    transformer.Transform(e);
                    e.DowngradeOpen();
                });

                // 过滤
                var spatialIndex = new ThCADCoreNTSSpatialIndexEx(totalObjs);
                var filters = new DBObjectCollection();
                regions.ForEach(r =>
                {
                    var clone = r.Clone() as Polyline;
                    transformer.Transform(clone);
                    var objs = spatialIndex.SelectWindowPolygon(clone);
                    Merge(filters, objs);
                });

                //还原回去
                totalObjs.Cast<Entity>().ForEach(e =>
                {
                    e.UpgradeOpen();
                    transformer.Reset(e);
                    e.DowngradeOpen();
                });

                // 删除
                if (filters.Count > 0)
                {
                    OpenLayer(db);
                    filters.Cast<Entity>().ForEach(e =>
                    {
                        if (!e.IsErased)
                        {
                            e.UpgradeOpen();
                            e.Erase();
                            e.DowngradeOpen();
                        }
                    });
                }
            }
        }

        private void OpenLayer(Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                acadDb.Database.UnLockLayer(RacewayParameter.LaneLineBlockParameter.Layer);
                acadDb.Database.UnLockLayer(RacewayParameter.NumberTextParameter.Layer);
                acadDb.Database.UnLockLayer(RacewayParameter.CenterLineParameter.Layer);
                acadDb.Database.UnLockLayer(RacewayParameter.CenterLineParameter.Layer);
                acadDb.Database.UnLockLayer(RacewayParameter.SideLineParameter.Layer);
            }
        }

        /// <summary>
        /// 将第2个集合里的元素合并到第一个集合中
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        private void Merge(DBObjectCollection first, DBObjectCollection second)
        {
            second.Cast<Entity>().ForEach(e=>first.Add(e));
        }

        /// <summary>
        /// 获取图纸上所有布置的灯块
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        private DBObjectCollection CollectLightBlks(Database db)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                return acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull)
                    .Where(b => b.Layer.ToUpper() == RacewayParameter.LaneLineBlockParameter.Layer.ToUpper())
                    .ToCollection();
            } 
        }
        private DBObjectCollection CollectMarks(Database db)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                return acdb.ModelSpace
                           .OfType<DBText>()
                           .Where(b => b.Layer.ToUpper() == RacewayParameter.NumberTextParameter.Layer.ToUpper())
                           .ToCollection();
            }
        }
        private DBObjectCollection CollectCableTrayCenterLine(Database db)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                return acdb.ModelSpace
                           .OfType<Line>()
                           .Where(b => b.Layer.ToUpper() == RacewayParameter.CenterLineParameter.Layer.ToUpper())
                           .ToCollection();
            }
        }
        private DBObjectCollection CollectCableTraySideLine(Database db)
        {
            // 线槽端口线和侧边线图层一直
            using (var acdb = AcadDatabase.Use(db))
            {
                return acdb.ModelSpace
                           .OfType<Line>()
                           .Where(b => b.Layer.ToUpper() == RacewayParameter.SideLineParameter.Layer.ToUpper())
                           .ToCollection();
            }
        }
    }
}
