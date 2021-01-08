using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class SubGroupMover
    {
        protected double m_extendLineLength = 1000;
        protected List<LightPlaceInfo> m_lightPlaceInfos;
        protected Polyline m_lanePoly;

        protected double m_posTolerance;

        public List<List<LightPlaceInfo>> LightPlaceGroupLst = new List<List<LightPlaceInfo>>();

        public SubGroupMover(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos, double tolerance)
        {
            m_lanePoly = lanePoly;
            m_lightPlaceInfos = lightPlaceInfos;
            m_posTolerance = tolerance;
        }

        public virtual void Do()
        {

        }

        protected void PreProcess()
        {
            for (int i = 0; i < m_lightPlaceInfos.Count; i++)
            {
                var curLightPlaceInfo = m_lightPlaceInfos[i];
                curLightPlaceInfo.IsUsed = false;

                for (int j = i + 1; j < m_lightPlaceInfos.Count; j++)
                {
                    var nextLightPlaceInfo = m_lightPlaceInfos[j];

                    if (IsValidRelatedInfo(curLightPlaceInfo, nextLightPlaceInfo, m_posTolerance))
                    {
                        curLightPlaceInfo.RelatedLightPlaceInfos.Add(nextLightPlaceInfo);
                        nextLightPlaceInfo.RelatedLightPlaceInfos.Add(curLightPlaceInfo);
                    }
                }
            }
        }

        protected void AdjustPosition()
        {
            var baseLine = CalculateBaseLine();

            foreach (var lightPlaceInfo in m_lightPlaceInfos)
            {
                var closestPt = baseLine.GetClosestPointTo(lightPlaceInfo.Position, true);

                var moveDistance = closestPt.DistanceTo(lightPlaceInfo.Position);

                if (lightPlaceInfo.ParkingSpace_TypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
                {
                    if (moveDistance < ParkingStallCommon.ReverseMaxMoveDistance)
                        lightPlaceInfo.Position = closestPt;
                }
                else if (lightPlaceInfo.ParkingSpace_TypeInfo == ParkingSpace_Type.Parallel_Parking)
                {
                    if (moveDistance < ParkingStallCommon.ParallelMaxMoveDistance)
                        lightPlaceInfo.Position = closestPt;
                }
            }
        }

        protected Line CalculateBaseLine()
        {
            // 找出经过最多灯的基准线
            List<LightPlaceInfo> maxCountPlaceInfos = LightPlaceGroupLst.First();
            int max = maxCountPlaceInfos.Count;

            for (int i = 0; i < LightPlaceGroupLst.Count; i++)
            {
                var curLightPlaceGroup = LightPlaceGroupLst[i];
                if (curLightPlaceGroup.Count > max)
                {
                    max = curLightPlaceGroup.Count;
                    maxCountPlaceInfos = curLightPlaceGroup;
                }
            }

            if (max > 1)
            {
                return CalculateMultiLinesBaseLine(maxCountPlaceInfos);
            }
            else
            {
                var transDatas = LightPlaceGroupLst.SelectMany(x => x).ToList();
                // 中心线经过灯数量一样多的时候，找出离车道线最远的中心线作为基准线
                return CalculateMaxDistanceBaseLine(transDatas);
            }
        }


        protected virtual Line CalculateMultiLinesBaseLine(List<LightPlaceInfo> lightPlaceInfos)
        {
            throw new Exception("CalculateMultiLinesBaseLine");
        }

        protected Point3d CalculateCentroid(List<LightPlaceInfo> lightPlaceInfos)
        {
            var lines = new List<Line>();
            var drawCurves = new List<Curve>();
            foreach (var lightPlaceInfo in lightPlaceInfos)
            {
                var centerLine = GenerateCenterLine(lightPlaceInfo);
                lines.Add(centerLine);
                drawCurves.Add(centerLine);
            }

            //DrawUtils.DrawProfileDebug(drawCurves, "drawCurves");
            return CenterBaseLineCalculator.MakeBaseLineCalculator(lines);
        }

        protected Line CalculateMaxDistanceBaseLine(List<LightPlaceInfo> lightPlaceInfos)
        {
            var lines = new List<Line>();
            foreach (var lightPlaceInfo in lightPlaceInfos)
            {
                lines.Add(GenerateCenterLine(lightPlaceInfo));
            }

            var laneLine = new Line(m_lanePoly.StartPoint, m_lanePoly.EndPoint);

            var baseLine = lines.First();
            double maxDistance = 0;
            foreach (var line in lines)
            {
                if (line.Equals(baseLine))
                    continue;

                var midPt = line.GetPointAtParameter((line.StartParam + line.EndParam) * 0.5);
                var closestPt = laneLine.GetClosestPointTo(midPt, true);
                var curDistance = midPt.DistanceTo(closestPt);
                if (curDistance > maxDistance)
                {
                    maxDistance = curDistance;
                    baseLine = line;
                }
            }

            //DrawUtils.DrawProfileDebug(new List<Curve>() { baseLine }, "singleBaseLine");
            return baseLine;
        }

        protected void GenerateGroup()
        {
            for (int i = 0; i < m_lightPlaceInfos.Count; i++)
            {
                var curLightPlaceInfo = m_lightPlaceInfos[i];
                if (curLightPlaceInfo.IsUsed)
                    continue;

                CollectFromOneNode(curLightPlaceInfo);
            }
        }

        protected void CollectFromOneNode(LightPlaceInfo relatedNode)
        {
            var relatedNodes = new List<LightPlaceInfo>();
            relatedNodes.Add(relatedNode);

            var collectNodes = new List<LightPlaceInfo>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                if (curNode.IsUsed)
                    continue;

                curNode.IsUsed = true;
                collectNodes.Add(curNode);

                foreach (var childNode in curNode.RelatedLightPlaceInfos)
                {
                    if (childNode.IsUsed)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            LightPlaceGroupLst.Add(collectNodes);
        }

        protected virtual bool IsValidRelatedInfo(LightPlaceInfo firstLightpLaceInfo, LightPlaceInfo secondLightPlaceInfo, double gapDistance)
        {
            return false;
        }

        protected virtual Line GenerateCenterLine(LightPlaceInfo lightPlaceInfo)
        {
            throw new Exception("abstract class");
        }

        protected bool IsRelatedCenterPosLine(Line firstLine, Line secLine, double gapDistance)
        {
            var point = firstLine.GetClosestPointTo(secLine.StartPoint, true);
            if (point.DistanceTo(secLine.StartPoint) <= gapDistance)
                return true;

            return false;
        }
    }
}
