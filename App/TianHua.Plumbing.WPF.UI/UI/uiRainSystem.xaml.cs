using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.ReleaseNs.RainSystemNs;
using static ThMEPWSS.Assistant.DrawUtils;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiRainSystem : ThCustomWindow
    {
        RainSystemDiagramViewModel vm;
        public static uiRainSystem TryCreate(RainSystemDiagramViewModel vm)
        {
            if (ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.commandContext != null) return null;
            var file = CadCache.CurrentFile;
            if (file == null) return null;
            var ok = !CadCache.Locks.Contains(CadCache.WaterGroupLock);
            if (!ok) return null;
            var w = new uiRainSystem(vm);
            w.Loaded += (s, e) => { CadCache.Locks.Add(CadCache.WaterGroupLock); };
            w.Closed += (s, e) => { CadCache.Locks.Remove(CadCache.WaterGroupLock); };
            return w;
        }
        private uiRainSystem(RainSystemDiagramViewModel vm)
        {
            InitializeComponent();
            this.vm = vm;
            this.DataContext = vm;
            this.Topmost = true;
            Loaded += (s, e) =>
            {
                ThRainService.commandContext = new CommandContext() { ViewModel = vm, };
                ThRainService.TryUpdateByRange(CadCache.TryGetRange(), true);
            };
            Closed += (s, e) => { ThRainService.commandContext = null; };
           
            Loaded += (s, e) => { CadCache.Register(this); };
            {
                DocumentCollectionEventHandler f = (s, e) => { CadCache.CloseAllWindows(); };
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated += f;
                Closed += (s, e) => { Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated -= f; };
            }

        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new uiRainSystemParams(vm.Params);
            uiParams.Topmost = true;
            uiParams.ShowDialog();
        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.commandContext.StoreyContext == null) throw new Exception("请重新框选楼层");
                CadCache.HideAllWindows();
                FocusMainWindow();
                RainDiagram.DrawRainDiagram(vm, false);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                CadCache.ShowAllWindows();
            }
        }

        //选择楼层
        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                CadCache.HideAllWindows();
                FocusMainWindow();
                vm.InitFloorListDatas(false);
                var file = CadCache.CurrentFile;
                if (file == null) return;
                var _ctx = ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.commandContext;
                if (_ctx == null) return;
                CadCache.SetCache(file, "StoreyContext", _ctx.StoreyContext);
                CadCache.SetCache(file, "FloorListDatas", _ctx.ViewModel.FloorListDatas);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                CadCache.ShowAllWindows();
            }
        }
        //这明明是“新建楼层图框”
        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CadCache.HideAllWindows();
                FocusMainWindow();
                ThMEPWSS.Common.Utils.CreateFloorFraming(false);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                CadCache.ShowAllWindows();
            }
        }
    }
}
