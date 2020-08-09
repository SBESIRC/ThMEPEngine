using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore
{
    public class ThProjectDirStructureUtils
    {
        /// <summary>
        /// 地下室
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static DirectoryInfo Garage(string root)
        {
            return new DirectoryInfo(Path.Combine(root, "地下室"));
        }
    }
}
