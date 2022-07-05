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
        public ObjectId SourceID { get; set; }

        /// <summary>
        /// 当前转换图元ID
        /// </summary>
        public ObjectId TargetID { get; set; }

        /// <summary>
        /// 比对结果
        /// </summary>
        public ThBConvertCompareType Type { get; set; }

        public ThBConvertCompareModel()
        {
            SourceID = ObjectId.Null;
            TargetID = ObjectId.Null;
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
