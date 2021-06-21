using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPElectrical;

namespace TianHua.Electrical.UI.SystemDiagram.UI
{
    public class DrainageLayerViewModel : NotifyPropertyChangedBase
    {
        public DrainageLayerViewModel()
        {
            DynamicCheckBoxs = new ObservableCollection<DynamicCheckBox>();
            DynamicCheckBoxs.Add(new DynamicCheckBox { Content = ThAutoFireAlarmSystemCommon.FireDistrictByLayer, IsChecked = true, ShowText = ThAutoFireAlarmSystemCommon.FireDistrictByLayer });
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var layer in acadDatabase.Layers)
                {
                    string LayerName = layer.Name;
                    if (LayerName != ThAutoFireAlarmSystemCommon.FireDistrictByLayer && (LayerName.Contains(ThAutoFireAlarmSystemCommon.FireDistrictByLayer) || LayerName.Contains("防火分区")))
                        DynamicCheckBoxs.Add(new DynamicCheckBox { Content = LayerName, IsChecked = false , ShowText = AbbreviationString(LayerName) });
                }
            }

        }

        private ObservableCollection<DynamicCheckBox> dynamicCheckBoxs { get; set; }
        public ObservableCollection<DynamicCheckBox> DynamicCheckBoxs
        {
            get { return dynamicCheckBoxs; }
            set
            {
                dynamicCheckBoxs = value;
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

        public bool AddLayer(string layerName)
        {

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    var layerObj = acadDatabase.Layers.ElementOrDefault(layerName, false);
                    if (layerObj != null && DynamicCheckBoxs.Count(O => O.Content == layerName) == 0)
                    {
                        DynamicCheckBoxs.Add(new DynamicCheckBox { Content = layerName, IsChecked = true, ShowText = AbbreviationString(layerName) });
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

    public class DynamicCheckBox
    {
        public string Content { get; set; }
        //public string GroupName { get; set; }
        public bool IsChecked { get; set; }

        public string ShowText { get; set; }
    }
}
