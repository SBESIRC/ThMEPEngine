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
        /// 回路包含的块
        /// </summary>
        public List<LoopBlockInfos> blocks = new List<LoopBlockInfos>();
    }

    public class LoopBlockInfos
    {
        /// <summary>
        /// 块名
        /// </summary>
        public string blockName { get; set; }

        /// <summary>
        /// 块形状
        /// </summary>
        public BlockShape blcokShape { get; set; }

        /// <summary>
        /// X正方向移动
        /// </summary>
        public double XRight { get; set; }

        /// <summary>
        /// X负方向移动
        /// </summary>
        public double XLeft { get; set; }

        /// <summary>
        /// Y正方向移动
        /// </summary>
        public double YRight { get; set; }

        /// <summary>
        /// Y负方向移动
        /// </summary>
        public double YLeft { get; set; }

        /// <summary>
        /// 安装方式
        /// </summary>
        public string InstallMethod { get; set; }

        /// <summary>
        /// 块质量
        /// </summary>
        public int Density { get; set; }
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

        /// <summary>
        /// 块形状
        /// </summary>
        public BlockShape blcokShape { get; set; }

        /// <summary>
        /// X正方向移动
        /// </summary>
        public double XRight { get; set; }

        /// <summary>
        /// X负方向移动
        /// </summary>
        public double XLeft { get; set; }

        /// <summary>
        /// Y正方向移动
        /// </summary>
        public double YRight { get; set; }

        /// <summary>
        /// Y负方向移动
        /// </summary>
        public double YLeft { get; set; }

        /// <summary>
        /// 安装方式
        /// </summary>
        public string InstallMethod { get; set; }

        /// <summary>
        /// 块质量
        /// </summary>
        public int Density { get; set; }
    }

    public enum BlockShape
    {
        Rectangle,

        Capsule,

        Square,

        Trapezoid,

        Circle,
    }
}
