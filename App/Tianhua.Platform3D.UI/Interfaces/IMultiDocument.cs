using Autodesk.AutoCAD.ApplicationServices;

namespace Tianhua.Platform3D.UI.Interfaces
{
    /// <summary>
    /// 考虑多文档时继承改接口，主页面管理这些事件，触发相应的事件
    /// （事件不需要监听的，可以不处理）
    /// (需要注意e.Document为空的问题)
    /// 
    /// 页面关闭后不在调用Document的相关事件
    /// 页面启动后先调用ShowDocument事件,相应的页面可以根据业务进行相应的处理
    /// </summary>
    interface IMultiDocument
    {
        /// <summary>
        /// 主页面在CAD中打开时会触发
        /// </summary>
        void MainUIShowInDocument();
        void DocumentActivated(DocumentCollectionEventArgs e);
        void DocumentDestroyed(DocumentDestroyedEventArgs e);
        void DocumentToBeActivated(DocumentCollectionEventArgs e);
        void DocumentToBeDestroyed(DocumentCollectionEventArgs e);
    }
}
