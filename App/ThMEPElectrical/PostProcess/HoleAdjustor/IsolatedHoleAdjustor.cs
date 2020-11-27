using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.PostProcess.HoleAdjustor
{
    public class IsolatedHoleAdjustor : HoleRegionPointAdjustor
    {
        private PlaceInputProfileData m_placeInputProfileData;

        public IsolatedHoleAdjustor(List<Point3d> points, PlaceInputProfileData placeInputProfileData)
            : base(points)
        {
            m_placeInputProfileData = placeInputProfileData;
        }

        public static List<Point3d> MakeIsolatedHoleAdjustor(List<Point3d> points, PlaceInputProfileData placeInputProfileData)
        {
            var isolatedHoleAdjustor = new IsolatedHoleAdjustor(points, placeInputProfileData);
            isolatedHoleAdjustor.DoAdjustor();
            return isolatedHoleAdjustor.ValidPoints;
        }

        public override void DoAdjustor()
        {
            var holes = m_placeInputProfileData.Holes;

            if (holes.Count == 0)
            {
                ValidPoints.AddRange(m_points);
                return;
            }
            else
            {
                var expandHoles = ExpandHoles(holes);
                DoWithHoles(expandHoles);
            }
        }

        private List<Polyline> ExpandHoles(List<Polyline> polylines)
        {
            var polys = new List<Polyline>();

            foreach (var singlePoly in polylines)
            {
                foreach (Polyline offsetPoly in singlePoly.Buffer(-ThMEPCommon.ShrinkDistance))
                    polys.Add(offsetPoly);
            }

            return polys;
        }

        private List<PairData> GeneratePairInfo(List<Polyline> polylines)
        {
            var pairDatas = new List<PairData>();
            foreach (var pt in m_points)
            {
                bool bInHole = false;
                foreach (var poly in polylines)
                {
                    if (GeomUtils.PtInLoop(poly, pt))
                    {
                        pairDatas.Add(new PairData(pt, poly));
                        bInHole = true;
                        break;
                    }
                }

                if (!bInHole)
                {
                    ValidPoints.Add(pt);
                }
            }

            return pairDatas;
        }

        private void DoWithHoles(List<Polyline> polylines)
        {
            var pairDatas = GeneratePairInfo(polylines);
            DoPairData(pairDatas);
        }

        private void DoPairData(List<PairData> pairDatas)
        {
            foreach (var pairData in pairDatas)
            {
                var pt = pairData.Point;
                var poly = pairData.Polyline;

                var validPt = poly.GetClosestPointTo(pt, false);
                ValidPoints.Add(validPt);
            }
        }
    }
}
