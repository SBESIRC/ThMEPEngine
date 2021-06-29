using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThCreateHydrantPathService
    {
        private double HydrantAngle;
        private Point3d StartPoint;
        private List<Line> Terminationlines;
        private List<Polyline> ObstaclePolylines;
        
        public ThCreateHydrantPathService()
        {
            HydrantAngle = 0.0;
            StartPoint = new Point3d();
            Terminationlines = new List<Line>();
            ObstaclePolylines = new List<Polyline>();
        }
        public void SetObstacle(Polyline polyline)
        {
            ObstaclePolylines.Add(polyline);
        }
        public void SetTermination(Line line)
        {
            Terminationlines.Add(line);
        }
        public void SetTermination(List<Line> lines)
        {
            Terminationlines.AddRange(lines);
        }
        public void SetHydrantAngle(double angle)
        {
            HydrantAngle = angle;
        }
        public void SetStartPoint(Point3d pt)
        {
            StartPoint = pt;
        }
        public Polyline CreateHydrantPath(bool flag)
        {
            Polyline hydrantPath = new Polyline();
            hydrantPath.AddVertexAt(0, StartPoint.ToPoint2D(), 0, 0, 0);
            
            if(flag) //有消火栓
            {
                //计算支管开始方向，沿着该方向走500
            } 
            else
            {
                //构造起始点和终止点直线
                //判断是否与障碍物有交点
                //如果无交点，返回路径
                //如果有交点，避开障碍物
            }

            return hydrantPath; 
        }
        public Point3d Plane(Point3d strartPoint)
        {
            Point3d point = new Point3d();
            return point;
        }
    }
}
