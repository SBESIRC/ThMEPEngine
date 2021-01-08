using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    /// <summary>
    /// 侧方提车位置调整计算
    /// </summary>
    public class ParallelSubGroupMover : SubGroupMover
    {
        public ParallelSubGroupMover(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos, double tolerance)
            :base (lanePoly, lightPlaceInfos, tolerance)
        {
        }

        public static void MakeParallelSubGroupMover(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos, double tolerance)
        {
            var parallelSubGroupMover = new ParallelSubGroupMover(lanePoly, lightPlaceInfos, tolerance);
            parallelSubGroupMover.Do();
        }

        public override void Do()
        {
            PreProcess();

            GenerateGroup();

            AdjustPosition();
        }


        protected override Line CalculateMultiLinesBaseLine(List<LightPlaceInfo> lightPlaceInfos)
        {
            var centerPoint = CalculateCentroid(lightPlaceInfos);

            var line = lightPlaceInfos.First().LongDirLength;
            var lineDirection = line.GetFirstDerivative(line.StartPoint).GetNormal();
            return new Line(centerPoint + lineDirection * m_extendLineLength, centerPoint - lineDirection * m_extendLineLength);
        }

        protected override bool IsValidRelatedInfo(LightPlaceInfo firstLightpLaceInfo, LightPlaceInfo secondLightPlaceInfo, double gapDistance)
        {
            var firstLine = GenerateCenterLine(firstLightpLaceInfo);

            var secline = GenerateCenterLine(secondLightPlaceInfo);

            return IsRelatedCenterPosLine(firstLine, secline, gapDistance);
        }

        protected override Line GenerateCenterLine(LightPlaceInfo lightPlaceInfo)
        {
            var pos = lightPlaceInfo.Position;
            var line = lightPlaceInfo.LongDirLength;
            var lineDirection = line.GetFirstDerivative(line.StartPoint).GetNormal();

            return new Line(pos + lineDirection * m_extendLineLength, pos - lineDirection * m_extendLineLength);
        }
    }
}
