using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.Assistant;
using ThCADCore.NTS;
using ThMEPElectrical.PostProcess;
using ThMEPElectrical.Business.MainSecondBeam;
using ThMEPElectrical.PostProcess.HoleAdjustor;

namespace ThMEPElectrical.Layout.MainSecBeamLayout
{
    /// <summary>
    /// 主次梁矩形
    /// </summary>
    public class MainSecondBeamRectLayout : MainSecondBeamLayout
    {
        public MainSecondBeamRectLayout(PlaceInputProfileData inputProfileDatas, PlaceParameter parameter, Polyline poly)
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

            // ABB主次梁矩形布置
            m_placePoints = MSBeamRectPlacer.MakeMSBeamRectPlacer(layoutData, m_parameter, m_inputProfileData);
            m_placePoints = IsolatedHoleAdjustor.MakeIsolatedHoleAdjustor(m_placePoints, m_inputProfileData);
            return m_placePoints;
        }
    }
}
