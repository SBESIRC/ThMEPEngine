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
        
        //
        static public List<Polygon> 
        static public List<LineSegment>
        static public List<LineSegment>

        public ProcessedData() { }


    }


    public class Parameter
    {
        static public Vector2D TestDirection = new Vector2D(0, -1);


    }

    public class LDOutput
    {
        public static DrawTmpOutPut DrawTmpOutPut0 = new DrawTmpOutPut();

    }

}
