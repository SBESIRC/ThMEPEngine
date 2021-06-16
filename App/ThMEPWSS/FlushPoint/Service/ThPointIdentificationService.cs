using AcHelper;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThPointIdentificationService
    {
        public static WashPtLayoutInfo LayoutInfo;
        static ThPointIdentificationService()
        {
            LayoutInfo = new WashPtLayoutInfo();
        }
        public static void ShowOrHide(bool farwayDrainageFacility, bool closeDrainageFacility)
        {
            if(farwayDrainageFacility == false && closeDrainageFacility == false)
            {
                UnHighLight();
            }
            else
            {
                var objs = new DBObjectCollection();
                if(farwayDrainageFacility)
                {
                    LayoutInfo.GetFarawayBlks().Cast<BlockReference>().ForEach(o => objs.Add(o));
                }
                if(closeDrainageFacility)
                {
                    LayoutInfo.GetNearbyBlks().Cast<BlockReference>().ForEach(o => objs.Add(o));
                }
                HighLight(objs);
            }
        }

        private static void HighLight(DBObjectCollection blks)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var objIds = new List<ObjectId>();
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
                            objIds.Add(o.ObjectId);
                        }
                    }
                });
                Active.Editor.PickFirstObjects(objIds.ToArray());
            }  
        }        
        private static void UnHighLight()
        {
            Active.Editor.PickFirstObjects(new ObjectId[0]);
        }
        private static bool IsInActiveView(Point3d pt, double minX, double maxX, double minY, double maxY)
        {
            if (pt.X >= minX && pt.X <= maxX &&
               pt.Y >= minY && pt.Y <= maxY)
            {
                return true;
            }
            else
            {
                return false;
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
