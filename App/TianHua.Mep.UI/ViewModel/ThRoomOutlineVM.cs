using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore;
using AcHelper.Commands;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using CommunityToolkit.Mvvm.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThRoomOutlineVM
    {
        private string currentLayer = "";
        public ThRoomOutlineVM()
        {
            currentLayer = GetCurrentLayer();
        }
        public ICommand DrawRoomOutlineCmd
        {
            get
            {
                return new RelayCommand(DrawRoomOutline);
            }
        }

        public ICommand DrawRoomSplitlineCmd
        {
            get
            {
                return new RelayCommand(DrawRoomSplitline);
            }
        }

        public ICommand PickRoomOutlineCmd
        {
            get
            {
                return new RelayCommand(PickRoomOutline);
            }
        }

        public ICommand PickDoorOutlineCmd
        {
            get
            {
                return new RelayCommand(PickDoorOutline);
            }
        }

        private void DrawRoomOutline()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAIRoomOutlineLayer();
                    acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        private void DrawRoomSplitline()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                SetFocusToDwgView();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAIRoomSplitlineLayer();
                    acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMSPLITLINE);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        private void PickRoomOutline()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THEROC");
        }

        private void PickDoorOutline()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THEXTRACTDOOR");
        }

        public void ResetCurrentLayer()
        {
            if(Active.Document!=null)
            {
                using (var lockDoc = Active.Document.LockDocument())
                {
                    SetCurrentLayer(currentLayer);
                }
            }
        }

        private void SetCurrentLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.SetCurrentLayer(layerName);
            }
        }

        private string GetCurrentLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Element<LayerTableRecord>(acdb.Database.Clayer).Name;
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
