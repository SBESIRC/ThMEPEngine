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

namespace ThParkingStall.Core.LaneDeformation
{
    public class DataPreprocess2
    {
        public VehicleLaneData rawData;
        public DataPreprocess2() 
        {
            rawData = RawData.rawData;
        }
        public void Pipeline()
        {
            InitBlockNodes();
            GetNeighbors();
        }
        private List<FreeAreaRec> AddFreeAreaInsideParkingPlace(ParkingPlaceBlock parkingPlace)
        {
            List<Geometry> geometries = new List<Geometry>();
            foreach (var plot in parkingPlace.Cars)
            {
                geometries.Add(plot.ParkingPlaceObb);
            }
            GeometryCollection differenceObjs = new GeometryCollection(geometries.ToArray());
            var freeAreeResult = OverlayNGRobust.Overlay(parkingPlace.ParkingPlaceBlockObb, differenceObjs, NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);

            var polyList = new List<Polygon>();
            if (freeAreeResult is Polygon a)
            {
                List<Polygon> polygons = PolygonUtils.ClearBufferHelper(a, -0.1, 0.1);
                polyList.AddRange(polygons);
            }
            else if (freeAreeResult is GeometryCollection collection)
            {
                foreach (var geo in collection.Geometries)
                {
                    if (geo is Polygon pl)
                    {
                        List<Polygon> polygons = PolygonUtils.ClearBufferHelper(pl, -0.1, 0.1);
                        polyList.AddRange(polygons);
                    }
                }
            }

            var freeList = new List<FreeAreaRec>();
            foreach (var p in polyList)
            {
                LDOutput.DrawTmpOutPut0.OriginalFreeAreaList.Add(p);
                var minX = p.Coordinates[0].X;
                var maxX = p.Coordinates[0].X;
                var minY = p.Coordinates[0].Y;
                var maxY = p.Coordinates[0].Y;
                for (int i = 1; i < p.Coordinates.Count(); i++)
                {
                    Coordinate coord = p.Coordinates[i];
                    if (coord.X < minX)
                        minX = coord.X;
                    if (coord.Y < minY)
                        minY = coord.Y;
                    if (coord.X > maxX)
                        maxX = coord.X;
                    if (coord.Y > maxY)
                        maxY = coord.Y;
                }
                var area = new FreeAreaRec(
                    new Coordinate(minX, minY), new Coordinate(maxX, minY), 
                    new Coordinate(maxX, maxY), new Coordinate(minX, maxY)
                    );
                freeList.Add(area);
            }
            return freeList;
        }
        private void InitBlockNodes()
        {
            // Init lanes, plots and free areas inside parking places
            var freeList = new List<FreeAreaRec>();
            for (int i = 0; i < rawData.VehicleLanes.Count; i++)
            {
                var lane = rawData.VehicleLanes[i];
                var laneBlock = new LaneBlock(lane, Parameter.TestDirection);
                ProcessedData.BlockNodeList.Add(laneBlock);

                if (i == 70 || i == 89) continue;

                foreach (var parkingPlace in lane.ParkingPlaceBlockList)
                {
                    if (laneBlock.IsHorizontal)
                    {
                        foreach (var plot in parkingPlace.Cars)
                        {
                            ProcessedData.BlockNodeList.Add(new SpotBlock(plot, Parameter.TestDirection));
                        }
                        var freeAreas = AddFreeAreaInsideParkingPlace(parkingPlace);
                        freeList.AddRange(freeAreas);
                    }
                    else
                    {
                        ProcessedData.BlockNodeList.Add(new ParkBlock(parkingPlace, Parameter.TestDirection, laneBlock.isOblique()));
                    }
                }
            }
            ProcessedData.FreeBlockList.Add(freeList);
            // init free areas
            foreach (var area in ProcessedData.FreeBlockList)
            {
                foreach (var block in area)
                {
                    ProcessedData.BlockNodeList.Add(new FreeBlock(block, Parameter.TestDirection));
                }
            }
            // Draw
            List<Polygon> laneToDraw = new List<Polygon>();
            List<Polygon> spotToDraw = new List<Polygon>();
            List<Polygon> areaToDraw = new List<Polygon>();
            foreach (var block in ProcessedData.BlockNodeList)
            {
                if (block is LaneBlock && !block.IsAnchor)
                    laneToDraw.Add(block.Obb);
                if (block is SpotBlock || block is ParkBlock)
                    spotToDraw.Add(block.Obb);
            }
            foreach (var area in freeList)
            {
                areaToDraw.Add(area.Obb);
            }
            LDOutput.DrawTmpOutPut0.LaneNodes = laneToDraw;
            LDOutput.DrawTmpOutPut0.SpotNodes = spotToDraw;
            LDOutput.DrawTmpOutPut0.FreeAreaRecs2 = areaToDraw;
        }
        private void GetNeighbors()
        {
            // Draw
            LDOutput.DrawTmpOutPut0.NeighborRelations.Clear();

            var validNodeList = new List<BlockNode>();
            foreach (BlockNode node in ProcessedData.BlockNodeList)
            {
                if (!node.IsAnchor)
                    validNodeList.Add(node);
            }
            var polygonSpatialIndex = new BNSpatialIndex(validNodeList);
            foreach (BlockNode node in validNodeList)
            {
                var leftDown = node.LeftDownPoint;
                var rightUp = node.RightUpPoint;
                List<Coordinate> pointList = new List<Coordinate>();
                pointList.Add(leftDown);
                pointList.Add(new Coordinate(rightUp.X, leftDown.Y));
                pointList.Add(new Coordinate(rightUp.X, leftDown.Y - 5));
                pointList.Add(new Coordinate(leftDown.X, leftDown.Y - 5));
                pointList.Add(leftDown);
                var query = new Polygon(new LinearRing(pointList.ToArray()));
                var result = polygonSpatialIndex.SelectCrossingGeometry(query);
                foreach (BlockNode child in result)
                {
                    if (child.RightUpPoint.Y.CompareTo(rightUp.Y) <= 0 &&
                        child.RightUpPoint.Y.CompareTo(leftDown.Y - 5) >= 0 &&
                        child.LeftDownPoint.Y.CompareTo(leftDown.Y - 5) < 0 &&
                        child.LeftDownPoint.X.CompareTo(rightUp.X) < 0 &&
                        child.RightUpPoint.X.CompareTo(leftDown.X) > 0)
                    {
                        node.NeighborNodes[(int)PassDirection.FORWARD].Add(child);
                        child.NeighborNodes[(int)PassDirection.BACKWARD].Add(node);
                        if (node is LaneBlock fa && child is LaneBlock ch)
                        {
                            if (fa.IsHorizontal && ch.IsHorizontal)
                            {
                                fa.IsFtherLane[(int)PassDirection.FORWARD] = true;
                                fa.IsFtherLane[(int)PassDirection.BACKWARD] = false;
                                ch.IsFtherLane[(int)PassDirection.FORWARD] = false;
                                ch.IsFtherLane[(int)PassDirection.BACKWARD] = false;
                            }
                        }

                        // Draw
                        var point1 = (new LineSegment(node.LeftDownPoint, node.RightUpPoint)).MidPoint;
                        var point2 = (new LineSegment(child.LeftDownPoint, child.RightUpPoint)).MidPoint;
                        LDOutput.DrawTmpOutPut0.NeighborRelations.Add(new LineSegment(point1, point2));
                    }
                }
            }
            // 排序
            //foreach (BlockNode node in validNodeList)
            //{
            //    foreach (var neighbors in node.NeighborNodes)
            //    {
            //        neighbors.OrderBy(o => o.LeftDownPoint.X);
            //    }
            //}
        }
    }
}
