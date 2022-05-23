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


namespace TianHua.Mep.UI.FrameCompare
{
    /// <summary>
    /// FrameCompareUI.xaml 的交互逻辑
    /// </summary>
    public partial class FrameCompareUI : ThCustomWindow
    {
        static FrameCompareVM VM;

        public FrameCompareUI()
        {
            InitializeComponent();

            if (VM == null)
            {
                VM = new FrameCompareVM();
            }
            this.DataContext = VM;

            LoadCurrentDrawing();
        }

        private void LoadCurrentDrawing()
        {
            var i = 0;
            foreach (CadApp.Document document in CadApp.Application.DocumentManager)
            {
                if (VM.PathEnginDict.ContainsKey(document.Name) == false)
                {
                    VM.PathEnginDict.Add(document.Name, new ThFrameCompareEnging());
                    var fileName = Path.GetFileName(document.Name);
                    VM.PathListItem.Add(new UListItemData(fileName, i, document.Name));
                    i++;
                }
            }
            VM.PathItem = VM.PathListItem.FirstOrDefault();
        }

        private void lvFrameGrid_LeftClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null)
            {
                var focusFrameItem = lvFrameGrid.SelectedItem as ThFrameChangeItem;
                VM.selectItem = focusFrameItem;
                if (focusFrameItem != null)
                {
                    Active.Editor.ZoomToObjects(new Entity[] { focusFrameItem.FocusPoly }, 2.0);
                    VM.ClearTransientGraphics();
                    VM.AddToTransient(VM.selectItem);
                }
            }
        }

        private void cbPathList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.ChangeFrameList.Clear();
            VM.PathEnginDict.TryGetValue((string)VM.PathItem.Tag, out var engine);
            if (engine != null)
            {
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var path = CadApp.Application.DocumentManager.MdiActiveDocument.Name;
            VM.PathEnginDict.TryGetValue(path, out var engine);
            if (VM.selectItem != null && engine != null)
            {
                engine.UpdateResult(VM.selectItem);
                VM.selectItem = null;
                VM.ChangeFrameList.Clear();
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));

                CadApp.Application.UpdateScreen();

            }



        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            var path = CadApp.Application.DocumentManager.MdiActiveDocument.Name;
            try
            {
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

                VM.ChangeFrameList.Clear();
                engine.ResultList.ForEach(x => VM.ChangeFrameList.Add(x));


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }


        }
        void FocusToCAD()
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
        }
    }
}
