using System;
using System.IO;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThCADExtension;

namespace ThMEPEngineCore.Service
{
    /// <summary>
    /// 解析*.storeyes.txt
    /// </summary>
    public class ThParseStoreyService
    {
        public static List<ThIfcStoreyInfo> ParseFromTxt(string txtFileName)
        {
            var storyes = new List<ThIfcStoreyInfo>();
            var lines = ReadAllLines(txtFileName);
            int j = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var storeyInfo = new ThIfcStoreyInfo();
                for (j = i; j < lines.Length; j++)
                {
                    if (lines[j].Trim() == "------------------------------------")
                    {
                        break;
                    }
                    var pair = Split(lines[j], ':');
                    if (pair.Count == 2)
                    {
                        SetValue(pair[0], pair[1], storeyInfo);
                    }
                }
                storyes.Add(storeyInfo);
                i = j;
            }
            return storyes;
        }

        public List<ThIfcStoreyInfo> ParseFromJson(string jsonFileName)
        {
            throw new NotImplementedException();
        }

        private static void SetValue(string key,string value, ThIfcStoreyInfo info)
        {
            switch(key)
            {
                case "storey name":
                    info.StoreyName = value;
                    break;
                case "elevation":
                    info.Elevation = ThStringTools.ConvertTo(value);
                    break;
                case "top_elevation":
                    info.Top_Elevation = ThStringTools.ConvertTo(value);
                    break;
                case "bottom_elevation":
                    info.Bottom_Elevation = ThStringTools.ConvertTo(value);  
                    break;
                case "description":
                    info.Description = value;
                    break;
                case "FloorNo":
                    info.FloorNo = value;
                    break;
                case "Height":
                    info.Height = ThStringTools.ConvertTo(value);
                    break;
                case "StdFlrNo":
                    info.StdFlrNo = value;
                    break;
            }
        }

        private static List<string> Split(string content,char separator)
        {
            var results = new List<string>();   
            foreach(string item in content.Split(separator))
            {
                results.Add(item.Trim());
            }
            return results; 
        }

        
        private static string[] ReadAllLines(string txtFileName)
        {
            return File.ReadAllLines(txtFileName);
        }
    }
}
