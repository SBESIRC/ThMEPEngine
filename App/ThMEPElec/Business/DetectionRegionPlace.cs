using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThCADCore.NTS;

namespace ThMEPElectrical.Business
{
    public class DetectionRegionPlace
    {
        private List<DetectionRegion> DetectionRegions = null;

        public DetectionRegionPlace(List<DetectionRegion> srcDetectionRegions)
        {
            DetectionRegions = srcDetectionRegions;
        }

        public void Do()
        {

        }


        private List<Polyline> CalculateOffsetPolyline(Polyline poly, double gapDistance = 500)
        {
            if (poly == null)
                return null;

            var resOffset = poly.Buffer(-gapDistance);
            var offsetPolylines = new List<Polyline>();

            foreach (DBObject singleOffset in resOffset)
            {
                if (singleOffset is Polyline offsetPoly)
                    offsetPolylines.Add(offsetPoly);
            }

            return offsetPolylines;
        }

        /// <summary>
        /// 布置区域
        /// </summary>
        /// <returns></returns>
        private List<PlaceData> CalculatePlaceRegion()
        {
            if (DetectionRegions == null)
                return null;

            var placeDatas = new List<PlaceData>();
            foreach (var singleDetectRegion in DetectionRegions)
            {
                var offsetPolylines = CalculateOffsetPolyline(singleDetectRegion.DetectionProfile);
                if (offsetPolylines == null || offsetPolylines.Count == 0)
                    continue;

                placeDatas.Add(new PlaceData(singleDetectRegion.DetectionProfile, offsetPolylines));
            }

            return placeDatas;
        }
    }
}
