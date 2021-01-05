using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class ScatterLines
    {
        private List<Line> m_lines;

        public ScatterLines(List<Line> lines)
        {
            m_lines = lines;
        }

        private List<List<Point2d>> m_pointNodeLst = new List<List<Point2d>>();

        private List<Line> m_geneLines = new List<Line>();

        public List<Line> Lines
        {
            get { return m_geneLines; }
        }

        public static List<Line> MakeNewLines(List<Line> srcLines)
        {
            var scatterLines = new ScatterLines(srcLines);
            scatterLines.Do();
            return scatterLines.Lines;
        }

        public void Do()
        {
            CalculateLinePoints();

            //求交
            IntersectLines();

            // 排序
            SortXYZPoints();

            // 生成新的直线
            NewLines();
        }

        private void CalculateLinePoints()
        {
            foreach (var line in m_lines)
            {
                var linePoints = new List<Point2d>();
                linePoints.Add(line.StartPoint.ToPoint2D());
                linePoints.Add(line.EndPoint.ToPoint2D());
                m_pointNodeLst.Add(linePoints);
            }
        }

        /// <summary>
        /// 求交点
        /// </summary>
        private void IntersectLines()
        {
            for (int i = 0; i < m_lines.Count; i++)
            {
                var curLine = Line2Line2d(m_lines[i]);
                for (int j = i + 1; j < m_lines.Count; j++)
                {
                    var nextLine = Line2Line2d(m_lines[j]);

                    var intersectPts = curLine.IntersectWith(nextLine, new Tolerance(1e-3, 1e-3));
                    if (intersectPts != null && intersectPts.Count() == 1)
                    {
                        m_pointNodeLst[i].AddRange(intersectPts);
                        m_pointNodeLst[j].AddRange(intersectPts);
                    }
                }
            }
        }

        private LineSegment2d Line2Line2d(Line line)
        {
            return new LineSegment2d(line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D());
        }

        /// <summary>
        /// 排序
        /// </summary>
        private void SortXYZPoints()
        {
            foreach (var pointNode in m_pointNodeLst)
            {
                var firstPoint = pointNode.First();
                pointNode.Sort((s1, s2) => { return s1.GetDistanceTo(firstPoint).CompareTo(s2.GetDistanceTo(firstPoint)); });
            }
        }

        /// <summary>
        /// 生成新的直线
        /// </summary>
        private void NewLines()
        {
            foreach (var points in m_pointNodeLst)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var curPoint = points[i];

                    if (i + 1 == points.Count)
                        break;

                    var nextPoint = points[i + 1];
                    try
                    {
                        if ((curPoint - nextPoint).Length > 1e-5)
                        {
                            m_geneLines.Add(new Line(curPoint.ToPoint3d(), nextPoint.ToPoint3d()));
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

    }
}
