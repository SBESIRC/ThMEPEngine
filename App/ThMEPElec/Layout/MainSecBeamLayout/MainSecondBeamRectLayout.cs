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

            var layoutData = new LayoutProfileData(m_inputProfileData.MainBeamOuterProfile, PostPoly);

            // ABB矩形布置
             m_placePoints = RectProfilePlace.MakeABBRectProfilePlacePoints(layoutData, m_parameter);

            // 计算有效的可布置区域
            var mainBeamSpanRegion = CalculateBeamSpanRegion(m_inputProfileData, m_placePoints);

            if (m_placePoints.Count == 1)
            {
                m_placePoints = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.SINGLEPLACE);
            }
            else if (m_placePoints.Count == 2)
            {
                m_placePoints = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.MEDIUMPLACE);
            }
            else if (m_placePoints.Count == 4)
            {
                m_placePoints = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.LARGEPLACE);
            }

            return m_placePoints;
        }

        /// <summary>
        /// 计算有效的可布置区域
        /// </summary>
        /// <param name="inputProfileData"></param>
        /// <returns></returns>
        private MainSecondBeamRegion CalculateBeamSpanRegion(PlaceInputProfileData inputProfileData, List<Point3d> srcPts)
        {
            var mainBeam = inputProfileData.MainBeamOuterProfile;
            var secondBeams = inputProfileData.SecondBeamProfiles;
            var dbLst = new DBObjectCollection();
            secondBeams.ForEach(e => dbLst.Add(e));

            // 计算内轮廓和偏移计算
            var resProfiles = new List<Polyline>();
            foreach (Polyline item in mainBeam.Difference(dbLst))
            {
                foreach (Polyline offsetPoly in item.Buffer(-500))
                    resProfiles.Add(offsetPoly);
            }

            //DrawUtils.DrawProfile(resProfiles.Polylines2Curves(), "resProfiles");
            return new MainSecondBeamRegion(resProfiles, srcPts);
        }
    }
}
