using AcHelper;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore;
using AcHelper.Commands;
using System.Windows.Input;
using ThMEPEngineCore.Service;
using GalaSoft.MvvmLight.Command;

namespace ThMEPWSS.ViewModel
{
    public class ThRoomOutlineVM
    {
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
                var poly = PlineJig.PolylineJig(256, "\n选择下一个点");
                var roomOutline = poly.WashClone();
                if (roomOutline == null || roomOutline.Area < 1.0)
                {
                    return;
                }

                // 添加到图纸中
                acdb.ModelSpace.Add(roomOutline);
                // 设置到指定图层

                acdb.Database.CreateAIRoomOutlineLayer();
                roomOutline.Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
            }
        }

        private void DrawRoomSplitline()
        {
            SetFocusToDwgView();

            using (var lockDoc = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                var roomSplitline = PlineJig.PolylineJig(256,"\n选择下一个点",false);
                if (roomSplitline == null )
                {
                    return;
                }

                // 添加到图纸中
                acdb.ModelSpace.Add(roomSplitline);
                // 设置到指定图层
                acdb.Database.CreateAIRoomSplitlineLayer();
                roomSplitline.Layer = ThMEPEngineCoreLayerUtils.ROOMSPLITLINE;
            }            
        }

        private void PickRoomOutline()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THKJSQ");
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
