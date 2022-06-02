using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.PublicMethod;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel
{
    /// <summary>
    /// 用于封装一次路径规划过程中的规划信息。
    /// </summary>
    public class RoutePlanData<T>
    {
        private Map<T> cellMap;
        /// <summary>
        /// 地图的矩形大小。经过单元格标准处理。
        /// </summary>
        public Map<T> CellMap
        {
            get { return cellMap; }
        }

        /// <summary>
        /// 关闭列表，即存放已经遍历处理过的节点。
        /// </summary>
        private List<AStarNode> closedList = new List<AStarNode>();
        public List<AStarNode> ClosedList
        {
            get { return closedList; }
        }

        /// <summary>
        /// OpenedList 开放列表，即存放已经开发但是还未处理的节点。
        /// </summary>
        private MinHeap<AStarNode> openedList = new MinHeap<AStarNode>();
        public MinHeap<AStarNode> OpenedList
        {
            get { return openedList; }
        }

        #region Ctor
        public RoutePlanData(Map<T> map)
        {
            this.cellMap = map;
        }
        #endregion
    }
}
