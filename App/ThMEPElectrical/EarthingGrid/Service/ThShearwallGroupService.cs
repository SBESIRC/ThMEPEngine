using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;

namespace ThMEPElectrical.EarthingGrid.Service
{
    internal class ThShearwallGroupService
    {
        private DBObjectCollection Shearwalls { get; set; }
        private DBObjectCollection MainBuildings { get; set; }
        private ThMEPOriginTransformer Transformer { get; set; }
        private double BufferLength { get; set; }
        private Dictionary<Polyline, DBObjectCollection> MainBuildingGroups { get; set; }
        public ThShearwallGroupService(
            DBObjectCollection shearwalls, 
            DBObjectCollection mainBuildings,
            double bufferLength =500.0)
        {
            Shearwalls = shearwalls;
            MainBuildings = mainBuildings;
            BufferLength = bufferLength;
            Transformer = new ThMEPOriginTransformer(shearwalls);
            MainBuildingGroups = new Dictionary<Polyline, DBObjectCollection>();
        }
        public Dictionary<Polyline, List<Polyline>> Group()
        {
            var results = new Dictionary<Polyline, List<Polyline>>();
            // 移到近原点处
            Transformer.Transform(Shearwalls);
            Transformer.Transform(MainBuildings);

            // 分组
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Shearwalls);
            MainBuildings.OfType<Polyline>().ForEach(p =>
            {
                var buffer = Buffer(p, BufferLength);
                var outline = buffer != null ? buffer : p;
                var objs = SelectWindowPolygon(outline, spatialIndex);
                MainBuildingGroups.Add(p, objs);
            });

            // 剩余未分组的墙
            var restShearwalls = Filter(Shearwalls);

            // 还原到原位置
            Transformer.Reset(Shearwalls);
            Transformer.Reset(MainBuildings);

            // 生成结果
            MainBuildingGroups.ForEach(g =>
            {
                var shearOutlines = new List<Polyline>();
                g.Value.OfType<DBObject>().ForEach(o =>
                  {
                      if(o is Polyline polyline)
                      {
                          shearOutlines.Add(polyline);
                      }
                      else if(o is MPolygon mPolygon)
                      {
                          shearOutlines.Add(mPolygon.Shell());
                      }
                  });
                results.Add(g.Key, shearOutlines);
            });

            restShearwalls.OfType<DBObject>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    results.Add(polyline,new List<Polyline> { polyline });
                }
                else if (o is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell();
                    results.Add(shell, new List<Polyline> { shell });
                }
            });
            return results;
        }

        private DBObjectCollection Filter(DBObjectCollection shearwalls)
        {
            return shearwalls.OfType<DBObject>().Where(o => !IsExisted(o)).ToCollection();
        }

        private bool IsExisted(DBObject shearwall)
        {
            return MainBuildingGroups.Where(o => o.Value.Contains(shearwall)).Any();
        }

        private Polyline Buffer(Polyline mainBuilding,double length)
        {
            var res = mainBuilding.Buffer(length).OfType<Polyline>().OrderByDescending(p => p.Area);
            return res.Count() > 0 ? res.First() : null;
        }
        private DBObjectCollection SelectWindowPolygon(
            Polyline outline,
            ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return spatialIndex.SelectWindowPolygon(outline);
        }
    }
}
