using Autodesk.AutoCAD.DatabaseServices;
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
    /// 计算每组内车灯调整到所需位置的基准线, 一个组一个基准线
    /// </summary>
    public class ReverseSubGroupMover : SubGroupMover
    {
        public ReverseSubGroupMover(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos, double tolerance)
            : base(lanePoly, lightPlaceInfos, tolerance)
        {
        }

        public static void MakeReverseSubGroupMover(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos, double tolerance)
        {
            var reverseSubGroupMover = new ReverseSubGroupMover(lanePoly, lightPlaceInfos, tolerance);
            reverseSubGroupMover.Do();
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

            var line = lightPlaceInfos.First().ShortDirLength;
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
            var line = lightPlaceInfo.ShortDirLength;
            var lineDirection = line.GetFirstDerivative(line.StartPoint).GetNormal();

            return new Line(pos + lineDirection * m_extendLineLength, pos - lineDirection * m_extendLineLength);
        }
    }
}
