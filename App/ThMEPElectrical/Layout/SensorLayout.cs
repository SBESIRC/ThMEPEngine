using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout
{
    /// <summary>
    /// 温感布置， 有一些成员变量
    /// </summary>
    public abstract class SensorLayout : ILayout
    {
        protected List<Point3d> m_placePoints; //布置点集

        protected PlaceInputProfileData m_inputProfileData; // 梁跨区域

        protected PlaceParameter m_parameter; // 界面用户输入参数

        public List<Point3d> PlacePoints
        {
            get { return m_placePoints; }
        }

        public abstract List<Point3d> CalculatePlace();


        public SensorLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter)
        {
            m_placePoints = new List<Point3d>();

            m_inputProfileData = inputProfileData;

            m_parameter = parameter;
        }
    }
}
