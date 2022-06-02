using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService
{
    public class Map<T>
    {
        double step = 800;
        double avoidHoleDistance = 800;
        double avoidFrameDistance = 200;
        public Polyline polyline = null; //外包框
        List<Polyline> holes = null;
        List<Line> rooms = null;
        int columns = 0;
        int rows = 0;
        public T endInfo;
        public Point startPt;
        public MapHelper<T> mapHelper;
        public List<Point> cells = new List<Point>();
        public Dictionary<Point, double> roomCast = new Dictionary<Point, double>();
        public bool[][] obstacles = null; //障碍物位置，维度：Column * Line    
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex;
        public Map() { }

        public Map(Polyline _polyline, Vector3d xDir, T _endInfo, double _step, double _avoidFrameDistance, double _avoidHoleDistance)
        {
            step = _step;
            avoidHoleDistance = _avoidHoleDistance;
            avoidFrameDistance = _avoidFrameDistance;
            endInfo = _endInfo;
            if (typeof(T) == typeof(Line))
            {
                mapHelper = new ToCurveMapHelper(step) as MapHelper<T>;
            }
            else if (typeof(T) == typeof(Point3d))
            {
                mapHelper = new ToPointMapHelper(step) as MapHelper<T>;
            }

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
        }

        /// <summary>
        /// 设置障碍
        /// </summary>
        /// <param name="holes"></param>
        public void SetObstacle(List<Polyline> _holes)
        {
            holes = _holes.SelectMany(x => x.Buffer(avoidHoleDistance).Cast<Polyline>()).ToList();

            DBObjectCollection dbObjColl = new DBObjectCollection();
            foreach(var h in holes)
            {
                var MPolygon = h.ToNTSPolygon().ToDbMPolygon();
                dbObjColl.Add(MPolygon);
            }

            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjColl);
        }

        /// <summary>
        /// 主要是为了解决c类型的ployline做Buffer以后，变成了两根ployline
        /// </summary>
        /// <param name="_holes"></param>
        public void SetObstacle2(List<Polyline> _holes)
        {
            var mPolygons = _holes.SelectMany(x => x.Buffer(avoidHoleDistance,true).Cast<MPolygon>()).ToList();
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(mPolygons.ToCollection());
        }

        /// <summary>
        /// 设置房间
        /// </summary>
        /// <param name="_rooms"></param>
        public void SetRoom(List<Line> _rooms)
        {
            rooms = _rooms;
        }

        /// <summary>
        /// 设置起点和终点信息
        /// </summary> 
        /// <param name="_startPt"></param>
        /// <param name="_endPt"></param>
        public void SetStartAndEndInfo(Point3d _startPt)
        {
            this.startPt = (Point)mapHelper.SetStartAndEndInfo(_startPt, endInfo);
            if(rooms != null)
            {
                InitRoom();
            }
        }

        public Polyline CreatePolyline(Point3d pt, int tolerance = 10)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(pt.X - tolerance, pt.Y - tolerance)); // low left
            pts.Add(new Point2d(pt.X - tolerance, pt.Y + tolerance)); // high left
            pts.Add(new Point2d(pt.X + tolerance, pt.Y + tolerance)); // high right
            pts.Add(new Point2d(pt.X + tolerance, pt.Y - tolerance)); // low right
            pts.Add(new Point2d(pt.X - tolerance, pt.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        public bool IsInBounds(Point cell)
        {
            Point3d cellPt = mapHelper.TransformMapPoint(cell);
            return polyline.Contains(cellPt);
        }

        public bool IsObstacle(Point cell)
        {
            Point3d cellPt = mapHelper.TransformMapPoint(cell);
            var polyline = CreatePolyline(cellPt);
            var isObstacle = ObstacleSpatialIndex.Intersects(polyline, true);
            polyline.Dispose();
            return isObstacle;
        }

        public bool IsRoomWell(Point cell1,Point cell2)
        {
            Point3d cellPt1 = mapHelper.TransformMapPoint(cell1);
            Point3d cellPt2 = mapHelper.TransformMapPoint(cell2);
            var line = new Line(cellPt1, cellPt2);
            foreach (var room in rooms)
            {
                if (room is Line)
                {
                    var l = room as Line;
                    if (l.Length < 10.0)
                    {
                        continue;
                    }
                    if(l.IsIntersects(line))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void InitRoom()
        {
            Dictionary<DBPoint, Point> pointCellDic = new Dictionary<DBPoint, Point>();
            foreach (var cell in cells)
            {
                Point3d cellPt = mapHelper.TransformMapPoint(cell);
                var dbpt = new DBPoint(cellPt);
                pointCellDic.Add(dbpt, cell);
            }
            var cellIndex = new ThCADCoreNTSSpatialIndex(pointCellDic.Keys.ToCollection());

            foreach(var room in rooms)
            {
                if(room is Line)
                {
                    var l = room as Line;
                    if(l.Length < 10.0)
                    {
                        continue;
                    }
                    var frame = l.Buffer(300);
                    var objs = cellIndex.SelectCrossingPolygon(frame);
                    foreach(var obj in objs)
                    {
                        if(!roomCast.ContainsKey(pointCellDic[obj as DBPoint]))
                        {
                            roomCast.Add(pointCellDic[obj as DBPoint], 10);
                        }
                    }
                }
            }

            foreach(var item in pointCellDic)
            {
                item.Key.Dispose();
            }
        }

        /// <summary>
        /// 构建path
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Polyline CreatePath(List<Point> points)
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

        /// <summary>
        /// 是否包含其他点
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool ContainsPt(Point point)
        {
            return cells.Any(x => x.X == point.X && x.Y == point.Y);
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
            Point3d minPt = boundingBox[0].TransformBy(mapHelper.moveMatrix.Inverse());
            Point3d maxPt = boundingBox[1].TransformBy(mapHelper.moveMatrix.Inverse());
            this.polyline.TransformBy(mapHelper.ucsMatrix.Inverse());

            //----规划地图尺寸----
            double xValue = Math.Abs(maxPt.X - minPt.X);
            xValue = xValue <= 10 ? step : xValue;
            double yValue = Math.Abs(maxPt.Y - minPt.Y);
            yValue = yValue <= 10 ? step : yValue;
            int _columns = Convert.ToInt32(Math.Ceiling(xValue / step)) + 3;
            int _rows = Convert.ToInt32(Math.Ceiling(yValue / step)) + 3;
            columns = _columns;
            rows = _rows;

            for (int i = 0; i < _columns; i++)
            {
                for (int j = 0; j < _rows; j++)
                {
                    cells.Add(new Point(i, j));
                }
            }

            InitializeObstacles();
        }

        /// <summary>
        /// 将所有位置均标记为无障碍物。
        /// </summary>
        private void InitializeObstacles()
        {
            this.obstacles = new bool[columns][];
            for (int i = 0; i < columns; i++)
            {
                this.obstacles[i] = new bool[rows];
            }

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    this.obstacles[i][j] = false;
                }
            }
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        protected List<Point3d> GetBoungdingBox(Polyline polyline)
        {
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allPts.Add(polyline.GetPoint3dAt(i));
            }

            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            List<Point3d> boundingbox = new List<Point3d>();
            boundingbox.Add(new Point3d(minX, minY, 0));
            boundingbox.Add(new Point3d(maxX, maxY, 0));

            return boundingbox;
        }
    }
}

