using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPHVAC.IndoorFanLayout.Models;
using System.Linq;
using System;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AreaLayoutFan
    {
        RectAreaLayoutFan rectangleAreaLayout;
        ArcAreaLayoutFan arcAreaLayout;
        public ArcAreaLayoutFanByVertical arcAreaLayoutFanByVertical;
        List<AreaLayoutGroup> _allGroups;
        Polyline _roomOutPLine;
        List<Polyline> _innerPLines;
        public AreaLayoutFan(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis) 
        {
            rectangleAreaLayout = new RectAreaLayoutFan(divisionAreaNearIds,xAxis,yAxis);
            arcAreaLayout = new ArcAreaLayoutFan(divisionAreaNearIds, xAxis, yAxis);
            arcAreaLayoutFanByVertical = new ArcAreaLayoutFanByVertical(divisionAreaNearIds, xAxis, yAxis);
            _allGroups = new List<AreaLayoutGroup>();
        }
        public void InitRoomData(List<AreaLayoutGroup> layoutAreas, Polyline roomOutPLine, List<Polyline> innerPLines, double roomLoad)
        {
            _roomOutPLine = roomOutPLine;
            _innerPLines = innerPLines;
            _allGroups.Clear();
            foreach (var item in layoutAreas)
                _allGroups.Add(item);
            rectangleAreaLayout.InitRoomData(roomOutPLine, innerPLines, roomLoad);
            arcAreaLayout.InitRoomData(roomOutPLine, innerPLines, roomLoad);
            arcAreaLayoutFanByVertical.InitRoomData(roomOutPLine, innerPLines, roomLoad);
        }
        public List<DivisionRoomArea> GetLayoutFanResult(FanRectangle fanRectangle) 
        {
            ClearHisData();
            var layoutRes =new List<DivisionRoomArea>();
            foreach (var group in _allGroups) 
            {
                var rectRes = new List<DivisionRoomArea>();
                if (group.IsArcGroup)
                {
                    
                    if (!group.ArcVertical)
                        rectRes = arcAreaLayout.GetRectangle(group, fanRectangle);
                    else
                        rectRes = arcAreaLayoutFanByVertical.GetRectangle(group, fanRectangle);
                }
                else 
                {
                    rectRes = rectangleAreaLayout.GetRectangle(group, fanRectangle);
                }
                layoutRes.AddRange(rectRes);
            }
            return layoutRes;
        }
        public List<FanLayoutRect> GetRoomCenterFan(FanRectangle fanRectangle,double roomTableLoad) 
        {
            var addFans = new List<FanLayoutRect>();
            var layoutDir = _allGroups.First().FirstRowDir;
            if (layoutDir.Length < 0.5)
                return addFans;
            var otherDir = Vector3d.ZAxis.CrossProduct(layoutDir);
            //如果房间没有布置任何风机，根据负荷，在中心处进行均布
            var fanCount = (int)Math.Ceiling(roomTableLoad / fanRectangle.Load);
            var roomCenter = IndoorFanCommon.PolylinCenterPoint(_roomOutPLine) + layoutDir.Negate().MultiplyBy(fanRectangle.MaxLength/2);
            var startPoint = roomCenter;
            var a = (int)(fanCount / 2);
            var b = (int)(fanCount % 2);
            if (b == 1)
            {
                //奇数
                startPoint = roomCenter + otherDir.MultiplyBy((fanRectangle.Width + IndoorFanCommon.FanSpaceMinDistance) * a);
            }
            else 
            {
                startPoint = roomCenter + otherDir.MultiplyBy((fanRectangle.Width/2 + IndoorFanCommon.FanSpaceMinDistance / 2) * a);
            }
            startPoint = startPoint + layoutDir.MultiplyBy(fanRectangle.MaxLength / 2);
            var stepLength = fanRectangle.Width + IndoorFanCommon.FanSpaceMinDistance;
            for (int i = 0; i < fanCount; i++) 
            {
                var fanCenterPoint = startPoint - otherDir.MultiplyBy(stepLength*i);
                var fanRect = CenterToRect(fanCenterPoint, layoutDir, fanRectangle.MaxLength, otherDir, fanRectangle.Width);
                if (fanRect.Area < 10) 
                {
                
                }
                var fanLayoutRect = new FanLayoutRect(fanRect, fanRectangle.Width, layoutDir);
                fanLayoutRect.FanDirection = layoutDir;
                CalcFanVent(fanLayoutRect, fanRectangle);
                addFans.Add(fanLayoutRect);
            }

            return addFans;
        }

        void ClearHisData() 
        {
            foreach (var group in _allGroups) 
            {
                foreach (var layoutArea in group.GroupDivisionAreas) 
                {
                    layoutArea.FanLayoutAreaResult.Clear();
                    layoutArea.NeedFanCount = 0;
                }
            }
        }
        Polyline CenterToRect(Point3d centerPoint, Vector3d lengthDir, double length, Vector3d widthDir, double width)
        {
            var pt1 = centerPoint + lengthDir.MultiplyBy(length / 2);
            var pt2 = centerPoint - lengthDir.MultiplyBy(length / 2);
            var moveOffSet = widthDir.MultiplyBy(width / 2);
            var newPt1 = pt1 + moveOffSet;
            var newPt1End = pt1 - moveOffSet;
            var newPt2 = pt2 + moveOffSet;
            var newPt2End = pt2 - moveOffSet;
            Polyline poly = new Polyline();
            poly.Closed = true;
            poly.AddVertexAt(0, newPt1.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, newPt1End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(2, newPt2End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(3, newPt2.ToPoint2D(), 0, 0, 0);
            return poly;
        }
        Polyline GetFanVentPolyline(FanRectangle fanRectangle,Point3d centerPoint, Vector3d fanDir)
        {
            var otherDir = fanDir.CrossProduct(Vector3d.ZAxis);
            var moveOffSetX = otherDir.MultiplyBy(fanRectangle.VentRect.VentLength / 2);
            var moveOffSetY = fanDir.MultiplyBy(fanRectangle.VentRect.VentWidth / 2);
            var rectPt1 = centerPoint + moveOffSetY;
            var rectPt2 = centerPoint - moveOffSetY;
            var pt1 = rectPt1 - moveOffSetX;
            var pt1End = rectPt1 + moveOffSetX;
            var pt2 = rectPt2 - moveOffSetX;
            var pt2End = rectPt2 + moveOffSetX;
            Polyline poly = new Polyline();
            poly.Closed = true;
            poly.ColorIndex = 2;
            poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, pt1End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(2, pt2End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(3, pt2.ToPoint2D(), 0, 0, 0);
            return poly;
        }
        void CalcFanVent(FanLayoutRect fanRect, FanRectangle fanRectangle) 
        {
            //判断可以放下几个
            var lengthLine = fanRect.LengthLines.First();
            var length = lengthLine.Length;
            var centerPoint = fanRect.CenterPoint;
            var canLayoutLength = length - fanRectangle.VentRect.VentMinDistanceToStart - fanRectangle.VentRect.VentMinDistanceToEnd;
            int count = fanRectangle.MaxVentCount;
            while (true)
            {
                if (count < fanRectangle.MinVentCount)
                    break;
                var needLength = (count - 1) * fanRectangle.VentRect.VentMinDistanceToPrevious;
                if (needLength > canLayoutLength)
                {
                    count -= 1;
                    continue;
                }
                break;
            }
            if (count < 1 || count < fanRectangle.MinVentCount)
                return;
            if (count == 1)
            {
                //一个时放置末尾
                var ventCenter = centerPoint + fanRect.FanDirection.MultiplyBy(length / 2 - fanRectangle.VentRect.VentMinDistanceToEnd);
                fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(fanRectangle, ventCenter, fanRect.FanDirection)));
            }
            else
            {
                //>=2;开始结尾各一个，中间等分放置
                var startCenter = centerPoint - fanRect.FanDirection.MultiplyBy(length / 2 - fanRectangle.VentRect.VentMinDistanceToStart);
                fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(fanRectangle,startCenter, fanRect.FanDirection)));
                var dis = canLayoutLength / (count - 1);
                var currentPoint = startCenter + fanRect.FanDirection.MultiplyBy(dis);
                while (currentPoint.DistanceTo(startCenter) < canLayoutLength + 100)
                {
                    fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(fanRectangle,currentPoint, fanRect.FanDirection)));
                    currentPoint = currentPoint + fanRect.FanDirection.MultiplyBy(dis);
                    if (fanRect.FanDirection.Length < 0.8)
                        break;
                }
            }
        }
    }
}
