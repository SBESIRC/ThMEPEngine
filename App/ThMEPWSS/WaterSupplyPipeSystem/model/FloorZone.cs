using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;


namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class FloorZone
    {
        private Point3d StartPt { get; set; }
        private Point3d EndPt { get; set; }
        private List<double> LineXList { get; set; }
        public FloorZone(Point3d startPt, Point3d endPt, List<double> lineXList)
        {
            StartPt = startPt;
            EndPt = endPt;
            LineXList = lineXList;
        }
        public Point3d[] CreatePolyLine(double X1, double X2, double Y1, double Y2)
        {
            var ptls = new Point3d[5];
            ptls[0] = new Point3d(X1, Y1, 0);
            ptls[1] = new Point3d(X2, Y1, 0);
            ptls[2] = new Point3d(X2, Y2, 0);
            ptls[3] = new Point3d(X1, Y2, 0);
            ptls[4] = new Point3d(X1, Y1, 0);
            return ptls;
        }

        public List<Point3dCollection> CreateRectList()
        {
            var rectls = new List<Point3dCollection>();
            if (LineXList.Count == 0)
            {
                Point3d[] rect;
                rect = CreatePolyLine(StartPt.X, EndPt.X, StartPt.Y, EndPt.Y);
                rectls.Add(new Point3dCollection(rect));
                return rectls;
            }
            for (int i = 0; i < LineXList.Count + 1; i++)
            {
                Point3d[] rect;
                if (i == 0)
                {
                    rect = CreatePolyLine(StartPt.X, LineXList[i], StartPt.Y, EndPt.Y);
                }
                else if (i == LineXList.Count)
                {
                    rect = CreatePolyLine(LineXList[i - 1], EndPt.X, StartPt.Y, EndPt.Y);
                }
                else
                {
                    rect = CreatePolyLine(LineXList[i - 1], LineXList[i], StartPt.Y, EndPt.Y);
                }
                rectls.Add(new Point3dCollection(rect));
            }
            return rectls;
        }
    }
}
