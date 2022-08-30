﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using ThMEPEngineCore.IO.JSON;
using TianHua.Platform3D.UI.Model;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class StoreyElevationSetVM
    {
        private static bool isAlwaysPopupSaveTip = true;
        //private static bool isAlwaysPopupSaveSuccessTip = true;
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

        public void CopyBuildingStoreys(string sourceBuildingName,string targetBuildName)
        {
            if(sourceBuildingName!= targetBuildName && IsExisted(sourceBuildingName) && IsExisted(targetBuildName))
            {
                var souceBuildStoreys = GetBuildingStoreys(sourceBuildingName);
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
                    //    var saveTipRes = MessageBox.Show("保存成功！是否基础弹出此提示？", "保存提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
            bool isSuccess = false;
            try
            {
                var storeyConfigName = GetStoreyConfigFileName();
                string jsonString = JsonHelper.SerializeObject(buildingStoreys, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(fileName, jsonString);
                isSuccess = true;
            }
            catch
            {
                //
            }
            return isSuccess;
        }

        private Dictionary<string, ObservableCollection<ThEditStoreyInfo>> DeSerialize(string fileName)
        {
            var results = new Dictionary<string, ObservableCollection<ThEditStoreyInfo>>();
            try
            {
                var jsonString =  File.ReadAllText(fileName);
                results = JsonHelper.DeserializeJsonToObject<Dictionary<string, ObservableCollection<ThEditStoreyInfo>>>(jsonString);
            }
            catch
            {
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
            if(acadApp.DocumentManager.Count>0)
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
