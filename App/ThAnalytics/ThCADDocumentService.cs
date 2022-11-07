using System;
using System.IO;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThAnalytics
{
    public class ThCADDocumentService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Lazy<ThCADDocumentService> lazy =
            new Lazy<ThCADDocumentService>(() => new ThCADDocumentService());

        public static ThCADDocumentService Instance { get { return lazy.Value; } }

        private ThCADDocumentService()
        {
        }
        //-------------SINGLETON-----------------

        /// <summary>
        /// CAD文档路径
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            Reset();
        }

        /// <summary>
        /// 反初始化
        /// </summary>
        public void UnInitialize()
        {
            Name = string.Empty;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            UnInitialize();
            if (AcApp.DocumentManager.MdiActiveDocument != null)
            {
                if (!AcApp.DocumentManager.MdiActiveDocument.IsNamedDrawing)
                {
                    Name = new FileInfo(AcApp.DocumentManager.MdiActiveDocument.Name).FullName;
                }
            }
        }
    }
}