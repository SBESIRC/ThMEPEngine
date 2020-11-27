using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.Model;
using ThMEPElectrical.PostProcess.HoleAdjustor;

namespace ThMEPElectrical.Layout.MBeamLayout
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

            var layoutData = new LayoutProfileData(m_inputProfileData.MainBeamOuterProfile, m_postMainBeamPoly);
            // 单个布置
            m_placePoints = MultiSegmentPlace.MakeABBPolygonProfilePoints(layoutData, m_parameter);
            m_placePoints = IsolatedHoleAdjustor.MakeIsolatedHoleAdjustor(m_placePoints, m_inputProfileData);
            return PlacePoints;
        }

        public MainBeamPolygonLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly)
            : base(inputProfileData, parameter, postPoly)
        {
            ;
        }
    }
}
