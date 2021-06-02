using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThPointIdentificationService
    {
        public static WashPtLayoutInfo LayoutInfo;
        static ThPointIdentificationService()
        {
            LayoutInfo = new WashPtLayoutInfo();
        }
        public static void HighLightFarawayWashPoints()
        {
            var blks = LayoutInfo.GetFarawayBlks();
            HighLight(blks);
        }
        public static void UnHighLightFarawayWashPoints()
        {
            var blks = LayoutInfo.GetFarawayBlks();
            UnHighLight(blks);
        }
        public static void HighLightNearbyWashPoints()
        {
            var blks = LayoutInfo.GetNearbyBlks();
            HighLight(blks);
        }
        public static void UnHighLightNearbyWashPoints()
        {
            var blks = LayoutInfo.GetNearbyBlks();
            UnHighLight(blks);
        }
        private static void HighLight(DBObjectCollection blks)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var extents = ThAuxiliaryUtils.GetCurrentViewBound();
                blks.Cast<BlockReference>().ForEach(o =>
                {
                    if (!o.IsErased && !o.IsDisposed)
                    {       
                        if(IsInActiveView(o.GeometricExtents.MinPoint, 
                            extents.MinPoint.X, extents.MaxPoint.X, 
                            extents.MinPoint.Y, extents.MaxPoint.Y) ||
                        IsInActiveView(o.GeometricExtents.MaxPoint,
                        extents.MinPoint.X, extents.MaxPoint.X, 
                        extents.MinPoint.Y, extents.MaxPoint.Y))
                        {
                            o.Highlight();
                        }
                    }
                });
            }  
        }
        private static bool IsInActiveView(Point3d pt,double minX,double maxX,double minY,double maxY)
        {
            if(pt.X>= minX && pt.X<=maxX &&
               pt.Y >= minY && pt.Y <= maxY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static void UnHighLight(DBObjectCollection blks)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                blks.Cast<BlockReference>().ForEach(o =>
                {
                    if (!o.IsErased && !o.IsDisposed)
                    {                        
                        o.Unhighlight();                        
                    }
                });
            }
        }
    }

    public class WashPtLayoutInfo
    {
        public Dictionary<Point3d, BlockReference> LayoutBlock { get; set; }
        public List<Point3d> NearbyPoints { get; set; }
        public List<Point3d> FarawayPoints { get; set; }
        public WashPtLayoutInfo()
        {
            NearbyPoints = new List<Point3d>();
            FarawayPoints = new List<Point3d>();
            LayoutBlock = new Dictionary<Point3d, BlockReference>();
        }
        public DBObjectCollection GetFarawayBlks()
        {
            return LayoutBlock
                 .Where(o => FarawayPoints.Contains(o.Key))
                 .Select(o => o.Value).ToCollection();
        }
        public DBObjectCollection GetNearbyBlks()
        {
            return LayoutBlock
                 .Where(o => NearbyPoints.Contains(o.Key))
                 .Select(o => o.Value).ToCollection();
        }
    }
}
