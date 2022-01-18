using System;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout
{
    class IndoorFanDistance
    {
        public const double ReducingLength = 150.0;//风机连接风管处变径长度
        public const double MultipleValue = 50.0;//距离倍数要求，风口中心到风机点长度为50的倍数
        public const double ReturnSideDistanceToStartAdd = 100.0;//回风口边到起点的长度，回风口中心点距边 宽度一半+ ReturnSideDistanceToStartAdd
        public const double LastVentDistanceToEndAdd = 200;//送风口到最后边的长度，中心点距边 宽度一半+LastVentDistanceToEndAdd
        public static double CoilReturnVentCenterDisToFan(FanLoadBase fanLoad, EnumAirReturnType enumAirReturnType)
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength + MultipleValue + fanLoad.ReturnAirSizeLength/2;
            if (enumAirReturnType == EnumAirReturnType.AirReturnPipe)
                returnVentCenterDisTonFan += ReducingLength - 100;
            var col = (int)Math.Floor(returnVentCenterDisTonFan / MultipleValue);
            var remainder = returnVentCenterDisTonFan % MultipleValue;
            returnVentCenterDisTonFan = (remainder * 2) > MultipleValue ? (col + 1) * MultipleValue : col * MultipleValue;
            return returnVentCenterDisTonFan;
        }
        public static double AirReturnVentCenterDisToFan(FanLoadBase fanLoad, EnumAirReturnType enumAirReturnType) 
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength + MultipleValue + 50.0 + fanLoad.ReturnAirSizeLength / 2;
            if (enumAirReturnType == EnumAirReturnType.AirReturnPipe)
                returnVentCenterDisTonFan += ReducingLength;
            return returnVentCenterDisTonFan;
        }
        public static double VRFReturnVentCenterDisToFan(FanLoadBase fanLoad) 
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength + MultipleValue + fanLoad.ReturnAirSizeLength / 2;
            var col = (int)Math.Floor(returnVentCenterDisTonFan / MultipleValue);
            var remainder = returnVentCenterDisTonFan % MultipleValue;
            returnVentCenterDisTonFan = (remainder * 2) > MultipleValue ? (col + 1) * MultipleValue : col * MultipleValue;
            return returnVentCenterDisTonFan;
        }
        public static double CoilFanDistanceToStart(FanLoadBase fanLoad, EnumAirReturnType enumAirReturnType) 
        {
            double ventCenterDisToFan = CoilReturnVentCenterDisToFan(fanLoad, enumAirReturnType);
            return ventCenterDisToFan + fanLoad.ReturnAirSizeLength / 2 + ReturnSideDistanceToStartAdd;
        }
        public static double VRFFanDistanceToStart(FanLoadBase fanLoad)
        {
            var returnVentCenterDisTonFan = VRFReturnVentCenterDisToFan(fanLoad);
            returnVentCenterDisTonFan = returnVentCenterDisTonFan + fanLoad.ReturnAirSizeLength / 2 + ReturnSideDistanceToStartAdd;
            return returnVentCenterDisTonFan;
        }
        public static double DistanceToMultiple(double value,double multiple) 
        {
            var col = (int)Math.Floor(value / multiple);
            var remainder = value % multiple;
            var newValue = (remainder * 2) > multiple ? (col + 1) * multiple : col * multiple;
            return newValue;
        }
    }
}
