using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThVTee
    {
        private string bypassSize;
        private FanParam fanParam;
        private Point3d roomVtPos;   //   room侧
        private Point3d notRoomVtPos;// 非room侧
        public static double roomVerticalPipeHeight = 600;// 服务侧立管高600
        public List<SegInfo> vtDuct;
        public List<SegInfo> vtElbow;

        public ThVTee(Point3d roomVtPos, Point3d notRoomVtPos, FanParam fanParam, string installStyle)
        {
            this.fanParam = fanParam;
            this.roomVtPos = roomVtPos;
            this.notRoomVtPos = notRoomVtPos;
            bypassSize = fanParam.bypassSize;
            vtDuct = new List<SegInfo>();
            vtElbow = new List<SegInfo>();
            if (installStyle == "落地")
            {
                CreateGroundVtDuct();
                CreateGroundVtElbow();
            }
            else
            {
                CreateTopVtDuct();
                CreateTopVtElbow();
            }
        }

        private void CreateTopVtDuct()
        {
            var roomElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var h = ThMEPHVACService.GetHeight(bypassSize);
            var ele = roomElevation - roomVerticalPipeHeight;
            var selfEleVec = (ele - h * 0.5) * Vector3d.ZAxis; // 上升半个h，退到中心点位置
            var dirVec = (notRoomVtPos - roomVtPos).GetNormal();
            var roomP = roomVtPos - dirVec * h * 0.5;
            var notRoomP = notRoomVtPos + dirVec * h * 0.5;
            var info = new SegInfo()
            {
                l = new Line(roomP + selfEleVec, notRoomP + selfEleVec),
                ductSize = bypassSize,
                airVolume = fanParam.airVolume * 0.3,
                elevation = ele.ToString("0.00")
            };
            vtDuct.Add(info);
        }

        private void CreateTopVtElbow()
        {
            var dirVec = (notRoomVtPos - roomVtPos).GetNormal();
            RecordTopRoomVtElbowInfo(dirVec);
            RecordTopNotRoomVtElbowInfo(-dirVec);
        }

        private void RecordTopNotRoomVtElbowInfo(Vector3d dirVec)
        {
            var roomElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var notRoomElevation = Double.Parse(fanParam.notRoomElevation) * 1000;
            var ep = notRoomVtPos + new Vector3d(0, 0, notRoomElevation);
            var sp = notRoomVtPos + new Vector3d(0, 0, roomElevation - roomVerticalPipeHeight);
            var l = new Line(sp, ep);
            // 立管风量为1/3的风机风量
            vtElbow.Add(new SegInfo()
            {
                l = l,
                horizontalVec = dirVec,
                airVolume = fanParam.airVolume * 0.3,
                ductSize = bypassSize
            });
        }

        private void RecordTopRoomVtElbowInfo(Vector3d dirVec)
        {
            var mmElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var ep = roomVtPos + new Vector3d(0, 0, mmElevation);
            var sp = roomVtPos + new Vector3d(0, 0, mmElevation - roomVerticalPipeHeight);
            var l = new Line(sp, ep);
            // 立管风量为1/3的风机风量
            vtElbow.Add(new SegInfo()
            {
                l = l,
                horizontalVec = dirVec,
                airVolume = fanParam.airVolume * 0.3,
                ductSize = bypassSize
            });
        }

        private void CreateGroundVtDuct()
        {
            var roomH = ThMEPHVACService.GetHeight(fanParam.roomDuctSize);
            var roomElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var h = ThMEPHVACService.GetHeight(bypassSize);
            var ele = roomH + roomElevation + roomVerticalPipeHeight;
            var selfEleVec = (ele + h * 0.5) * Vector3d.ZAxis; // 上升半个h，退到中心点位置
            var dirVec = (notRoomVtPos - roomVtPos).GetNormal();
            var roomP = roomVtPos - dirVec * h * 0.5;
            var notRoomP = notRoomVtPos + dirVec * h * 0.5;
            var info = new SegInfo() { l = new Line(roomP + selfEleVec, notRoomP + selfEleVec), 
                                       ductSize = bypassSize, 
                                       airVolume = fanParam.airVolume * 0.3,
                                       elevation = ele.ToString("0.00") };
            vtDuct.Add(info);
        }

        public void CreateGroundVtElbow()
        {
            var dirVec = (notRoomVtPos - roomVtPos).GetNormal();
            RecordGroundRoomVtElbowInfo(dirVec);
            RecordGroundNotRoomVtElbowInfo(-dirVec);
        }

        private void RecordGroundRoomVtElbowInfo(Vector3d dirVec)
        {
            var h = ThMEPHVACService.GetHeight(fanParam.roomDuctSize);
            var mmElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var ep = roomVtPos + new Vector3d(0, 0, h + mmElevation + roomVerticalPipeHeight);
            var sp = roomVtPos + new Vector3d(0, 0, h + mmElevation);
            var l = new Line(sp, ep);
            // 立管风量为1/3的风机风量
            vtElbow.Add(new SegInfo() {
                l = l,
                horizontalVec = dirVec,
                airVolume = fanParam.airVolume * 0.3,
                ductSize = bypassSize
            });
        }

        private void RecordGroundNotRoomVtElbowInfo(Vector3d dirVec)
        {
            var roomH = ThMEPHVACService.GetHeight(fanParam.roomDuctSize);
            var roomElevation = Double.Parse(fanParam.roomElevation) * 1000;
            var notRoomH = ThMEPHVACService.GetHeight(fanParam.notRoomDuctSize);
            var notRoomElevation = Double.Parse(fanParam.notRoomElevation) * 1000;
            var ep = notRoomVtPos + new Vector3d(0, 0, roomH + roomElevation + roomVerticalPipeHeight);
            var diff = (notRoomElevation + notRoomH) - (roomElevation + roomH);
            var sp = ep - (diff + roomVerticalPipeHeight) * Vector3d.ZAxis;
            var l = new Line(sp, ep);
            // 立管风量为1/3的风机风量
            vtElbow.Add(new SegInfo()
            {
                l = l,
                horizontalVec = dirVec,
                airVolume = fanParam.airVolume * 0.3,
                ductSize = bypassSize
            });
        }
    }
}