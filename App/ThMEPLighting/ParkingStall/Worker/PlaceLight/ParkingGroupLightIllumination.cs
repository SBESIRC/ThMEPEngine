using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPLighting.Common;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    /// <summary>
    /// 根据照度计算实际排布的个数
    /// </summary>
    class ParkingGroupLightIllumination
    {
        public List<LightPlaceInfo> LightPlaceInfos
        {
            get;
            set;
        } = new List<LightPlaceInfo>();
        Parkingillumination parkingillumination { get; }
        public ParkingGroupLightIllumination(List<LightPlaceInfo> parkingRelatedGroups, Parkingillumination illumination)
        {
            LightPlaceInfos = parkingRelatedGroups;
            parkingillumination = illumination;
        }

        public static void ParkingGroupLightIlluminationCheck(List<LightPlaceInfo> parkingRelatedGroups, Parkingillumination parkingillumination)
        {
            var parkingLightGenerator = new ParkingGroupLightIllumination(parkingRelatedGroups, parkingillumination);
            parkingLightGenerator.Do();
        }

        public void Do()
        {
            foreach (var item in LightPlaceInfos)
            {
                var groupNeedLightCount = AreaNearLightCount(item.BigGroupInfo.BigGroupPoly);
                if (groupNeedLightCount == 1)
                {
                    item.InsertLightPosisions.Add(item.Position);
                    continue;
                }
                //有多个时，排布点按照车位短边方向进行
                var shortDir = item.ShortDirLength.LineDirection();
                var longDir = item.LongDirLength.LineDirection();
                var line = item.BigGroupInfo.BigGroupLongLine;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                if (Math.Abs(lineDir.DotProduct(shortDir)) < 0.1)
                {
                    line = item.BigGroupInfo.BigGroupShortLine;
                    lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                }
                var spaceLength = line.Length / groupNeedLightCount;
                var lineSp = line.StartPoint + lineDir.MultiplyBy(spaceLength / 2);
                for (int i = 0; i < groupNeedLightCount; i++)
                {
                    var pt = lineSp + lineDir.MultiplyBy(spaceLength * i);
                    var prjPt = ThPointVectorUtil.PointToFace(pt, item.Position, longDir);
                    item.InsertLightPosisions.Add(prjPt);
                }
            }
        }
        private int AreaNearLightCount(Polyline polyline)
        {
            var area = polyline.Area;
            area = area / (1000.0 * 1000.0);
            var needCount = (parkingillumination.MastIllumination * area) / (parkingillumination.LightRatedIllumination * parkingillumination.UtilizationCoefficient * parkingillumination.MaintenanceFactor);
            int count = (int)Math.Ceiling(needCount);
            var temp = needCount % 1;
            if (temp > 0 && temp < 0.1)
                count = (int)Math.Floor(needCount);
            count = count < 1 ? 1 : count;
            return count;
        }
    }
}
