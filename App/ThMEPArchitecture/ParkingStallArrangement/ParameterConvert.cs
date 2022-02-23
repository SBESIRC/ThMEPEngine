using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPArchitecture.ViewModel;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public static class ParameterConvert
    {
        public static void ConvertParametersToPartitionPro(LayoutParameter layoutPara, int j, ref ParkingPartitionPro partition, ParkingStallArrangementViewModel vm = null)
        {
            int index = layoutPara.AreaNumber[j];
            layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
            layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
            layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> buildingObstacleList);
            layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> orgBuildingBoxes);
            layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> outerWallLines);
            layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);

            var OuterBoundary = layoutPara.OuterBoundary;
            var ramps = layoutPara.RampList;
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
            //BUG:存在一个暂未解决的bug，使用SelectCrossingPolygon无法找出想要的障碍物
            //图纸：齐少华:toyu0215.dwg
            var obstacles = ObstaclesSpatialIndex.SelectCrossingPolygon(bound).Cast<Polyline>().ToList();
            var buildingBoxes = new List<Polyline>();
            foreach (var obs in buildingObstacleList)
            {
                Extents3d ext = new Extents3d();
                foreach (var o in obs)
                {
                    if (boundary.Contains(o) || boundary.Intersect(o, Intersect.OnBothOperands).Count > 0)
                    {
                        ext.AddExtents(o.GeometricExtents);
                    }
                }
                if (ext.ToExtents2d().GetArea() >= 2 * 10e7 && ext.ToExtents2d().GetArea() < 10e15)
                    buildingBoxes.Add(ext.ToRectangle());
            }
            partition = new ParkingPartitionPro(outerWallLines, inilanes, obstacles, bound, vm);
            partition.OutBoundary = OuterBoundary;
            partition.BuildingBoxes = buildingBoxes;
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
            partition.RampList = ramps.Where(e => bound.Contains(e.InsertPt)).ToList();
        }
    }
}
