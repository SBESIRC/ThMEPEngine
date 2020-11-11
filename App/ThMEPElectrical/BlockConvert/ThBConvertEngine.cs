using System;
using Autodesk.AutoCAD.Geometry;
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
        public abstract ObjectId Insert(string name, Scale3d scale, ThBConvertBlockReference srcBlockReference);

        /// <summary>
        /// 调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void TransformBy(ObjectId blkRef, ThBConvertBlockReference srcBlockReference);

        /// <summary>
        /// 设置属性信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void MatchProperties(ObjectId blkRef, ThBConvertBlockReference srcBlockReference);

        /// <summary>
        /// 设置数据库信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void SetDatbaseProperties(ObjectId blkRef, ThBConvertBlockReference srcBlockReference);
    }
}
