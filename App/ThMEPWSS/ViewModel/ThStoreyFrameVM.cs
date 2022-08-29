using AcHelper;
using Linq2Acad;
using DotNetARX;
using AcHelper.Commands;
using System.Windows.Input;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Service;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPWSS.ViewModel
{
    public class ThStoreyFrameVM : ObservableObject
    {
        public ThStoreyFrameVM()
        {
            DrawCellSplitLineCmd = new RelayCommand(DrawCellSplit);
            InsertStoreyFrameCmd = new RelayCommand(InsertStoreyFrame);
            DrawHouseTypeSplitLineCmd = new RelayCommand(DrawHouseTypeSplitLine);
        }

        public ICommand DrawCellSplitLineCmd { get; }
        public ICommand InsertStoreyFrameCmd { get; }
        public ICommand DrawHouseTypeSplitLineCmd { get; }

        private void InsertStoreyFrame()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();
                CommandHandlerBase.ExecuteFromCommandLine(false, "THLCKX");
            }
        }

        private void DrawHouseTypeSplitLine()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    ThInsertStoreyFrameService.ImportHouseTypeSplitLineLayer();
                    acdb.Database.SetCurrentLayer(ThWPipeCommon.HouseTypeSplitLineLayer);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        private void DrawCellSplit()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    ThInsertStoreyFrameService.ImportCellSplitLineLayer();
                    acdb.Database.SetCurrentLayer(ThWPipeCommon.CellSplitLineLayer);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        private void SetFocusToDwgView()
        {
            Active.Document.Window.Focus();
        }
    }
}
