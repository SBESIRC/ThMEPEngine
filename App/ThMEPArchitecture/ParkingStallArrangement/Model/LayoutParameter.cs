using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LayoutParameter
    {
        public Polyline OuterBoundary { get; set; }//最外包围框
        public List<int> AreaNumber { get; set; }//区域索引，从0开始
        public List<Polyline> Obstacles { get; set; }//所有障碍物
        public List<Line> SegLines { get; set; }//所有分割线
        public List<Polyline> Areas { get; set; }//所有区域包围框
        public Dictionary<int, Polyline> AreaDic { get; set; }//区域包围框
        public Dictionary<int, List<Polyline>> ObstacleDic { get; set; }//区域内的障碍物
        public Dictionary<int, List<Line>> SegLineDic { get; set; }//区域边界分割线
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引

        public LayoutParameter(Polyline outerBoundary, List<Polyline> obstacles, List<Line> segLines)
        {
            OuterBoundary = outerBoundary;
            Obstacles = obstacles;
            SegLines = segLines;
            AreaNumber = new List<int>();
            Areas = new List<Polyline>();
            Areas.Add(outerBoundary);
            AreaDic = new Dictionary<int, Polyline>();
            ObstacleDic = new Dictionary<int, List<Polyline>>();
            SegLineDic = new Dictionary<int, List<Line>>();
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacles.ToCollection());
        }

        public void Set(List<Gene> genome)
        {
            var areas = new List<Polyline>();
            areas.Add(OuterBoundary);
            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                Split(gene, ref areas);
            }
            Areas.Clear();
            AreaDic.Clear();
            Areas.AddRange(areas);
            for (int i = 0; i < areas.Count; i++)
            {
                AreaNumber.Add(i);
                AreaDic.Add(i, areas[i]);
                ObstacleDic.Add(i, GetObstacles(areas[i]));
                SegLineDic.Add(i, GetSegLines(areas[i]));
            }
        }

        private void Split(Gene gene, ref List<Polyline> areas)
        {
            var line = gene.ToLine();//对于每一条线
            if(AreaSplit.IsCorrectSegLines(line, ref areas))
            {
                return;
            }
            else
            {
                ;
                AreaSplit.IsCorrectSegLines2(line, ref areas);
            }
        }

        private List<Polyline> GetObstacles(Polyline area)
        {
            var obstacles = new List<Polyline>();
            var dbObjs = ObstacleSpatialIndex.SelectCrossingPolygon(area);
            dbObjs.Cast<Entity>()
                .ForEach(e => obstacles.Add(e as Polyline));
            return obstacles;
        }

        private List<Line> GetSegLines(Polyline area)
        {
            var segLines = new List<Line>();
            var dbObjs = SegLineSpatialIndex.SelectCrossingPolygon(area);
            dbObjs.Cast<Entity>()
                .ForEach(e => segLines.Add(e as Line));
            return segLines;
        }
    }
}
