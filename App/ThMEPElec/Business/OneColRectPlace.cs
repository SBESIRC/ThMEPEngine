using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.PostProcess.Adjustor;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 一列矩形布置
    /// </summary>
    class OneColRectPlace
    {
        private PlaceParameter m_parameter;

        private PlaceRect m_placeRectInfo;

        /// <summary>
        /// ABB坐标系一列计算
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="placeRectInfo"></param>
        /// <returns></returns>
        public static List<Point3d> MakeOneColPlaceRect(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            var oneColPlace = new OneColRectPlace(parameter, placeRectInfo);
            return oneColPlace.DoPlace();
        }

        private OneColRectPlace(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            m_parameter = parameter;
            m_placeRectInfo = placeRectInfo;
        }

        private List<Point3d> DoPlace()
        {
            var pts = OneColPlace();
            return pts;
        }

        private List<Point3d> OneColPlace()
        {
            var ptLst = new List<Point3d>();
            var moveDis = m_placeRectInfo.BottomLine.Length * 0.5;
            var verticalMidLine = GeomUtils.MoveLine(m_placeRectInfo.LeftLine, Vector3d.XAxis, moveDis);

            var leftBottomPt = m_placeRectInfo.LeftBottomPt;
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftTopPt = m_placeRectInfo.LeftTopPt;

            // 左下顶点
            var leftBottomCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var leftBottomFirstPtNode = CalculateIntersectPt(leftBottomCircle, verticalMidLine);
            if (!leftBottomFirstPtNode.HasValue)
                return ptLst;

            var leftBottomFirstPt = leftBottomFirstPtNode.Value;

            // 左上顶点
            var leftTopCircle = new Circle(leftTopPt, Vector3d.ZAxis, vertexProtectRadius);
            var leftTopLastPtNode = CalculateIntersectPt(leftTopCircle, verticalMidLine);
            if (!leftTopLastPtNode.HasValue)
                return ptLst;

            var leftTopLastPt = leftTopLastPtNode.Value;
            var verticalMaxGap = m_parameter.ProtectArea / 4.0 / moveDis * 2;

            // 计算垂直间隔长度
            var verticalLength = (leftTopLastPt - leftBottomFirstPt).Length;
            var verticalCount = Math.Ceiling(verticalLength / verticalMaxGap);
            var verticalPosGap = verticalLength / verticalCount;

            ptLst.Add(leftBottomFirstPt);
            for (int i = 1; i < verticalCount; i++)
            {
                var moveGap = i * verticalPosGap;
                var pt = leftBottomFirstPt + Vector3d.YAxis * moveGap;
                ptLst.Add(pt);
            }

            ptLst.Add(leftTopLastPt);

            if (ptLst.Count == 2)
            {
                ptLst = RegularPlacePointAdjustor.MakeRegularPlacePointAdjustor(verticalMidLine, ptLst);
            }

            return ptLst;
        }

        private Point3d? CalculateIntersectPt(Circle circle, Line line)
        {
            var ptLst = new Point3dCollection();
            circle.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return ptLst[0];
            }
            else if (ptLst.Count == 0)
            {
                return null;
            }
            else
            {
                return ptLst[0];
            }
        }
    }
}
