using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.EQPMFanSelect
{
    class FanSelectCheck
    {
        public static bool IsModelStyleChanged(FanDataModel model, FanDataModel dataModel)
        {
            return model.VentStyle != dataModel.VentStyle;
        }
        public static bool IsModelNameChanged(ObjectId model, FanDataModel dataModel)
        {
            var hisVentStyle = CommonUtil.GetEnumDescription(dataModel.VentStyle);
            var hisFrom = CommonUtil.GetEnumDescription(dataModel.IntakeForm);
            var modelName = model.GetModelName();
            if (hisVentStyle.Contains("轴流"))
            {
                return modelName != EQPMFanCommon.AXIALModelName(dataModel.FanModelCCCF, dataModel.MountType);
            }
            else
            {
                return modelName != EQPMFanCommon.HTFCModelName(hisVentStyle, hisFrom, dataModel.FanModelTypeCalcModel.FanModelNum);
            }
        }

        public static bool IsModelBlockNameChanged(FanDataModel model, FanDataModel dataModel)
        {
            string firstName = "";
            string secondName = "";
            if (model.VentStyle == EQPMFanModelEnums.EnumFanModelType.AxialFlow)
            {
                firstName = EQPMFanCommon.AXIALModelName(model.FanModelCCCF, model.MountType);
            }
            else
            {
                firstName = EQPMFanCommon.HTFCBlockName(model.VentStyle, model.IntakeForm, model.MountType);
            }
            if (dataModel.VentStyle == EQPMFanModelEnums.EnumFanModelType.AxialFlow)
            {
                secondName = EQPMFanCommon.AXIALModelName(dataModel.FanModelCCCF, dataModel.MountType);
            }
            else
            {
                secondName = EQPMFanCommon.HTFCBlockName(dataModel.VentStyle, dataModel.IntakeForm, dataModel.MountType);
            }
            return firstName != secondName;
        }

        public static bool IsAttributeModified(Dictionary<string, string> checkAttributes, Dictionary<string, string> attributes)
        {
            foreach (var item in checkAttributes)
            {
                var key = item.Key;
                var value = item.Value;
                if (attributes.TryGetValue(key, out string attrValue))
                {
                    if (value != attrValue)
                        return true;
                }
            }
            return false;
        }
    }
}
