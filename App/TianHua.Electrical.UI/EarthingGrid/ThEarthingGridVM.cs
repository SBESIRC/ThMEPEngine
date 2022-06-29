using System.Collections.ObjectModel;
using AcHelper;
using ThControlLibraryWPF.ControlUtils;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPElectrical.Command;
using ThMEPElectrical.EarthingGrid.Service;

namespace TianHua.Electrical.UI.EarthingGrid
{
    internal class ThEarthingGridVM : NotifyPropertyChangedBase
    {
        private string currentLayer = "";
        private string earthGridSize ="";        
        public string EarthGridSize
        {
            get
            {
                return earthGridSize;
            }
            set
            {
                earthGridSize = value;
                this.RaisePropertyChanged("EarthGridSize");
            }
        }
        public ObservableCollection<string> EarthGridSizes { get; set; }
       
        public ThEarthingGridVM()
        {
            currentLayer = GetCurrentLayer();
            EarthGridSizes = new ObservableCollection<string>(
                ThEarthingGridDataService.Instance.EarthingGridSizes);
            earthGridSize = ThEarthingGridDataService.Instance.EarthingGridSize;
        }
        public void DrawInnerOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                acdb.Database.CreateAILayer(ThEarthingGridDataService.Instance.AIAreaInternalLayer,
                    ThEarthingGridDataService.Instance.AIAreaInternalColorIndex);
                acdb.Database.SetCurrentLayer(ThEarthingGridDataService.Instance.AIAreaInternalLayer);
                acdb.Database.SetLayerColor(ThEarthingGridDataService.Instance.AIAreaInternalLayer, 1);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }
        public void DrawOutterOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                acdb.Database.CreateAILayer(ThEarthingGridDataService.Instance.AIAreaExternalLayer,
                    ThEarthingGridDataService.Instance.AIAreaExternalColorIndex);
                acdb.Database.SetCurrentLayer(ThEarthingGridDataService.Instance.AIAreaExternalLayer);
                acdb.Database.SetLayerColor(ThEarthingGridDataService.Instance.AIAreaExternalLayer, 2);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }
        public void DrawEarthingGrid()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var cmd = new ThEarthingGridCommand())
            {
                SetFocusToDwgView();
                SetValues();
                cmd.Execute();
            }
        }
        public void ResetCurrentLayer()
        {
            if(Active.Document!=null)
            {
                using (var lockDoc = Active.Document.LockDocument())
                {
                    SetCurrentLayer(currentLayer);
                }
            }
        }
        private string GetCurrentLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Element<LayerTableRecord>(acdb.Database.Clayer).Name;
            }
        }
        private void SetCurrentLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                if(acdb.Layers.Contains(layerName))
                {
                    acdb.Database.SetCurrentLayer(layerName);
                }
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
        private void SetValues()
        {
            // 把UI参数传递到主功能区
            ThEarthingGridDataService.Instance.EarthingGridSize = earthGridSize;
        }
    }
}
