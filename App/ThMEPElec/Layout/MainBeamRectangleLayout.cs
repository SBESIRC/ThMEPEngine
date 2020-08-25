using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Model;
using ThMEPElectrical.Business;

namespace ThMEPElectrical.Layout
{
    // 主梁矩形布置
    public class MainBeamRectangleLayout : MainBeamLayout
    {
        public override List<Point3d> CalculatePlace()
        {
            if (m_inputProfileData == null || m_parameter == null)
                return null;

            var mainBeamProfile = m_inputProfileData.MainBeamOuterProfile;
            var postPoly = m_postMainBeamPoly;

            
            var layoutData = new LayoutProfileData(m_inputProfileData.MainBeamOuterProfile, m_postMainBeamPoly);
            // 单个布置
            m_placePoints = RectProfilePlace.MakeRectProfilePlacePoints(layoutData, m_parameter);

            return PlacePoints;
        }

        public MainBeamRectangleLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly)
            : base(inputProfileData, parameter, postPoly)
        {
            ;
        }
    }
}
