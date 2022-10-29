using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.LaneDeformation
{
    public class FreeBlock:BlockNode
    {
        public Coordinate LeftUpPoint = new Coordinate(0, 0);
        public Coordinate LeftDownPoint = new Coordinate(0, 0);
        public Coordinate RightDownPoint = new Coordinate(0, 0);
        public Coordinate RightUpPoint = new Coordinate(0, 0);
        public double FreeLength = 0;        

        public FreeBlock(Coordinate leftDown, Coordinate leftUp, Coordinate rightDown, Coordinate rightUp) 
        {
            LeftDownPoint = leftDown;
            LeftUpPoint = leftUp;
            RightDownPoint = rightDown;
            RightUpPoint = rightUp;
            FreeLength = new Vector2D(rightDown,rightUp).Length();

            //init
            this.Type = 0;
        }

        public void UpdataBase() 
        {
            List<Coordinate> pointList = new List<Coordinate>();
            pointList.Add(LeftDownPoint);
            pointList.Add(LeftUpPoint);
            this.Obb = new Polygon(new LinearRing(pointList.ToArray()));
            this.SelfTolerance = FreeLength;

        }
    }
}
