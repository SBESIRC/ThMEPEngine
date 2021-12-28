using Autodesk.AutoCAD.ApplicationServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPElectrical;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;

namespace TianHua.Electrical.UI.SystemDiagram.UI
{
    public class DrainageLayerViewModel : NotifyPropertyChangedBase
    {
        public DrainageLayerViewModel()
        {
            DynamicCheckBoxs = new ObservableCollection<DynamicCheckBox>();
            DynamicOpenFiles = new ObservableCollection<DynamicCheckBox>();
            if (FireCompartmentParameter.CacheDynamicCheckBoxs.Count > 0)
            {
                foreach (var item in FireCompartmentParameter.CacheDynamicCheckBoxs)
                {
                    DynamicCheckBoxs.Add(item);
                }
            }
            else
            {
                DynamicCheckBoxs.Add(new DynamicCheckBox { Content = ThAutoFireAlarmSystemCommon.FireDistrictByLayer, IsChecked = true, ShowText = ThAutoFireAlarmSystemCommon.FireDistrictByLayer });
                DynamicCheckBoxs.Add(new DynamicCheckBox { Content = "防火分区", IsChecked = true, ShowText = "防火分区" });
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    foreach (var layer in acadDatabase.Layers)
                    {
                        string LayerName = layer.Name;
                        if (LayerName != ThAutoFireAlarmSystemCommon.FireDistrictByLayer && LayerName != "防火分区" && (LayerName.Contains(ThAutoFireAlarmSystemCommon.FireDistrictByLayer) || LayerName.Contains("防火分区")))
                            DynamicCheckBoxs.Add(new DynamicCheckBox { Content = LayerName, IsChecked = false, ShowText = AbbreviationString(LayerName) });
                    }
                }
            }
            RefreshOpenFileList();
        }

        private ObservableCollection<DynamicCheckBox> dynamicCheckBoxs { get; set; }
        private ObservableCollection<DynamicCheckBox> dynamicOpenFiles { get; set; }
        public ObservableCollection<DynamicCheckBox> DynamicCheckBoxs
        {
            get { return dynamicCheckBoxs; }
            set
            {
                dynamicCheckBoxs = value;
                this.RaisePropertyChanged();
            }
        }

        public ObservableCollection<DynamicCheckBox> DynamicOpenFiles
        {
            get { return dynamicOpenFiles; }
            set
            {
                dynamicOpenFiles = value;
                this.RaisePropertyChanged();
            }
        }
        public List<DynamicCheckBox> SelectCheckBox
        {
            get
            {
                if (null == DynamicCheckBoxs || DynamicCheckBoxs.Count < 1)
                    return null;
                return DynamicCheckBoxs.Where(c => c.IsChecked).ToList();
            }
        }

        public List<DynamicCheckBox> SelectCheckFiles
        {
            get
            {
                if (null == DynamicOpenFiles || DynamicOpenFiles.Count < 1)
                    return null;
                return DynamicOpenFiles.Where(c => c.IsChecked).ToList();
            }
        }

        public void RefreshOpenFileList()
        {
            var dm = Application.DocumentManager;
            DynamicOpenFiles = new ObservableCollection<DynamicCheckBox>();
            foreach (Document doc in dm)
            {
                var fileName = doc.Name.Split('\\').Last();
                DynamicOpenFiles.Add(new DynamicCheckBox { Content = fileName, IsChecked = true, ShowText = AbbreviationString(fileName) });
            }
        }

        public bool AddLayer(string layerName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    var layerObj = acadDatabase.Layers.ElementOrDefault(layerName, false);
                    if (layerObj != null && DynamicCheckBoxs.Count(O => O.Content == layerName) == 0)
                    {
                        DynamicCheckBoxs.Insert(0, new DynamicCheckBox { Content = layerName, IsChecked = true, ShowText = AbbreviationString(layerName) });
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        private string AbbreviationString(string layerName)
        {
            if (layerName.Length <= 30)
                return layerName;
            else
                return "****" + layerName.Substring(layerName.Length - 30);
        }
        public int ShortCircuitIsolatorTxt { get; set; }
        public int FireBroadcastingTxt { get; set; }
        public int ControlBusCountTXT { get; set; }
        
    }
}
