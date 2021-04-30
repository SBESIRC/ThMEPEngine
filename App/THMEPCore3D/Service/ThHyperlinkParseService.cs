using THMEPCore3D.Utils;
using THMEPCore3D.Interface;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace THMEPCore3D.Service
{
    public class ThHyperlinkParseService : IParse
    {
        public Dictionary<string, string> Properties { get; private set; }
        public ThHyperlinkParseService()
        {
            Properties = new Dictionary<string, string>();
        }
        public void Parse(string content)
        {
            // 首先获取第一个分割符“：”
            int index = content.IndexOf(':');
            if (index == -1)
            {
                return ;
            }
            // 按分割符“__”分割属性
            var properties = Regex.Split(content, "_");
            foreach (var property in properties)
            {
                var keyValue = Regex.Split(property, ":");
                if(keyValue.Length!=2)
                {
                    continue;
                }
                if (ThQueryFieldManager.Contains(keyValue[0]))
                {
                    Properties.Add(keyValue[0], keyValue[1]);
                }
            }
        }
    }
}
