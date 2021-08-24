using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Common;


namespace ThMEPLighting.EmgLight.Model
{
    public class ThLane
    {

        private List<Line> m_laneTrans;
        private Polyline m_headProtectPoly;
        private Polyline m_endProtectPoly;


        public ThLane(List<Line> lane)
        {
            geom = lane;
            dir = (geom.Last().EndPoint - geom.First().StartPoint).GetNormal();
            length = geom.Sum(x => x.Length);
            matrix = GeomUtils.getLineMatrix(geom.First().StartPoint, geom.Last().EndPoint);
        }

        public List<Line> geom { get; }

        public double length { get; }

        public Vector3d dir { get; }

        public Matrix3d matrix { get; }

        public List<Line> laneTrans
        {
            get
            {
                if (m_laneTrans == null || m_laneTrans.Count == 0)
                {
                    m_laneTrans = geom.Select(x => new Line(TransformPointToLine(x.StartPoint), TransformPointToLine(x.EndPoint))).ToList();
                }
                return m_laneTrans;
            }
        }

        public Polyline headProtectPoly
        {
            get
            {
                if (m_headProtectPoly == null)
                {
                    m_headProtectPoly = GeomUtils.CreateExtendPoly(geom.First().StartPoint, dir, EmgLightCommon.TolLaneProtect, EmgLightCommon.TolLaneProtect);
                }
                return m_headProtectPoly;
            }
        }

        public Polyline endProtectPoly
        {
            get
            {
                if (m_endProtectPoly == null)
                {
                    m_endProtectPoly = GeomUtils.CreateExtendPoly(geom.Last().EndPoint, dir, EmgLightCommon.TolLaneProtect, EmgLightCommon.TolLaneProtect);
                }
                return m_endProtectPoly;
            }
        }

        public Point3d TransformPointToLine(Point3d pt)
        {
            var transedPt = pt.TransformBy(matrix.Inverse());
            return transedPt;
        }

        /// <summary>
        /// 车道线往前做框buffer
        /// </summary>
        /// <param name="tol"></param>
        /// <returns></returns>
        public List<Line> LaneHeadExtend(double tol)
        {
            var moveDir = (geom.First().EndPoint - geom.First().StartPoint).GetNormal();
            var ExtendLineStart = geom.First().StartPoint - moveDir * tol;
            var ExtendLineEnd = geom.First().StartPoint + moveDir * tol;
            var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
            var ExtendLineList = new List<Line>();
            ExtendLineList.Add(ExtendLine);

            return ExtendLineList;
        }

    }
}

