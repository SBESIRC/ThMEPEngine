using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    public class ParkLightAngleCalculator
    {
        private List<LightPlaceInfo> m_lightPlaceInfos;
        private Light_Place_Type m_light_Place_type;

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
                if (m_light_Place_type == Light_Place_Type.LONG_EDGE)
                {
                    var longLine = lightInfo.LongDirLength;
                    //DrawUtils.DrawProfileDebug(new List<Curve>() { longLine }, "longLine");
                    var vecLong = (longLine.EndPoint - longLine.StartPoint).GetNormal();
                    lightInfo.Angle = vecLong.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
                }
                else
                {
                    var shortLine = lightInfo.ShortDirLength;
                    //DrawUtils.DrawProfileDebug(new List<Curve>() { shortLine }, "shortLine");
                    var vecShort = (shortLine.EndPoint - shortLine.StartPoint).GetNormal();
                    lightInfo.Angle = vecShort.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
                }
            }
        }
    }
}
