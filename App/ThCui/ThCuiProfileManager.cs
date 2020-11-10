using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.AutoCAD.ThCui
{
    /// <summary>
    /// 专业类型
    /// </summary>
    public enum Profile
    {
        /// <summary>
        /// 方案
        /// </summary>
        ARCHITECTURE = 0,
        /// <summary>
        /// 施工
        /// </summary>
        CONSTRUCTION = 1,
        /// <summary>
        /// 结构
        /// </summary>
        STRUCTURE = 2,
        /// <summary>
        /// 暖通
        /// </summary>
        HAVC = 3,
        /// <summary>
        /// 电气
        /// </summary>
        ELECTRICAL = 4,
        /// <summary>
        /// 给排水
        /// </summary>
        WSS = 5,
    }

    public class ThCuiProfileManager
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThCuiProfileManager instance = new ThCuiProfileManager();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThCuiProfileManager() { }
        internal ThCuiProfileManager() { }
        public static ThCuiProfileManager Instance { get { return instance; } }
        //-------------SINGLETON-----------------
        
        
        public Profile CurrentProfile
        {
            get
            {
                return (Profile)Properties.Settings.Default.Profile;
            }

            set
            {
                Properties.Settings.Default.Profile = (uint)value;
                Properties.Settings.Default.Save();
            }
        }

        public void Reset()
        {
            Properties.Settings.Default.Profile = 0;
            Properties.Settings.Default.Save();
        }
    }
}
