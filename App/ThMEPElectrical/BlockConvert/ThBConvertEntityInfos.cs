using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertEntityInfos
    {
        /// <summary>
        /// 图元Id
        /// </summary>
        public ObjectId ObjectId { get; set; }

        /// <summary>
        /// 来源专业
        /// </summary>
        public EquimentCategory Category { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string EquimentType { get; set; }

        /// <summary>
        /// 目标图层
        /// </summary>
        public string Layer { get; set; }

        public ThBConvertEntityInfos()
        {

        }

        public ThBConvertEntityInfos(ObjectId objectId, EquimentCategory category, string equimentType, string layer)
        {
            ObjectId = objectId;
            Category = category;
            EquimentType = equimentType;
            Layer = layer;
        }
    }

    public enum EquimentCategory
    {
        暖通,
        给排水,
    }

    public static class ThBConvertEquimentCategoryExtensions
    {
        public static EquimentCategory Convert(this string str)
        {
            return str.Equals(EquimentCategory.暖通.ToString()) ? EquimentCategory.暖通 : EquimentCategory.给排水;
        }
    }
}
