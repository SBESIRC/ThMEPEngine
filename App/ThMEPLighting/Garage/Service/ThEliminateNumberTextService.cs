using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThEliminateNumberTextService
    {        
        /// <summary>
        /// 布灯的边界
        /// </summary>
        private ThRegionLightEdge LightRegion { get; set; }
        /// <summary>
        /// 文字距离中心线的距离
        /// </summary>
        private double Distance { get; set; }
        private ThEliminateNumberTextService(
            ThRegionLightEdge lightRegion,
             double distance)
        {
            LightRegion = lightRegion;
            Distance = distance;
        }
        public static void Eliminate(
            ThRegionLightEdge lightRegion,
            double distance)
        {
            var instance = new ThEliminateNumberTextService(lightRegion, distance);
            instance.Eliminate();
        }

        private void Eliminate()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(LightRegion.Texts.ToCollection())
                {
                    AllowDuplicate = true,
                };
                var texts = new DBObjectCollection();
                LightRegion.Edges.ForEach(o =>
                {
                    var rec = ThDrawTool.ToRectangle(o.StartPoint, o.EndPoint, Distance*2.0);
                    var sideTexts = spatialIndex.SelectCrossingPolygon(rec);
                    foreach (DBText dbText in sideTexts)
                    {
                        if (!texts.Contains(dbText))
                        {
                            texts.Add(dbText);
                        }
                    }
                });
                texts.Cast<DBText>().ForEach(o =>
                {
                    o.UpgradeOpen();
                    o.Erase();
                    o.DowngradeOpen();
                });
                LightRegion.Texts = LightRegion.Texts.Where(o => !o.IsErased).ToList();
            }
        }
    }
}
