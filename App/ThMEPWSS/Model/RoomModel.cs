using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Model
{
    /// <summary>
    /// 房间基本模型信息
    /// </summary>
    class RoomModel
    {
        /// <summary>
        /// 房间的原模型信息
        /// </summary>
        public ThIfcRoom thIFCRoom { get; set; }
        /// <summary>
        /// 房间的轮廓信息
        /// 1、可能和原模型信息不一样，这里只考虑了外轮廓，没有考虑内轮廓，
        /// 2、有些空间会有融合的可能，这里的轮廓会和原模型信息不一样(这融合一般是烟道井，管道井，正常的房间一般不会进行融合)
        /// </summary>
        public Polyline outLine { get; set; }
        /// <summary>
        /// 获取房间的几何中心点
        /// </summary>
        /// <returns></returns>
        public Point3d GetRoomCenterPoint() 
        {
            Point3d centerPoint = new Point3d();
            if (this.outLine == null || this.outLine.Area < 1)
                return centerPoint;

            var center = this.outLine.ToNTSGeometry().EnvelopeInternal.Centre;
            centerPoint = new Point3d(center.X, center.Y, 0);
            return centerPoint;
        }
        /// <summary>
        /// 获取房间的obb轮廓
        /// </summary>
        /// <returns></returns>
        public Polyline GetRoomOBBPolyline() 
        {
            Polyline polyline = null;
            if (this.outLine == null || this.outLine.Area < 1)
                return polyline;
            polyline = this.outLine.CalObb();
            return polyline;
        }
        /// <summary>
        /// 房间类型（根据房间名称，判定属于那类房间）
        /// </summary>
        public EnumRoomType roomTypeName { get; set; }
    }
    /// <summary>
    /// 房间类型
    /// </summary>
    enum EnumRoomType
    {
        /// <summary>
        /// 卧室
        /// </summary>
        [Description("卧室")]
        Bedroom = 0,
        /// <summary>
        /// 卫生间
        /// </summary>
        [Description("卫生间")]
        Toilet = 1,
        /// <summary>
        /// 厨房
        /// </summary>
        [Description("厨房")]
        Kitchen = 2,
        /// <summary>
        /// 客厅
        /// </summary>
        [Description("客厅")]
        Parlour = 3,
        /// <summary>
        /// 餐厅
        /// </summary>
        [Description("餐厅")]
        DiningRoom = 5,
        /// <summary>
        /// 阳台
        /// </summary>
        [Description("阳台")]
        Balcony = 6,
        /// <summary>
        /// 连廊
        /// </summary>
        [Description("连廊")]
        Corridor = 7,
        /// <summary>
        /// 烟道井
        /// </summary>
        [Description("烟道井")]
        FlueWell = 8,
        /// <summary>
        /// 管道井
        /// </summary>
        [Description("管道井")]
        TubeWell = 9,
        /// <summary>
        /// 设备平台
        /// </summary>
        [Description("设备平台")]
        EquipmentPlatform = 10,
        /// <summary>
        /// 其它
        /// </summary>
        [Description("其它")]
        Other = 9999,
    }
}
