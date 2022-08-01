using System;
using System.IO;
using System.Collections.Generic;

namespace ThMEPStructure.Common
{
    public class ThParseStoreyService
    {
        public List<StoreyInfo> ParseFromTxt(string txtFileName)
        {
            var storyes = new List<StoreyInfo>();
            var lines = ReadAllLines(txtFileName);
            int j = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var storeyInfo = new StoreyInfo();
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

        public List<StoreyInfo> ParseFromJson(string jsonFileName)
        {
            throw new NotImplementedException();
        }

        private void SetValue(string key,string value, StoreyInfo info)
        {
            switch(key)
            {
                case "storey name":
                    info.StoreyName = value;
                    break;
                case "elevation":
                    info.Elevation = value;
                    break;
                case "top_elevation":
                    info.Top_Elevation = value;
                    break;
                case "bottom_elevation":
                    info.Bottom_Elevation = value;  
                    break;
                case "description":
                    info.Description = value;
                    break;
                case "FloorNo":
                    info.FloorNo = value;
                    break;
                case "Height":
                    info.Height = value;
                    break;
                case "StdFlrNo":
                    info.StdFlrNo = value;
                    break;
            }
        }

        private List<string> Split(string content,char separator)
        {
            var results = new List<string>();   
            foreach(string item in content.Split(separator))
            {
                results.Add(item.Trim());
            }
            return results; 
        }

        
        private string[] ReadAllLines(string txtFileName)
        {
            return File.ReadAllLines(txtFileName);
        }
    }

    public class StoreyInfo
    {
        public string StoreyName { get; set; } ="";
        public string Elevation { get; set; } = "";
        public string Top_Elevation { get; set; } = "";
        public string Bottom_Elevation { get; set; } = "";
        public string Description { get; set; } = "";
        public string FloorNo { get; set; } = "";
        public string Height { get; set; } = "";
        public string StdFlrNo { get; set; } = "";
    }
}
