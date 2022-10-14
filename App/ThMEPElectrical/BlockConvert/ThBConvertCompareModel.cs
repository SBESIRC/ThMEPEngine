using System.ComponentModel;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareModel
    {
        /// <summary>
        /// Guid
        /// </summary>
        public string Guid { get; }

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
        /// 来源专业
        /// </summary>
        public EquimentCategory Category { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string EquimentType { get; set; }

        /// <summary>
        /// 比对结果
        /// </summary>
        public ThBConvertCompareType Type { get; set; }

        /// <summary>
        /// 是否为不同样式的风机
        /// </summary>
        public bool DifferentStyleFans { get; set; }

        public ThBConvertCompareModel()
        {
            Guid = System.Guid.NewGuid().ToString();
            SourceId = ObjectId.Null;
            TargetId = ObjectId.Null;
            TargetIdList = new List<ObjectId>();
            Category = EquimentCategory.暖通;
            EquimentType = "";
            Type = ThBConvertCompareType.Unchanged;
            DifferentStyleFans = false;
        }
    }

    public enum ThBConvertCompareType
    {
        /// <summary>
        /// 无变化
        /// </summary>
        [Description("无变化")]
        Unchanged,

        /// <summary>
        /// 删除
        /// </summary>
        [Description("删除")]
        Delete,

        /// <summary>
        /// 新增
        /// </summary>
        [Description("新增")]
        Add,

        /// <summary>
        /// 位移
        /// </summary>
        [Description("位移")]
        Displacement,

        /// <summary>
        /// 参数变化
        /// </summary>
        [Description("参数变化")]
        ParameterChange,

        /// <summary>
        /// 重复ID
        /// </summary>
        [Description("重复ID")]
        RepetitiveID,
    }
}
