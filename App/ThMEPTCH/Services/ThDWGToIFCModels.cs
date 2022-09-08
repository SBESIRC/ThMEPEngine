using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.Services
{
    class FloorBlock
    {
        public Polyline FloorOutLine { get; }
        public Point3d FloorOrigin { get; }
        public string FloorName { get; }
        public List<object> FloorEntitys { get; }

        public FloorBlock(string floorName, Polyline outLine, Point3d point)
        {
            FloorName = floorName;
            FloorOutLine = outLine;
            FloorOrigin = point;
            FloorEntitys = new List<object>();

        }
    }
    class FloorCurveEntity
    {
        public ulong Id { get; }
        public Entity EntityCurve { get; }
        public PropertyBase Property { get; }
        public string EntitySystem { get; }
        public object FloorEntity { get; set; }
        public FloorCurveEntity(ulong id, Entity curve, string system, PropertyBase property)
        {
            EntityCurve = curve;
            EntitySystem = system;
            Property = property;
            Id = id;
        }
    }
    class LevelElevation
    {
        /// <summary>
        /// 楼层编号
        /// </summary>
        public string Num { get; set; }
        /// <summary>
        /// 楼层标高
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// 层高
        /// </summary>
        public double LevelHeight { get; set; }
        /// <summary>
        /// 楼层框名称（图纸中楼层框内的字段）
        /// </summary>
        public string FloorName { get; set; }
    }
    class SlabPolyline
    {
        public Polyline OutPolyline { get; }
        public bool IsOpening { get; set; }
        public double Thickness { get; set; }
        public double LowerPlateHeight { get; set; }
        public double SurroundingThickness { get; set; }
        public List<SlabPolyline> InnerSlabOpenings { get; }
        public SlabPolyline(Polyline polyline, double thickness)
        {
            OutPolyline = polyline;
            Thickness = thickness;
            LowerPlateHeight = 0.0;
            InnerSlabOpenings = new List<SlabPolyline>();
            IsOpening = false;
        }
    }
}
