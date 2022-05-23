using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPEngineCore.Algorithm.FrameComparer;
using ThMEPEngineCore.Algorithm.FrameComparer.Model;


namespace TianHua.Mep.UI.ViewModel
{
    public class ThFrameCompareVM : NotifyPropertyChangedBase
    {
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

        public Dictionary<string, ThFrameCompareEnging> PathEnginDict;//full path file,compare enging
        private List<Polyline> HighlightItem;

        public ThFrameCompareVM()
        {
            PathEnginDict = new Dictionary<string, ThFrameCompareEnging>();
            HighlightItem = new List<Polyline>();
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
                else if (focusItem.ChangeType == ThFrameChangedCommon.ChangeType_Append )
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

    }


}
