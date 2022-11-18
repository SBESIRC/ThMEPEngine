using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;

namespace ThParkingStall.Core.LaneDeformation
{
    internal class DataPackage
    {
    }

    public class RawData
    {
        static public VehicleLaneData rawData = new VehicleLaneData();
    }

    public class ProcessedData
    {
        //临时做个示例
        static public List<List<FreeAreaRec>> FreeBlockList = new List<List<FreeAreaRec>>();
        static public List<BlockNode> BlockNodeList = new List<BlockNode>();

        // 名字没改
        static public List<List<Polygon>> output1 = new List<List<Polygon>>();
        static public List<LineSegment> output2 = new List<LineSegment>();
        static public List<List<LineSegment>> output3 = new List<List<LineSegment>>();
        static public List<LaneBlock> RearrangedLanes = new List<LaneBlock>();
        static public List<List<SpotBlock>> RearrangedSpots = new List<List<SpotBlock>>();

        //CarGenerator
        static public List<NewCarDataPass> NewCarDataPasses = new List<NewCarDataPass>();

        public ProcessedData() { }


    }


    public class Parameter
    {
        static public Vector2D TestDirection = new Vector2D(0, -1);
        static public double SingleParkingPlaceWidth = 2400;

    }

    public class LDOutput
    {
        public static DrawTmpOutPut DrawTmpOutPut0 = new DrawTmpOutPut();

    }

}
