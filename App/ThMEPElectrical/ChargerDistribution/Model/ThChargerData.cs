using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;

namespace ThMEPElectrical.ChargerDistribution.Model
{
    public class ThChargerData
    {
        /// <summary>
        /// Id
        /// </summary>
        public ObjectId ObjectId { get; set; }

        /// <summary>
        /// 块名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 旋转角
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// 块比例
        /// </summary>
        public Scale3d ScaleFactors { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string CircuitNumber { get; set; }

        /// <summary>
        /// 外包框
        /// </summary>
        public Polyline Geometry { get; set; }

        public ThChargerData(BlockReference block)
        {
            ObjectId = block.ObjectId;
            Name = block.Name;
            Position = block.Position;
            Rotation = block.Rotation;
            ScaleFactors = block.ScaleFactors;
            CircuitNumber = "";
            Geometry = block.GeometricExtents.ToRectangle();
        }
    }
}
