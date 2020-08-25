using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Business;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout
{
    /// <summary>
    /// 主梁异形布置
    /// </summary>
    public class MainBeamPolygonLayout : MainBeamLayout
    {
        public override List<Point3d> CalculatePlace()
        {
            if (m_inputProfileData == null || m_parameter == null)
                return null;

            var mainBeamProfile = m_inputProfileData.MainBeamOuterProfile;
            var postPoly = m_postMainBeamPoly;


            var layoutData = new LayoutProfileData(m_inputProfileData.MainBeamOuterProfile, m_postMainBeamPoly);
            // 单个布置
            m_placePoints = PolygonProfilePlace.MakePolygonProfilePoints(layoutData, m_parameter);

            return PlacePoints;
        }

        public MainBeamPolygonLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly)
            : base(inputProfileData, parameter, postPoly)
        {
            ;
        }
    }
}
