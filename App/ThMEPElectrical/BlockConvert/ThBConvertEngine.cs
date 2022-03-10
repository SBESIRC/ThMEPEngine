using System;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public abstract class ThBConvertEngine : IDisposable
    {
        public void Dispose()
        {
            //
        }

        /// <summary>
        /// 插入图块
        /// </summary>
        public abstract ObjectId Insert(string name, Scale3d scale, ThBlockReferenceData srcBlockData);

        /// <summary>
        /// 位置变换
        /// </summary>
        public abstract void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);

        public abstract void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData, List<ThRawIfcDistributionElementData> list, Scale3d scale);

        /// <summary>
        /// 旋转角度
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public abstract void Rotate(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);

        /// <summary>
        /// 设置动态块可见性
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public abstract void SetVisibilityState(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);

        /// <summary>
        /// 设置属性信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public abstract void MatchProperties(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);

        /// <summary>
        /// 设置数据库信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void SetDatabaseProperties(ThBlockReferenceData targetBlockData, string layer);

        /// <summary>
        /// 镜像变化
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public abstract void Mirror(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);

        /// <summary>
        /// 块的特殊处理
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public abstract void SpecialTreatment(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData);
    }
}
