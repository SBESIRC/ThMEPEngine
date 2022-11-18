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

namespace ThParkingStall.Core.LaneDeformation
{
    public class DataPreprocess2
    {
        public VehicleLaneData rawData;
        private MNTSSpatialIndex obstableSpatialIndex;
        private double CatchRange = 200;
        public DataPreprocess2() 
        {
            rawData = RawData.rawData;
            obstableSpatialIndex = new MNTSSpatialIndex(VehicleLane.Blocks);
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
            var freeAreaResult = OverlayNGRobust.Overlay(parkingPlace.ParkingPlaceBlockObb, differenceObjs, NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);

            var polyListOrigin = new List<Polygon>();
            foreach (Polygon pl in PolygonUtils.GetPolygonsFromGeometry(freeAreaResult))
            {
                List<Polygon> polygons = PolygonUtils.ClearBufferHelper(pl, -1, 1);
                polyListOrigin.AddRange(polygons);
            }

            // Diff掉障碍物
            var obstables = obstableSpatialIndex.SelectCrossingGeometry(parkingPlace.ParkingPlaceBlockObb);
            var polyList = new List<Polygon>();
            foreach (var p in polyListOrigin)
            {
                double minX = 0, maxX = 0, minY = 0, maxY = 0;
                PolygonUtils.GetBoundaryBoxCoords(p, ref minX, ref minY, ref maxX, ref maxY);
                Geometry tmpG = PolygonUtils.CreatePolygonRec(minX, maxX, minY, maxY);

                foreach (var ob in obstables.Where(ob => tmpG.Intersects(ob)))
                {
                    double minXO = 0, maxXO = 0, minYO = 0, maxYO = 0;
                    PolygonUtils.GetBoundaryBoxCoords(ob, ref minXO, ref minYO, ref maxXO, ref maxYO);
                    Polygon diff = PolygonUtils.CreatePolygonRec(minX - 100, maxX + 100, minYO, maxYO);
                    tmpG = OverlayNGRobust.Overlay(tmpG, diff, NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);

                }
                foreach (Polygon pl in PolygonUtils.GetPolygonsFromGeometry(tmpG))
                {
                    polyList.Add(pl);
                }
            }

            // 转为FreeAreaRec
            var freeList = new List<FreeAreaRec>();
            foreach (var p in polyList)
            {
                double minX = 0, maxX = 0, minY = 0, maxY = 0;
                PolygonUtils.GetBoundaryBoxCoords(p, ref minX, ref minY, ref maxX, ref maxY);
                var area = new FreeAreaRec(
                    new Coordinate(minX, minY), new Coordinate(maxX, minY),
                    new Coordinate(maxX, maxY), new Coordinate(minX, maxY)
                );
                //area.FreeLength = 0;
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
                    parkingPlace.FatherVehicleLane = lane;
                    parkingPlace.Cars.ForEach(car => car.FatherParkingPlaceBlock = parkingPlace);
                    parkingPlace.Cars.ForEach(car => car.FatherVehicleLane = parkingPlace.FatherVehicleLane);
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
                var query = PolygonUtils.CreatePolygonRec(leftDown.X, rightUp.X, leftDown.Y - CatchRange, leftDown.Y);
                var result = polygonSpatialIndex.SelectCrossingGeometry(query);
                foreach (BlockNode child in result)
                {
                    if (child.RightUpPoint.Y.CompareTo(rightUp.Y) <= 0 &&
                        child.RightUpPoint.Y.CompareTo(leftDown.Y - CatchRange) >= 0 &&
                        child.LeftDownPoint.Y.CompareTo(leftDown.Y - CatchRange) < 0 &&
                        child.LeftDownPoint.X.CompareTo(rightUp.X - 1) < 0 &&
                        child.RightUpPoint.X.CompareTo(leftDown.X + 1) > 0)
                    {
                        node.NeighborNodes[(int)PassDirection.FORWARD].Add(child);
                        child.NeighborNodes[(int)PassDirection.BACKWARD].Add(node);
                        if (node is LaneBlock faLane && child is LaneBlock chLane)
                        {
                            if (faLane.IsHorizontal && chLane.IsHorizontal)
                            {
                                faLane.IsFtherLane[(int)PassDirection.FORWARD] = true;
                                faLane.IsFtherLane[(int)PassDirection.BACKWARD] = false;
                                chLane.IsFtherLane[(int)PassDirection.FORWARD] = false;
                                chLane.IsFtherLane[(int)PassDirection.BACKWARD] = true;
                            }
                        }
                        else if (node is SpotBlock faSpot && child is SpotBlock chSpot)
                        {
                            faSpot.Spot.Type = 2;
                            chSpot.Spot.Type = 2;
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
