using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPLighting.ParkingStall.Model
{
    class LightGroup
    {
        public List<Point3d> GroupPoints { get; }
        public int GroupCount 
        {
            get 
            {
                if (null == GroupPoints)
                    return 0;
                return GroupPoints.Count;
            }
        }
        public List<List<Point3d>> InnerXGroups { get; }
        public List<List<Point3d>> InnerYGroups { get; }
        public List<LightBlockReference> BlockReferences { get; }
        public Vector3d XAxis { get; }
        public Vector3d YAxis { get; }
        public LightGroup(List<Point3d> groupPoints,Vector3d xAxis,Vector3d yAxis) 
        {
            this.GroupPoints = new List<Point3d>();
            this.InnerXGroups = new List<List<Point3d>>();
            this.InnerYGroups = new List<List<Point3d>>();
            this.BlockReferences = new List<LightBlockReference>();
            this.XAxis = xAxis;
            this.YAxis = yAxis;
            if (null != groupPoints && groupPoints.Count > 0) 
            {
                foreach (var item in groupPoints)
                {
                    if (null != item)
                        this.GroupPoints.Add(item);
                }
            }
            
        }
    }
    class LightBlockReference
    {
        public BlockReference LightBlock { get; }
        public Point3d LightPosition2d { get; }
        public Vector3d LightVector { get; }
        public double LightAngle { get; }
        public Point3d ConnectPoint1 { get; }
        public Point3d ConnectPoint2 { get; }
        public Point3d ConnectPoint3 { get; }
        public LightBlockReference(BlockReference blockReference) 
        {
            LightBlock = blockReference;
            var position = blockReference.Position;
            this.LightAngle = blockReference.Rotation;
            var normal = blockReference.Normal;
            var vectorX = Vector3d.XAxis;
            this.LightVector = vectorX.RotateBy(this.LightAngle, normal);
            this.LightPosition2d = new Point3d(position.X, position.Y, 0);
            this.ConnectPoint2 = this.LightPosition2d;
            var blockScale = this.LightBlock.ScaleFactors;
            var length = 12.0;
            var blockRealLength = length * blockScale.X;
            this.ConnectPoint1 = this.ConnectPoint2 - this.LightVector.MultiplyBy(blockRealLength / 2);
            this.ConnectPoint3 = this.ConnectPoint2 + this.LightVector.MultiplyBy(blockRealLength / 2);
        }

    }
}
