using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Temp
{
    class ThFireHydrantResultParseService : IParse
    {
        public List<FirehydrantData> Results { get; set; }
        public ThFireHydrantResultParseService()
        {
            Results = new List<FirehydrantData>();
        }
        public void Parse(string fileName)
        {
            try
            {                
                // 创建一个 StreamReader 的实例来读取文件 
                // using 语句也能关闭 StreamReader
                using (StreamReader sr = new StreamReader(fileName,Encoding.Default))
                {                    
                    StringBuilder sb = new StringBuilder();
                    string line = "";
                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {                        
                        if(line.Contains("消火栓")|| line.Contains("灭火器"))
                        {
                            ParseContent(sb.ToString());
                            sb.Clear();
                        }
                        sb.Append(line);
                    }
                    ParseContent(sb.ToString());
                }
            }
            catch (Exception e)
            {
                throw e; // 向用户显示出错消息
            }
        }
        private void ParseContent(string value)
        {
            if(string.IsNullOrEmpty(value))
            {
                return;
            }
            int outerIndex = value.IndexOf("outer bound");
            int innerIndex = value.IndexOf("inner bound");
            if (outerIndex==-1)
            {
                return;
            }
            if(innerIndex==-1)
            {
                innerIndex = value.Length;
            }
            var data = new FirehydrantData();
            string fireNamePos = value.Substring(0, outerIndex);
            data.Name = FindName(fireNamePos);
            bool findPos = false;
            var position = GetPosition(fireNamePos,out findPos);
            if(findPos)
            {
                data.Position = position;
            }
            string outerContent = value.Substring(outerIndex, innerIndex- outerIndex);
            string innerContent = value.Substring(innerIndex);
            data.OuterPoints = GetPoints(outerContent);
            data.InnerPoints = GetPoints(innerContent);

            Results.Add(data);
        }
        private List<Point3d> GetPoints(string content)
        {
            var results = new List<Point3d>();
            string pattern = @"[[]\s*\d+\.?\d*\s*[,]\s*\d+\.?\d*\s*]";
            Regex rg = new Regex(pattern);
            foreach (Match match in rg.Matches(content))
            {
                bool findPos = false;
                var position = GetPosition(match.Value, out findPos);
                if(findPos)
                {
                    results.Add(position);
                }
            }
            return results;
        }
        private Point3d GetPosition(string content,out bool res)
        {
            res = false;
            string pattern = @"\d+\.?\d*";
            Regex rg = new Regex(pattern);
            var values = new List<double>();
            foreach(Match match in rg.Matches(content))
            {
                double value = 0.0;
                if(double.TryParse(match.Value,out value))
                {
                    values.Add(value);
                }
            }
            if(values.Count==2)
            {
                res = true;
                return new Point3d(values[0],values[1],0);
            }
            else
            {
                return Point3d.Origin;
            }
        }
        private string FindName(string content)
        {
            if (content.Contains("消火栓"))
            {
                return "消火栓";
            }
            else if (content.Contains("灭火器"))
            {
                return "灭火器";
            }
            else
            {
                return "";
            }
        }
    }
    class FirehydrantData
    {
        public string Name { get; set; }
        public Point3d Position { get; set; }

        public List<Point3d> OuterPoints { get; set; }
        public List<Point3d> InnerPoints { get; set; }
        public FirehydrantData()
        {
            Name = "";
            OuterPoints = new List<Point3d>();
            InnerPoints = new List<Point3d>();
        }
        public bool IsValid()
        {
            if(string.IsNullOrEmpty(Name))
            {
                return false;
            }
            if(OuterPoints.Count<3)
            {
                return false;
            }
            return true;
        }
    }
}
