using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using CadApp = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm.FrameComparer;
using ThMEPEngineCore.Algorithm.FrameComparer.Model;
using System.IO;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThFrameCompareVM : NotifyPropertyChangedBase
    {
        //UI Control/////////////////////////
        private ObservableCollection<UListItemData> _PathListItem = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> PathListItem
        {
            get { return _PathListItem; }
            set
            {
                _PathListItem = value;
                this.RaisePropertyChanged();
            }
        }

        private UListItemData _PathItem { get; set; }
        public UListItemData PathItem
        {
            get { return _PathItem; }
            set
            {
                _PathItem = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<ThFrameChangeItem> _ChangeFrameList = new ObservableCollection<ThFrameChangeItem>();
        public ObservableCollection<ThFrameChangeItem> ChangeFrameList
        {
            get { return _ChangeFrameList; }
            set
            {
                _ChangeFrameList = value;
                this.RaisePropertyChanged();
            }
        }

        public ThFrameChangeItem selectItem { get; set; }
        /////////////////////////////////////

        //Engine Control///////////////////////////////////
        public Dictionary<string, ThFrameCompareEnging> PathEnginDict;//full path file,compare enging
        private List<Polyline> HighlightItem;
        public bool IsClose { get; set; }
        public ThFrameCompareVM()
        {
            PathEnginDict = new Dictionary<string, ThFrameCompareEnging>();
            HighlightItem = new List<Polyline>();
            IsClose = false;

        }

        public void LoadOpenningDrawing()
        {
            var i = 0;
            foreach (CadApp.Document document in CadApp.Application.DocumentManager)
            {
                if (PathEnginDict.ContainsKey(document.Name) == false)
                {
                    PathEnginDict.Add(document.Name, new ThFrameCompareEnging());
                    var fileName = Path.GetFileName(document.Name);
                    PathListItem.Add(new UListItemData(fileName, i, document.Name));
                    i++;
                }
            }
            PathItem = PathListItem.Where(x => x.Name == Active.Document.Name).FirstOrDefault();
            if (PathItem == null)
            {
                PathItem = PathListItem.First();
            }
        }


        public void AddToTransient(ThFrameChangeItem focusItem)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                int colorIdx = 1;
                if (focusItem.ChangeType == ThFrameChangedCommon.ChangeType_Change)
                {
                    colorIdx = 2;
                }
                else if (focusItem.ChangeType == ThFrameChangedCommon.ChangeType_Append)
                {
                    colorIdx = 3;
                }

                var p = focusItem.FocusPoly.Clone() as Polyline;
                p.ColorIndex = colorIdx;
                p.Linetype = LineTypeInfo.Hidden;
                p.LineWeight = LineWeight.LineWeight030;
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection();

                tm.AddTransient(p, cadGraph.TransientDrawingMode.Main, 128, intCol);
                HighlightItem.Add(p);
            }
        }
        public void ClearTransientGraphics()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection();
                HighlightItem.ForEach(x => tm.EraseTransient(x, intCol));
                HighlightItem.Clear();

            }
        }
        public void DocumentStatusReturn()
        {
            // PathEnginDict.ForEach(x => x.Value.ReturnStatus());
        }

        public void RefreshToCurrentDocument()
        {

            PathEnginDict.TryGetValue((string)PathItem.Tag, out var engine);
            if (engine != null && IsClose == false)
            {
                engine.ResultList.ForEach(x => ChangeFrameList.Add(x));

            }
            else
            {
                if (IsClose == false)
                {
                    PathEnginDict.Remove((string)PathItem.Tag);
                }
            }
        }
        public bool CheckCurrentDocument()
        {
            var isSame = false;
            if (PathItem != null && (string)PathItem.Tag == Active.Document.Name)
            {
                isSame = true;
            }
            return isSame;

        }
    }


}
