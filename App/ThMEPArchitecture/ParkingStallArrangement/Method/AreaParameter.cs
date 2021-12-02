using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public class AreaParameter
    {
        public Polyline OuterBoundary { get; set; }//外包框
        public Dictionary<int, Polyline> RegionBoundDic { get; set; }//区域边框
        public Dictionary<int, List<Polyline>> BuildingBoundDic { get; set; }//建筑物边框
        public List<Line> SegLines { get; set; } //分割线

        public AreaParameter()
        {
            OuterBoundary = new Polyline();
            RegionBoundDic = new Dictionary<int, Polyline>();
            BuildingBoundDic = new Dictionary<int, List<Polyline>>();
            SegLines = new List<Line>();
        }
        public AreaParameter(Polyline outerBoundary, List<Polyline> areas, List<Polyline> buildings, List<Line> segLines)
        {
            OuterBoundary = outerBoundary;
            SegLines = segLines;

            var buildSpatialIndex = new ThCADCoreNTSSpatialIndex(buildings.ToCollection());
            for (int i = 0; i < areas.Count; i++)
            {
                RegionBoundDic.Add(i + 1, areas[i]);
                var rstBuild = buildSpatialIndex.SelectCrossingPolygon(areas[i]);
                var plines = new List<Polyline>();
                if (rstBuild != null)
                {
                    foreach (var rst in rstBuild)
                    {
                        plines.Add(rst as Polyline);
                    }
                }
                BuildingBoundDic.Add(i + 1, plines);
            }
        }

        public void Set()
        {
           
        }
    }
}
