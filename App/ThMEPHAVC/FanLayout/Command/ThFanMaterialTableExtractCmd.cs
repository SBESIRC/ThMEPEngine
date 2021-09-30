using AcHelper;
using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.FanLayout.ViewModel;

namespace ThMEPHVAC.FanLayout.Command
{
    public class FanFormItem
    {
        public string StrType;//设备类型
        public string StrNumber;//设备编号
        public string StrServiceArea;//服务区域
        public string StrAirVolume;//风量
        public string StrPressure;//全压
        public string StrPower;//功率
        public string StrNoise;//噪声
        public string StrWeight;//重量
        public string StrCount;//台数
        public string StrRemark;//备注
    }
    public class ThFanMaterialTableExtractCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            try
            {
                var wafFanInfoList = ThFanExtractServiece.GetWAFFanConfigInfoList();
                HandleFanInfoList(wafFanInfoList, "壁式轴流风机");
                var WexhFanInfoList = ThFanExtractServiece.GetWEXHFanConfigInfoList();
                HandleFanInfoList(wafFanInfoList, "壁式排气扇");
                var cexhFanInfoList = ThFanExtractServiece.GetCEXHFanConfigInfoList();
                HandleFanInfoList(wafFanInfoList, "吊顶式排气扇");
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        private void HandleFanInfoList(List<ThFanConfigInfo> infoList , string type)
        {
            var fanDictionary = new Dictionary<string, List<ThFanConfigInfo>>();

            foreach (ThFanConfigInfo fan in infoList)
            {
                string key = fan.FanNumber;
                if (fanDictionary.ContainsKey(key))
                {
                    fanDictionary[key].Add(fan);
                }
                else
                {
                    List<ThFanConfigInfo> tmpFanList = new List<ThFanConfigInfo>();
                    tmpFanList.Add(fan);
                    fanDictionary.Add(key, tmpFanList);
                }
            }

            //整理数据，合并，统计等操作
            List<FanFormItem> formItmes = new List<FanFormItem>();
            foreach(var d in fanDictionary)
            {
                FanFormItem tmpItem = new FanFormItem();
                tmpItem.StrType = type;
                tmpItem.StrNumber = d.Key;
                tmpItem.StrServiceArea = "";
                tmpItem.StrAirVolume = d.Value[0].FanVolume.ToString();
                tmpItem.StrPressure = d.Value[0].FanPressure.ToString();
                tmpItem.StrPower = d.Value[0].FanPower.ToString();
                tmpItem.StrNoise = d.Value[0].FanNoise.ToString();
                tmpItem.StrWeight = "";
                tmpItem.StrCount = d.Value.Count.ToString();
                tmpItem.StrRemark ="";
                formItmes.Add(tmpItem);
            }

            if(type == "壁式轴流风机" && type == "壁式排气扇")
            {

            }
            else if(type == "吊顶式排气扇")
            {

            }
        }
    }
}
