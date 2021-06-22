using AcHelper;
using ThMEPWSS.Command;
using System.Windows.Input;
using ThMEPWSS.Hydrant.Model;
using ThMEPWSS.Hydrant.Service;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace ThMEPWSS.ViewModel
{
    public class ThFireHydrantVM
    {       
        public ThFireHydrantModel Parameter { get; set; }
        public ObservableCollection<string> DangerLevels { get; set; }
        public ObservableCollection<string> FireTypes { get; set; }

        public ThFireTypeDataManager FireTypeDataManager { get; private set; }
        public ThFireHydrantVM()
        {
            Parameter = new ThFireHydrantModel();
            FireTypeDataManager = new ThFireTypeDataManager();
            DangerLevels = new ObservableCollection<string>(FireTypeDataManager.DangerLevels);
            FireTypes = new ObservableCollection<string>(FireTypeDataManager.FireTypes);
        }
        public ICommand RegionCheckCmd
        {
            get
            {
                return new RelayCommand(RegionCheckClick);
            }
        }

        private void RegionCheckClick()
        {
            if(CheckParameter())
            {
                SetFocusToDwgView();
                Parameter.IsShowCheckResult = true;
                using (var hydrantCmd = new ThHydrantProtectionRadiusCmd())
                {
                    hydrantCmd.Execute();
                }
            }            
        }
        private bool CheckParameter()
        {
            if(Parameter.HoseLength>=100 || Parameter.HoseLength<=0)
            {
                return false;
            }
            if(Parameter.IsSelf)
            {
                if (Parameter.SelfLength >= 100 || Parameter.SelfLength <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void ShowCheckExpression()
        {
            ThCheckExpressionControlService.ShowCheckExpression();
            SetFocusToDwgView();
        }

        public void CloseCheckExpress()
        {
            ThCheckExpressionControlService.CloseCheckExpression();
            SetFocusToDwgView();
        }  
        
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
