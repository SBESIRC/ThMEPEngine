using System.Collections.Generic;
using System.Linq;
using TianHua.Hvac.UI.UI;
using TianHua.Hvac.UI.ViewModels;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI
{
    class EQPMUIServices
    {
        public static EQPMUIServices Instance = new EQPMUIServices();
        private FanEQPMSelection fanSelectUI;
        public List<FanSelectHisModel> HisFanViewModels;
        EQPMUIServices() 
        {
            fanSelectUI = new FanEQPMSelection();
            fanSelectUI.Hide();
            SelectFanBlockGuid = string.Empty;
            HisFanViewModels = new List<FanSelectHisModel>();
        }

        public string SelectFanBlockGuid { get; set; }
        public void ShowFanSelectUI(string dwgId) 
        {
            if (fanSelectUI == null)
                return;
            if (!string.IsNullOrEmpty(dwgId))
            {
                var hisValue = HisFanViewModels.Where(c => c.DWGID == dwgId).FirstOrDefault();
                if (hisValue == null || !hisValue.ShowInThisDwg)
                    return;
            }
            if (!fanSelectUI.IsVisible)
            {
                AcadApp.ShowModelessWindow(fanSelectUI);
            }
            if (string.IsNullOrEmpty(SelectFanBlockGuid))
                ChangeActiveDocument();

        }
        public void HideFanSelectUI() 
        {
            if (null == fanSelectUI || !fanSelectUI.IsVisible)
                return;
            fanSelectUI.Hide();
        }
        public void ChangeActiveDocument() 
        {
            fanSelectUI.ChangeActiveDocument();
        }
        public void SelectFanBlock() 
        {
            fanSelectUI.SelectModelSpaceFanBlock(SelectFanBlockGuid);
            SelectFanBlockGuid = string.Empty;
        }
        public void RefreshCopyData() 
        {
            fanSelectUI.RefreshCopyData();
        }
        public void RefreshDeleteData(List<string> ids)
        {
            fanSelectUI.RefreshDeleteData(ids);
        }
    }
    class FanSelectHisModel 
    {
        public string DWGID { get; }
        public bool ShowInThisDwg { get; set; }
        public EQPMFanSelectViewModel DwgViewModel { get; set; }
        public FanSelectHisModel(string dwgId, bool showInThisDwg, EQPMFanSelectViewModel viewModel) 
        {
            this.DWGID = dwgId;
            this.ShowInThisDwg = showInThisDwg;
            this.DwgViewModel = viewModel;
        }
    }
}
