using System;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPEngineCoreService instance = new ThMEPEngineCoreService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPEngineCoreService() { }
        internal ThMEPEngineCoreService() { }
        public static ThMEPEngineCoreService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThBuildingElementRecognitionEngine CreateBeamEngine()
        {
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                return new ThDB3BeamRecognitionEngine();
            }
            else
            {
                return new ThRawBeamRecognitionEngine();
            }
        }

        /// <summary>
        /// 在识别柱轮廓时是否扩大柱轮廓
        /// 扩大柱轮廓可以帮助处理柱和梁的连接
        /// </summary>
        public bool ExpandColumn { get; set; } = true;
    }
}
