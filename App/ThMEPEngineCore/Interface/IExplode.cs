using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface IExplode
    {
        /// <summary>
        /// 炸成直线
        /// </summary>
        /// <param name="objs"></param>
        /// <returns>一堆直线</returns>
        DBObjectCollection Explode(DBObjectCollection objs);
    }
}
