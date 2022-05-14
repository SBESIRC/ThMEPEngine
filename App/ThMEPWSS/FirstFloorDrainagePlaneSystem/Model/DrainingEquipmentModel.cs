using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Model
{
    public class DrainingEquipmentModel
    {
        public DrainingEquipmentModel(EnumEquipmentType type, Polyline blockGeo, Point3d pt)
        {
            EnumEquipmentType = type;
            BlockReferenceGeo = blockGeo;
            BlockPoint = pt;
        }

        /// <summary>
        /// 连接点位
        /// </summary>
        public Point3d DiranPoint { get; set; }

        /// <summary>
        /// 构建类型
        /// </summary>
        public EnumEquipmentType EnumEquipmentType { get; set; }

        /// <summary>
        /// 块外包框线（obb）
        /// </summary>
        public Polyline BlockReferenceGeo { get; set; }

        /// <summary>
        /// 块的几何点位
        /// </summary>
        public Point3d BlockPoint { get; set; }
    }
}
