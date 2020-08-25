using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Geometry
{
    public class CoordinateTransform
    {
        private List<Matrix3d> m_InverseMatrixs = new List<Matrix3d>(); // 逆矩阵变换
        private Polyline m_Poly; // 原始的多段线

        private List<Matrix3d> m_TransMatrixs = new List<Matrix3d>(); // 正矩阵转换
        private Polyline m_postPoly; // 坐标转换后的


        public List<Matrix3d> TransMatrixs
        {
            get { return m_TransMatrixs; }
        }

        public Polyline TransPolyline
        {
            get { return m_postPoly; }
        }

        public List<Matrix3d> InverseMatrixs
        {
            get { return m_InverseMatrixs; }
        }

        public CoordinateTransform(Polyline poly)
        {
            m_Poly = poly;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public void DataTrans()
        {
            if (m_Poly.NumberOfVertices < 4)
                return;

            var lineFir = m_Poly.GetLineSegmentAt(0).Line3dLine();
            var lineFirDir = (lineFir.EndPoint - lineFir.StartPoint).GetNormal();

            var lineSec = m_Poly.GetLineSegmentAt(1).Line3dLine();
            var lineSecDir = (lineSec.EndPoint - lineSec.StartPoint).GetNormal();
            var matrix = Matrix3d.AlignCoordinateSystem(lineFir.EndPoint, lineFirDir, lineSecDir, Vector3d.ZAxis, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
            m_TransMatrixs.Add(matrix);
            var transPoly = m_Poly.GetTransformedCopy(matrix) as Polyline;
            //DrawUtils.DrawProfile(new List<Curve>() { profile }, "transPoly");
            m_InverseMatrixs.Insert(0, matrix.Inverse());

             Move(transPoly);
            //DrawUtils.DrawProfile(new List<Curve>() { movePoly }, "movePoly");
        }

                /// <summary>
        /// 平移
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        private void Move(Polyline poly)
        {
            var ptLst = poly.Polyline2Point2d();
            var xLst = ptLst.Select(e => e.X).ToList();
            var yLst = ptLst.Select(e => e.Y).ToList();
            var gap = 100;
            var xMin = xLst.Min() - gap;
            var yMin = yLst.Min() - gap;

            var vec = Point3d.Origin - new Point3d(xMin, yMin, 0);
            var matrix = Matrix3d.Displacement(vec);
            m_TransMatrixs.Add(matrix);
            m_postPoly = poly.GetTransformedCopy(matrix) as Polyline;
            m_InverseMatrixs.Insert(0, matrix.Inverse());
        }
    }
}
