using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.OTools;

namespace ThParkingStall.Core.ShortestPath
{
    //最短路径修正
    public class PathCorrector
    {
        private STRtree<LineSegment> WallEngine;//新增墙线引擎
        //private LineString AddedWall;//新增墙线
        private LineString InitPath;//原始路径
        //private List<List<Coordinate>> Groups;// 凸包组
        public PathCorrector(STRtree<LineSegment> wallEngine,LineString initPath)
        {
            WallEngine = wallEngine;
            InitPath = initPath;
        }
        public LineString _CorrectPath()
        {
            var coordinates = new List<Coordinate>();
            //Groups = new List<List<Coordinate>>();
            var group = new List<Coordinate>();
            for (int i = 0; i < InitPath.Count; i++)
            {
                var p0 = InitPath[i];
                var p1 = InitPath[i+1];
                var lseg = new LineSegment(p0, p1);
                var envelop = new Envelope(p0, p1);
                var queried = WallEngine.Query(envelop).Where(l => l.Intersection(lseg)!= null);
                if (queried.Count() > 0)//新增不为空
                {
                    group.Add(p0);
                    group.Add(p1);
                    foreach(var l in queried)
                    {
                        group.Add(l.P0);
                        group.Add(l.P1);
                    }
                }
                else//新增为空，之前的组合为凸包
                {
                    if(group.Count > 3)
                    {
                        var hullCreator = new ConvexHull(group, Geometry.DefaultFactory);
                        var hull = hullCreator.GetConvexHull();
                        //Groups.Add(group.ToList());
                        if(hull is Polygon poly)
                        {
                            var ring = poly.Shell;//将环一分为二基于p0，p1。p0,p1必须在环上
                        }
                        
                    }
                    if(group.Count > 0) group.Clear();
                }
            }
            //if (group.Count > 0) Groups.Add(group.ToList());
            return null;
        }

        public (LineString, LineString) CorrectPath()
        {
            int startIdx = -1;//以该索引所在点 作为起点 更新路径
            int endIdx = 0;//以该索引所在点 作为终点 更新路径
            var coordinates = new HashSet<Coordinate>();
            for (int i = 0; i < InitPath.Count-1; i++)
            {
                var p0 = InitPath[i];
                var p1 = InitPath[i + 1];
                var lseg = new LineSegment(p0, p1);
                var envelop = new Envelope(p0, p1);
                var queried = WallEngine.Query(envelop).
                    Where(l => l.Intersection(lseg) != null && !l.EqualsTopologically(lseg));
                if (queried.Count() > 0)//新增不为空
                {
                    if(startIdx == -1)
                    {
                        startIdx = i;
                    }
                    endIdx = i + 1;
                    foreach (var l in queried)
                    {
                        var p0In = coordinates.Add(l.P0);
                        var p1In = coordinates.Add(l.P1);
                    }
                }
            }
            if (startIdx == -1) return (null,null);//无需修正
            var hull = new ConvexHull(coordinates, Geometry.DefaultFactory).GetConvexHull() as Polygon;
            return Split(hull.Shell, InitPath[startIdx], InitPath[endIdx]);
        }

        //从某一方向距离起始点最近的点转到距离终点(该方向反向)最近的点
        //再换一个方向
        private (LineString,LineString) Split(LinearRing hull,Coordinate p0,Coordinate p1)
        {
            var vec = new Vector2D(p0, p1);
            var coors = hull.Coordinates.Take(hull.Count - 1).ToList();
            return(GetLstr(coors,vec,p0),GetLstr(coors,vec,p1));
        }
        private LineString GetLstr(List<Coordinate> coorseq,Vector2D vector,Coordinate p0)
        {
            var coors = new List<Coordinate>();
            var cnt = coorseq.Count;
            var start = false;
            for(int i = 0; i < cnt*2; i++)
            {
                var coor = coorseq[i];
                var vec0 = new Vector2D(p0,coor);
                if(vector.CrossProduct(vec0) <= 0)
                {
                    if (!start) start = true;
                    else break;
                }
                if (start && vector.CrossProduct(vec0) > 0) coors.Add(coor);
            }
            return new LineString(coors.ToArray());
        }
        //private Stack<Coordinate> ShortestPath(List<Coordinate> coordinates, int startIdx,int endIdx)
        //{
        //    return null;
        //}
    }
}
