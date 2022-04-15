using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection.Service;
using TianHua.FanSelection.Function;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.EQPMFanSelect;

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
                if (model.IsVariableSpeedModel())
                {
                    if (subModel != null)
                    {
                        return new List<double>()
                        {
                            FanAirVolumeService.CalcAirVolume(model),
                            FanAirVolumeService.CalcAirVolume(subModel),
                        };
                    }
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
        public List<double> CalcAirVolumeEx(ObjectId objId)
        {
            var resList = new List<double>();
            var identifier = objId.GetModelIdentifier(ThHvacCommon.RegAppName_FanSelectionEx);
            if (string.IsNullOrEmpty(identifier))
                return resList;

            var xData = objId.ReadBlockFanXData(out FanBlockXDataBase xDataBase);
            if (null == xData || xDataBase == null || string.IsNullOrEmpty(xData.AirCalcValue))
                return resList;
            var spliteAirCalcValue = xData.AirCalcValue.Split('/');
            double.TryParse(spliteAirCalcValue[0], out double heightValue);
            resList.Add(heightValue);
            if (spliteAirCalcValue.Length>1)
            {
                double.TryParse(spliteAirCalcValue[1], out double lowValue);
                resList.Add(lowValue);
            }
            return resList;
        }
    }
}
