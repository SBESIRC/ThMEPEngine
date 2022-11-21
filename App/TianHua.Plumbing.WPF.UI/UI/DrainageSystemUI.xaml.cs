using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Model;
using static ThMEPWSS.Assistant.DrawUtils;
using MessageBox = System.Windows.MessageBox;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class DrainageSystemUI : ThCustomWindow
    {
        private DrainageSystemDiagramViewModel vm;
        public static DrainageSystemUI TryCreate(DrainageSystemDiagramViewModel vm)
        {
            if (ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext != null) return null;
            var file = CadCache.CurrentFile;
            if (file == null) return null;
            var ok = !CadCache.Locks.Contains(CadCache.WaterGroupLock);
            if (!ok) return null;
            var w = new DrainageSystemUI(vm);
            w.Loaded += (s, e) => { CadCache.Locks.Add(CadCache.WaterGroupLock); };
            w.Closed += (s, e) => { CadCache.Locks.Remove(CadCache.WaterGroupLock); };
            return w;
        }
        private DrainageSystemUI(DrainageSystemDiagramViewModel vm)
        {
            InitializeComponent();
            this.vm = vm;
            this.DataContext = vm;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Loaded += (s, e) =>
            {
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext = new ThMEPWSS.ReleaseNs.DrainageSystemNs.CommandContext()
                { ViewModel = vm, window = this };
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.TryUpdateByRange(CadCache.TryGetRange(), true);
            };
            Closed += (s, e) => { ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext = null; };

            Loaded += (s, e) => { CadCache.Register(this); };
            {
                DocumentCollectionEventHandler f = (s, e) => { CadCache.CloseAllWindows(); };
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated += f;
                Closed += (s, e) => { Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated -= f; };
            }

        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new DrainageSystemParamsUI(vm.Params);
            uiParams.Topmost = true;
            uiParams.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            uiParams.ShowDialog();
        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext.StoreyContext == null) throw new Exception("请重新框选楼层");
                CadCache.HideAllWindows(); ;
                FocusMainWindow();
                ThMEPCommandService.Execute(() => ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.DrawDrainageSystemDiagram(vm, false), "THPSXTT", "生成");
            }
            catch (Exception ex)
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
                CadCache.HideAllWindows(); ;
                FocusMainWindow();
                vm.CollectFloorListDatas(false);
                var file = CadCache.CurrentFile;
                if (file == null) return;
                var _ctx = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext;
                if (_ctx == null) return;
                CadCache.SetCache(file, "StoreyContext", _ctx.StoreyContext);
                CadCache.SetCache(file, "FloorListDatas", _ctx.ViewModel.FloorListDatas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                CadCache.ShowAllWindows();
            }
        }
        ////这明明是“新建楼层图框”
        //private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        CadCache.HideAllWindows(); ;
        //        FocusMainWindow();
        //        ThMEPWSS.Common.Utils.CreateFloorFraming(false);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //    finally
        //    {
        //        CadCache.ShowAllWindows();
        //    }
        //}

        private void ImageButton_Click_2(object sender, RoutedEventArgs e)
        {
            ThMEPCommandService.Execute(() =>
            {
                try
                {
                    CadCache.HideAllWindows();
                    FocusMainWindow();
                    ThMEPWSS.ReleaseNs.DrainageSystemNs.THDrainageService.DrawDrainageFlatDiagram(vm);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    CadCache.ShowAllWindows();
                }
            }, "THPSXTT", "标注管径");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/view/video/26cdce3cf8124ccfa95dd5c47da53ca2.html");
        }
    }
}
