using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.Model;
using ThMEPElectrical.Business.MainSecondBeam;
using ThCADCore.NTS;

namespace ThMEPElectrical.Layout.MainSecBeamLayout
{
    //次梁异形处理
    public class MainSecondBeamPolygonLayout : MainSecondBeamLayout
    {
        public MainSecondBeamPolygonLayout(PlaceInputProfileData inputProfileDatas, PlaceParameter parameter, Polyline poly)
            : base(inputProfileDatas, parameter)
        {
            PostPoly = poly;
        }

        public override List<Point3d> CalculatePlace()
        {
            if (m_inputProfileData == null || m_parameter == null)
                return new List<Point3d>();

            var mainBeamOuterProfile = m_inputProfileData.MainBeamOuterProfile;
             if (mainBeamOuterProfile.Buffer(ThMEPCommon.ShrinkDistance).Count < 1)
                return new List<Point3d>();

            var layoutData = new LayoutProfileData(mainBeamOuterProfile, PostPoly);

            // 主次梁异形布置
            m_placePoints = MSBeamNoRegularPlacer.MakeMSNoRegularPlacer(layoutData, m_parameter, m_inputProfileData);

            return PlacePoints;
        }
    }
}
