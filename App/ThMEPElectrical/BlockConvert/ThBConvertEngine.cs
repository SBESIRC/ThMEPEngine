﻿using System;
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
        /// 位置变换
        /// </summary>
        public abstract void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 旋转角度
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void Rotate(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

        /// <summary>
        /// 设置动态块可见性
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void SetVisibilityState(ObjectId blkRef, ThBlockReferenceData srcBlockReference);

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
        public abstract void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference, string layer);

        /// <summary>
        /// 镜像变化
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockReference"></param>
        public abstract void Mirror(ObjectId blkRef, ThBlockReferenceData srcBlockReference);
    }
}
