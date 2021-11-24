using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.ConnectWiring.Model
{
    /// <summary>
    /// 一类回路的连接信息
    /// </summary>
    public class WiringLoopModel
    {
        public List<LoopInfoModel> loopInfoModels = new List<LoopInfoModel>();
    }

    public class LoopInfoModel
    {
        /// <summary>
        /// 连线内容
        /// </summary>
        public string LineContent { get; set; }

        /// <summary>
        /// 线型
        /// </summary>
        public string LineType { get; set; }

        /// <summary>
        /// 回路连接上限
        /// </summary>
        public int PointNum { get; set; }

        /// <summary>
        /// 回路包含的块名
        /// </summary>
        public List<string> blockNames = new List<string>();
    }

    public class BlockConfigModel
    {
        /// <summary>
        /// 块名
        /// </summary>
        public string blockName { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 回路
        /// </summary>
        public List<string> loops = new List<string>();
    }
}
