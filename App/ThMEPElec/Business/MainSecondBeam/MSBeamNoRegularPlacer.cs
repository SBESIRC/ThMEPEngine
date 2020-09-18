using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.PostProcess;
using ThCADCore.NTS;

namespace ThMEPElectrical.Business.MainSecondBeam
{
    /// <summary>
    /// 主次梁异形排布
    /// </summary>
    public class MSBeamNoRegularPlacer : MultiSegmentPlace
    {
        private PlaceInputProfileData m_placeInputProfileData; // 有效区域和点集数据

        public MSBeamNoRegularPlacer(LayoutProfileData layoutProfileData, PlaceParameter parameter, PlaceInputProfileData placeInputProfileData)
            :base(layoutProfileData, parameter)
        {
            m_placeInputProfileData = placeInputProfileData;
        }

        // 异形主次梁布置
        public static List<Point3d> MakeMSNoRegularPlacer(LayoutProfileData layoutProfileData, PlaceParameter parameter, PlaceInputProfileData placeInputProfileData)
        {
            var msNoRegularPlacer = new MSBeamNoRegularPlacer(layoutProfileData, parameter, placeInputProfileData);
            msNoRegularPlacer.DoABBPlace();

            return msNoRegularPlacer.PlacePts;
        }

        protected override List<Point3d> CalculatePts(PlaceRect placeRectInfo)
        {
            var points = new List<Point3d>();
            if (placeRectInfo == null)
                return points;

            // 原始的经过变化过的多段线
            var srcTransPoly = placeRectInfo.srcPolyline;

            var leftLine = placeRectInfo.LeftLine;
            var bottomLine = placeRectInfo.BottomLine;

            var rectArea = leftLine.Length * bottomLine.Length;

            // 一个可以布置完的
            if (leftLine.Length < 2 * m_parameter.ProtectRadius && bottomLine.Length < 2 * m_parameter.ProtectRadius && rectArea < m_parameter.ProtectArea)
            {
                var center = GeomUtils.GetCenterPt(srcTransPoly);
                if (center.HasValue)
                {
                    points.Add(center.Value);
                    // 计算有效的可布置区域
                    var mainBeamSpanRegion = CalculateBeamSpanRegion(m_placeInputProfileData.MainBeamOuterProfile, m_placeInputProfileData.SecondBeamProfiles, points);
                    if (points.Count == 1)
                    {
                        points = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.SINGLEPLACE);
                    }
                    return points;
                }
            }

            // 垂直个数
            var verticalCount = Math.Ceiling(leftLine.Length / m_parameter.VerticalMaxGap);
            var verticalGap = leftLine.Length / verticalCount;

            // 水平个数
            var horizontalCount = Math.Ceiling(bottomLine.Length / m_parameter.HorizontalMaxGap);

            // 布置一行的
            if (verticalCount == 1)
            {
                // 一行布置
                var midLine = GeomUtils.MoveLine(bottomLine, Vector3d.YAxis, leftLine.Length * 0.5);
                return OneRowPlace(midLine, placeRectInfo, leftLine.Length * 0.5);
            }
            else if (horizontalCount == 1)
            {
                // 一列布置
                return OneColMultiSegmentsPlace.MakeOneColPlacePolygon(m_parameter, placeRectInfo);
            }
            else
            {
                // 多行布置 - 分行处理
                return MultiOneRowPlacePts(placeRectInfo, verticalCount, verticalGap);
            }
        }

        protected override List<Point3d> OneRowPlace(Line midLine, PlaceRect placeRectInfo, double verticalA)
        {
            var points = base.OneRowPlace(midLine, placeRectInfo, verticalA);
            var mainBeamSpanRegion = CalculateBeamSpanRegion(placeRectInfo.srcPolyline, m_placeInputProfileData.SecondBeamProfiles, points);

            if (points.Count == 1)
            {
                points = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.SINGLEPLACE);
            }
            else if (points.Count == 2)
            {
                points = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamSpanRegion, MSPlaceAdjustorType.MEDIUMPLACE);
            }

            return points;
        }

        /// <summary>
        /// 计算有效的可布置区域
        /// </summary>
        /// <param name="mainBeam"></param>
        /// <param name="secondBeams"></param>
        /// <param name="srcPts"></param>
        /// <returns></returns>
        private MainSecondBeamRegion CalculateBeamSpanRegion(Polyline mainBeam, List<Polyline> secondBeams, List<Point3d> srcPts)
        {
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
