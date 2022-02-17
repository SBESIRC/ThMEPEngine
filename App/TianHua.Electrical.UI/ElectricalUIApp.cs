using System.Windows.Forms;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical;
using ThMEPElectrical.Model;
using ThMEPElectrical.Command;
using ThMEPElectrical.BlockConvert;
using TianHua.Electrical.UI.FrameComparer;
using TianHua.Electrical.UI.FireAlarm;
using TianHua.Electrical.UI.ThBroadcast;
using TianHua.Electrical.UI.SecurityPlaneUI;
using TianHua.Electrical.UI.CapitalConverter;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPEngineCore.Algorithm.FrameComparer;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }

        public void Initialize()
        {
            SmokeLayoutUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
        }

        [CommandMethod("TIANHUACAD", "THYWG", CommandFlags.Modal)]
        public void THYWG()
        {
            if (SmokeLayoutUI == null)
            {
                SmokeLayoutUI = new fmSmokeLayout();
                SmokeLayoutDataModel _SmokeLayoutDataModel = new SmokeLayoutDataModel()
                {
                    LayoutType = LayoutType,
                    AreaLayout = AreaLayout,
                    RoofThickness = Parameter.RoofThickness,
                    BlockScale = Parameter.BlockScale,
                };
                SmokeLayoutUI.InitForm(_SmokeLayoutDataModel);
            }
            AcadApp.ShowModelessDialog(SmokeLayoutUI);
        }

        [CommandMethod("TIANHUACAD", "THTZZH", CommandFlags.Modal)]
        public void THTZZH()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var uiCapitalConverter = new CapitalConverterUI();
                uiCapitalConverter.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                AcadApp.ShowModalWindow(uiCapitalConverter);
                if (!uiCapitalConverter.GoOn)
                {
                    return;
                }
                // 执行命令
                var cmd = new ThBConvertCommand()
                {
                    Scale = uiCapitalConverter.Parameter.BlkScaleValue,
                    FrameStyle = uiCapitalConverter.Parameter.BlkFrameValue,
                };
                if (uiCapitalConverter.Parameter.HavcOps &&
                    uiCapitalConverter.Parameter.WssOps)
                {
                    cmd.Category = ConvertCategory.ALL;
                }
                else if (uiCapitalConverter.Parameter.HavcOps)
                {
                    cmd.Category = ConvertCategory.HVAC;
                }
                else if (uiCapitalConverter.Parameter.WssOps)
                {
                    cmd.Category = ConvertCategory.WSS;
                }
                else
                {
                    return;
                }
                switch (uiCapitalConverter.Parameter.EquipOps)
                {
                    case CapitalOP.Strong:
                        cmd.Mode = ConvertMode.STRONGCURRENT;
                        break;
                    case CapitalOP.Weak:
                        cmd.Mode = ConvertMode.WEAKCURRENT;
                        break;
                    case CapitalOP.All:
                        cmd.Mode = ConvertMode.ALL;
                        break;
                }
                cmd.Execute();
            }
        }

        /// <summary>
        /// 安防平面
        /// </summary>
        [CommandMethod("TIANHUACAD", "THAFPM", CommandFlags.Modal)]
        public void THAFPM()
        {
            var ui = new SecurityPlaneSystemUI();
            AcadApp.ShowModalWindow(ui);
        }

        /// <summary>
        /// 消防广播
        /// </summary>
        [CommandMethod("TIANHUACAD", "THXFGB", CommandFlags.Modal)]
        public void THXFGB()
        {
            var ui = new ThBroadcastUI();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 火灾报警
        /// </summary>
        [CommandMethod("TIANHUACAD", "THHZBJ", CommandFlags.Modal)]
        public void THHZBJUI()
        {
            var ui = new UIThFireAlarm();
            AcadApp.ShowModelessWindow(ui);
        }

        private string LayoutType
        {
            get
            {
                return Parameter.sensorType == SensorType.SMOKESENSOR ?
                    ElectricalUICommon.SMOKE_INDUCTION : ElectricalUICommon.TEMPERATURE_INDUCTION;
            }
        }

        private string AreaLayout
        {
            get
            {
                return ElectricalUICommon.AREA_COMMON;
            }
        }

        private PlaceParameter Parameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.Parameter == null)
                {
                    ThMEPElectricalService.Instance.Parameter = new PlaceParameter();
                }
                return ThMEPElectricalService.Instance.Parameter;
            }
        }

        /// <summary>
        /// 框线比较
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKXDB", CommandFlags.Modal)]
        public void FrameComparerUI()
        {
            var dlg = new UIFrameComparer();
            AcadApp.ShowModalDialog(dlg);
            var t = dlg.fence;
            if (!dlg.isModel)
            {
                dlg = new UIFrameComparer();
                dlg.fence = t;
                AcadApp.ShowModelessDialog(dlg);// 面板中fence会被更新
                DoRoomComparer(dlg.fence, CompareFrameType.ROOM, out ThFrameExactor roomExactor, out ThMEPFrameComparer roomComp);
                DoComparer(dlg.fence, CompareFrameType.DOOR, out ThFrameExactor doorExactor, out ThMEPFrameComparer doorComp);
                DoComparer(dlg.fence, CompareFrameType.WINDOW, out ThFrameExactor windowExactor, out ThMEPFrameComparer windowComp);
                DoComparer(dlg.fence, CompareFrameType.FIRECOMPONENT, out ThFrameExactor fireExactor, out ThMEPFrameComparer fireComp);

                dlg.DoAddFrame(roomComp, roomExactor.dicCode2Id, "房间框线");
                dlg.DoAddFrame(doorComp, doorExactor.dicCode2Id, "门");
                dlg.DoAddFrame(windowComp, windowExactor.dicCode2Id, "窗");
                dlg.DoAddFrame(fireComp, fireExactor.dicCode2Id, "防火分区");
            }
        }
        private void DoRoomComparer(Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
        {
            frameExactor = new ThFrameExactor(type, fence);
            frameComp = new ThMEPFrameComparer(frameExactor.curGraph, frameExactor.reference);
            var textExactor = new ThFrameTextExactor();
            _ = new ThMEPFrameTextComparer(frameComp, textExactor);// 对房间框线需要对文本再进行比对
            using (var acadDatabase = AcadDatabase.Active())
            {
                // 此处单独使用using域是为了立即显示绘制效果
                var painter = new ThFramePainter();
                painter.Draw(frameComp, frameExactor.dicCode2Id, type);
            }
        }
        private void DoComparer(Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
        {
            frameExactor = new ThFrameExactor(type, fence);
            frameComp = new ThMEPFrameComparer(frameExactor.curGraph, frameExactor.reference);
            using (var acadDatabase = AcadDatabase.Active())
            {
                // 此处单独使用using域是为了立即显示绘制效果
                var painter = new ThFramePainter();
                painter.Draw(frameComp, frameExactor.dicCode2Id, type);
            }
        }
    }
}
