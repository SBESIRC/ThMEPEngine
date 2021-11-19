using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore;
using AcHelper.Commands;
using System.Windows.Input;
using ThMEPEngineCore.CAD;
using GalaSoft.MvvmLight.Command;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.ViewModel
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

        private void DrawRoomOutline()
        {
            SetFocusToDwgView();
            using (var lockDoc = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                AddRoomOutlineLayer();
                UnLockLayer(ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                Active.Document.SendStringToExecute("_Polyline ", true, false, true);
            }
        }

        private void DrawRoomSplitline()
        {
            SetFocusToDwgView();
            using (var lockDoc = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                AddRoomSplitlineLayer();
                UnLockLayer(ThMEPEngineCoreLayerUtils.ROOMSPLITLINE);
                SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMSPLITLINE);
                Active.Document.SendStringToExecute("_Polyline ", true, false, true);
            }
        }

        public void ResetCurrentLayer()
        {
            using (var lockDoc = Active.Document.LockDocument())
            {
                SetCurrentLayer(currentLayer);
            }  
        }

        private void DrawRoomOutlineByJig()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var poly = ThMEPPolylineEntityJig.PolylineJig(256, "\n选择下一个点");
                var roomOutline = poly.WashClone();
                if (roomOutline == null || roomOutline.Area < 1.0)
                    return;
                AddRoomOutlineLayer();

                // 添加到图纸中
                acdb.ModelSpace.Add(roomOutline);
                roomOutline.Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
                roomOutline.ColorIndex = (int)ColorIndex.BYLAYER;
                roomOutline.LineWeight = LineWeight.ByLayer;
                roomOutline.Linetype = "ByLayer";
            }
        }

        private void DrawRoomSplitlineByJig()
        {
            SetFocusToDwgView();
            using (var lockDoc = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                var roomSplitline = ThMEPPolylineEntityJig.PolylineJig(256,"\n选择下一个点",false);
                if (roomSplitline == null )
                {
                    return;
                }
                AddRoomSplitlineLayer();

                // 添加到图纸中
                acdb.ModelSpace.Add(roomSplitline);
                // 设置到指定图层
                roomSplitline.Layer = ThMEPEngineCoreLayerUtils.ROOMSPLITLINE;
                roomSplitline.ColorIndex = (int)ColorIndex.BYLAYER;
                roomSplitline.LineWeight = LineWeight.ByLayer;
                roomSplitline.Linetype = "ByLayer";
            }            
        }

        private void PickRoomOutline()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THKJSQ");
        }

        private void AddRoomOutlineLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(ThMEPEngineCoreLayerUtils.ROOMOUTLINE))
                {
                    acdb.Database.CreateAIRoomOutlineLayer();
                }
            }
        }

        private void AddRoomSplitlineLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(ThMEPEngineCoreLayerUtils.ROOMSPLITLINE))
                {
                    acdb.Database.CreateAIRoomSplitlineLayer();
                }
            }
        }

        private void UnLockLayer(string layer)
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (acdb.Layers.Contains(layer))
                {
                    acdb.Database.UnLockLayer(layer);
                    acdb.Database.UnFrozenLayer(layer);
                    acdb.Database.UnOffLayer(layer);
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
