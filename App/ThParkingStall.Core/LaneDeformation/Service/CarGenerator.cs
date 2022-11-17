﻿using NetTopologySuite.Geometries;
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



namespace ThParkingStall.Core.LaneDeformation
{
    public class CarGenerator
    {
        //输入
        public List<List<Polygon>> FreePolygonsList = new List<List<Polygon>>();
        public List<Polygon> VehicleLaneBoundary = new List<Polygon>();
        public List<List<LineSegment>> UpVehicleLaneList = new List<List<LineSegment>>();
        public List<LineSegment> DownVehicleLane = new List<LineSegment>();

        //输出
        public List<NewCarDataPass> NewCarDataPasses = new List<NewCarDataPass>();


        //临时变量
        public LineSegment NowDownLine = new LineSegment();
        public List<Polygon> NowFreeAreaList = new List<Polygon>(); 

        public List<Polygon> CarParkingObbs = new List<Polygon>();
        public List<Polygon> Columns = new List<Polygon>();


        public CarGenerator()
        {
            ProcessedData.NewCarDataPasses = new List<NewCarDataPass>();

            FreePolygonsList = ProcessedData.output1;
            UpVehicleLaneList = ProcessedData.output3;
            DownVehicleLane = ProcessedData.output2;
            //Pipeline();
        }

        public void Pipeline()
        {
            for (int i = 0; i < FreePolygonsList.Count; i++)
            {
                SingleRecGenerator(i);
            }
        }


        public void SingleRecGenerator(int index)
        {
            VariableInit();
            VariableSet(index);

            List<Polygon> nowFreePolyList = FreePolygonsList[index];
            List<LineSegment> nowLineSegments = UpVehicleLaneList[index];

            List<Polygon> blocks = GetBlock(nowLineSegments);

            List<Polygon> newFreePolyList = GetFreeAreaList(DownVehicleLane[index], FreePolygonsList[index]);

            List<LineSegment> lineList = GetLineList(newFreePolyList);

            List<Polygon> cars = new List<Polygon>();
            List<Polygon> columns = new List<Polygon>();
            Vector2D tmpVec = new Vector2D(0, 1);

            LDOutput.DrawTmpOutPut0.CarBlocks.AddRange(blocks);
            LDOutput.DrawTmpOutPut0.CarLine.AddRange(lineList);
            LDOutput.DrawTmpOutPut0.NewFreeArea.AddRange(newFreePolyList);

            for (int i = 0; i < lineList.Count; i++) {

                List<Polygon> singleAreaCars = new List<Polygon>();
                List<Polygon> singleAreaColumns = new List<Polygon>();

                LineSegment nowLine = new LineSegment(new Coordinate(lineList[i].MinX, lineList[i].MinY - 2750), new Coordinate(lineList[i].MaxX, lineList[i].MaxY - 2750));
                
                blocks = new List<Polygon>();

                GlobalBusiness.GenerateCars(nowLine, tmpVec, blocks,
                    ref singleAreaCars, ref singleAreaColumns);

                cars.AddRange(singleAreaCars);
                columns.AddRange(singleAreaColumns);

                if (cars.Count > 0) 
                {
                    int stop = 0;
                }
            }

            CarParkingObbs = cars;
            Columns = columns;
            AdjustCars();
            NewCarDataPass newCarDataPass = GetResult();
            NewCarDataPasses.Add(newCarDataPass);
        }

        //预处理环节
        public void VariableInit() 
        {
            CarParkingObbs = new List<Polygon>();
            Columns = new List<Polygon>();  
        }

        public void VariableSet(int index = 0)
        {
            CarParkingObbs = new List<Polygon>();
            Columns = new List<Polygon>();
        }


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
                if (polygons[i].Area < 5000000) continue;

                Polygon boundary = polygons[i];
                List<Coordinate> points = boundary.Coordinates.ToList();

                for (int j = 0; j < points.Count; j++)
                {
                    Coordinate pt0 = points[j];
                    Coordinate pt1 = points[(j + 1) % points.Count];
                    Vector2D vecDir0 = new Vector2D(1, 0);
                    Vector2D vecDir1 = new Vector2D(-1, 0);

                    Vector2D vec0 = new Vector2D(pt0, pt1).Normalize();

                    if (vec0.Dot(vecDir1) > 0.95)
                    {
                        LineSegment tmpLineSegment = new LineSegment(pt0, pt1);
                        lineSegments.Add(tmpLineSegment);
                    }
                }
            }
            return lineSegments;
        }


        public List<LineSegment> RearrangeLine(List<LineSegment> lineSegments) 
        {
            List<LineSegment> newLineList = new List<LineSegment>();

            double nowY = lineSegments[0].MaxX;

            for (int i = 0; i < lineSegments.Count; i++) 
            {
                LineSegment nowLine = lineSegments[i];
                if (nowLine.MaxX  < nowY && Math.Abs(nowY - nowLine.MaxY) < 2000) 
                {

                    LineSegment tmpLine = new LineSegment(new Coordinate(nowLine.MinX+10,nowY + 10),new Coordinate(nowLine.MaxX-10,nowY+10));
                    LineSegment tmpLine2 = new LineSegment(new Coordinate(nowLine.MinX, nowY), new Coordinate(nowLine.MaxX, nowY ));
                    //if (InPolygon(tmpLine,))
                }
            }

            return newLineList;
        }


        public int InPolygon(LineSegment a,List<Polygon> polygons) 
        {
            int flag = 0;
            return flag;
        }

        //后处理环节
        public void AdjustCars() 
        {
        
        }

        //保存结果
        public NewCarDataPass GetResult() 
        {
            LDOutput.DrawTmpOutPut0.Cars.AddRange(CarParkingObbs);

            NewCarDataPass result = new NewCarDataPass();
            result.NewCars = CarParkingObbs;
            result.NewColumns = Columns;
            List<LineSegment> lineSegments = new List<LineSegment>();
            List<double> beyondDisList = new List<double>(); 
            for (int i = 0; i < CarParkingObbs.Count; i++) 
            {
                Polygon nowBoundary = CarParkingObbs[i].Boundary as Polygon;
                List<double> xyList = GetBoundaryXY(nowBoundary);
                LineSegment upLine = new LineSegment(new Coordinate(xyList[0], xyList[3]), new Coordinate(xyList[1], xyList[3]));
                double beyondDis = upLine.MaxY  - NowDownLine.MinY;
                

                lineSegments.Add(upLine);
                beyondDisList.Add(beyondDis);
                
            }

            result.CarUpLineOccupy = beyondDisList;
            result.CarUpLine = lineSegments;
            return result;
        }


        //utils
        public List<double> GetBoundaryXY(Polygon polygon) 
        {
            List<double> result = new List<double>();
            List<Coordinate> points = polygon.Coordinates.ToList();
            double minX = 1000000000;
            double maxX = -1000000000;
            double minY = 1000000000;
            double maxY = -1000000000;
            for (int i = 0; i < points.Count; i++) 
            {
                if (points[i].X > maxX) maxX = points[i].X;
                if (points[i].Y > maxY) maxY = points[i].Y;
                if (points[i].X < minX) minX = points[i].X;
                if (points[i].Y < minY) minY = points[i].Y;
            }
            result.Add(minX);
            result.Add(maxX);
            result.Add(minY);
            result.Add(maxY);   

            return result;
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
