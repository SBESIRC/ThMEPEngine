using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;

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
                var spatialIndex = new ThCADCoreNTSSpatialIndexEx(LightRegion.Texts.ToCollection());
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
