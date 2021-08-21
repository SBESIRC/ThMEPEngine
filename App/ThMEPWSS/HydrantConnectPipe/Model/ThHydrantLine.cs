using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantPoint
    {
        public int ConnectPtCount = 0;//连接点数量
        public Point3d _pt { set; get; }
        public ThHydrantPoint()
        {
            _pt = new Point3d(0.0, 0.0, 0.0);
        }
        public ThHydrantPoint(Point3d pt)
        {
            _pt = pt;
        }
    }
    public class ThHydrantLine
    {
        public List<DBObject> Dbj { set; get; }
        public ThHydrantPoint StartPt { set; get; }
        public ThHydrantPoint EndPt { set; get; }
        public ThHydrantLine()
        {
            Dbj = new List<DBObject>();
            StartPt = new ThHydrantPoint();
            EndPt = new ThHydrantPoint();
        }
    }
}
