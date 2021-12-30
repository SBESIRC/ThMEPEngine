using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public static class ParameterConvert
    {
        public static bool ConvertParametersToCalculateCarSpots(LayoutParameter layoutPara, int j, ref PartitionV3 partition, Logger logger = null)
        {
            int index = layoutPara.AreaNumber[j];
            layoutPara.SegLineDic.TryGetValue(index, out List<Line> lanes);
            layoutPara.AreaDic.TryGetValue(index, out Polyline boundary);
            layoutPara.ObstaclesList.TryGetValue(index, out List<List<Polyline>> obstacleList);
            layoutPara.AreaWalls.TryGetValue(index, out List<Polyline> walls);
            layoutPara.AreaSegs.TryGetValue(index, out List<Line> inilanes);
            List<Polyline> buildingBoxes = new List<Polyline>();
            var bound = GeoUtilities.JoinCurves(walls, inilanes)[0];
            var obstacleListNew = new List<List<Polyline>>();
            for (int i = 0; i < obstacleList.Count; i++)
            {
                var pls = obstacleList[i];
                obstacleListNew.Add(new List<Polyline>());
                foreach (var pl in pls)
                {
                    obstacleListNew[obstacleListNew.Count - 1].AddRange((GeoUtilities.SplitCurve(pl, bound).Where(e => bound.IsPointIn(e.GetCenter())).Select(e =>
                    {
                        if (e is Polyline) return (Polyline)e;
                        else return GeoUtilities.PolyFromLine((Line)e);
                    })));
                }
            }
            foreach (var obs in obstacleListNew)
            {
                if (obs.Count == 0) continue;
                Extents3d ext = new Extents3d();
                foreach (var pl in obs) ext.AddExtents(pl.GeometricExtents);
                buildingBoxes.Add(ext.ToRectangle());
            }
            var obstacles = new List<Polyline>();
            obstacleList.ForEach(e => obstacles.AddRange(e));
            var Cutters = new DBObjectCollection();
            obstacles.ForEach(e => Cutters.Add(e));
            var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
            var CuttersM = new DBObjectCollection();
            obstacles.ForEach(e => CuttersM.Add(e.ToNTSPolygon().ToDbMPolygon()));
            var ObstaclesMpolygonSpatialIndex = new ThCADCoreNTSSpatialIndex(CuttersM);
            string w = "";
            string l = "";
            foreach (var e in walls)
            {
                foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                    w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            foreach (var e in inilanes)
            {
                l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                    + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
            }
#if DEBUG
            FileStream fs1 = new FileStream("D:\\GALog.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1);
            sw.WriteLine(w);
            sw.WriteLine(l);
            sw.Close();
            fs1.Close();
#endif
            inilanes = inilanes.Distinct().ToList();
            partition = new PartitionV3(walls, inilanes, obstacles, bound, buildingBoxes);
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
            partition.ObstaclesMPolygonSpatialIndex = ObstaclesMpolygonSpatialIndex;
            if (partition.Validate()) return true;
            else
            {
                logger?.Error("数据无效, wall: " + w + "lanes: " + l + "Boundary: " + GeoUtilities.AnalysisPoly(boundary));
                return false;
            }
        }
    }
}
