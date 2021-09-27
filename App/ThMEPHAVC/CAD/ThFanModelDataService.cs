using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection.Service;
using ThMEPEngineCore.Service.Hvac;
using System.Collections.Generic;

namespace ThMEPHVAC.CAD
{
    public class ThFanModelDataService
    {
        public List<double> CalcAirVolume(ObjectId objId)
        {
            var identifier = objId.GetModelIdentifier();
            if (string.IsNullOrEmpty(identifier))
            {
                return new List<double>();
            }

            var ds = ThFanModelDataDbSource.Create(objId.Database);
            var model = ds.Models.Find(o => o.ID == identifier);
            var subModel = ds.Models.Find(o => o.PID == identifier);
            if (model != null)
            {
                if (subModel != null)
                {
                    return new List<double>()
                    {
                        FanAirVolumeService.CalcAirVolume(model),
                        FanAirVolumeService.CalcAirVolume(subModel),
                    }; 
                }
                else
                {
                    return new List<double>()
                    {
                        FanAirVolumeService.CalcAirVolume(model),
                    };
                }
            }

            return new List<double>();
        }
    }
}
