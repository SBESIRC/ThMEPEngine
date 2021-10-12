using AcHelper;
using System.Windows.Input;
using ThMEPWSS.Sprinkler.Model;
using GalaSoft.MvvmLight.Command;
using ThMEPWSS.Sprinkler.Analysis;
using ThMEPWSS.Command;

namespace ThMEPWSS.ViewModel
{
    public class ThSprinklerCheckerVM
    {
        public ThSprinklerModel Parameter { get; set; }

        public ThSprinklerCheckerVM()
        {
            Parameter = new ThSprinklerModel();
        }
        public ICommand SprinklerCheckCmd => new RelayCommand(CheckClick);

        private void CheckClick()
        {
            if (CheckParameter())
            {
                SetFocusToDwgView();
                using (var cmd = new ThSprinklerCheckCmd { CommandName = "THPTJH", ActionName = "校核"})
                {
                    cmd.Execute();
                }
            }
        }
        private bool CheckParameter()
        {
            // ToDO
            return true;
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
