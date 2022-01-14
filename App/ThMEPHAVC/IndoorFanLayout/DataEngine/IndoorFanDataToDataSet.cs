using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout.DataEngine
{
    class IndoorFanDataToDataSet
    {
        /// <summary>
        /// 两管或四管制风机转为dataTable
        /// </summary>
        /// <param name="coilUnitFans"></param>
        /// <returns></returns>
        public DataTable FansToDataTable(List<CoilUnitFan> coilUnitFans)
        {
            DataTable dataTable = new DataTable();
            //构建datatable的列
            for (int i = 0; i < 35; i++)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            foreach (var fanData in coilUnitFans)
            {
                var dataRow = dataTable.NewRow();

                dataRow[0] = fanData.FanNumber;//设备编号
                dataRow[1] = fanData.FanLayout;
                dataRow[2] = fanData.FanAirVolume;//风机风量
                dataRow[3] = fanData.ExternalStaticVoltage;//风机机外静压
                dataRow[4] = fanData.PowerSupply;//风机电源
                dataRow[5] = fanData.Power;//风机功率
                dataRow[6] = fanData.CoolTotalHeat;//冷却盘管全热
                dataRow[7] = fanData.CoolShowHeat;//冷却盘管显热
                dataRow[8] = fanData.CoolAirInletDryBall;//冷却盘管进风干球
                dataRow[9] = fanData.CoolAirInletHumidity;//冷却盘管进风相对湿度
                dataRow[10] = fanData.CoolEnterPortWaterTEMP;//冷却盘管进口水温
                dataRow[11] = fanData.CoolExitWaterTEMP;//冷却盘管出口水温
                dataRow[12] = fanData.CoolPipeSize;//冷却盘管接管尺寸
                dataRow[13] = fanData.CoolWorkXeF;//冷却盘管工作压力
                dataRow[14] = fanData.CoolXeFDrop;//冷却盘管压降
                dataRow[15] = fanData.CoolFlow;//冷却盘管流量
                dataRow[16] = fanData.HotHeat;//加热盘管热量
                dataRow[17] = fanData.HotAirInletDryBall;//冷却盘管接管尺寸
                dataRow[18] = fanData.HotEnterPortWaterTEMP;//冷却盘管接管尺寸
                dataRow[19] = fanData.HotExitWaterTEMP;//冷却盘管接管尺寸
                dataRow[20] = fanData.HotPipSize;//冷却盘管接管尺寸
                dataRow[21] = fanData.HotWorkXeF;//冷却盘管接管尺寸
                dataRow[22] = fanData.HotXeFDrop;//冷却盘管接管尺寸
                dataRow[23] = fanData.HotFlow;//冷却盘管接管尺寸
                dataRow[24] = fanData.Noise;//冷却盘管接管尺寸
                dataRow[25] = fanData.OverallDimensionWidth;//冷却盘管接管尺寸
                dataRow[26] = fanData.OverallDimensionHeight;//冷却盘管接管尺寸
                dataRow[27] = fanData.OverallDimensionLength;//冷却盘管接管尺寸
                dataRow[28] = fanData.AirSupplyuctSize;//冷却盘管接管尺寸
                dataRow[29] = fanData.AirSupplyOutletType;//送风口形式
                dataRow[30] = fanData.AirSupplyOutletOneSize;//送风口尺寸一个
                dataRow[31] = fanData.AirSupplyOutletTwoSize;//送风口尺寸两个
                dataRow[32] = fanData.ReturnAirOutletSize;//冷却盘管接管尺寸
                dataRow[33] = fanData.FanCount;//数量
                dataRow[34] = fanData.Remarks;//备注

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        /// <summary>
        /// 吊顶一体式空调风机转为dataTabta
        /// </summary>
        /// <param name="airFans"></param>
        /// <returns></returns>
        public DataTable FansToDataTable(List<AirConditioninFan> airFans)
        {
            DataTable dataTable = new DataTable();
            //构建datatable的列
            for (int i = 0; i < 34; i++)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            foreach (var fanData in airFans)
            {
                var dataRow = dataTable.NewRow();
                dataRow[0] = fanData.FanNumber;//设备编号
                dataRow[1] = fanData.FanAirVolume;//风机风量
                dataRow[2] = fanData.FanFullPressure;//风机全压
                dataRow[3] = fanData.FanResidualPressure;//风机余压
                dataRow[4] = fanData.PowerSupply;//风机电源
                dataRow[5] = fanData.Power;//风机功率（单台）
                dataRow[6] = fanData.AirConditionCount;//风机台数
                dataRow[7] = fanData.FanCoilRow;
                dataRow[8] = fanData.CoolCoolingCapacity;
                dataRow[9] = fanData.CoolAirInletDryBall;
                dataRow[10] = fanData.CoolAirInletWetBall;
                dataRow[11] = fanData.CoolEnterPortWaterTEMP;
                dataRow[12] = fanData.CoolExitWaterTEMP;
                dataRow[13] = fanData.CoolFlow;
                dataRow[14] = fanData.CoolHydraulicResistance;
                dataRow[15] = fanData.HotHeatingCapacity;
                dataRow[16] = fanData.HotAirInletTEMP;
                dataRow[17] = fanData.HotEnterPortWaterTEMP;
                dataRow[18] = fanData.HotExitWaterTEMP;
                dataRow[19] = fanData.HotWorkXeF;
                dataRow[20] = fanData.HotFlow;
                dataRow[21] = fanData.BruchCollHotWaterPipeSize;
                dataRow[22] = fanData.BruchCondensationPipeSize;
                dataRow[23] = fanData.Noise;
                dataRow[24] = fanData.Weight;
                dataRow[25] = fanData.OverallDimensionWidth;
                dataRow[26] = fanData.OverallDimensionHeight;
                dataRow[27] = fanData.OverallDimensionLength;
                dataRow[28] = fanData.AirSupplyuctSize;
                dataRow[29] = fanData.ReturnAirOutletSize;
                dataRow[30] = fanData.Filter;
                dataRow[31] = fanData.FanCount;
                dataRow[32] = fanData.DampingMode;
                dataRow[33] = fanData.Remarks;
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }

        public DataTable FansToDataTable(List<VRFFan> varFans, bool isPipeFan)
        {
            DataTable dataTable = new DataTable();
            //构建datatable的列
            int columnCount = isPipeFan ? 23 : 18;
            for (int i = 0; i < columnCount; i++)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            foreach (var fanData in varFans)
            {
                var dataRow = dataTable.NewRow();
                dataRow[0] = fanData.FanNumber;//设备编号
                dataRow[1] = fanData.CoolRefrigeratingCapacity;//风机风量
                dataRow[2] = fanData.CoolAirInletDryBall;//风机全压
                dataRow[3] = fanData.CoolAirInletWetBall;//风机余压
                dataRow[4] = fanData.CoolOutdoorTemperature;//风机电源
                dataRow[5] = fanData.HotRefrigeratingCapacity;//风机功率（单台）
                dataRow[6] = fanData.HotAirInletDryBall;//风机台数
                dataRow[7] = fanData.HotOutdoorTemperature;
                dataRow[8] = fanData.FanAirVolume;
                dataRow[9] = fanData.ExternalStaticVoltage;
                dataRow[10] = fanData.PowerSupply;
                dataRow[11] = fanData.Power;
                dataRow[12] = fanData.Noise;
                dataRow[13] = fanData.Weight;
                dataRow[14] = fanData.OverallDimensionWidth;
                dataRow[15] = fanData.OverallDimensionHeight;
                dataRow[16] = fanData.OverallDimensionLength;
                if (isPipeFan)
                {
                    dataRow[17] = fanData.AirSupplyuctSize;
                    dataRow[18] = fanData.AirSupplyOutletType;
                    dataRow[19] = fanData.AirSupplyOutletOneSize;
                    dataRow[20] = fanData.AirSupplyOutletTwoSize;
                    dataRow[21] = fanData.ReturnAirOutletSize;
                    dataRow[22] = fanData.FanCount;
                }
                else
                {
                    dataRow[17] = fanData.FanCount;
                }

                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }
    }
}
