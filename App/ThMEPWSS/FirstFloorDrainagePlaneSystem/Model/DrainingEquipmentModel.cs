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
        public DrainingEquipmentModel(Point3d point, EnumEquipmentType type, BlockReference block)
        {
            DiranPoint = point;
            EnumEquipmentType = type;
            BlockReference = block;
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
        /// 块
        /// </summary>
        public BlockReference BlockReference { get; set; }
    }
}
