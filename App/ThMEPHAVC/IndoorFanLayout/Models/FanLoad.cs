using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout.Models
{
    public abstract class FanLoadBase
    {
        public IndoorFanBase FanBase { get; }
        public string FanNumber { get; }
        public double FanAirVolumeDouble
        {
            get
            {
                var str = FanAirVolume;
                if (string.IsNullOrEmpty(str))
                    return 0.0;
                double.TryParse(str, out double volume);
                return volume;
            }
        }
        public EnumFanType FanType { get; }
        public EnumHotColdType HotColdType { get; }
        public double CorrectionFactor { get; }
        public string FanAirVolume { get; protected set; }
        public double ReturnAirSizeWidth { get; protected set; }
        public double ReturnAirSizeLength { get; protected set; }
        public double FanLoad { get; protected set; }
        /// <summary>
        /// 风机制冷量
        /// </summary>
        public double FanCoolLoad { get; protected set; }
        /// <summary>
        /// 风机实际制冷量（制冷量*系数）
        /// </summary>
        public double FanRealCoolLoad { get; protected set; }
        public double FanHotLoad { get; protected set; }
        public double FanRealHotLoad { get; protected set; }
        public double FanWidth { get; protected set; }
        public double FanHeight { get; protected set; }
        public double FanLength { get; protected set; }
        public int FanVentSizeCount { get; protected set; }
        /// <summary>
        /// 风机功率（W）
        /// </summary>
        public double FanPower { get; set; }
        public string AirSupplyOutletType { get { return FanBase.AirSupplyOutletType; } }
        public FanLoadBase(IndoorFanBase indoorFan, EnumFanType fanType, EnumHotColdType hotColdType, double correctionFactor)
        {
            this.FanBase = indoorFan;
            this.FanType = fanType;
            this.FanNumber = indoorFan.FanNumber;
            this.CorrectionFactor = correctionFactor;
            this.HotColdType = hotColdType;
            this.FanAirVolume = indoorFan.FanAirVolume;
            CalcFanReturnAirSize();
            CalcFanSize();
            CalcFanCount();
            double.TryParse(indoorFan.Power, out double power);
            FanPower = power;
        }
        void CalcFanReturnAirSize()
        {
            string sizeStr = FanBase.ReturnAirOutletSize;
            if (string.IsNullOrEmpty(sizeStr))
                return;
            double width = 0.0;
            double length = 0.0;
            sizeStr = sizeStr.ToLower();
            var spliteWidth = sizeStr.Split('x');
            double.TryParse(spliteWidth.FirstOrDefault(), out width);
            double.TryParse(spliteWidth.Last(), out length);
            this.ReturnAirSizeWidth = width;
            this.ReturnAirSizeLength = length;
        }
        public double GetCoilFanVentSize(int fanCount,out double length)
        {
            //这里是正方形
            double width = 0.0;
            length = 0.0;
            var sizeStr = fanCount>1? FanBase.AirSupplyOutletTwoSize: FanBase.AirSupplyOutletOneSize;
            if (string.IsNullOrEmpty(sizeStr) || !sizeStr.Contains("x"))
                sizeStr = FanBase.AirSupplyOutletOneSize;
            if (string.IsNullOrEmpty(sizeStr))
                return width;
            sizeStr = sizeStr.ToLower();
            var spliteVentWidth = sizeStr.Split('x');
            double.TryParse(spliteVentWidth[0], out length);
            double.TryParse(spliteVentWidth[1], out width);
            return width;
        }
        void CalcFanSize()
        {
            double width = 0.0;
            double.TryParse(FanBase.OverallDimensionWidth, out width);
            double height = 0.0;
            double.TryParse(FanBase.OverallDimensionHeight, out height);
            double length = 0.0;
            double.TryParse(FanBase.OverallDimensionLength, out length);
            FanHeight = height;
            FanLength = length;
            FanWidth = width;
        }
        void CalcFanCount() 
        {
            FanVentSizeCount = 0;
            var oneSize = FanBase.AirSupplyOutletOneSize;
            if (string.IsNullOrEmpty(oneSize))
                return;
            oneSize = oneSize.ToLower().Trim();
            FanVentSizeCount = 1;
            var twoSize = FanBase.AirSupplyOutletTwoSize;
            if (string.IsNullOrEmpty(oneSize))
                return;
            twoSize = twoSize.ToLower().Trim();
            if (string.IsNullOrEmpty(twoSize))
                return;
            if (!twoSize.Contains("x"))
                return;
            FanVentSizeCount = 2;
        }
    }
    class CoilFanLoad : FanLoadBase
    {
        CoilUnitFan coilUnitFan;
        
        public CoilFanLoad(IndoorFanBase indoorFan, EnumFanType fanType, EnumHotColdType hotColdType, double correctionFactor) 
            : base(indoorFan, fanType,hotColdType, correctionFactor) 
        {
            coilUnitFan = indoorFan as CoilUnitFan;
            CalcFanColdHotLoad();
        }
        void CalcFanColdHotLoad() 
        {
            double coolLoad  = 0.0;
            double.TryParse(coilUnitFan.CoolTotalHeat, out coolLoad);
            double hotLoad = 0.0;
            double.TryParse(coilUnitFan.HotHeat, out hotLoad);
            double load = 0.0;
            switch (HotColdType)
            {
                case EnumHotColdType.Cold:
                    load = coolLoad;
                    break;
                case EnumHotColdType.Hot:
                    load = hotLoad;
                    break;
            }
            FanLoad = load * CorrectionFactor;
            FanCoolLoad = coolLoad;
            FanHotLoad = hotLoad;
            FanRealCoolLoad = coolLoad * CorrectionFactor;
            FanRealHotLoad = hotLoad * CorrectionFactor;
        }
        public string GetCoolHotString(out string WaterTemp)
        {
            var coolHotString = string.Format("{0}kW/{1}kW", string.Format("{0:F}", FanRealCoolLoad), string.Format("{0:F}", FanRealHotLoad));

            var coolEnterTemp = 0.0;
            double.TryParse(coilUnitFan.CoolEnterPortWaterTEMP, out coolEnterTemp);
            var coolOutTemp = 0.0;
            double.TryParse(coilUnitFan.CoolExitWaterTEMP, out coolOutTemp);
            var cool = coolOutTemp - coolEnterTemp;

            var hotEnterTemp = 0.0;
            double.TryParse(coilUnitFan.HotEnterPortWaterTEMP, out hotEnterTemp);
            var hotOutTemp = 0.0;
            double.TryParse(coilUnitFan.HotExitWaterTEMP, out hotOutTemp);
            var hot = hotEnterTemp - hotOutTemp;
            WaterTemp = string.Format("{0}℃/{1}℃", string.Format("{0:F}", cool), string.Format("{0:F}", hot));
            return coolHotString;
        }
    }

    class VRFImpellerFanLoad : FanLoadBase 
    {
        VRFFan vRFFan;
        public VRFImpellerFanLoad(IndoorFanBase indoorFan, EnumFanType fanType, EnumHotColdType hotColdType, double correctionFactor)
            : base(indoorFan, fanType, hotColdType, correctionFactor)
        {
            vRFFan = indoorFan as VRFFan;
            CalcFanColdHotLoad();
        }
        void CalcFanColdHotLoad()
        {
            double coolLoad = 0.0;
            double.TryParse(vRFFan.CoolRefrigeratingCapacity, out coolLoad);
            double hotLoad = 0.0;
            double.TryParse(vRFFan.HotRefrigeratingCapacity, out hotLoad);
            double load = 0.0;
            switch (HotColdType)
            {
                case EnumHotColdType.Cold:
                    load = coolLoad;
                    break;
                case EnumHotColdType.Hot:
                    load = hotLoad;
                    break;
            }
            FanLoad = load * CorrectionFactor;
            FanCoolLoad = coolLoad;
            FanHotLoad = hotLoad;
            FanRealCoolLoad = coolLoad * CorrectionFactor;
            FanRealHotLoad = hotLoad * CorrectionFactor;
        }
    }
    class AirConditionFanLoad : FanLoadBase 
    {
        AirConditioninFan airFan;
        public AirConditionFanLoad(IndoorFanBase indoorFan, EnumFanType fanType, EnumHotColdType hotColdType, double correctionFactor)
            : base(indoorFan, fanType, hotColdType, correctionFactor)
        {
            airFan = indoorFan as AirConditioninFan;
            double.TryParse(indoorFan.Power, out double power);
            int.TryParse(airFan.AirConditionCount, out int count);
            base.FanPower = power * count;
        }
        public string GetCoolHotString(out string WaterTemp)
        {
            var coolHotString = string.Format("{0}kW/{1}kW", string.Format("{0:F}", airFan.CoolCoolingCapacity), string.Format("{0:F}", airFan.HotHeatingCapacity));

            var coolEnterTemp = 0.0;
            double.TryParse(airFan.CoolEnterPortWaterTEMP, out coolEnterTemp);
            var coolOutTemp = 0.0;
            double.TryParse(airFan.CoolExitWaterTEMP, out coolOutTemp);
            var cool = coolOutTemp - coolEnterTemp;

            var hotEnterTemp = 0.0;
            double.TryParse(airFan.HotEnterPortWaterTEMP, out hotEnterTemp);
            var hotOutTemp = 0.0;
            double.TryParse(airFan.HotExitWaterTEMP, out hotOutTemp);
            var hot = hotEnterTemp - hotOutTemp;
            WaterTemp = string.Format("{0}℃/{1}℃", string.Format("{0:F}", cool), string.Format("{0:F}", hot));
            return coolHotString;
        }
    }
}
