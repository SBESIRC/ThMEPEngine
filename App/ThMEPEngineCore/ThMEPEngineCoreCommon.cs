using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreCommon
    {
        public static string BUILDELEMENT_PROPERTY_CATEGORY = "Category";
        public static string BUILDELEMENT_PROPERTY_USER_TYPE = "UserType";
        public static string BUILDELEMENT_PROPERTY_MATERIAL = "材质";
        public static string BUILDELEMENT_PROPERTY_LAYER = "图层";
        public static string BUILDELEMENT_PROPERTY_PROJECT_LEVEL = "投影层次";
        public static List<string> BUILDELEMENT_PROPERTIES = new List<string>
        {
            BUILDELEMENT_PROPERTY_CATEGORY,
            BUILDELEMENT_PROPERTY_USER_TYPE,
            BUILDELEMENT_PROPERTY_MATERIAL,
            BUILDELEMENT_PROPERTY_LAYER,
            BUILDELEMENT_PROPERTY_PROJECT_LEVEL
        };

        // 图层
        public static string BUILDELEMENT_LAYER_WALL = "AE-WALL";
        public static string BUILDELEMENT_LAYER_WINDOW = "AE-WIND";
        public static string BUILDELEMENT_LAYER_CURTAIN_WALL = "AE-FNSH";

        // 分类
        public static string BUILDELEMENT_CATEGORY_WALL = "墙";
        public static string BUILDELEMENT_CATEGORY_S_BEAM = "结构梁";
        public static string BUILDELEMENT_CATEGORY_S_WALL = "结构墙";
        public static string BUILDELEMENT_CATEGORY_S_COLUMN = "结构柱";

        // 公差
        public const double SIMILARITYMEASURETOLERANCE = 0.9;
        /// <summary>
        /// 梁端口绘制密封圈扩展宽度
        /// </summary>
        public const double BeamExtensionRatio = 1.01;
        /// <summary>
        /// 梁分割，梁宽度延伸比例
        /// </summary>
        public const double BeamIntersectionRatio = 2.0;
        /// <summary>
        /// 梁边界与其它物体靠近的距离
        /// </summary>
        public const double BeamIntersectExtentionTolerance = 50.0;
        /// <summary>
        /// 梁与梁间隔最大容差
        /// </summary>
        public const double BeamIntervalMaximumTolerance = 600.0;
        /// <summary>
        /// 梁与梁连接的最小查找距离
        /// </summary>
        public const double BeamIntervalMinimumTolerance = 20.0;
        /// <summary>
        /// 梁与竖向构件连接容差
        /// </summary>
        public const double BeamComponentConnectionTolerance = 600.0;
        /// <summary>
        /// 最短梁长
        /// </summary>
        public const double BeamMinimumLength = 100.0;
    }
}
