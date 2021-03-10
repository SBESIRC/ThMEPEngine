using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PipeConnector
{
    /// <summary>
    /// 单侧灯组内部的多个集合之间的连线
    /// </summary>
    public class DivideSideLightCalculator
    {
        private PipeGroup m_pipeGroup;
        private Light_Place_Type m_light_Place_Type;

        public List<Line> ConnectAddLines;
        private Polyline m_lanePoly;

        public static List<Line> MakeDivideSideLightAddLines(PipeGroup pipeGroup, Light_Place_Type light_Place_Type)
        {
            var divideSideLightCalculator = new DivideSideLightCalculator(pipeGroup, light_Place_Type);
            divideSideLightCalculator.Do();
            return divideSideLightCalculator.ConnectAddLines;
        }


        public DivideSideLightCalculator(PipeGroup pipeGroup, Light_Place_Type light_Place_Type)
        {
            m_pipeGroup = pipeGroup;
            m_light_Place_Type = light_Place_Type;
            m_lanePoly = pipeGroup.LanePolyline;
            ConnectAddLines = new List<Line>();
        }


        public void Do()
        {
            ConnectWithGroup(m_pipeGroup, m_light_Place_Type);
        }

        private void ConnectWithGroup(PipeGroup pipeGroup, Light_Place_Type light_Place_Type)
        {
            // 选择点位进行连接
            for (int i = 0; i < pipeGroup.LightPlaceInfos.Count - 1; i++)
            {
                var curLightInfo = pipeGroup.LightPlaceInfos[i];
                var nextLightInfo = pipeGroup.LightPlaceInfos[i + 1];

                if (light_Place_Type == Light_Place_Type.LONG_EDGE)
                    ConnectAddLines.Add(NearPipeConnecLongEdge(curLightInfo, nextLightInfo));
                else
                    ConnectAddLines.Add(NearPipeConnecShortEdge(curLightInfo, nextLightInfo));
            }
        }

        private Line NearPipeConnecShortEdge(LightPlaceInfo curLightPlaceInfo, LightPlaceInfo nextLightPlaceInfo)
        {
            var firstLightParkTypeInfo = curLightPlaceInfo.ParkingSpace_TypeInfo;
            var secondLightParkTypeInfo = nextLightPlaceInfo.ParkingSpace_TypeInfo;
            if (firstLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
            {
                var curFirstEndPoint = curLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var curSecondEndPoint = curLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;

                var nextFirstEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var nextSecondEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;

                var firstPoint = curFirstEndPoint.DistanceTo(nextFirstEndPoint) > curFirstEndPoint.DistanceTo(nextSecondEndPoint) ? nextSecondEndPoint : nextFirstEndPoint;
                var secondPoint = firstPoint.DistanceTo(curFirstEndPoint) > firstPoint.DistanceTo(curSecondEndPoint) ? curSecondEndPoint : curFirstEndPoint;
                return new Line(firstPoint, secondPoint);

            }
            else if (firstLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking)
            {
                var firstPoint = nextLightPlaceInfo.LightBlockConnectInfo.MidPoint;

                var curFirstEndPoint = curLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var curSecondEndPoint = curLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;

                var secondPoint = firstPoint.DistanceTo(curFirstEndPoint) > firstPoint.DistanceTo(curSecondEndPoint) ? curSecondEndPoint : curFirstEndPoint;

                return new Line(firstPoint, secondPoint);
            }
            else if (firstLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
            {
                var firstPoint = curLightPlaceInfo.LightBlockConnectInfo.MidPoint;
                var nextFirstEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var nextSecondEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;

                var secondPoint = firstPoint.DistanceTo(nextFirstEndPoint) > firstPoint.DistanceTo(nextSecondEndPoint) ? nextFirstEndPoint : nextSecondEndPoint;
                return new Line(firstPoint, secondPoint);
            }
            else
            {
                //(firstLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking)
                var firstPoint = curLightPlaceInfo.LightBlockConnectInfo.MidPoint;
                var nextPoint = nextLightPlaceInfo.LightBlockConnectInfo.MidPoint;
                return new Line(firstPoint, nextPoint);
            }
        }

        private Line NearPipeConnecLongEdge(LightPlaceInfo curLightPlaceInfo, LightPlaceInfo nextLightPlaceInfo)
        {
            var firstLightParkTypeInfo = curLightPlaceInfo.ParkingSpace_TypeInfo;
            var secondLightParkTypeInfo = nextLightPlaceInfo.ParkingSpace_TypeInfo;
            if (firstLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
            {
                return new Line(curLightPlaceInfo.LightBlockConnectInfo.MidPoint, nextLightPlaceInfo.LightBlockConnectInfo.MidPoint);
            }
            else if (firstLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking)
            {
                var firstPoint = curLightPlaceInfo.LightBlockConnectInfo.MidPoint;
                var nextFirstEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var nextSecondEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;
                var secondPoint = firstPoint.DistanceTo(nextFirstEndPoint) > firstPoint.DistanceTo(nextSecondEndPoint) ? nextSecondEndPoint : nextFirstEndPoint;
                return new Line(firstPoint, secondPoint);
            }
            else if (firstLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
            {
                var curFirstEndPoint = curLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var curSecondEndPoint = curLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;
                var secondPoint = nextLightPlaceInfo.LightBlockConnectInfo.MidPoint;
                var firstPoint = secondPoint.DistanceTo(curFirstEndPoint) > secondPoint.DistanceTo(curSecondEndPoint) ? curSecondEndPoint : curFirstEndPoint;
                return new Line(firstPoint, secondPoint);
            }
            else
            {
                //(firstLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking && secondLightParkTypeInfo == ParkingSpace_Type.Parallel_Parking)
                var curFirstEndPoint = curLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var curSecondEndPoint = curLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;

                var curFirstEndPointDistance = m_lanePoly.GetClosestPointTo(curFirstEndPoint, true).DistanceTo(curFirstEndPoint);
                var curSecondEndPointDistance = m_lanePoly.GetClosestPointTo(curSecondEndPoint, true).DistanceTo(curSecondEndPoint);

                var firstPoint = curFirstEndPointDistance > curSecondEndPointDistance ? curFirstEndPoint : curSecondEndPoint;

                var nextFirstEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.FirstEndPoint;
                var nextSecondEndPoint = nextLightPlaceInfo.LightBlockConnectInfo.SecondEndPoint;
                var secondPoint = firstPoint.DistanceTo(nextFirstEndPoint) > firstPoint.DistanceTo(nextSecondEndPoint) ? nextSecondEndPoint : nextFirstEndPoint;
                return new Line(firstPoint, secondPoint);
            }
        }
    }
}
