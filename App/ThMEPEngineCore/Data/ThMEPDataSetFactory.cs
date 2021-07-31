namespace ThMEPEngineCore.Data
{
    public abstract class ThMEPDataSetFactory
    {
        /// <summary>
        /// 创建数据集
        /// </summary>
        /// <returns></returns>
        public ThMEPDataSet Create()
        {
            // 获取原材料
            GetSpatialElements();
            GetBuildingElements();

            // 加工原材料
            return BuildDataSet();
        }

        /// <summary>
        /// 获取空间元素
        /// </summary>
        protected abstract void GetSpatialElements();

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        protected abstract void GetBuildingElements();

        /// <summary>
        /// 创建数据集
        /// </summary>
        protected abstract ThMEPDataSet BuildDataSet();
    }
}
