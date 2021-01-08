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
                if (!(boundary.GetCenter().Y - waterBucket.Position.Y > 14000))//排除技术要求
                {
                    GravityWaterBucketCenter.Add(waterBucket.Position);
                }
            }
            foreach (var waterBucket in sideWaterBucket)
            {
                var minPt = waterBucket.GeometricExtents.MinPoint;
                var maxPt = waterBucket.GeometricExtents.MaxPoint;                              
                double SIDEWATERBUCKET_X_INDENT = 0;
                double SIDEWATERBUCKET_Y_INDENT = 0;               
                if (!(boundary.GetCenter().Y - waterBucket.Position.Y > 14000))//排除技术要求
                {
                    if (maxPt.X - minPt.X < maxPt.Y - minPt.Y)//纵向放置
                    {
                        SIDEWATERBUCKET_X_INDENT = (maxPt.X - minPt.X)/2;
                        SIDEWATERBUCKET_Y_INDENT= (maxPt.X - minPt.X)/8*3- SIDEWATERBUCKET_X_INDENT/320*6;
                        Vector3d dis = new Vector3d(SIDEWATERBUCKET_X_INDENT, -SIDEWATERBUCKET_Y_INDENT, 0);
                        Vector3d dis1 = new Vector3d(-SIDEWATERBUCKET_X_INDENT, -SIDEWATERBUCKET_Y_INDENT, 0);
                        if (Onleft(waterBucket))
                        {
                            SideWaterBucketCenter.Add(waterBucket.Position - dis1);
                        }
                        else { SideWaterBucketCenter.Add(waterBucket.Position - dis); }
                    }
                    else
                    {
                        if (waterBucket.Position.Y == minPt.Y)//基点偏下放置
                        {
                            SIDEWATERBUCKET_Y_INDENT = (maxPt.Y - minPt.Y) / 2;
                        }
                        else
                        {
                            SIDEWATERBUCKET_Y_INDENT = (-maxPt.Y +minPt.Y) / 2;
                        }
                        SIDEWATERBUCKET_X_INDENT = (maxPt.Y - minPt.Y) / 8 * 3 - SIDEWATERBUCKET_X_INDENT / 320 * 6;
                        Vector3d dis = new Vector3d(SIDEWATERBUCKET_X_INDENT, -SIDEWATERBUCKET_Y_INDENT, 0);
                        Vector3d dis1 = new Vector3d(-SIDEWATERBUCKET_X_INDENT, -SIDEWATERBUCKET_Y_INDENT, 0);
                        if (Onleft(waterBucket))
                        {
                            SideWaterBucketCenter.Add(waterBucket.Position -dis1);
                        }
                        else { SideWaterBucketCenter.Add(waterBucket.Position - dis); }
                    }
                }            
            }
            
            foreach (Point3d waterBucket in SideWaterBucketCenter)
            {
                int s = 0;
                foreach (var rainPipe in roofRainPipe)
                {
                    if (rainPipe.GetCenter().X-(waterBucket).X<5)
                    {
                        s = 1;
                        break;
                    }                 
                }
                if (roofRainPipe.Count>0&&s == 0)//此处用于单层计算时生效
                {
                    Center_point.Add(waterBucket);
                }
            }                          
            SideWaterBucketTag = Index(SideWaterBucketCenter,boundary);
            GravityWaterBucketTag = Index(GravityWaterBucketCenter,boundary);
        }
        private bool Onleft(BlockReference waterBucket)
        {
            Point3d center = waterBucket.Bounds.Value.MaxPoint+(waterBucket.Bounds.Value.MaxPoint.GetVectorTo(waterBucket.Bounds.Value.MinPoint))/2;        
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
                index.Add(center + Vector3d.YAxis.GetNormal() * ThWPipeCommon.MAX_TAG_YPOSITION + Vector3d.XAxis.GetNormal() * ThWPipeCommon.MAX_TAG_XPOSITION);
                index.Add(center + Vector3d.YAxis.GetNormal() * ThWPipeCommon.MAX_TAG_YPOSITION + Vector3d.XAxis.GetNormal() * (ThWPipeCommon.MAX_TAG_XPOSITION + ThWPipeCommon.MAX_TAG_LENGTH));
                index.Add(center + Vector3d.YAxis.GetNormal() * (ThWPipeCommon.MAX_TAG_YPOSITION - ThWPipeCommon.TEXT_INDENT- ThWPipeCommon.TEXT_HEIGHT) + Vector3d.XAxis.GetNormal() * (ThWPipeCommon.MAX_TAG_XPOSITION + ThWPipeCommon.TEXT_INDENT)) ;
                index.Add(center + Vector3d.YAxis.GetNormal() * (ThWPipeCommon.MAX_TAG_YPOSITION + ThWPipeCommon.TEXT_INDENT) + Vector3d.XAxis.GetNormal() * (ThWPipeCommon.MAX_TAG_XPOSITION + ThWPipeCommon.TEXT_INDENT));
            }
            return index;
        }
    }
}
