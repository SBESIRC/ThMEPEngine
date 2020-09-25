using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.PostProcess;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Business.MainSecondBeam
{
    /// <summary>
    /// 主次梁的矩形布置
    /// </summary>
    internal class MSBeamRectPlacer : RectProfilePlace
    {
        private PlaceInputProfileData m_inputProfileData;


        public static List<Point3d> MakeMSBeamRectPlacer(LayoutProfileData layoutData, PlaceParameter parameter, PlaceInputProfileData inputProfileData)
        {
            var msBeamRectPlacer = new MSBeamRectPlacer(layoutData, parameter, inputProfileData);
            msBeamRectPlacer.DoABB();
            return msBeamRectPlacer.SinglePlacePts;
        }

        public MSBeamRectPlacer(LayoutProfileData layoutData, PlaceParameter parameter, PlaceInputProfileData inputProfileData)
            : base(layoutData, parameter)
        {
            m_inputProfileData = inputProfileData;
        }

        protected override void DoABB()
        {
            base.DoABB();

            // 计算有效的可布置区域
            var mainBeamSpanRegion = CalculateBeamSpanRegion(m_inputProfileData, m_singlePlacePts);

            if (m_singlePlacePts.Count == 1)
            {
                m_singlePlacePts = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.SINGLEPLACE);
            }
            else if (m_singlePlacePts.Count == 2)
            {
                m_singlePlacePts = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.MEDIUMPLACE);
            }
            else if (m_singlePlacePts.Count == 4)
            {
                m_singlePlacePts = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.LARGEPLACE);
            }
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

            return new MainSecondBeamRegion(resProfiles, srcPts);
        }
    }
}
