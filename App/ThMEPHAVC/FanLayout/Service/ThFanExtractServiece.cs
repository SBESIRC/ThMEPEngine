using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanLayout.Engine;
using ThMEPHVAC.FanLayout.ViewModel;

namespace ThMEPHVAC.FanLayout.Service
{
    public static class ThFanExtractServiece
    {
        public static List<ThFanConfigInfo> GetWAFFanConfigInfoList(Point3dCollection area)
        {
            var fanEngine = new ThFanWAFRecognitionEngine();
            List<ThFanConfigInfo> infoList = fanEngine.GetFanConfigInfo(area);
            return infoList;
        }

        public static List<ThFanConfigInfo> GetWEXHFanConfigInfoList(Point3dCollection area)
        {
            var fanEngine = new ThFanWEXHRecognitionEngine();
            List<ThFanConfigInfo> infoList = fanEngine.GetFanConfigInfo(area);
            return infoList;
        }

        public static List<ThFanConfigInfo> GetCEXHFanConfigInfoList(Point3dCollection area)
        {
            var fanEngine = new ThFanCEXHRecognitionEngine();
            List<ThFanConfigInfo> infoList = fanEngine.GetFanConfigInfo(area);
            return infoList;
        }
    }
}
