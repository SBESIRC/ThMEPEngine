using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.MapService;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model
{
    /// <summary>
    /// 用于封装一次路径规划过程中的规划信息。
    /// </summary>
    public class RoutePlanData
    {
        /// <summary>
        /// 地图的矩形大小。经过单元格标准处理。
        /// </summary>
        public Map CellMap { get; set; }

        /// <summary>
        /// 关闭列表，即存放已经遍历处理过的节点。
        /// </summary>
        public List<AStarNode> ClosedList = new List<AStarNode>();

        /// <summary>
        /// OpenedList 开放列表，即存放已经开发但是还未处理的节点。
        /// </summary>
        public List<AStarNode> OpenedList = new List<AStarNode>();

        #region Ctor
        public RoutePlanData(Map map)
        {
            this.CellMap = map;
        }
        #endregion
    }
}
