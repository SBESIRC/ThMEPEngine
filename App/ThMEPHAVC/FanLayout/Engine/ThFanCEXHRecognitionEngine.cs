using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.FanLayout.ViewModel;

namespace ThMEPHVAC.FanLayout.Engine
{
    public class ThFanCEXHRecognitionEngine : ThFanRecognitionEngine
    {
        public ThFanCEXHRecognitionEngine()
        {
            FanLayer = "H-EQUP-FANS";
            EffectiveName = "吊顶式排风扇";
        }
        public override List<ThFanConfigInfo> GetFanConfigInfo(Point3dCollection area)
        {
            using (var acadDb= AcadDatabase.Active())
            {
                List<ThFanConfigInfo> resList = new List<ThFanConfigInfo>();
                var blks = GetFanBlockReference(area);
                foreach (var b in blks)
                {
                    ThFanConfigInfo info = new ThFanConfigInfo();
                    ThBlockReferenceData brData = new ThBlockReferenceData(b.Id);
                    var attributes = brData.Attributes;
                    info.FanNumber = attributes["设备编号"];
                    info.FanVolume = ThFanLayoutDealService.GetFanVolum(attributes["风量"]);
                    info.FanPower = ThFanLayoutDealService.GetFanPower(attributes["电量"]);

                    var values = b.Id.GetXData("FanProperty");
                    info.FanPressure = (double)values[1].Value;
                    info.FanNoise = (double)values[2].Value;
                    info.FanWeight = (double)values[3].Value;
                    resList.Add(info);
                }
                return resList;
            }
        }
    }
}
