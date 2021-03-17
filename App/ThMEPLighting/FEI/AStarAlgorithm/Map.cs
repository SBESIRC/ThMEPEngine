using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    public class Map
    {
        double step = 800;
        Polyline polyline = null; //外包框
        List<Polyline> holes = null;
        MapService mapService;
        int columns = 0;
        int rows = 0;
        public Point startPt;
        public Point endPt;
        public List<Point> cells = new List<Point>();
        public bool[][] obstacles = null; //障碍物位置，维度：Column * Line    

        public Map(Polyline _polyline, Vector3d xDir, double _step = 800)
        {
            step = _step;

            //初始化地图服务
            mapService = new MapService(step);

            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            mapService.ucsMatrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            var clonePoly = _polyline.Clone() as Polyline;
            clonePoly.TransformBy(mapService.ucsMatrix.Inverse());
            this.polyline = clonePoly;

            //初始化地图
            CreateMap();
        }

        /// <summary>
        /// 设置障碍
        /// </summary>
        /// <param name="holes"></param>
        public void SetObstacle(List<Polyline> _holes)
        {
            holes = _holes;
            holes = holes.SelectMany(x => x.Buffer(5).Cast<Polyline>()).ToList();

            foreach (var cell in cells)
            {
                Point3d cellPt = mapService.TransformMapPointByOriginMap(cell);
                if (!polyline.Contains(cellPt))
                {
                    this.obstacles[cell.X][cell.Y] = true;
                    continue;
                }
                foreach (var hole in holes)
                {
                    if (hole.Contains(cellPt))
                    {
                        this.obstacles[cell.X][cell.Y] = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 设置起点和终点
        /// </summary> 
        /// <param name="_startPt"></param>
        /// <param name="_endPt"></param>
        public void SetStartAndEndPoint(Point3d _startPt, Point3d _endPt)
        {
            Point3d transSP = _startPt.TransformBy(mapService.ucsMatrix.Inverse()).TransformBy(mapService.moveMatrix.Inverse());
            Point3d transEP = _endPt.TransformBy(mapService.ucsMatrix.Inverse()).TransformBy(mapService.moveMatrix.Inverse());
            var valueLst = mapService.SetMapServiceInfo(transSP, transEP);
            int sPx = valueLst[0];
            int sPy = valueLst[1];
            int ePx = valueLst[2];
            int ePy = valueLst[3];

            this.startPt = new Point(sPx, sPy);    //初始化起点
            this.endPt = new Point(ePx, ePy);   //初始化终点

            //重新检测障碍物
            int minX = sPx < ePx ? sPx : ePx;
            int maxX = sPx >= ePx ? sPx : ePx;
            int minY = sPy < ePy ? sPy : ePy;
            int maxY = sPy >= ePy ? sPy : ePy;
            List<Point> checkPts = new List<Point>();
            for (int i = columns - 1; i >= 0; i--)
            {
                for (int j = rows - 1; j >= 0; j--)
                {
                    if (i == minX || i == maxX)
                    {
                        checkPts.Add(new Point(i, j));
                    }
                    if (j == minY || j == maxY)
                    {
                        checkPts.Add(new Point(i, j));
                    }

                    if (i - 1 < 0 || i - 2 < 0 || j - 1 < 0 || j - 2 < 0)
                    {
                        checkPts.Add(new Point(i, j));
                        continue;
                    }

                    int changeI = i;
                    int changeJ = j;
                    if (i >= minX && i < maxX)
                    {
                        changeI = i - 1;
                    }
                    else if (i >= maxX)
                    {
                        changeI = i - 2; 
                    }
                    if (j >= minY && j < maxY)
                    {
                        changeJ = j - 1;
                    }
                    else if (j >= maxY)
                    {
                        changeJ = j - 2;
                    }
                    this.obstacles[i][j] = this.obstacles[changeI][changeJ];
                }
            }
            foreach (var pt in checkPts)
            {
                this.obstacles[pt.X][pt.Y] = false;
                Point3d cellPt = mapService.TransformMapPoint(pt);
                if (!polyline.Contains(cellPt))
                {
                    this.obstacles[pt.X][pt.Y] = true;
                    continue;
                }
                foreach (var hole in holes)
                {
                    if (hole.Contains(cellPt))
                    {
                        this.obstacles[pt.X][pt.Y] = true;
                        break;
                    }
                }
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
                Point3d cellPt = mapService.TransformMapPoint(points[i]);
                path.AddVertexAt(0, cellPt.ToPoint2D(), 0, 0, 0);
            }

            return path;
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
            Point3d minPt = boundingBox[0];
            Point3d maxPt = boundingBox[1];
            mapService.moveMatrix = Matrix3d.Displacement(boundingBox[0].GetAsVector());

            //----规划地图尺寸----
            double xValue = Math.Abs(maxPt.X - minPt.X);
            xValue = xValue <= 10 ? 800 : xValue;
            double yValue = Math.Abs(maxPt.Y - minPt.Y);
            yValue = yValue <= 10 ? 800 : yValue;
            int _columns = Convert.ToInt32(Math.Ceiling(xValue / step)) + 2;
            int _rows = Convert.ToInt32(Math.Ceiling(yValue / step)) + 2;
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
        private List<Point3d> GetBoungdingBox(Polyline polyline)
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

