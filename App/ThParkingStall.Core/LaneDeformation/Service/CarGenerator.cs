using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;

namespace ThParkingStall.Core.LaneDeformation.Service
{
    public class CarGenerator
    {
        //输入
        public List<List<Polygon>> FreePolygonsList = new List<List<Polygon>>();
        public List<Polygon> VehicleLaneBoundary = new List<Polygon>();
        public List<List<LineSegment>> UpVehicleLaneList = new List<List<LineSegment>>();
        public List<LineSegment> DownVehicleLane = new List<LineSegment>();

        //输出
        public List<NewCarDataPass> newCarDataPasses = new List<NewCarDataPass>();
        public List<Polygon> CarParkingObbs = new List<Polygon>();
        public List<Polygon> Columns = new List<Polygon>();

        //临时变量


        public CarGenerator()
        {

            Pipeline();
        }

        public void Pipeline()
        {
            for (int i = 0; i < 2; i++)
            {
                SingleRecGenerator(i);
            }
        }


        public void SingleRecGenerator(int index)
        {
            List<Polygon> nowFreePolyList = FreePolygonsList[index];
            List<LineSegment> nowLineSegments = new List<LineSegment>();

            List<Polygon> blocks = GetBlock(nowLineSegments);

            List<Polygon> newFreePolyList = GetFreeAreaList(DownVehicleLane[index], FreePolygonsList[index]);

            List<LineSegment> lineList = GetLineList(newFreePolyList);

            List<Polygon> cars = new List<Polygon>();
            List<Polygon> columns = new List<Polygon>();
            Vector2D tmpVec = new Vector2D(0, 1);


            for (int i = 0; i < lineList.Count; i++) {

                List<Polygon> singleAreaCars = new List<Polygon>();
                List<Polygon> singleAreaColumns = new List<Polygon>();

                GlobalBusiness.GenerateCars(lineList[i], tmpVec, blocks,
                    ref singleAreaCars, ref singleAreaColumns);
            }



        }

        //预处理环节
        public List<Polygon> GetBlock(List<LineSegment> lineList)
        {
            List<Polygon> ret = new List<Polygon>();
            for (int i = 0; i < lineList.Count; i++)
            {
                int xMin = (int)lineList[i].MinX;
                int xMax = (int)lineList[i].MaxX;
                int yMin = (int)lineList[i].MinY;

                Polygon rec = PolygonUtils.CreatePolygonRec(xMin, xMax, yMin, yMin + 2000);
                ret.Add(rec);
            }
            return ret;
        }


        public List<Polygon> GetFreeAreaList(LineSegment downLine, List<Polygon> freeAreaList, double length = 5300)
        {
            List<Polygon> newFreeAreaList = new List<Polygon>();
            int xMin = (int)downLine.MinX;
            int xMax = (int)downLine.MaxX;
            int yMin = (int)downLine.MinY;

            Polygon rec = PolygonUtils.CreatePolygonRec(xMin, xMax, yMin - length, yMin);

            for (int i = 0; i < freeAreaList.Count; i++)
            {
                Polygon tmpFreePoly = freeAreaList[i];
                var result = OverlayNGRobust.Overlay(tmpFreePoly, rec, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
                if (result is Polygon a)
                {
                    newFreeAreaList.Add(a);
                }
                else if (result is GeometryCollection collection)
                {
                    foreach (var geo in collection.Geometries)
                    {
                        if (geo is Polygon pl)
                        {
                            newFreeAreaList.Add(pl);
                        }
                    }
                }
            }

            return newFreeAreaList;
        }


        public List<LineSegment> GetLineList(List<Polygon> polygons)
        {
            List<LineSegment> lineSegments = new List<LineSegment>();
            for (int i = 0; i < polygons.Count; i++)
            {
                Polygon boundary = polygons[i];
                List<Coordinate> points = VehicleLane.Boundary.Coordinates.ToList();

                for (int j = 0; j < points.Count; j++)
                {
                    Coordinate pt0 = points[j];
                    Coordinate pt1 = points[(j + 1) % points.Count];
                    Vector2D vecDir0 = new Vector2D(1, 0);
                    //Vector2D vecDir1 = new Vector2D(-1, 0);

                    Vector2D vec0 = new Vector2D(pt0, pt1).Normalize();

                    if (vec0.Dot(vecDir0) > 0.95)
                    {
                        LineSegment tmpLineSegment = new LineSegment(pt0, pt1);
                        lineSegments.Add(tmpLineSegment);
                    }
                }
            }
            return lineSegments;
        }

        //后处理环节
        public void AdjustCars(List<Polygon> cars) 
        {

        
        }
    }

    public class NewCarDataPass
    {
        public List<Polygon> NewCars;
        public List<LineSegment> CarUpLine;
        public List<double> CarUpLineOccupy;
        public List<Polygon> NewColumns;
        public NewCarDataPass() { }
    }
}
