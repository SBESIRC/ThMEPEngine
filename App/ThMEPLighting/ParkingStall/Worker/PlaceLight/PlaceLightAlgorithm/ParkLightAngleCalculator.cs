using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    public class ParkLightAngleCalculator
    {
        private List<LightPlaceInfo> m_lightPlaceInfos;
        private Light_Place_Type m_light_Place_type;
        private double m_langLength_4300 = 4300.0;
        private double m_langLength_5300 = 5300.0;

        public ParkLightAngleCalculator(List<LightPlaceInfo> lightPlaceInfos, Light_Place_Type light_Place_Type)
        {
            m_lightPlaceInfos = lightPlaceInfos;
            m_light_Place_type = light_Place_Type;
        }

        public static void MakeParkLightAngleCalculator(List<LightPlaceInfo> lightPlaceInfos, Light_Place_Type light_Place_Type)
        {
            var parkLightCalculator = new ParkLightAngleCalculator(lightPlaceInfos, light_Place_Type);
            parkLightCalculator.Do();
        }

        public void Do()
        {
            foreach (var lightInfo in m_lightPlaceInfos)
            {
                //因为有些车位块时多个车位的组合，微型车位(大概) 4300x2200,正常车位(大概) 5300x2400
                //分别判断两个边，判断接近4300和5300的，取最接近的作为单个车位的长边
                Dictionary<Line, double> lengthNear4300 = new Dictionary<Line, double>();
                Dictionary<Line, double> lengthNear5300 = new Dictionary<Line, double>();
                lengthNear4300.Add(lightInfo.LongDirLength, Math.Abs(lightInfo.LongDirLength.Length % m_langLength_4300));
                lengthNear4300.Add(lightInfo.ShortDirLength, Math.Abs(lightInfo.ShortDirLength.Length % m_langLength_4300));
                lengthNear5300.Add(lightInfo.LongDirLength, Math.Abs(lightInfo.LongDirLength.Length % m_langLength_5300));
                lengthNear5300.Add(lightInfo.ShortDirLength, Math.Abs(lightInfo.ShortDirLength.Length % m_langLength_5300));

                Line longLine = null;
                Line shortLine = null;
                var min4300 = lengthNear4300.OrderBy(c => c.Value).FirstOrDefault().Value;
                var min5300 = lengthNear5300.OrderBy(c => c.Value).FirstOrDefault().Value;
                if (min4300 < min5300)
                {
                    //更接近4300
                    foreach (var item in lengthNear4300) 
                    {
                        if (Math.Abs(item.Value - min4300) < 1)
                            longLine = item.Key;
                        else 
                            shortLine = item.Key;
                    }
                }
                else 
                {
                    //更接近5300
                    foreach (var item in lengthNear5300)
                    {
                        if (Math.Abs(item.Value - min5300) < 1)
                            longLine = item.Key;
                        else
                            shortLine = item.Key;
                    }
                }
                if (m_light_Place_type == Light_Place_Type.LONG_EDGE)
                {
                    var vecLong = (longLine.EndPoint - longLine.StartPoint).GetNormal();
                    lightInfo.Angle = vecLong.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
                }
                else
                {
                    var vecShort = (shortLine.EndPoint - shortLine.StartPoint).GetNormal();
                    lightInfo.Angle = vecShort.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
                }
            }
        }
    }
}
