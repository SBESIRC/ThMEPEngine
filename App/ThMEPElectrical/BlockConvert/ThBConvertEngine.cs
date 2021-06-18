using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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
        public abstract ObjectId Insert(string name, Scale3d scale, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 坐标变换
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 坐标调整
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void Adjust(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 设置属性信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void MatchProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 设置数据库信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference);
    }
}
