using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThStructurePlaneConfig
    {
        /// <summary>
        /// 传入的IFC文件
        /// </summary>
        public string IfcFilePath { get; set; }
        /// <summary>
        /// 输出的Svg文件名
        /// </summary>
        public string SavePath { get; set; }
        /// <summary>
        /// 当前层名
        /// </summary>
        public string CurrentFloor { get; set; }
        /// <summary>
        /// 上一层名
        /// </summary>
        public string HighFloor { get; set; }
        /// <summary>
        /// config.json的完整路径
        /// </summary>
        public string SvgConfigFilePath
        {
            get
            {
                return GetSvgConfgFilePath();
            }
        }
        /// <summary>
        /// 生成的Svg文件存放的目录
        /// </summary>
        public string SvgDefaultSavePath
        {
            get
            {
                return GetSvgDefaultSavePath();
            }
        }
        public string IfcFileName
        {
            get
            {
                var fi = new FileInfo(IfcFilePath);
                if(fi.Exists)
                {
                    return Path.GetFileNameWithoutExtension(IfcFilePath);
                }
                else
                {
                    return "";
                }
            }
        }
        public ThStructurePlaneConfig()
        {
            IfcFilePath = "";
            SavePath = "";
        }
        public void Configure()
        {
            if (!CheckValid())
            {
                return;
            }
            SelfSetSavePath();            
            var svgConfig = ReadJson(SvgConfigFilePath);
            if(svgConfig==null)
            {
                return;
            }
            SetValues(svgConfig);
            WriteJson(SvgConfigFilePath, svgConfig);
        }

        private string GetSvgConfgFilePath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(path,"config.json");
        }

        private string GetSvgDefaultSavePath()
        {
            return Path.GetTempPath();
            //return Path.Combine(Environment.GetEnvironmentVariable("windir"),"TEMP");
        }

        private void SelfSetSavePath()
        {
            if (string.IsNullOrEmpty(SavePath))
            {
                SavePath = SvgDefaultSavePath;
            }
            else
            {
                var dirInfo = new DirectoryInfo(SavePath);
                if (!dirInfo.Exists)
                {
                    SavePath = SvgDefaultSavePath;
                }
            }
        }

        private void SetValues(JObject jObject)
        {
            try
            {
                if(!string.IsNullOrEmpty(IfcFilePath))
                {
                    jObject["ObjConfig"]["path"] = ModifyIfcSavePath(IfcFilePath);
                }
                if (!string.IsNullOrEmpty(SavePath))
                {
                    jObject["SvgConfig"]["save_path"] = SavePath;
                }
                if (!string.IsNullOrEmpty(CurrentFloor))
                {
                    jObject["ObjConfig"]["current_floor"] = CurrentFloor;
                }
                if(!string.IsNullOrEmpty(HighFloor))
                {
                    jObject["ObjConfig"]["high_floor"] = HighFloor;
                }
            }
            catch
            {
            }
        }

        private string ModifyIfcSavePath(string ifcSavePath)
        {
            return ifcSavePath.Replace("\\","/");
        }

        private bool CheckValid()
        {
            if (string.IsNullOrEmpty(IfcFilePath))
            {
                return false;
            }
            var fileInfo1 = new FileInfo(IfcFilePath);
            if(!fileInfo1.Exists)
            {
                return false;
            }
            return true;
        }        
        /// <summary>
        /// 写入JSON文件
        /// </summary>
        /// <param name="jsonFile"></param>
        /// <param name="jObject"></param>
        private void WriteJson(string jsonFile, JObject jObject)
        {
            using (var streamWriter = new System.IO.StreamWriter(jsonFile))
            {
                streamWriter.Write(jObject.ToString());
            }
        }
        private JObject ReadJson(string jsonFile)
        {
            try
            {
                using (var streamReader = System.IO.File.OpenText(jsonFile))
                {
                    using (JsonTextReader reader = new JsonTextReader(streamReader))
                    {
                        JObject jObject = (JObject)JToken.ReadFrom(reader);
                        return jObject;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
