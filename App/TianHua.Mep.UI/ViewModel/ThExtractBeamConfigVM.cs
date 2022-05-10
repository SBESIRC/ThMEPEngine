using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Model.Common;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractBeamConfigVM:INotifyPropertyChanged
    {
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        public ThExtractBeamConfigVM()
        {
            // 加载配置
            LayerInfos = new ObservableCollection<ThLayerInfo>(LoadLayers());
            beamEngineOption = ThExtractBeamConfig.Instance.BeamEngineOption;
        }
        private BeamEngineOps beamEngineOption;
        /// <summary>
        /// 梁引擎配置
        /// </summary>
        public BeamEngineOps BeamEngineOption
        {
            get
            {
                return beamEngineOption;
            }
            set
            {
                beamEngineOption = value;
                RaisePropertyChanged("BeamEngineOption");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Confirm()
        {
            // 保存配置
            SaveLayers();
            ThExtractBeamConfig.Instance.BeamEngineOption = this.beamEngineOption;
        }
        public void SelectLayer()
        {
            // 选择图层
            var layer = PickUp();
            if (string.IsNullOrEmpty(layer))
            {
                return;
            }
            if (!IsExisted(layer))
            {
                AddLayer(layer);
            }
        }
        private void AddLayer(string layer)
        {
            LayerInfos.Add(new ThLayerInfo()
            {
                Layer = layer,
                IsSelected = true,
            });
        }
        private bool IsExisted(string layer)
        {
            return LayerInfos.Where(o => o.Layer == layer).Any();
        }
        private string PickUp()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                var pneo = new PromptNestedEntityOptions("\n请选择梁线:");
                var pner = Active.Editor.GetNestedEntity(pneo);
                if (pner.Status == PromptStatus.OK)
                {
                    if (pner.ObjectId != ObjectId.Null)
                    {
                        var entity = acdb.Element<Entity>(pner.ObjectId);
                        if (entity is Curve)
                        {
                            return entity.Layer;
                        }
                    }
                }
                return "";
            }
        }
        private List<ThLayerInfo> FilterLayers(List<ThLayerInfo> layerInfos)
        {
            // existed in current database
            using (var acdb = AcadDatabase.Active())
            {
                return layerInfos.Where(o=>acdb.Layers.Contains(o.Layer)).ToList();  
            }
        }
        private List<string> GetSBeamLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => IsSBeamLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private bool IsSBeamLayer(string layer)
        {
            //以S_BEAM结尾的所有图层，默认为梁所在的图层。
            return layer.ToUpper().EndsWith("S_BEAM");
        }
        private List<ThLayerInfo> LoadLayers()
        {
            var results = new List<ThLayerInfo>(); 
            // 优先获取以S_BEAM结尾的梁
            var sbeamLayers = GetSBeamLayers().Select(o=>new ThLayerInfo()
            {
                Layer=o,
                IsSelected=true,
            }).ToList();
            results.AddRange(sbeamLayers);

            // 存在于DB中的
            var storeInfos = FilterLayers(ThExtractBeamConfig.Instance.LayerInfos);
            storeInfos.Where(o => !sbeamLayers.Select(s => s.Layer).Contains(o.Layer))
                .ForEach(o => results.Add(o));

            results = Sort(results);
            return results;
        }
        private void SaveLayers()
        {
            // 保存LayerInfos结果
            var results = new List<ThLayerInfo>();
            results.AddRange(LayerInfos);
            //ThExtractBeamConfig.Instance.LayerInfos
            //    .Where(o => !IsExisted(o.Layer))
            //    .ForEach(o=>results.Add(o));
            ThExtractBeamConfig.Instance.LayerInfos = FilterLayers(results);
        }
        private List<ThLayerInfo> Sort(List<ThLayerInfo> infos)
        {
            // 把选中的放前面，再按名称排名
            var results = new List<ThLayerInfo>();
            var selected = infos.Where(o => o.IsSelected).ToList();
            var unSelected = infos.Where(o => !o.IsSelected).ToList();
            results.AddRange(selected.OrderBy(o=>o.Layer));
            results.AddRange(unSelected.OrderBy(o => o.Layer));
            return results;
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
    } 
}
