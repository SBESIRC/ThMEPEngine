using System.Windows.Input;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DotNetARX;
using Dreambuild.AutoCAD;
using GalaSoft.MvvmLight.Command;
using Linq2Acad;
using NFox.Cad;

using ThMEPWSS.Command;
using ThMEPWSS.SprinklerConnect.Cmd;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.ViewModel
{
    public  class ThSprinklerConnectVM
    {
        public ThSprinklerConnectUIModel Parameter { get; set; }
        public ThSprinklerConnectVM()
        {
            Parameter = new ThSprinklerConnectUIModel();
        }

        public ICommand ThSprinklerDrawPipeCmd => new RelayCommand(DrawMainPipeClick);
        private void DrawMainPipeClick()
        {
            SetFocusToDwgView();
            using (var cmd = new ThSprinklerConnectUICmd())
            {
                cmd.DrawPipe = true;
                cmd.Execute();
            }
        }

        public ICommand ThSprinklerDrawSubPipeCmd => new RelayCommand(DrawSubMainPipeClick);
        private void DrawSubMainPipeClick()
        {
            SetFocusToDwgView();
            using (var cmd = new ThSprinklerConnectUICmd())
            {
                cmd.DrawPipe = false;
                cmd.Execute();
            }
        }

        public ICommand ThSprinklerConnectCmd => new RelayCommand(ConnectClick);
        private void ConnectClick()
        {
            SetFocusToDwgView();
            using (var cmd = new ThSprinklerConnectCmd_test 
            {
                ParameterFromUI=true,
                LayoutDirection= Parameter .LayoutDirection
            })
            {
                cmd.Execute();
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
