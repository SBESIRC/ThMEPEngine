using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 元器件配置类
    /// </summary>
    public class ComponentConfig
    {
        /// <summary>
        /// 极数
        /// </summary>
        public static List<string> PolesNums = new List<string>() { "1P", "2P", "3P", "4P", "1P+N", "3P+N" };
    }
}
