using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 阳台、连廊、设备平台
    /// </summary>
    class BalconyCorridorEquPlatform
    {
        /// <summary>
        /// 阳台外扩找设备立管距离
        /// </summary>
        private List<RoomModel> _balconyRooms = new List<RoomModel>();
        private List<RoomModel> _corridorRooms = new List<RoomModel>();
        List<EquipmentBlockSpace> _riserPipe = new List<EquipmentBlockSpace>();
        public BalconyCorridorEquPlatform(List<RoomModel> balconyRooms,List<RoomModel> corridorRooms, List<EquipmentBlockSpace> riserPipe) 
        {
            if (null != balconyRooms && balconyRooms.Count > 0) 
            {
                foreach (var room in balconyRooms) 
                {
                    if (room == null)
                        continue;
                    _balconyRooms.Add(room);
                }
            }
            if (null != corridorRooms && corridorRooms.Count > 0)
            {
                foreach (var room in corridorRooms)
                {
                    if (room == null)
                        continue;
                    _corridorRooms.Add(room);
                }
            }
            List<BlockReference> pipeBlocks = new List<BlockReference>();
            if (null != riserPipe && riserPipe.Count > 0) 
            {
                foreach (var item in riserPipe) 
                {
                    if (item.enumEquipmentType != EnumEquipmentType.condensateRiser && item.enumEquipmentType != EnumEquipmentType.balconyRiser && item.enumEquipmentType != EnumEquipmentType.roofRainRiser)
                        continue;
                    _riserPipe.Add(item);
                }
            }
        }
        public void BalconyMopPool(List<EquipmentBlockSpace> mopPools) 
        {
            //阳台拖把池 在拖把池图块的中心生成一个排水点位。 图层：W - DRAI - EQPM 图元：半径50的圆 位置：拖把池的obb的中心
            if (null == mopPools || mopPools.Count < 1)
                return;
            foreach (var item in mopPools) 
            {
                if (item == null || item.enumRoomType != EnumRoomType.Balcony)
                    continue;
                //在拖把池图块的中心生成一个排水点位。 图层：W - DRAI - EQPM 图元：半径50的圆 位置：拖把池的obb的中心
                var centerPoint = item.blockCenterPoint;
                var circle = new Circle(centerPoint, Vector3d.ZAxis, 50);

            }
        }
        public void EqumPlatformConnect(List<EquipmentBlockSpace> equmDrain) 
        {
            //设备平台内的地漏连线
            foreach (var item in equmDrain) 
            {
                if (item == null || item.enumEquipmentType != EnumEquipmentType.floorDrain)
                    continue;
                Point3d lnCenter = new Point3d();
                Point3d ytCenter = new Point3d();
                double lnNearDis = double.MaxValue;
                double ytNearDis = double.MaxValue;
                foreach (var pipe in _riserPipe) 
                {
                    if (pipe != null)
                        continue;
                    var dis = pipe.blockCenterPoint.DistanceTo(item.blockCenterPoint);
                    if (pipe.enumEquipmentType == EnumEquipmentType.condensateRiser)
                    {
                        if (dis < lnNearDis)
                        {
                            lnCenter = pipe.blockCenterPoint;
                            lnNearDis = dis;
                        }
                    }
                    else if (pipe.enumEquipmentType == EnumEquipmentType.balconyRiser) 
                    {
                        if (dis < ytNearDis)
                        {
                            ytCenter = pipe.blockCenterPoint;
                            ytNearDis = dis;
                        }
                    }
                }
                if (lnNearDis <= 600)
                {
                    //若地漏的600范围内存在冷凝立管，则直接连接地漏和直线距离最近的冷凝立管的圆心。
                    Line addLine = new Line(item.blockCenterPoint, lnCenter);
                }
                else if (ytNearDis <= 600)
                {
                    //若地漏的600范围内不存在冷凝立管，则在600范围内找阳台立管。若能找到，则直接连接地漏和直线距离最近的阳台立管的圆心。
                    Line addLine = new Line(item.blockCenterPoint, ytCenter);
                }
                else 
                {
                    //若地漏的600范围内不存在任何冷凝立管或阳台立管，则此地漏不做连管。用0图层全局宽度为50的红色矩形圈出提醒。矩形的尺寸比地漏图元的bbox大50 %。
                }
            }

        }
        public void BalconyConnect() 
        {
            //step1 阳台内部处理
            //step2 阳台内没有找到立管，找设备平台的立管进行连接
            //step3 既没有内部，也没有设备平台的立管
        }
    }
}
