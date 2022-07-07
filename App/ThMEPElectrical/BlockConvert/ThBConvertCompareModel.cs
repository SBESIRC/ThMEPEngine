using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareModel
    {
        /// <summary>
        /// 图元database
        /// </summary>
        public Database Database { get; set; }

        /// <summary>
        /// 源图纸图元ID
        /// </summary>
        public ObjectId SourceId { get; set; }

        /// <summary>
        /// 当前转换图元ID
        /// </summary>
        public ObjectId TargetId { get; set; }

        /// <summary>
        /// 当前转换图元ID
        /// </summary>
        public List<ObjectId> TargetIdList { get; set; }

        /// <summary>
        /// 比对结果
        /// </summary>
        public ThBConvertCompareType Type { get; set; }

        public ThBConvertCompareModel()
        {
            SourceId = ObjectId.Null;
            TargetId = ObjectId.Null;
            TargetIdList = new List<ObjectId>();
            Type = ThBConvertCompareType.Unchanged;
        }
    }

    public enum ThBConvertCompareType
    {
        /// <summary>
        /// 无变化
        /// </summary>
        Unchanged,

        /// <summary>
        /// 删除
        /// </summary>
        Delete,

        /// <summary>
        /// 新增
        /// </summary>
        Add,

        /// <summary>
        /// 位移
        /// </summary>
        Displacement,

        /// <summary>
        /// 参数变化
        /// </summary>
        ParameterChange,

        /// <summary>
        /// 重复ID
        /// </summary>
        RepetitiveID,
    }
}
