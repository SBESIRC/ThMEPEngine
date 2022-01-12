using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPArchitecture.ViewModel;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public static class ParameterConvert
    {
        public static bool ConvertParametersToCalculateCarSpots(LayoutParameter layoutPara, int j, ref ParkingPartition partition, ParkingStallArrangementViewModel vm = null, Logger logger = null)
        {
            int index = layoutPara.AreaNumber[j];
            layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
            layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
            layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> buildingObstacleList);
            layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> orgBuildingBoxes);
            layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> outerWallLines);
            layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);
            List<Polyline> buildingBoxes = new List<Polyline>();
            var bound = GeoUtilities.JoinCurves(outerWallLines, inilanes)[0];

            for (int i = 0; i < orgBuildingBoxes.Count; ++i)
            {
                if (!bound.Intersects(orgBuildingBoxes[i]))
                {
                    buildingBoxes.Add(orgBuildingBoxes[i]);
                }
                else
                {
                    var intersections = bound.GeometryIntersection(orgBuildingBoxes[i]);
                    Extents3d buildingExtent = new Extents3d();
                    foreach (Entity intersection in intersections)
                    {
                        buildingExtent.AddExtents(intersection.GeometricExtents);
                        intersection.Dispose();
                    }
                    buildingBoxes.Add(buildingExtent.ToRectangle());
                }
            }
            
            var ObstaclesSpatialIndex = layoutPara.AllShearwallsSpatialIndex;

            var ObstaclesMpolygonSpatialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;

#if DEBUG
            string w = "";
            string l = "";
            foreach (var e in outerWallLines)
            {
                foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                    w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            foreach (var e in inilanes)
            {
                l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                    + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
            }
            FileStream fs1 = new FileStream("D:\\GALog.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1);
            sw.WriteLine(w);
            sw.WriteLine(l);
            sw.Close();
            fs1.Close();
#endif
            inilanes = inilanes.Distinct().ToList();
            partition = new ParkingPartition(outerWallLines, inilanes, null, bound, buildingBoxes, vm);
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
            partition.ObstaclesMPolygonSpatialIndex = ObstaclesMpolygonSpatialIndex;
            partition.CheckObstacles();
            partition.CheckBuildingBoxes();
            if (partition.Validate())
            {
                //partition.Dispose();
                return true;
            }
            else
            {
                //partition.Dispose();
                return false;
            }
        }

        public static void DebugParkingPartitionO(LayoutParameter layoutPara, int j, ref ParkingPartitionBackup partition)
        {
            int index = layoutPara.AreaNumber[j];
            layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
            layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
            layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> buildingObstacleList);
            layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> orgBuildingBoxes);
            layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> outerWallLines);
            layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);
            var bound = GeoUtilities.JoinCurves(outerWallLines, inilanes)[0];
            var ObstaclesSpatialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;
#if DEBUG
            string w = "";
            string l = "";
            foreach (var e in outerWallLines)
            {
                foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                    w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            foreach (var e in inilanes)
            {
                l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                    + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
            }
            FileStream fs1 = new FileStream("D:\\GALog.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1);
            sw.WriteLine(w);
            sw.WriteLine(l);
            sw.Close();
            fs1.Close();
#endif
            inilanes = inilanes.Distinct().ToList();
            var obstacles = ObstaclesSpatialIndex.SelectCrossingPolygon(bound).Cast<Polyline>().ToList();
            partition = new ParkingPartitionBackup(outerWallLines, inilanes, obstacles, bound);
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
        }

    }
}
