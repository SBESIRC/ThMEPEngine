using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameComparer
    {
        private ThCADCoreNTSSpatialIndex TargetSI;
        private ThCADCoreNTSSpatialIndex SourceSI;

        public ThMEPFrameComparer(DBObjectCollection target, DBObjectCollection source)
        {
            TargetSI = new ThCADCoreNTSSpatialIndex(target);
            SourceSI = new ThCADCoreNTSSpatialIndex(source);
        }

        /// <summary>
        /// 目标集合和源集合比较，目标集合中未变化的元素
        /// </summary>
        public DBObjectCollection Unchanged
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 目标集合和源集合比较，目标集合中有变化的元素
        /// </summary>
        public DBObjectCollection Changed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 目标集合和源集合比较，目标集合中新添加的元素
        /// </summary>
        public DBObjectCollection Appended
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 目标集合和源集合比较，目标集合中被删除的元素
        /// </summary>
        public DBObjectCollection Erased
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
