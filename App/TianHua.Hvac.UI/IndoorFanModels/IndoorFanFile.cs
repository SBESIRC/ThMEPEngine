using System;
using System.Collections.Generic;
using System.Data;
using ThMEPHVAC.IndoorFanModels;

namespace TianHua.Hvac.UI.IndoorFanModels
{
    class IndoorFanFile
    {
        public string Guid { get; }
        public string ShowName { get; set; }
        public string FilePath { get; }
        public bool IsDefult { get; }
        public DataSet FanDataSet { get; }
        public List<IndoorFanData> FileFanDatas { get; }
        public IndoorFanFile(string filePath,DataSet data,string showName,bool isDefault) 
        {
            this.Guid = System.Guid.NewGuid().ToString();
            this.FilePath = filePath;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            this.ShowName = string.IsNullOrEmpty(showName) ? fileName : showName;
            this.FanDataSet = data;
            this.IsDefult = isDefault;
            this.FileFanDatas = new List<IndoorFanData>();
        }
    }
    class IndoorFanData
    {
        /// <summary>
        /// Id，防止表格中工况名称有重复的，这里使用Id
        /// </summary>
        public string Uid { get; }
        public string FanFileId { get; }
        public EnumFanType FanType { get; }
        public string SheetName { get; }
        public List<IndoorFanBase> FanAllDatas { get; }
        public FanWorkingCondition ShowWorkingData { get; set; }
        public IndoorFanData(string fanFileId,EnumFanType fanType,string sheetName)
        {
            this.FanFileId = fanFileId;
            this.FanType = fanType;
            this.SheetName = sheetName;
            this.Uid = Guid.NewGuid().ToString();
            this.FanAllDatas = new List<IndoorFanBase>();
        }
    }
}
