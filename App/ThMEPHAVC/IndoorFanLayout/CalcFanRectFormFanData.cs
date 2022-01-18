using System.Collections.Generic;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout
{
    class CalcFanRectFormFanData
    {
        List<IndoorFanBase> targetFanModels;
        public CalcFanRectFormFanData(List<IndoorFanBase> indoorFans) 
        {
            targetFanModels = new List<IndoorFanBase>();
            if (null == indoorFans)
                return;
            foreach (var fan in indoorFans)
                targetFanModels.Add(fan);
        }
        public IndoorFanBase GetFanModel(string fanTypeName)
        {
            if (null == targetFanModels)
                return null;
            foreach (var item in targetFanModels)
            {
                if (item.FanNumber != fanTypeName)
                    continue;
                return item;
            }
            return null;
        }
        public FanRectangle GetFanRectangle(string fanTypeName, EnumFanType fanType,bool isCold, double correctionFactor, EnumAirReturnType enumAirReturn) 
        {
            FanRectangle fanRectangle =null;
            var fanModel = GetFanModel(fanTypeName);
            var fanLoad = IndoorFanCommon.GetFanLoadBase(fanModel, fanType, correctionFactor);
            switch (fanType) 
            {
                case EnumFanType.FanCoilUnitTwoControls:
                case EnumFanType.FanCoilUnitFourControls:
                    fanRectangle = CoilFanRectangle(fanLoad, isCold, enumAirReturn);
                    break;
                case EnumFanType.VRFConditioninConduit:
                    fanRectangle = VRFFanRectangle(fanLoad, isCold, enumAirReturn);
                    break;
                case EnumFanType.VRFConditioninFourSides:
                    fanRectangle = VRFFourSideRectangle(fanModel as VRFFan,isCold, correctionFactor);
                    break;
            }
            if (fanRectangle != null)
                fanRectangle.Name = fanTypeName;
            return fanRectangle;
        }
        private FanRectangle CoilFanRectangle(FanLoadBase fanLoad,bool isCold, EnumAirReturnType enumAirReturn) 
        {
            var fanRectangle = new FanRectangle();
            double load = isCold?fanLoad.FanRealCoolLoad:fanLoad.FanRealHotLoad;
            fanRectangle.Load = load;
            fanRectangle.MinLength = 1600;
            fanRectangle.MaxLength = 5000;
            fanRectangle.Width = fanLoad.FanWidth;
            fanRectangle.MinVentCount = 1;
            fanRectangle.VentRect = new VentRectangle();
            var oneSize = fanLoad.FanBase.AirSupplyOutletOneSize.ToLower();
            fanRectangle.MaxVentCount = fanLoad.FanVentSizeCount;
            var spliteVentWidth = oneSize.Split('x');
            double ventWidth = 0.0;
            double.TryParse(spliteVentWidth[0], out ventWidth);
            fanRectangle.FanDistanceToStart = IndoorFanDistance.CoilFanDistanceToStart(fanLoad, enumAirReturn);
            fanRectangle.VentRect.VentWidth = ventWidth;
            fanRectangle.VentRect.VentLength = ventWidth;
            fanRectangle.VentRect.VentMinDistanceToStart = 1300;
            fanRectangle.VentRect.VentMinDistanceToEnd = 300;
            fanRectangle.VentRect.VentMinDistanceToPrevious = 2000;
            return fanRectangle;
        }
        private FanRectangle VRFFanRectangle(FanLoadBase fanLoad, bool isCold, EnumAirReturnType enumAirReturn) 
        {
            var fanRectangle = new FanRectangle();
            double load = isCold?fanLoad.FanRealCoolLoad:fanLoad.FanRealHotLoad;
            fanRectangle.Load = load;
            fanRectangle.MinLength = 2300;
            fanRectangle.MaxLength = 5000;
            fanRectangle.Width = fanLoad.FanWidth;
            fanRectangle.MinVentCount = 1;
            fanRectangle.VentRect = new VentRectangle();
            var oneSize = fanLoad.FanBase.AirSupplyOutletOneSize.ToLower();
            var twoSize = fanLoad.FanBase.AirSupplyOutletTwoSize;
            fanRectangle.MaxVentCount = 1;
            if (!string.IsNullOrEmpty(twoSize) && twoSize.ToLower().Contains("x"))
                fanRectangle.MaxVentCount = 2;
            var spliteVentWidth = oneSize.Split('x');
            double ventWidth = 0.0;
            double.TryParse(spliteVentWidth[0], out ventWidth);
            fanRectangle.FanDistanceToStart = IndoorFanDistance.VRFReturnVentCenterDisToFan(fanLoad);
            fanRectangle.VentRect.VentWidth = ventWidth;
            fanRectangle.VentRect.VentLength = ventWidth;
            fanRectangle.VentRect.VentMinDistanceToStart = 2000;
            fanRectangle.VentRect.VentMinDistanceToEnd = 300;
            fanRectangle.VentRect.VentMinDistanceToPrevious = 1000;
            return fanRectangle;
        }
        private FanRectangle VRFFourSideRectangle(VRFFan fan,bool isCold, double correctionFactor) 
        {
            var fanRectangle = new FanRectangle();
            double load = 0.0;
            //计算负荷
            if (isCold)
            {
                double.TryParse(fan.CoolRefrigeratingCapacity, out load);
            }
            else
            {
                double.TryParse(fan.HotRefrigeratingCapacity, out load);
            }
            load *= correctionFactor;
            fanRectangle.Load = load;
            fanRectangle.MinLength = 840;
            fanRectangle.MaxLength = 840;
            fanRectangle.Width = 840;
            fanRectangle.MinVentCount = 0;
            fanRectangle.MaxVentCount = 0;
            fanRectangle.VentRect = new VentRectangle();
            fanRectangle.VentRect.VentWidth = 100;
            fanRectangle.VentRect.VentLength = 100;
            return fanRectangle;
        }
    }
}
