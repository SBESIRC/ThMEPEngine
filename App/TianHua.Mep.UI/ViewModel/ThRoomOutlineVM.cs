using AcHelper;
using DotNetARX;
using Linq2Acad;
using AcHelper.Commands;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.Toolkit.Mvvm.Input;

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
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THKJHZ");
        }

        private void DrawRoomSplitline()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THKJFG");
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
