using System.Collections.Generic;
using System.Data;
using ThMEPHVAC.IndoorFanModels;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    class ReadFanData
    {
        public List<CoilUnitFan> GetCoilUnitFanDatas(DataTable dataTable, int startRow)
        {
            var coilUnitFans = new List<CoilUnitFan>();
            var rowCount = dataTable.Rows.Count;
            for (int i = startRow; i < rowCount; i++)
            {
                var row = dataTable.Rows[i];
                var coulmmCount = row.ItemArray.Length;
                var fanData = new CoilUnitFan();
                for (int j = 0; j < coulmmCount; j++)
                {
                    //复杂表头解析目前没有什么好的方式，先固定列
                    var cell = row[j];
                    var value = cell.ToString();
                    if (j == 0 && string.IsNullOrEmpty(value))
                        continue;
                    value = string.IsNullOrEmpty(value) ? "-" : value;
                    switch (j)
                    {
                        case 0://设备编号
                            fanData.FanNumber = value;
                            break;
                        case 1:
                            fanData.FanLayout = value;
                            break;
                        case 2://风机风量
                            //double.TryParse(value, out double dValue);
                            fanData.FanAirVolume = value;
                            break;
                        case 3://风机机外静压
                            //double.TryParse(value, out double dExternalValue);
                            fanData.ExternalStaticVoltage = value;
                            break;
                        case 4://风机电源
                            fanData.PowerSupply = value;
                            break;
                        case 5://风机功率
                            //double.TryParse(value, out double dPower);
                            fanData.Power = value;
                            break;
                        case 6://冷却盘管全热
                            //double.TryParse(value, out double cTHeat);
                            fanData.CoolTotalHeat = value;
                            break;
                        case 7://冷却盘管显热
                            //double.TryParse(value, out double cSHeat);
                            fanData.CoolShowHeat = value;
                            break;
                        case 8://冷却盘管进风干球
                            //double.TryParse(value, out double cDryBall);
                            fanData.CoolAirInletDryBall = value;
                            break;
                        case 9://冷却盘管进风相对湿度
                            //double.TryParse(value, out double cHumidity);
                            fanData.CoolAirInletHumidity = value;
                            break;
                        case 10://冷却盘管进口水温
                            //double.TryParse(value, out double cETEMP);
                            fanData.CoolEnterPortWaterTEMP = value;
                            break;
                        case 11://冷却盘管出口水温
                            //double.TryParse(value, out double cOTEMP);
                            fanData.CoolExitWaterTEMP = value;
                            break;
                        case 12://冷却盘管接管尺寸
                            fanData.CoolPipeSize = value;
                            break;
                        case 13://冷却盘管工作压力
                            //double.TryParse(value, out double cWXef);
                            fanData.CoolWorkXeF = value;
                            break;
                        case 14://冷却盘管压降
                            //double.TryParse(value, out double cXefDrop);
                            fanData.CoolXeFDrop = value;
                            break;
                        case 15://冷却盘管流量
                            //double.TryParse(value, out double cFlow);
                            fanData.CoolFlow = value;
                            break;

                        case 16://加热盘管热量
                            //double.TryParse(value, out double hTHeat);
                            fanData.HotHeat = value;
                            break;
                        case 17:
                            //double.TryParse(value, out double hDryBall);
                            fanData.HotAirInletDryBall = value;
                            break;
                        case 18:
                            //double.TryParse(value, out double hETEMP);
                            fanData.HotEnterPortWaterTEMP = value;
                            break;
                        case 19:
                            //double.TryParse(value, out double hOTEMP);
                            fanData.HotExitWaterTEMP = value;
                            break;
                        case 20:
                            fanData.HotPipSize = value;
                            break;
                        case 21:
                            //double.TryParse(value, out double hWXef);
                            fanData.HotWorkXeF = value;
                            break;
                        case 22:
                            //double.TryParse(value, out double hXefDrop);
                            fanData.HotXeFDrop = value;
                            break;
                        case 23:
                            //double.TryParse(value, out double hFlow);
                            fanData.HotFlow = value;
                            break;
                        case 24:
                            fanData.Noise = value;
                            break;
                        case 25://外形尺寸-宽
                            fanData.OverallDimensionWidth = value;
                            break;
                        case 26:
                            fanData.OverallDimensionHeight = value;
                            break;
                        case 27:
                            fanData.OverallDimensionLength = value;
                            break;
                        case 28:
                            fanData.AirSupplyuctSize = value;
                            break;
                        case 29:
                            //送风口形式
                            fanData.AirSupplyOutletType = value;
                            break;
                        case 30:
                            //送风口尺寸一个
                            fanData.AirSupplyOutletOneSize = value;
                            break;
                        case 31:
                            //送风口尺寸两个
                            fanData.AirSupplyOutletTwoSize = value;
                            break;
                        case 32://回风口尺寸
                            fanData.ReturnAirOutletSize = value;
                            break;
                        case 33://数量
                            fanData.FanCount = value;
                            break;
                        case 34://备注
                            fanData.Remarks = value;
                            break;
                    }
                }
                if (string.IsNullOrEmpty(fanData.FanNumber) || string.IsNullOrEmpty(fanData.PowerSupply) || fanData.PowerSupply.Equals("-"))
                    continue;
                coilUnitFans.Add(fanData);
            }
            return coilUnitFans;
        }

        public List<VRFFan> GetVRFPipeFanDatas(DataTable dataTable, int startRow)
        {
            var coilUnitFans = new List<VRFFan>();
            var rowCount = dataTable.Rows.Count;
            for (int i = startRow; i < rowCount; i++)
            {
                var row = dataTable.Rows[i];
                var coulmmCount = row.ItemArray.Length;
                var fanData = new VRFFan();
                for (int j = 0; j < coulmmCount; j++)
                {
                    //复杂表头解析目前没有什么好的方式，先固定列
                    var cell = row[j];
                    var value = cell.ToString();
                    if (j == 0 && string.IsNullOrEmpty(value))
                        continue;
                    value = string.IsNullOrEmpty(value) ? "-" : value;
                    switch (j)
                    {
                        case 0://设备编号
                            fanData.FanNumber = value;
                            break;
                        case 1://制冷工况 制冷量
                            //double.TryParse(value, out double volume);
                            fanData.CoolRefrigeratingCapacity = value;
                            break;
                        case 2://冷却盘管进风干球
                            //double.TryParse(value, out double cDryBall);
                            fanData.CoolAirInletDryBall = value;
                            break;
                        case 3://冷却盘管进风相对湿度
                            //double.TryParse(value, out double cHumidity);
                            fanData.CoolAirInletWetBall = value;
                            break;
                        case 4:
                            //double.TryParse(value, out double cOutTemp);
                            fanData.CoolOutdoorTemperature = value;
                            break;
                        case 5:
                            fanData.HotRefrigeratingCapacity = value;
                            break;
                        case 6:
                            //double.TryParse(value, out double hDryBall);
                            fanData.HotAirInletDryBall = value;
                            break;
                        case 7:
                            //double.TryParse(value, out double hOutTemp);
                            fanData.HotOutdoorTemperature = value;
                            break;
                        case 8://风机风量
                            //double.TryParse(value, out double dValue);
                            fanData.FanAirVolume = value;
                            break;
                        case 9://风机机外静压
                            //double.TryParse(value, out double dExternalValue);
                            fanData.ExternalStaticVoltage = value;
                            break;
                        case 10://风机电源
                            fanData.PowerSupply = value;
                            break;
                        case 11://风机功率
                            //double.TryParse(value, out double dPower);
                            fanData.Power = value;
                            break;
                        case 12:
                            fanData.Noise = value;
                            break;
                        case 13:
                            fanData.Weight = value;
                            break;
                        case 14://外形尺寸
                            fanData.OverallDimensionWidth = value;
                            break;
                        case 15:
                            fanData.OverallDimensionHeight = value;
                            break;
                        case 16:
                            fanData.OverallDimensionLength = value;
                            break;
                        case 17:
                            fanData.AirSupplyuctSize = value;
                            break;
                        case 18:
                            //送风口形式
                            fanData.AirSupplyOutletType = value;
                            break;
                        case 19:
                            //送风口尺寸一个
                            fanData.AirSupplyOutletOneSize = value;
                            break;
                        case 20:
                            //送风口尺寸两个
                            fanData.AirSupplyOutletTwoSize = value;
                            break;
                        case 21:
                            //回风口
                            fanData.ReturnAirOutletSize = value;
                            break;
                        case 22: //数量
                            fanData.FanCount = value;
                            break;
                    }
                }
                if (string.IsNullOrEmpty(fanData.FanNumber) || string.IsNullOrEmpty(fanData.PowerSupply) || fanData.PowerSupply.Equals("-"))
                    continue;
                coilUnitFans.Add(fanData);
            }
            return coilUnitFans;
        }

        public List<VRFFan> GetVRFFanDatas(DataTable dataTable, int startRow)
        {
            var vrfFans = new List<VRFFan>();
            var rowCount = dataTable.Rows.Count;
            for (int i = startRow; i < rowCount; i++)
            {
                var row = dataTable.Rows[i];
                var coulmmCount = row.ItemArray.Length;
                var fanData = new VRFFan();
                for (int j = 0; j < coulmmCount; j++)
                {
                    //复杂表头解析目前没有什么好的方式，先固定列
                    var cell = row[j];
                    var value = cell.ToString();
                    if (j == 0 && string.IsNullOrEmpty(value))
                        continue;
                    value = string.IsNullOrEmpty(value) ? "-" : value;
                    switch (j)
                    {
                        case 0://设备编号
                            fanData.FanNumber = value;
                            break;
                        case 1://制冷量
                            //double.TryParse(value, out double volume);
                            fanData.CoolRefrigeratingCapacity = value;
                            break;
                        case 2://冷却盘管进风干球
                            //double.TryParse(value, out double cDryBall);
                            fanData.CoolAirInletDryBall = value;
                            break;
                        case 3://冷却盘管进风相对湿度
                            //double.TryParse(value, out double cHumidity);
                            fanData.CoolAirInletWetBall = value;
                            break;
                        case 4:
                            //double.TryParse(value, out double cOutTemp);
                            fanData.CoolOutdoorTemperature = value;
                            break;
                        case 5:
                            fanData.HotRefrigeratingCapacity = value;
                            break;
                        case 6:
                            //double.TryParse(value, out double hDryBall);
                            fanData.HotAirInletDryBall = value;
                            break;
                        case 7:
                            //double.TryParse(value, out double hOutTemp);
                            fanData.HotOutdoorTemperature = value;
                            break;
                        case 8://风机风量
                            //double.TryParse(value, out double dValue);
                            fanData.FanAirVolume = value;
                            break;
                        case 9://风机机外静压
                            //double.TryParse(value, out double dExternalValue);
                            fanData.ExternalStaticVoltage = value;
                            break;
                        case 10://风机电源
                            fanData.PowerSupply = value;
                            break;
                        case 11://风机功率
                            //double.TryParse(value, out double dPower);
                            fanData.Power = value;
                            break;
                        case 12:
                            fanData.Noise = value;
                            break;
                        case 13:
                            fanData.Weight = value;
                            break;
                        case 14://外形尺寸
                            fanData.OverallDimensionWidth = value;
                            break;
                        case 15:
                            fanData.OverallDimensionHeight = value;
                            break;
                        case 16:
                            fanData.OverallDimensionLength = value;
                            break;
                        case 17: //数量
                            fanData.FanCount = value;
                            break;
                    }
                }
                if (string.IsNullOrEmpty(fanData.FanNumber) || string.IsNullOrEmpty(fanData.PowerSupply) || fanData.PowerSupply.Equals("-"))
                    continue;
                vrfFans.Add(fanData);
            }
            return vrfFans;
        }

        public List<AirConditioninFan> GetAirConditioninDatas(DataTable dataTable, int startRow)
        {
            var airFans = new List<AirConditioninFan>();
            var rowCount = dataTable.Rows.Count;
            for (int i = startRow; i < rowCount; i++)
            {
                var row = dataTable.Rows[i];
                var coulmmCount = row.ItemArray.Length;
                var fanData = new AirConditioninFan();
                for (int j = 0; j < coulmmCount; j++)
                {
                    //复杂表头解析目前没有什么好的方式，先固定列
                    var cell = row[j];
                    var value = cell.ToString();
                    if (j == 0 && string.IsNullOrEmpty(value))
                        continue;
                    value = string.IsNullOrEmpty(value) ? "-" : value;
                    switch (j)
                    {
                        case 0://设备编号
                            fanData.FanNumber = value;
                            break;
                        case 1://制冷量
                            //double.TryParse(value, out double volume);
                            fanData.FanAirVolume = value;
                            break;
                        case 2://风机全压
                            //double.TryParse(value, out double cFPress);
                            fanData.FanFullPressure = value;
                            break;
                        case 3://风机余压
                            //double.TryParse(value, out double cHumidity);
                            fanData.FanResidualPressure = value;
                            break;
                        case 4://风机电源
                            fanData.PowerSupply = value;
                            break;
                        case 5://风机功率
                            //double.TryParse(value, out double dPower);
                            fanData.Power = value;
                            break;
                        case 6://风机台数
                            fanData.AirConditionCount = value;
                            break;
                        case 7://盘管排数
                            fanData.FanCoilRow = value;
                            break;
                        case 8://冷却工况冷量
                            //double.TryParse(value, out double cCapacity);
                            fanData.CoolCoolingCapacity = value;
                            break;
                        case 9://冷却工况 进风 干球温度
                            //double.TryParse(value, out double cDryBall);
                            fanData.CoolAirInletDryBall = value;
                            break;
                        case 10://冷却工况 进风 湿球温度
                            //double.TryParse(value, out double cWetBall);
                            fanData.CoolAirInletWetBall = value;
                            break;
                        case 11://冷却工况 进口水温
                            //double.TryParse(value, out double cEnterTemp);
                            fanData.CoolEnterPortWaterTEMP = value;
                            break;
                        case 12://冷却工况 出口水温
                            //double.TryParse(value, out double cOutTemp);
                            fanData.CoolExitWaterTEMP = value;
                            break;
                        case 13://冷却工况 流量
                            //double.TryParse(value, out double cFlow);
                            fanData.CoolFlow = value;
                            break;
                        case 14://冷却工况 水侧阻力
                            //double.TryParse(value, out double cHResistance);
                            fanData.CoolHydraulicResistance = value;
                            break;
                        case 15://加热工况 热量
                            //double.TryParse(value, out double hCapacity);
                            fanData.HotHeatingCapacity = value;
                            break;
                        case 16://加热工况 进风温度
                            //double.TryParse(value, out double hAirTemp);
                            fanData.HotAirInletTEMP = value;
                            break;
                        case 17://加热工况 进口水温
                            //double.TryParse(value, out double hEWTEMP);
                            fanData.HotEnterPortWaterTEMP = value;
                            break;
                        case 18://加热工况 出口水温
                            //double.TryParse(value, out double hExitWTEMP);
                            fanData.HotExitWaterTEMP = value;
                            break;
                        case 19:
                            //double.TryParse(value, out double hWXef);
                            fanData.HotWorkXeF = value;
                            break;
                        case 20:
                            //double.TryParse(value, out double hFlow);
                            fanData.HotFlow = value;
                            break;
                        case 21://分支水管 冷/热水 管径
                            fanData.BruchCollHotWaterPipeSize = value;
                            break;
                        case 22://分支水管 冷凝水 管径
                            fanData.BruchCondensationPipeSize = value;
                            break;
                        case 23:
                            fanData.Noise = value;
                            break;
                        case 24:
                            fanData.Weight = value;
                            break;
                        case 25://外形尺寸宽
                            //double.TryParse(value, out double dWidth);
                            fanData.OverallDimensionWidth = value;
                            break;
                        case 26://外形尺寸高
                            //double.TryParse(value, out double dHeight);
                            fanData.OverallDimensionHeight = value;
                            break;
                        case 27://外形尺寸深
                            //double.TryParse(value, out double dLength);
                            fanData.OverallDimensionLength = value;
                            break;
                        case 28:
                            fanData.AirSupplyuctSize = value;
                            break;
                        case 29:
                            fanData.ReturnAirOutletSize = value;
                            break;
                        case 30://过滤器
                            fanData.Filter = value;
                            break;
                        case 31://数量
                            fanData.FanCount = value;
                            break;
                        case 32://减震方式
                            fanData.DampingMode = value;
                            break;
                        case 33://备注
                            fanData.Remarks = value;
                            break;
                    }
                }
                if (string.IsNullOrEmpty(fanData.FanNumber) || string.IsNullOrEmpty(fanData.PowerSupply) || fanData.PowerSupply.Equals("-"))
                    continue;
                airFans.Add(fanData);
            }
            return airFans;
        }
    }
}
