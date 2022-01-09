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
        public FanRectangle GetTestFanRectangle() 
        {
            var rectangle = new FanRectangle();
            rectangle.Width = 1000.0;
            rectangle.Load = 6.69; //6.69;//5.52;
            rectangle.MinLength = 2000;
            rectangle.MaxLength = 5000;
            rectangle.MinVentCount = 1;
            rectangle.MaxVentCount = 2;
            rectangle.VentRect = new VentRectangle();
            rectangle.VentRect.VentLength = 400;
            rectangle.VentRect.VentWidth = 400;
            rectangle.VentRect.VentMinDistanceToStart = 1500;
            rectangle.VentRect.VentMinDistanceToEnd = 500;
            rectangle.VentRect.VentMinDistanceToPrevious = 2000;
            return rectangle;
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
        public FanRectangle GetFanRectangle(string fanTypeName, EnumFanType fanType,bool isCold, double correctionFactor) 
        {
            
            FanRectangle fanRectangle =null;
            var fanModel = GetFanModel(fanTypeName);
            switch (fanType) 
            {
                case EnumFanType.FanCoilUnitTwoControls:
                case EnumFanType.FanCoilUnitFourControls:
                    fanRectangle = CoilFanRectangle(fanModel as CoilUnitFan,isCold, correctionFactor);
                    break;
                case EnumFanType.VRFConditioninConduit:
                    fanRectangle = VRFFanRectangle(fanModel as VRFFan,isCold, correctionFactor);
                    break;
                case EnumFanType.VRFConditioninFourSides:
                    fanRectangle = VRFFourSideRectangle(fanModel as VRFFan,isCold, correctionFactor);
                    break;
            }
            if (fanRectangle != null)
                fanRectangle.Name = fanTypeName;
            return fanRectangle;
        }
        private FanRectangle CoilFanRectangle(CoilUnitFan coilUnitFan,bool isCold, double correctionFactor) 
        {
            var fanRectangle = new FanRectangle();
            double load = 0.0;
            //计算负荷
            if (isCold)
            {
                double.TryParse(coilUnitFan.CoolTotalHeat, out load);
            }
            else 
            {
                double.TryParse(coilUnitFan.HotHeat, out load);
            }
            load *= correctionFactor;
            fanRectangle.Load = load;
            fanRectangle.MinLength = 1600;
            fanRectangle.MaxLength = 5000;
            var size = coilUnitFan.AirSupplyuctSize.ToLower();
            var spliteWidth = size.Split('x');
            double width = 0.0;
            double.TryParse(spliteWidth[0], out width);
            fanRectangle.Width = width;
            fanRectangle.MinVentCount = 1;
            fanRectangle.VentRect = new VentRectangle();
            var oneSize = coilUnitFan.AirSupplyOutletOneSize.ToLower();
            var twoSize = coilUnitFan.AirSupplyOutletTwoSize;
            fanRectangle.MaxVentCount = 1;
            if (!string.IsNullOrEmpty(twoSize) && twoSize.ToLower().Contains("x"))
                fanRectangle.MaxVentCount = 2;
            var spliteVentWidth = oneSize.Split('x');
            double ventWidth = 0.0;
            double.TryParse(spliteVentWidth[0], out ventWidth);
            fanRectangle.VentRect.VentWidth = ventWidth;
            fanRectangle.VentRect.VentLength = ventWidth;
            fanRectangle.VentRect.VentMinDistanceToStart = 1300;
            fanRectangle.VentRect.VentMinDistanceToEnd = 300;
            fanRectangle.VentRect.VentMinDistanceToPrevious = 2000;
            return fanRectangle;
        }
        private FanRectangle VRFFanRectangle(VRFFan fan, bool isCold,double correctionFactor) 
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
            fanRectangle.MinLength = 2300;
            fanRectangle.MaxLength = 5000;
            var size = fan.AirSupplyuctSize.ToLower();
            var spliteWidth = size.Split('x');
            double width = 0.0;
            double.TryParse(spliteWidth[0], out width);
            fanRectangle.Width = width;
            fanRectangle.MinVentCount = 1;
            fanRectangle.VentRect = new VentRectangle();
            var oneSize = fan.AirSupplyOutletOneSize.ToLower();
            var twoSize = fan.AirSupplyOutletTwoSize;
            fanRectangle.MaxVentCount = 1;
            if (!string.IsNullOrEmpty(twoSize) && twoSize.ToLower().Contains("x"))
                fanRectangle.MaxVentCount = 2;
            var spliteVentWidth = oneSize.Split('x');
            double ventWidth = 0.0;
            double.TryParse(spliteVentWidth[0], out ventWidth);
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
