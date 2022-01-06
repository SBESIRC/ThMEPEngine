using System;
using System.Timers;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI.SystemDiagram.UI
{
    /// <summary>
    /// ShowAlarm.xaml 的交互逻辑
    /// </summary>
    public partial class ShowAlarm : ThCustomWindow
    {
        static ShowAlarmViewModel viewModel;
        public ShowAlarm()
        {
            InitializeComponent();
            viewModel = new ShowAlarmViewModel();
            this.DataContext = viewModel;
        }
        private void Dwg_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = (TextBlock)sender;
            if (e.ClickCount == 1)
            {
                var timer = new System.Timers.Timer(500);
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler((o, ex) => element.Dispatcher.Invoke(new Action(() =>
                {
                    var timer2 = (System.Timers.Timer)element.Tag;
                    timer2.Stop();
                    timer2.Dispose();
                })));
                timer.Start();
                element.Tag = timer;
            }
            if (e.ClickCount > 1)
            {
                var timer = element.Tag as System.Timers.Timer;
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    UIElement_DoubleClick(sender, e);
                }
            }
        }

        private void Alarm_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = (TextBlock)sender;
            if (e.ClickCount == 1)
            {
                var timer = new System.Timers.Timer(500);
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler((o, ex) => element.Dispatcher.Invoke(new Action(() =>
                {
                    var timer2 = (System.Timers.Timer)element.Tag;
                    timer2.Stop();
                    timer2.Dispose();
                })));
                timer.Start();
                element.Tag = timer;
            }
            if (e.ClickCount > 1)
            {
                var timer = element.Tag as System.Timers.Timer;
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    UIAlarmElement_DoubleClick(sender, e);
                }
            }
        }

        private void UIElement_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            var data = textBlock.DataContext as UIAlarmModel;
            try
            {
                if (!data.Doc.Database.IsNull())
                    Application.DocumentManager.MdiActiveDocument = data.Doc;
            }
            catch(Exception ex)
            {
                MessageBox.Show("错误：无法切换图纸,请注意图纸是否已被关闭或删除？", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UIAlarmElement_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                TextBlock textBlock = (TextBlock)sender;
                var data = textBlock.DataContext as UIAlarmEntityModel;
                using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    var entity = acad.ModelSpace.ElementOrDefault(data.AlarmObjID);
                    if (entity != null)
                    {
                        Active.Editor.ZoomToObjects(new Entity[] { entity }, 4.0);
                    }
                }
            }
            catch(Exception ex)
            {
            }
        }
        
    }
}
