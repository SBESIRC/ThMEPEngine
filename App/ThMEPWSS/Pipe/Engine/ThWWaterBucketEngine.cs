using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;


namespace ThMEPWSS.Pipe.Engine
{
    public class ThWWaterBucketEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public Point3dCollection SideWaterBucketCenter { get; set; }
        public Point3dCollection SideWaterBucketTag { get; set; }
        public Point3dCollection GravityWaterBucketCenter { get; set; }
        public Point3dCollection GravityWaterBucketTag { get; set; }
        public Point3dCollection Center_point { get; set; }
        public ThWWaterBucketEngine()
        {
            SideWaterBucketCenter = new Point3dCollection();
            GravityWaterBucketCenter = new Point3dCollection();
            Center_point= new Point3dCollection();
        }
        public void Run(List<BlockReference> gravityWaterBucket, List<BlockReference> sideWaterBucket, List<Polyline> roofRainPipe,Polyline boundary)
        {
            foreach (var waterBucket in gravityWaterBucket)
            {
                GravityWaterBucketCenter.Add(waterBucket.Position);
            }
            foreach (var waterBucket in sideWaterBucket)
            {
                Vector3d dis = new Vector3d(160, -114, 0);
                Vector3d dis1 = new Vector3d(-160, 114, 0);
                if (OnLeftPart(waterBucket, boundary))
                {

                    SideWaterBucketCenter.Add(waterBucket.Position - dis);
                }
                else { SideWaterBucketCenter.Add(waterBucket.Position - dis1); }
            }
            
            foreach (Point3d waterBucket in SideWaterBucketCenter)
            {
                foreach (var rainPipe in roofRainPipe)
                {
                    if (rainPipe.GetCenter().Equals(waterBucket))
                    {
                        continue;
                    }
                    Center_point.Add(waterBucket);
                }             
            }                          
            SideWaterBucketTag = Index(SideWaterBucketCenter,boundary);
            GravityWaterBucketTag = Index(GravityWaterBucketCenter,boundary);
        }
        private bool OnLeftPart(BlockReference waterBucket, Polyline boundary)
        {
            var center = boundary.GetCenter();
            if (waterBucket.Position.X< center.X)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private Point3dCollection Index(Point3dCollection centers, Polyline boundary)
        {
            var index = new Point3dCollection();
            foreach (Point3d center in centers)
            {         
                index.Add(center + Vector3d.YAxis.GetNormal() * 1260 + Vector3d.XAxis.GetNormal() * 540);
                index.Add(center + Vector3d.YAxis.GetNormal() * 1260 + Vector3d.XAxis.GetNormal() * 800);
                index.Add(center + Vector3d.YAxis.GetNormal() * 1260 + Vector3d.XAxis.GetNormal() * 760) ;
            }
            return index;
        }
    }
}
