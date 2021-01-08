using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class CoordinateTransform
    {
        private List<Line> m_lines;

        public Matrix3d m_matrix3D;

        public List<Line> TransLines
        {
            get;
            set;
        } = new List<Line>();

        public CoordinateTransform(List<Line> lines)
        {
            m_lines = lines;
        }

        public void Do()
        {
            CalculateMatrix();

            DataTransform();
        }

        private void CalculateMatrix()
        {
            var xLine = m_lines.First();
            var xDir = (xLine.EndPoint - xLine.StartPoint).GetNormal();
            var yDir = xDir.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            m_matrix3D = Matrix3d.AlignCoordinateSystem(xLine.EndPoint, xDir, yDir, Vector3d.ZAxis, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
        }

        private void DataTransform()
        {
            foreach (var srcLine in m_lines)
            {
                TransLines.Add(srcLine.GetTransformedCopy(m_matrix3D) as Line);
            }
        }
    }
}
