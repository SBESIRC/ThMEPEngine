namespace TianHua.FanSelection.Service
{
    public class FanAirVolumeService
    {
        public static double CalcAirVolume(FanDataModel model)
        {
            if (model.IsManualInputAirVolume)
            {
                return model.SysAirVolume;
            }
            else
            {
                return model.AirCalcValue;
            }
        }
    }
}
