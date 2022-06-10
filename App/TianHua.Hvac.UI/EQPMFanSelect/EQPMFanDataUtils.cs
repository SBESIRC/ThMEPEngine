using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    class EQPMFanDataUtils
    {
        public static void SetFanModelParameter(FanDataModel fanDataModel, FanModelPicker basePick, FanModelPicker selectPick) 
        {
            if (basePick ==null || selectPick == null)
                return;
            fanDataModel.FanModelCCCF = selectPick.Model;
            if (fanDataModel.VentStyle == EnumFanModelType.AxialFlow)
            {
                var baseParameter = EQPMFanDataService.Instance.GetAxialFanParameters(fanDataModel.Control, basePick);
                var axialParameter = EQPMFanDataService.Instance.GetAxialFanParameters(fanDataModel.Control, selectPick);
                if (baseParameter  == null || null == axialParameter)
                    return;
                fanDataModel.FanModelTypeCalcModel.FanModelName = axialParameter.ModelNum;
                fanDataModel.FanModelTypeCalcModel.FanModelNum = axialParameter.No;

                fanDataModel.FanModelTypeCalcModel.FanModelFanSpeed = baseParameter.Rpm;
                fanDataModel.FanModelTypeCalcModel.FanModelNoise = baseParameter.Noise;
                fanDataModel.IsPointSafe = !basePick.IsOptimalModel;

                fanDataModel.FanModelTypeCalcModel.FanModelLength = axialParameter.Length;
                fanDataModel.FanModelTypeCalcModel.FanModelWeight = axialParameter.Weight;
                fanDataModel.FanModelTypeCalcModel.FanModelDIA = axialParameter.Diameter;
            }
            else
            {
                var fanParameter = EQPMFanDataService.Instance.GetFanParameters(fanDataModel.Control, fanDataModel.VentStyle, selectPick);
                var baseParameter = EQPMFanDataService.Instance.GetFanParameters(fanDataModel.Control, fanDataModel.VentStyle, basePick);
                if (null == fanParameter)
                    return;

                fanDataModel.FanModelTypeCalcModel.FanModelName = fanParameter.CCCF_Spec;
                fanDataModel.FanModelTypeCalcModel.FanModelNum = fanParameter.No;

                fanDataModel.FanModelTypeCalcModel.FanModelFanSpeed = baseParameter.Rpm;
                fanDataModel.FanModelTypeCalcModel.FanModelNoise = baseParameter.Noise;
                fanDataModel.IsPointSafe = !basePick.IsOptimalModel;

                fanDataModel.FanModelTypeCalcModel.FanModelLength = fanParameter.Length;
                fanDataModel.FanModelTypeCalcModel.FanModelWeight = fanParameter.Weight;
                fanDataModel.FanModelTypeCalcModel.FanModelWidth = fanParameter.Width;
                fanDataModel.FanModelTypeCalcModel.FanModelHeight = fanParameter.Height;
                var str = CommonUtil.GetEnumDescription(fanDataModel.VentStyle);
                if (str.Contains("电机内置"))
                {
                    fanDataModel.FanModelTypeCalcModel.FanModelLength = fanParameter.Length2;
                    fanDataModel.FanModelTypeCalcModel.FanModelWidth = fanParameter.Width1;
                    fanDataModel.FanModelTypeCalcModel.FanModelHeight = fanParameter.Height2;
                }
            }
        }
        public static List<FanDataViewModel> OrderFanViewModels(List<FanDataViewModel> fanDatas,bool isDes) 
        {
            var resModels = new List<FanDataViewModel>();
            var allCModels = new List<FanDataViewModel>();
            var allPModels = new List<FanDataViewModel>();
            foreach (var item in fanDatas) 
            {
                if (item.IsChildFan)
                    allCModels.Add(item);
                else
                    allPModels.Add(item);
            }
            //风机排序，先根据子项，楼层进行分组，最后再根据楼层中的最小楼层进行排序
            var allChildStr = OrderString(allPModels.Select(c => c.InstallSpace).ToList(), isDes);//allPModels.Select(c => c.InstallSpace).Distinct().ToList().OrderBy(c=>c).ToList();
            //if(isDes)
            //    allChildStr = allChildStr.OrderByDescending(c => c).ToList();
            foreach (var str in allChildStr) 
            {
                var thisChildList = allPModels.Where(c => c.InstallSpace == str).ToList();
                var allFloorStr = OrderString(thisChildList.Select(c => c.InstallFloor).ToList(), isDes);//thisChildList.Select(c => c.InstallFloor).Distinct().ToList().OrderBy(c => c).ToList();
                foreach (var floorStr in allFloorStr) 
                {
                    var thisFans = thisChildList.Where(c => c.InstallFloor == floorStr).ToList();
                    if (isDes)
                        thisFans = thisFans.OrderByDescending(c => c.fanDataModel.ListVentQuan.FirstOrDefault()).ToList();
                    else
                        thisFans = thisFans.OrderBy(c => c.fanDataModel.ListVentQuan.FirstOrDefault()).ToList();
                    resModels.AddRange(thisFans);
                }
            }
            foreach (var item in allCModels) 
            {
                var pIndex = resModels.FindIndex(c => !c.IsChildFan && c.fanDataModel.ID == item.fanDataModel.PID);
                resModels.Insert(pIndex+1, item);
            }
            return resModels;
        }
        public static void ChangeFanViewModelOrderIds(List<FanDataViewModel> fanDatas,bool isSortScenario)
        {
            if (null == fanDatas || fanDatas.Count < 1)
                return;
            for (int i = 0; i < fanDatas.Count; i++) 
            {
                int number = i + 1;
                if(isSortScenario)
                    fanDatas[i].fanDataModel.SortScenario = number * 2;
                else
                    fanDatas[i].fanDataModel.SortID = number * 2;
            }
        }
        private static List<string> OrderString(List<string> targetStrs,bool isDes) 
        {
            var orderRes = new List<string>();
            var stringOrders = new List<StringOrderModel>();
            foreach (var item in targetStrs) 
            {
                if (stringOrders.Any(c => c.StrValue == item))
                    continue;
                stringOrders.Add(new StringOrderModel(item));
            }
            //数字开头优先排序，后面在排字母开头
            var numberStart = stringOrders.Where(c => c.IsNumberStart).ToList();
            if (isDes)
                numberStart = numberStart.OrderByDescending(c => c.Order).ToList();
            else
                numberStart = numberStart.OrderBy(c => c.Order).ToList();
            orderRes.AddRange(numberStart.Select(c => c.StrValue).ToList());
            //字母开头，按字符串排序后在按数字排序
            var strStart = stringOrders.Where(c => !c.IsNumberStart).ToList();
            if (isDes)
                strStart = strStart.OrderByDescending(c => c.StrValue).OrderByDescending(c => c.Order).ToList();
            else
                strStart = strStart.OrderBy(c => c.StrValue).ThenBy(c => c.Order).ToList();
            orderRes.AddRange(strStart.Select(c => c.StrValue).ToList());
            return orderRes;
        }
    }
    class StringOrderModel
    {
        public string StrValue { get; set; }
        public bool IsNumberStart { get; set; }
        public bool HaveNumber { get; set; }
        public int Order { get; set; }
        public StringOrderModel(string strValue)
        {
            if (string.IsNullOrEmpty(strValue)) 
            {
                strValue = "";
                IsNumberStart = false;
                HaveNumber = false;
                Order = 0;
                return;
            }
            StrValue = strValue;
            IsNumberStart = Regex.IsMatch(strValue, "^\\d");
            var strNum  = Regex.Replace(strValue, @"[^0-9]+", "");
            if (!string.IsNullOrEmpty(strNum))
            {
                int.TryParse(strNum, out int num);
                HaveNumber = true;
                Order = num;
            }
            else 
            {
                HaveNumber = false;
                Order = 0;
            }
        }
    }
}
