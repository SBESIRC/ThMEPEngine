using System.IO;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThMEPTCH.Services;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class StoreyElevationSetVM
    {
        private string _activeTabName = "";
        private string _copySourceTabName = "";
        private bool isAlwaysPopupSaveTip = true;
        //private  bool isAlwaysPopupSaveSuccessTip = true;
        private string _jsonExtensionName = ".StoreyInfo.json";
        private Dictionary<string, ObservableCollection<ThEditStoreyInfo>> _buildStoreys;
        public StoreyElevationSetVM() 
        {
            _buildStoreys = ParseFromJson();
            if(_buildStoreys.Count==0)
            {
                _buildStoreys.Add("1#", new ObservableCollection<ThEditStoreyInfo>());
            }            
        }

        public List<string> BuildingNames
        {
            get
            {
                return _buildStoreys.Keys.ToList();
            }
        }
        /// <summary>
        /// 记录复制选项卡的源Tab
        /// </summary>
        public string CopySourceTabName
        {
            get => _copySourceTabName;
            set => _copySourceTabName = value;
        }

        /// <summary>
        /// 记录StoreyTabControl活跃的TabName
        /// </summary>
        public string ActiveTabName
        {
            get => _activeTabName;
            set => _activeTabName = value;
        }

        public void DeleteBuilding(string buildingName)
        {
            if(IsExisted(buildingName))
            {
                _buildStoreys.Remove(buildingName);
            }
        }
        public void AddNewBuilding(string buildingName)
        {
            if (!IsExisted(buildingName))
            {
                _buildStoreys.Add(buildingName,new ObservableCollection<ThEditStoreyInfo>());
            }
        }

        public void UpdateBuildingName(string oldBuildingName,string newBuildingName)
        {
            if(oldBuildingName == newBuildingName)
            {
                return;
            }
            if(IsExisted(oldBuildingName))
            {
                var value = _buildStoreys[oldBuildingName];
                _buildStoreys.Remove(oldBuildingName);
                _buildStoreys.Add(newBuildingName, value);
            }
        }

        public void UpdateBuildingStoryes(string buildingName, ObservableCollection<ThEditStoreyInfo> storeys)
        {
            if(IsExisted(buildingName))
            {
                _buildStoreys[buildingName] = storeys;
            }
        }

        public bool IsExisted(string buildingName)
        {
            return _buildStoreys.ContainsKey(buildingName);
        }

        public void CopyBuildingStoreys(string targetBuildName)
        {
            if(this._copySourceTabName!= targetBuildName && IsExisted(this._copySourceTabName) && IsExisted(targetBuildName))
            {
                var souceBuildStoreys = GetBuildingStoreys(this._copySourceTabName);
                UpdateBuildingStoryes(targetBuildName, souceBuildStoreys);
            }
        }

        public void Save()
        {
            string currentName =  GetActiveDocName();
            if(string.IsNullOrEmpty(currentName) || !File.Exists(currentName))
            {
                if(isAlwaysPopupSaveTip)
                {
                    var tipRes = MessageBox.Show("当前打开的Cad文档没有保存，无法保存楼层信息！是否继续弹出此提示？",
                    "保存提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (tipRes == MessageBoxResult.No)
                    {
                        isAlwaysPopupSaveTip = false;
                    }
                }
            }
            else
            {
                var storeyConfigName = GetStoreyConfigFileName();
                bool isSuccess =  Serialize(storeyConfigName, _buildStoreys);
                if(isSuccess)
                {
                    //if(isAlwaysPopupSaveSuccessTip)
                    //{
                    //    var saveTipRes = MessageBox.Show("保存成功！是否继续弹出此提示？", "保存提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    //    if (saveTipRes == MessageBoxResult.No)
                    //    {
                    //        isAlwaysPopupSaveSuccessTip = false;
                    //    }
                    //}                    
                }
                else
                {
                    MessageBox.Show("保存失败，请联系技术支持！", "保存提示", MessageBoxButton.OK,MessageBoxImage.Warning);
                }
            }
        }

        public ObservableCollection<ThEditStoreyInfo> GetBuildingStoreys(string buildingName)
        {
            if (IsExisted(buildingName))
            {
                return _buildStoreys[buildingName];
            }
            else
            {
                return new ObservableCollection<ThEditStoreyInfo>();
            }
        }

        private Dictionary<string, ObservableCollection<ThEditStoreyInfo>> ParseFromJson()
        {
            // 根据当前活动的cad Document 获取其相同路径下的json
            var results = new Dictionary<string, ObservableCollection<ThEditStoreyInfo>>();
            var storetJsonFileName = GetStoreyConfigFileName();
            if(!string.IsNullOrEmpty(storetJsonFileName))
            {
                results = DeSerialize(storetJsonFileName);
            }
            if (results.Count == 0)
            {
                results.Add("1#", new ObservableCollection<ThEditStoreyInfo>());
            }
            return results;
        }

        private bool Serialize(string fileName, Dictionary<string, ObservableCollection<ThEditStoreyInfo>> buildingStoreys)
        {
            return ThIfcStoreyParseTool.Serialize(fileName, ConvertTo(buildingStoreys));
        }

        private Dictionary<string, ObservableCollection<ThEditStoreyInfo>> DeSerialize(string fileName)
        {
            return ConvertTo(ThIfcStoreyParseTool.DeSerialize(fileName));
        }

        private Dictionary<string, ObservableCollection<ThEditStoreyInfo>> ConvertTo(Dictionary<string, List<ThEditStoreyInfo>> buildingStoreys)
        {
            var results = new Dictionary<string, ObservableCollection<ThEditStoreyInfo>>();
            foreach(var item in buildingStoreys)
            {
                results.Add(item.Key, new ObservableCollection<ThEditStoreyInfo>(item.Value));
            }
            return results;
        }

        private Dictionary<string, List<ThEditStoreyInfo>> ConvertTo(Dictionary<string, ObservableCollection<ThEditStoreyInfo>> buildingStoreys)
        {
            var results = new Dictionary<string, List<ThEditStoreyInfo>>();
            foreach (var item in buildingStoreys)
            {
                results.Add(item.Key, item.Value.ToList());
            }
            return results;
        }

        private string GetStoreyConfigFileName()
        {
            var activeDocName = GetActiveDocName();
            if(File.Exists(activeDocName))
            {
                var fileInfo = new FileInfo(activeDocName);
                var dir = fileInfo.Directory.FullName;
                var fileName = Path.GetFileNameWithoutExtension(activeDocName);
                return Path.Combine(dir, fileName+_jsonExtensionName);
            }
            else
            {
                return "";
            }
        }

        private string GetActiveDocName()
        {
            if(acadApp.DocumentManager.MdiActiveDocument!=null)
            {
                return acadApp.DocumentManager.MdiActiveDocument.Name;
            }
            else
            {
                return "";
            }
        }
    }
}
