using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CadApp = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using ThControlLibraryWPF.CustomControl;
using ThControlLibraryWPF.ControlUtils;
using ThCADExtension;

using ThMEPEngineCore.Algorithm.FrameComparer;
using ThMEPEngineCore.Algorithm.FrameComparer.Model;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.FrameCompare
{
    /// <summary>
    /// FrameCompareUI.xaml 的交互逻辑
    /// </summary>
    public partial class FrameCompareUI : ThCustomWindow
    {
        static ThFrameCompareVM VM;

        public FrameCompareUI()
        {
            InitializeComponent();

            if (VM == null)
            {
                VM = new ThFrameCompareVM();
            }
            this.DataContext = VM;
            VM.LoadOpenningDrawing();
            VM.IsClose = false;
        }


        private void lvFrameGrid_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (VM.CheckCurrentDocument() == false)
            {
                return;
            }
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var item = sender as ListViewItem;
                if (item != null)
                {
                    var focusFrameItem = lvFrameGrid.SelectedItem as ThFrameChangeItem;
                    VM.selectItem = focusFrameItem;
                    if (VM.selectItem != null)
                    {
                        if ((short)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("LWDISPLAY") == 0)
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("LWDISPLAY", 1);
                        }
                        Active.Editor.ZoomToObjects(new Entity[] { focusFrameItem.FocusPoly }, 2.0);
                        VM.ClearTransientGraphics();
                        VM.AddToTransient(VM.selectItem);
                        CadApp.Application.UpdateScreen();
                    }
                }
            }
        }

        private void cbPathList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.ChangeFrameList.Clear();
            if (VM.CheckCurrentDocument() == false)
            {
                return;
            }

            VM.RefreshToCurrentDocument();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (VM.CheckCurrentDocument() == false)
            {
                return;
            }
            var path = CadApp.Application.DocumentManager.MdiActiveDocument.Name;
            VM.PathEnginDict.TryGetValue(path, out var engine);
            if (VM.selectItem != null && engine != null)
            {
                engine.UpdateResult(VM.selectItem);
                VM.selectItem = null;
                VM.ChangeFrameList.Clear();
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));
                VM.ClearTransientGraphics();
                CadApp.Application.UpdateScreen();

            }
        }



        private void btnUpdateAll_Click(object sender, RoutedEventArgs e)
        {
            if (VM.CheckCurrentDocument() == false)
            {
                return;
            }

            var path = CadApp.Application.DocumentManager.MdiActiveDocument.Name;
            VM.PathEnginDict.TryGetValue(path, out var engine);
            if (engine != null)
            {
                engine.UpdateAll();

                VM.ChangeFrameList.Clear();
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));
                VM.ClearTransientGraphics();
                CadApp.Application.UpdateScreen();

            }
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            if (VM.CheckCurrentDocument() == false)
            {
                return;
            }
            var path = CadApp.Application.DocumentManager.MdiActiveDocument.Name;
            try
            {
                VM.ChangeFrameList.Clear();
                ThFrameCompareEnging engine;
                if (VM.PathEnginDict.ContainsKey(path))
                {
                    engine = VM.PathEnginDict[path];
                }
                else
                {
                    VM.PathEnginDict.Add(path, new ThFrameCompareEnging());
                    var fileName = Path.GetFileName(path);
                    VM.PathListItem.Add(new UListItemData(fileName, VM.PathListItem.Count, path));
                    engine = VM.PathEnginDict[path];
                }

                FocusToCAD();

                engine.Excute();

              
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }


        }
        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        private void ThCustomWindow_Closed(object sender, EventArgs e)
        {
            VM.ClearTransientGraphics();
            VM.DocumentStatusReturn();
            VM.IsClose = true;
        }
    }
}
