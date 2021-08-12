﻿using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Data
{
    public abstract class ThMEPDataSetFactory
    {
        /// <summary>
        /// 创建数据集
        /// </summary>
        /// <returns></returns>
        public ThMEPDataSet Create(Database database, Point3dCollection collection)
        {
            // 获取原材料
            GetElements(database, collection);

            // 加工原材料
            return BuildDataSet();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        protected abstract void GetElements(Database database, Point3dCollection collection);

        /// <summary>
        /// 创建数据集
        /// </summary>
        protected abstract ThMEPDataSet BuildDataSet();
    }
}
