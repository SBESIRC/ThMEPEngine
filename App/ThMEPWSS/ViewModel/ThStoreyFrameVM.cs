using System.Windows.Input;
using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CommunityToolkit.Mvvm.Input;
using ThMEPWSS.Pipe;
using ThMEPWSS.Command;

namespace ThMEPWSS.ViewModel
{
    public class ThStoreyFrameVM 
    {
        public ThStoreyFrameVM()
        {
            //
        }
        public ICommand InsertStoreyFrameCmd => new RelayCommand(InsertStoreyFrame);
        
        private void InsertStoreyFrame()
        {
            if(acadApp.DocumentManager.Count>0)
            {
                SetFocusToDwgView();
                CommandHandlerBase.ExecuteFromCommandLine(false, "THLCKX");
            }            
        }

        public ICommand DrawHouseTypeSplitLineCmd => new RelayCommand(DrawHouseTypeSplitLine);

        private void DrawHouseTypeSplitLine()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();
                CommandHandlerBase.ExecuteFromCommandLine(false, "THHTSL");
            } 
        }

        public ICommand DrawCellSplitLineCmd => new RelayCommand(DrawCellSplit);

        private void DrawCellSplit()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();
                CommandHandlerBase.ExecuteFromCommandLine(false, "THCSL");
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
