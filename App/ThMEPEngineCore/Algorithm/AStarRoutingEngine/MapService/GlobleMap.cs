using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService
{
    public class GlobleMap<T> : Map<T>
    {
        double step = 800;
        double avoidHoleDistance = 800;
        double avoidFrameDistance = 200;
        public new GloblePoint startPt;
        public new GlobleMapHelper<T> mapHelper;
        public Dictionary<MPolygon, double> obstacleCast = new Dictionary<MPolygon, double>();  
        public new ThCADCoreNTSSpatialIndex ObstacleSpatialIndex;
        DBObjectCollection holeObjs;
        public GlobleMap(Polyline _polyline, Vector3d xDir, T _endInfo, double _step, double _avoidFrameDistance, double _avoidHoleDistance)
        {
            step = _step;
            avoidHoleDistance = _avoidHoleDistance;
            avoidFrameDistance = _avoidFrameDistance;
            endInfo = _endInfo;
            mapHelper = new GlobleMapHelper<T>(step);

            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            mapHelper.ucsMatrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            var clonePoly = _polyline.Clone() as Polyline;
            clonePoly.TransformBy(mapHelper.ucsMatrix);
            this.polyline = clonePoly.Buffer(-avoidFrameDistance)[0] as Polyline;

            //初始化地图
            CreateMap();

            //初始化洞口集合
            holeObjs = new DBObjectCollection();
        }

        /// <summary>
        /// 构建地图
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="xDir"></param>
        private void CreateMap()
        {
            var boundingBox = GetBoungdingBox(polyline);
            mapHelper.moveMatrix = Matrix3d.Displacement(boundingBox[0].GetAsVector());
            this.polyline.TransformBy(mapHelper.ucsMatrix.Inverse());
        }

        /// <summary>
        /// 设置起点、终点和洞口信息
        /// </summary> 
        /// <param name="_startPt"></param>
        /// <param name="_endPt"></param>
        public new void SetStartAndEndInfo(Point3d _startPt)
        {
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(holeObjs);
            this.startPt = mapHelper.SetStartAndEndGlobleInfo(_startPt, endInfo);
        }

        /// <summary>
        /// 判断是否再框线内
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsInBounds(GloblePoint cell)
        {
            Point3d cellPt = mapHelper.TransformMapPoint(cell);
            return polyline.Contains(cellPt);
        }

        public double GetObstacleWeight(GloblePoint cell)
        {
            double weight = 0;
            Point3d cellPt = mapHelper.TransformMapPoint(cell);
            var polyline = CreatePolyline(cellPt);
            var isObstacle = ObstacleSpatialIndex.SelectCrossingPolygon(polyline);
            foreach (MPolygon obs in isObstacle)
            {
                if (obstacleCast.Keys.Contains(obs))
                {
                    var obsWeight = obstacleCast[obs];
                    if (obsWeight > weight)
                    {
                        weight = obsWeight;
                    }
                }
            }
            polyline.Dispose();
            return weight;
        }

        /// <summary>
        /// 设置障碍
        /// </summary>
        /// <param name="holes"></param>
        public void SetObstacle(List<Polyline> _holes, double Weight)
        {
            var holes = _holes.SelectMany(x => x.Buffer(avoidHoleDistance).Cast<Polyline>()).ToList();

            foreach (var h in holes)
            {
                var MPolygon = h.ToNTSPolygon().ToDbMPolygon();
                holeObjs.Add(MPolygon);
                obstacleCast.Add(MPolygon, Weight);
            }
        }

        /// <summary>
        /// 构建path
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Polyline CreatePath(List<GloblePoint> points)
        {
            if (points == null || points.Count <= 0)
            {
                return null;
            }
            Polyline path = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                Point3d cellPt = mapHelper.TransformMapPoint(points[i]);
                path.AddVertexAt(0, cellPt.ToPoint2D(), 0, 0, 0);
            }

            if (path.NumberOfVertices <= 1)
            {
                return null;
            }
            return path.DPSimplify(1);
        }
    }
}
