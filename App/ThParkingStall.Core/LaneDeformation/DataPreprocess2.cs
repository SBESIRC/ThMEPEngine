using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

namespace ThParkingStall.Core.LaneDeformation
{
    public class DataPreprocess2
    {
        public DataPreprocess2() 
        {
        }
        public void Pipeline()
        {
            InitBlockNodes();
            GetNeighbors();
        }
        private void InitBlockNodes()
        {
            var lanes = RawData.rawData.VehicleLanes;
            foreach (var lane in lanes)
            {
                ProcessedData.BlockNodeList.Add(new LaneBlock(lane, Parameter.TestDirection));
                foreach (var block in lane.ParkingPlaceBlockList)
                {
                    foreach (var plot in block.Cars)
                    {
                        ProcessedData.BlockNodeList.Add(new SpotBlock(plot, Parameter.TestDirection));
                    }
                }
            }
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
            foreach (var block in ProcessedData.BlockNodeList)
            {
                if (block.Type is BlockType.LANE)
                    laneToDraw.Add(block.Obb);
                if (block.Type is BlockType.SPOT)
                    spotToDraw.Add(block.Obb);
            }
            LDOutput.DrawTmpOutPut0.LaneNodes = laneToDraw;
            LDOutput.DrawTmpOutPut0.SpotNodes = spotToDraw;
        }
        private void GetNeighbors()
        {
            var polygonSpatialIndex = new BNSpatialIndex(ProcessedData.BlockNodeList);
            foreach (BlockNode node in ProcessedData.BlockNodeList)
            {
                var leftDown = node.LeftDownPoint;
                var rightUp = node.RightUpPoint;
                List<Coordinate> pointList = new List<Coordinate>();
                pointList.Add(leftDown);
                pointList.Add(new Coordinate(rightUp.X, leftDown.Y));
                pointList.Add(new Coordinate(rightUp.X, leftDown.Y - 10));
                pointList.Add(new Coordinate(leftDown.X, leftDown.Y - 10));
                pointList.Add(leftDown);
                var query = new Polygon(new LinearRing(pointList.ToArray()));
                var result = polygonSpatialIndex.SelectCrossingGeometry(query);
                foreach (BlockNode child in result)
                {
                    if (child.RightUpPoint.Y.Equals(leftDown.Y))
                    {
                        node.NeighborNodes[(int)PassDirection.FORWARD].Add(child);
                        child.NeighborNodes[(int)PassDirection.BACKWARD].Add(node);
                    }
                }
            }
            // 排序
            foreach (BlockNode node in ProcessedData.BlockNodeList)
            {
                foreach (var neighbors in node.NeighborNodes)
                {
                    neighbors.OrderBy(o => o.LeftDownPoint.X);
                }
            }
        }
    }
}
