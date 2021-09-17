using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;

namespace ThMEPLighting.ParkingStall.Model
{
    class MaxGroupLight
    {
        public Point3d NearRoutePoint { get; set; }
        public double DistanctToStartPoint { get; set; }
        public Point3d NearNodeLightPoint { get; }
        public Point3d ConnectLightPoint { get; set; }
        public Point3d WireTroughLinePoint { get; set; }
        public List<Line> ConnectLines { get; }
        public List<LightDirGroup> LightGroups { get; }
        public int LightCount
        {
            get
            {
                int count = 0;
                if (null == LightGroups || LightGroups.Count < 1)
                    return count;
                foreach (var item in LightGroups)
                {
                    if (null == item || item.LightPoints == null || item.LightPoints.Count < 1)
                        continue;
                    count += item.LightPoints.Count;
                }
                return count;
            }
        }
        public MaxGroupLight(Point3d point, double distancToStart)
        {
            this.NearNodeLightPoint = point;
            this.DistanctToStartPoint = distancToStart;
            this.ConnectLightPoint = point;
            this.ConnectLines = new List<Line>();
            this.LightGroups = new List<LightDirGroup>();
        }
    }
    class LightDirGroup
    {
        public List<Point3d> LightPoints { get; }//灯的点，可能不是按照顺序排的
        public Vector3d LineDir { get; }//该组的方向
        public string GroupId { get; }//该组的Id
        public string ParentId { get; set; }//该组连接的其它组Id
        public Point3d ParentConnectPoint { get; set; }//其它组连接该组的点
        public Point3d ConnectParentPoint { get; set; }//该组连接其它组的点
        public LightConnectLine ConnectParent { get; set; }//该组连接其它组的路线
        public Point3d NearGroupPoint { get; }//该组距离线槽最近的点
        public Point3d NearRoutePoint { get; }//线槽路径中距离该组最近的点
        public Line NearLine { get; }//距离该组最近的线槽线
        public double NearRouteDisToEnd { get; }//线槽路径点到配电箱的距离
        public List<LightConnectLine> LightConnectLines { get; }//灯之间的连线，有些不能直接到达，可能会有多个线
        public LightDirGroup(List<Point3d> lightPoints, Vector3d dir,Point3d nearGroupPoint,Point3d nearRoutePoint,Line nearLine,double disToEnd)
        {
            this.LightPoints = new List<Point3d>();
            this.LineDir = dir;
            this.ParentId = "";
            if (null != lightPoints && lightPoints.Count > 0)
            {
                foreach (var item in lightPoints)
                {
                    if (null == item)
                        continue;
                    this.LightPoints.Add(item);
                }
            }
            this.NearGroupPoint = nearGroupPoint;
            this.NearRoutePoint = nearRoutePoint;
            this.NearLine = nearLine;
            this.NearRouteDisToEnd = disToEnd;
            this.GroupId = Guid.NewGuid().ToString();
            this.LightConnectLines = new List<LightConnectLine>();
            this.ConnectParent = null;
        }
        public double PointDisToConnectPoint(Point3d point) 
        {
            var end = this.ConnectParentPoint;
            var start = point;
            double dis = 0.0;
            var maxDis = start.DistanceTo(end);
            if (maxDis < 10)
                return dis;
            var dir = (end - start).GetNormal();
            //这里灯的点已经是排序后的额点
            var startInt = -1;
            var endInt = -1;
            var tempOreders = ThPointVectorUtil.PointsOrderByDirection(LightPoints, dir, false).ToList();
            for (int i = 0; i < tempOreders.Count; i++) 
            {
                var checkPoint = tempOreders[i];
                if (checkPoint.DistanceTo(start) < 10)
                    startInt = i;
                if (checkPoint.DistanceTo(end) < 10)
                    endInt = i;
            }
            if (startInt < 0)
                return dis;
            var min = Math.Min(startInt,endInt);
            var max = Math.Max(startInt, endInt);
            
            for (int i = min; i < max; i++) 
            {
                var sp = tempOreders[i];
                var ep = tempOreders[i + 1];
                var lines = GetLightConnectLines(sp, ep);
                dis += lines.Sum(c => c.Length);
            }
            return dis;
        }
        public int GetLightOutCount(Point3d point) 
        {
            return 1;
        }
        public List<Line> GetLightConnectLines(Point3d startPoint, Point3d endPoint) 
        {
            var lines = new List<Line>();
            foreach(var item in LightConnectLines) 
            {

                if (item.StartLightPoint.DistanceTo(startPoint) < 1)
                {
                    if (item.EndLightPoint.DistanceTo(endPoint) < 1)
                        return item.ConnectLines;
                }
                else if (item.EndLightPoint.DistanceTo(startPoint) < 1)
                    if (item.StartLightPoint.DistanceTo(endPoint) < 1)
                        return item.ConnectLines;

            }
            return lines;
        }
    }
    class LightConnectLine 
    {
        /// <summary>
        /// 灯的中心点
        /// </summary>
        public Point3d StartLightPoint { get; }
        /// <summary>
        /// 灯的实际连接点
        /// </summary>
        public Point3d StartLightConnectPoint { get; set; }
        /// <summary>
        /// 灯的实际连接线，以实际连接点连接的线
        /// </summary>
        public List<Line> ConnectLines { get; }
        public Point3d EndLightPoint { get; }
        public Point3d EndLightConnectPoint { get; set; }
        public double StartToEndDis 
        {
            get 
            {
                double dis = 0.0;
                if (ConnectLines == null || ConnectLines.Count < 1)
                    return dis;
                dis = ConnectLines.Sum(c => c.Length);
                return dis;
            }
        }
        public LightConnectLine(Point3d startPoint,Point3d endPoint,List<Line> connectLines) 
        {
            this.StartLightPoint = startPoint;
            this.EndLightPoint = endPoint;
            this.ConnectLines = new List<Line>();
            if (null != connectLines && connectLines.Count > 0) 
            {
                foreach (var line in connectLines) 
                {
                    if (null == line)
                        continue;
                    this.ConnectLines.Add(line);
                }
            }
        }
    }
}
