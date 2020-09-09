using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Layout;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 确定单个需要布置区域的布置方式
    /// </summary>
    public class CalculateLayout
    {
        private SensorLayout LayoutSensor
        {
            get;
            set;
        }

        private PlaceInputProfileData m_placeInputProfileData = null; // 单个需要布置的区域

        private PlaceParameter m_parameter; // 用户界面输入的烟感、温感的参数信息

        private CalculateLayout(PlaceInputProfileData placeInputProfileData, PlaceParameter parameter)
        {
            m_placeInputProfileData = placeInputProfileData;
            m_parameter = parameter;
        }

        public static SensorLayout MakeLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter)
        {
            var layoutCalculate = new CalculateLayout(inputProfileData, parameter);
            layoutCalculate.DoCalculate();
            return layoutCalculate.LayoutSensor;
        }

        private void DoCalculate()
        {
            if (m_placeInputProfileData == null)
                LayoutSensor = null;

            var mainBeamProfile = m_placeInputProfileData.MainBeamOuterProfile;
            // 生成ABB多段线
            var postMinRect = ABBRectangle.MakeABBPolyline(mainBeamProfile);

            var areaAddRatio = (Math.Abs(postMinRect.Area) - Math.Abs(mainBeamProfile.Area)) / Math.Abs(mainBeamProfile.Area);

            // 判断布置方式
            if (m_placeInputProfileData.SecondBeamProfiles == null || m_placeInputProfileData.SecondBeamProfiles.Count == 0)
            {
                if (areaAddRatio < 0.1)
                {
                    // 矩形布置
                    LayoutSensor = new MainBeamRectangleLayout(m_placeInputProfileData, m_parameter, postMinRect);
                }
                else
                {
                    // 异形布置
                    LayoutSensor = new MainBeamPolygonLayout(m_placeInputProfileData, m_parameter, postMinRect);
                }
            }
            else if (m_placeInputProfileData.SecondBeamProfiles.Count > 0)
            {
                //主次梁分类计算布置方式
                if (areaAddRatio < 0.1)
                {
                    // 主次梁矩形布置
                    LayoutSensor = new MainSecondBeamRectangleLayout(m_placeInputProfileData, m_parameter, postMinRect);
                }
                else
                {
                    // 主次梁异形布置
                    LayoutSensor = new MainSecondBeamPolygonLayout(m_placeInputProfileData, m_parameter, postMinRect);
                }
            }
        }
    }
}
