using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public class SmokeLayoutDataModel
    {
        /// <summary>
        /// 布置类型
        /// </summary>
        public string LayoutType { get; set; }

        /// <summary>
        /// 布置场所
        /// </summary>
        public string AreaLayout { get; set; }

        /// <summary>
        /// 布置逻辑
        /// </summary>
        public string LayoutLogic { get; set; }


        /// <summary>
        /// 梁高
        /// </summary>
        public int BeamDepth { get; set; }

        /// <summary>
        /// 顶板厚度
        /// </summary>
        public int RoofThickness { get; set; }

        /// <summary>
        /// 房间面积
        /// </summary>
        public string RoomArea { get; set; }

        /// <summary>
        /// 房间高度
        /// </summary>
        public string RoomHeight { get; set; }

        /// <summary>
        /// 屋顶坡度
        /// </summary>
        public string SlopeRoof { get; set; }

    }
}
