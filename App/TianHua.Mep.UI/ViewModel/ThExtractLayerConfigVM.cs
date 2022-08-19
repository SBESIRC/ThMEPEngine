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
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractLayerConfigVM:INotifyPropertyChanged
    {
        public ObservableCollection<ThLayerInfo> BeamLayerInfos { get; set; }
        public ObservableCollection<ThLayerInfo> ShearWallLayerInfos { get; set; }
        public ThExtractLayerConfigVM()
        {
            // 加载配置
            BeamLayerInfos = new ObservableCollection<ThLayerInfo>(LoadBeamLayers());
            ShearWallLayerInfos = new ObservableCollection<ThLayerInfo>(LoadShearwallLayers());
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
        public void PickBeamLayer()
        {
            // 选择图层
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                while(true)
                {
                    var pneo = new PromptNestedEntityOptions("\n请选择梁线:");
                    var pner = Active.Editor.GetNestedEntity(pneo);
                    if (pner.Status == PromptStatus.OK)
                    {
                        if (pner.ObjectId != ObjectId.Null)
                        {
                            var pickedEntity = acdb.Element<Entity>(pner.ObjectId);
                            if (pickedEntity is Curve || pickedEntity is Mline)
                            {
                                if (ThMEPXRefService.OriginalFromXref(pickedEntity.Layer) != "0")
                                {
                                    AddBeamLayer(pickedEntity.Layer);
                                }
                                else
                                {
                                    var containers = pner.GetContainers();
                                    if (containers.Length > 0)
                                    {
                                        // 如果pick到的实体是0图层，就返回其父亲的图层
                                        var parentEntity = acdb.Element<Entity>(containers.First());
                                        AddBeamLayer(parentEntity.Layer);
                                    }
                                }
                            }
                        }
                    }
                    else if (pner.Status == PromptStatus.Cancel)
                    {
                        break;
                    }
                }
            }
        }
        public void PickShearWallLayer()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                while(true)
                {
                    var pneo = new PromptNestedEntityOptions("\n请选择剪力墙填充[Hatch or Solid]:");
                    var pner = Active.Editor.GetNestedEntity(pneo);
                    if (pner.Status == PromptStatus.OK)
                    {
                        if (pner.ObjectId != ObjectId.Null)
                        {
                            var pickedEntity = acdb.Element<Entity>(pner.ObjectId);
                            if (pickedEntity is Hatch || pickedEntity is Solid)
                            {
                                if (ThMEPXRefService.OriginalFromXref(pickedEntity.Layer) != "0")
                                {
                                    AddShearWallLayer(pickedEntity.Layer);
                                }
                                else
                                {
                                    var containers = pner.GetContainers();
                                    if (containers.Length > 0)
                                    {
                                        // 如果pick到的实体是0图层，就返回其父亲的图层
                                        var parentEntity = acdb.Element<Entity>(containers.First());
                                        AddShearWallLayer(parentEntity.Layer);
                                    }
                                }
                            }
                        }                        
                    }
                    else if (pner.Status == PromptStatus.Cancel)
                    {
                        break;
                    }
                }
            }
        }
        public void Save()
        {
            // 保存配置
            ThExtractBeamConfig.Instance.LayerInfos = FilterLayers(BeamLayerInfos.ToList());
            ThExtractShearWallConfig.Instance.LayerInfos = FilterLayers(ShearWallLayerInfos.ToList());
            ThExtractBeamConfig.Instance.BeamEngineOption = this.beamEngineOption;
        }
        public void RemoveBeamLayers(List<string> layers)
        {
            if(layers.Count>0)
            {
                var layerInfos = BeamLayerInfos
                .OfType<ThLayerInfo>()
                .Where(o => !layers.Contains(o.Layer))
                .ToList();
                BeamLayerInfos = new ObservableCollection<ThLayerInfo>(layerInfos);
            }
        }
        public void RemoveShearWallLayers(List<string> layers)
        {
            if (layers.Count > 0)
            {
                var layerInfos = ShearWallLayerInfos
                .OfType<ThLayerInfo>()
                .Where(o => !layers.Contains(o.Layer))
                .ToList();
                ShearWallLayerInfos = new ObservableCollection<ThLayerInfo>(layerInfos);
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
        private List<string> GetShearWallLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => IsShearWallLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private bool IsSBeamLayer(string layer)
        {
            //以S_BEAM结尾的所有图层，默认为梁所在的图层。
            return layer.ToUpper().EndsWith("S_BEAM");
        }
        private bool IsShearWallLayer(string layer)
        {
            //以S_BEAM结尾的所有图层，默认为梁所在的图层。
            return layer.ToUpper().EndsWith("S_WALL_HACH");
        }
        private void AddBeamLayer(string beamLayer)
        {
            if (!IsBeamLayerExisted(beamLayer))
            {
                BeamLayerInfos.Add(new ThLayerInfo()
                {
                    Layer = beamLayer,
                    IsSelected = true,
                });
            }
        }
        private bool IsBeamLayerExisted(string beamLayer)
        {
            return BeamLayerInfos.Where(o => o.Layer == beamLayer).Any();
        }
        private void AddShearWallLayer(string shearWallLayer)
        {
            if (!IsShearWallLayerExisted(shearWallLayer))
            {
                ShearWallLayerInfos.Add(new ThLayerInfo()
                {
                    Layer = shearWallLayer,
                    IsSelected = true,
                });
            }
        }
        private bool IsShearWallLayerExisted(string shearWallLayer)
        {
            return ShearWallLayerInfos.Where(o => o.Layer == shearWallLayer).Any();
        }
        private List<ThLayerInfo> LoadBeamLayers()
        {
            var results = new List<ThLayerInfo>(); 
            // 优先获取以S_BEAM结尾的梁
            var sbeamLayers = GetSBeamLayers().Select(o=>new ThLayerInfo()
            {
                Layer=o,
                IsSelected=true,
            }).ToList();
            results.AddRange(sbeamLayers);

            var storeInfos = FilterLayers(ThExtractBeamConfig.Instance.LayerInfos);
            storeInfos.Where(o => !sbeamLayers.Select(s => s.Layer).Contains(o.Layer))
                .ForEach(o => results.Add(o));

            return results;
        }
        private List<ThLayerInfo> LoadShearwallLayers()
        {
            var results = new List<ThLayerInfo>();
            // 优先获取以S_WALL_HACH结尾的梁
            var shearwallLayers = GetShearWallLayers().Select(o => new ThLayerInfo()
            {
                Layer = o,
                IsSelected = true,
            }).ToList();
            results.AddRange(shearwallLayers);

            var storeInfos = FilterLayers(ThExtractShearWallConfig.Instance.LayerInfos);
            storeInfos.Where(o => !shearwallLayers.Select(s => s.Layer).Contains(o.Layer))
                .ForEach(o => results.Add(o));

            return results;
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
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    } 
}
