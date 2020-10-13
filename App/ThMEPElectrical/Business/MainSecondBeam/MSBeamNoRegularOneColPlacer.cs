using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Business.MainBeam;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPElectrical.PostProcess;

namespace ThMEPElectrical.Business.MainSecondBeam
{
    /// <summary>
    /// 异形一列布置位置move调整
    /// </summary>
    internal class MSBeamNoRegularOneColPlacer : OneColMultiSegmentsPlaceEx
    {
        private PlaceInputProfileData m_inputProfileData;
        public MSBeamNoRegularOneColPlacer(PlaceParameter parameter, PlaceRect placeRectInfo, PlaceInputProfileData inputProfileData)
            : base(parameter, placeRectInfo)
        {
            m_inputProfileData = inputProfileData;
        }

        /// <summary>
        /// 主次梁异形一列调整
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="placeRectInfo"></param>
        /// <param name="inputProfileData"></param>
        /// <returns></returns>
        public static List<Point3d> MakeMSBeamNoRegularOneColPlacer(PlaceParameter parameter, PlaceRect placeRectInfo, PlaceInputProfileData inputProfileData)
        {
            var msBeamNoRegularOneColPlacer = new MSBeamNoRegularOneColPlacer(parameter, placeRectInfo, inputProfileData);
            return msBeamNoRegularOneColPlacer.DoPlace();
        }

        protected override List<Point3d> OneColPlace()
        {
            var points =  base.OneColPlace();

            var mainBeamSpanRegion = CalculateBeamSpanRegion(m_inputProfileData, points);

            if (points.Count == 2)
            {
                points = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.MEDIUMPLACE);
            }
            
            return points;
        }

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
                foreach (Polyline offsetPoly in item.Buffer(ThMEPCommon.ShrinkDistance))
                    resProfiles.Add(offsetPoly);
            }

            return new MainSecondBeamRegion(resProfiles, srcPts);
        }
    }
}
