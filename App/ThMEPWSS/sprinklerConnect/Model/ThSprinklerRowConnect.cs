using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;

namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerRowConnect : ICloneable
    {
        public Dictionary<int, List<Point3d>> OrderDict { get; set; } = new Dictionary<int, List<Point3d>>();
        public int Count { get; set; } = 0;
        public bool IsStallArea { get; set; }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public bool IsSmallRoom { get; set; } = false;
        public List<Line> ConnectLines { get; set; } = new List<Line>();
        public Line Base
        {
            get{ return new Line(StartPoint, EndPoint);}
        }

        public object Clone()
        {
            var clone = new ThSprinklerRowConnect();
            clone.OrderDict = new Dictionary<int, List<Point3d>>();
            OrderDict.ForEach(o =>
            {
                var newList = new List<Point3d>();
                o.Value.ForEach(pt => newList.Add(pt));
                clone.OrderDict.Add(o.Key, newList);
            });
            clone.Count = Count;
            clone.IsStallArea = IsStallArea;
            clone.StartPoint = StartPoint;
            clone.EndPoint = EndPoint;
            clone.IsSmallRoom = IsSmallRoom;
            return clone;
        }
    }
}
