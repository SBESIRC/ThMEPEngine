using AcHelper;
using ThMEPWSS.Command;
using System.Windows.Input;
using ThMEPWSS.Hydrant.Model;
using ThMEPWSS.Hydrant.Service;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

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
        public ICommand HelpCmd
        {
            get
            {
                return new RelayCommand(OnHelpClick);
            }
        }

        private void OnHelpClick()
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/view/video/3f2cc6f7b5914d4ca42e42e98b628326.html");
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
            if(Parameter.CheckObjectOption == CheckObjectOps.FireHydrant)
            {
                if (Parameter.HoseLength >= 100 || Parameter.HoseLength <= 0)
                {
                    return false;
                }
            }
            if(Parameter.CheckObjectOption == CheckObjectOps.FireExtinguisher)
            {
                if (Parameter.MaxProtectDisOption == MaxProtectDisOps.Custom)
                {
                    if (Parameter.SelfLength >= 100 || Parameter.SelfLength <= 0)
                    {
                        return false;
                    }
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

        public double QueryMaxProtectDistance(string fireExtinguisherName)
        {
            if (Parameter.MaxProtectDisOption == MaxProtectDisOps.Calculation)
            {
                return FireTypeDataManager.Query(
                Parameter.FireType,
                Parameter.DangerLevel,
                fireExtinguisherName);
            }
            else
            {
                return Parameter.SelfLength;
            }
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
