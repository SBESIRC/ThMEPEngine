using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using ThMEPElectrical.Layout;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 根据输入参数主次梁的信息和界面输入约束参数进行布置
    /// </summary>
    public class CalculatePlacePoint
    {
        private List<PlaceInputProfileData> m_placeInputDatas = null;
        private PlaceParameter m_placeParameter = null;

        private List<Point3d> m_placePoints = new List<Point3d>();
        private List<SensorLayout> m_layoutLst = new List<SensorLayout>(); // 布置映射关系

        public List<Point3d> PlacePoints
        {
            get { return m_placePoints; }
        }

        public static List<Point3d> MakeCalculatePlacePoints(List<PlaceInputProfileData> inputDatas, PlaceParameter parameter)
        {
            var calculatePlacePoint = new CalculatePlacePoint(inputDatas, parameter);
            calculatePlacePoint.DoPlace();
            return calculatePlacePoint.PlacePoints;
        }

        private CalculatePlacePoint(List<PlaceInputProfileData> inputDatas, PlaceParameter parameter)
        {
            m_placeInputDatas = inputDatas;
            m_placeParameter = parameter;
        }

        /// <summary>
        /// 布置计算
        /// </summary>
        private void DoPlace()
        {
            // 计算布置方式
            CalculateLayoutMaps();

            // 计算插入点
            CalculateLayoutPoints();
        }

        /// <summary>
        /// 计算布置方式
        /// </summary>
        private void CalculateLayoutMaps()
        {
            if (m_placeInputDatas == null || m_placeInputDatas.Count == 0)
                return;

            foreach (var singlePlaceInputData in m_placeInputDatas)
            {
                var layout = SelectLayout(singlePlaceInputData, m_placeParameter);

                if (layout != null)
                    m_layoutLst.Add(layout);
            }
        }

        /// <summary>
        /// 计算布置点计算
        /// </summary>
        private void CalculateLayoutPoints()
        {
            if (m_layoutLst.Count == 0)
                return;

            // 布置
            foreach (var singleLayout in m_layoutLst)
            {
                if (singleLayout is MainBeamRectangleLayout rectLayout)
                {
                    var pts = rectLayout.CalculatePlace();

                    if (pts != null && pts.Count != 0)
                    {
                        m_placePoints.AddRange(pts);
                    }
                }
                else if (singleLayout is MainBeamPolygonLayout polygonLayout)
                {
                    var pts = polygonLayout.CalculatePlace();

                    if (pts != null && pts.Count != 0)
                    {
                        m_placePoints.AddRange(pts);
                    }
                }
                else if (singleLayout is MainSecondBeamLayout secondBeamLayout)
                {
                    var pts = secondBeamLayout.CalculatePlace();

                    if (pts != null && pts.Count != 0)
                    {
                        m_placePoints.AddRange(pts);
                    }
                }
            }
        }

        /// <summary>
        /// 计算布局方式
        /// </summary>
        /// <param name="placeData"></param>
        /// <returns></returns>
        private SensorLayout SelectLayout(PlaceInputProfileData placeData, PlaceParameter parameter)
        {
            var layout = CalculateLayout.MakeLayout(placeData, parameter);
            return layout;
        }
    }
}
