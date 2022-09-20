using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.CAD
{
    public static class ThDBVisitorExtension
    {
        /// <summary>
        /// 判断是否是95%的结构数据
        /// </summary>
        public static bool Is95PercentStructureElement(this Entity entity)
        {
            var links = entity.Hyperlinks;
            if (links.Count > 0)
            {
                //eg: "Major:Structure,Spec:200x400"
                var description = links[links.Count -1].Description;
                foreach (string item in description.Split(','))
                {
                    var fields = item.Split(':');
                    if (fields.Length ==2 && fields[0].ToUpper() == "MAJOR" && fields[1].TrimStart().ToUpper() =="STRUCTURE")
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
    }
}
