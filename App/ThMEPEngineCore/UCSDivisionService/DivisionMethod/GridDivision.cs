using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.UCSDivisionService.Utils;

namespace ThMEPEngineCore.UCSDivisionService.DivisionMethod
{
    public class GridDivision
    {
        public void Division(List<List<Curve>> girds, Polyline polyline, List<Polyline> walls)
        {
            //将所有轴网打成line
            var gridLines = girds.Select(x => GeoUtils.ConvertToLine(x, 500)).ToList();

            //获得所有轴网的网格区域
            var gridAreas = GetGridArea(gridLines.SelectMany(x => x).ToList());

            //获得每个轴网的凸包
            var gridHulls = gridLines.Select(x => GetGridHull(x)).ToList();
        }

        private void ClassifyGridArea(List<Polyline> areas, List<Polyline> gridHulls, List<Polyline> walls)
        {
            var areaCollction = areas.ToCollection();
            Dictionary<Polyline, DBObjectCollection> areaDic = new Dictionary<Polyline, DBObjectCollection>();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(areaCollction);
            gridHulls.ForEach(x =>
            {
                var containsAreas = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(x);
                areaDic.Add(x, containsAreas);
            });

            Dictionary<Polyline, DBObjectCollection> areaPoly = new Dictionary<Polyline, DBObjectCollection>();
            foreach (var area in areas)
            {
                var dicInfo = areaDic.Where(x => x.Value.Contains(area)).ToDictionary(x => x.Key, y => y.Value);
                if (dicInfo.Count > 1)
                {
                    DBObjectCollection dBObject = new DBObjectCollection();
                    foreach (var dic in dicInfo)
                    {
                        areaDic[dic.Key].Remove(area);
                        dBObject.Add(dic.Key);
                    }
                    areaPoly.Add(area, dBObject);
                }
            }
        }

        private void CheckAreaBelong(Polyline arae, DBObjectCollection gridArea, List<Polyline> walls)
        {

        }

        //private void Get

        private List<Polyline> GetGridArea(List<Line> gridLines)
        {
            return gridLines.ToCollection().PolygonsEx().Cast<Polyline>().ToList();
        }

        private Polyline GetGridHull(List<Line> gridLines)
        {
            var gridPts = StructUtils.GetCurvePoints(gridLines.Cast<Curve>().ToList());
            return ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(gridPts).ToDbCollection().Cast<Polyline>().OrderBy(x => x.Area).First();
        }
    }
}
