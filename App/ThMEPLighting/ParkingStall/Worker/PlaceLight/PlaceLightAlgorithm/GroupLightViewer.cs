using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PlaceLight
{
    public class GroupLightViewer
    {
        private List<LightPlaceInfo> m_lightPlaceInfos;
        private Light_Place_Type m_place_type;

        private GroupLightViewer(List<LightPlaceInfo> lightPlaceInfos, Light_Place_Type light_Place_Type)
        {
            m_lightPlaceInfos = lightPlaceInfos;
            m_place_type = light_Place_Type;
        }

        public static void MakeGroupLightViewer(List<LightPlaceInfo> lightPlaceInfos, Light_Place_Type light_Place_Type = Light_Place_Type.LONG_EDGE)
        {
            var groupLightViewer = new GroupLightViewer(lightPlaceInfos, light_Place_Type);
            groupLightViewer.Do();
        }

        public void Do()
        {
            foreach (var lightInfo in m_lightPlaceInfos)
            {
                DisplayLightInfo(lightInfo);
            }
        }

        private void DisplayLightInfo(LightPlaceInfo lightPlaceInfo)
        {
            var position = lightPlaceInfo.Position;
            double length = 100;

            var curves = new List<Curve>();
            if (m_place_type == Light_Place_Type.LONG_EDGE)
            {
                var longLine = lightPlaceInfo.LongDirLength;
                var longDir = longLine.GetFirstDerivative(longLine.StartPoint).GetNormal();

                curves.Add(new Line(position - longDir * length, position + longDir * length));
            }
            else if (m_place_type == Light_Place_Type.SHORT_EDGE)
            {
                var shortLine = lightPlaceInfo.ShortDirLength;
                var shortDir = shortLine.GetFirstDerivative(shortLine.StartPoint).GetNormal();

                curves.Add(new Line(position - shortDir * length, position + shortDir * length));
            }

            DrawUtils.DrawProfileDebug(curves, this.ToString());
        }
    }
}
