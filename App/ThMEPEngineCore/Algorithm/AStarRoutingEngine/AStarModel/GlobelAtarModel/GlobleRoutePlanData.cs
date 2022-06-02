using System;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel
{
    /// <summary>
    /// 用于封装一次路径规划过程中的规划信息。
    /// </summary>
    public class GlobleRoutePlanData<T>
    {
        private GlobleMap<T> cellMap;
        /// <summary>
        /// 地图的矩形大小。经过单元格标准处理。
        /// </summary>
        public GlobleMap<T> CellMap
        {
            get { return cellMap; }
        }

        /// <summary>
        /// 关闭列表，即存放已经遍历处理过的节点。
        /// </summary>
        private List<GlobleNode> closedList = new List<GlobleNode>();
        public List<GlobleNode> ClosedList
        {
            get { return closedList; }
        }

        /// <summary>
        /// OpenedList 开放列表，即存放已经开发但是还未处理的节点。
        /// </summary>
        private List<GlobleNode> openedList = new List<GlobleNode>();
        public List<GlobleNode> OpenedList
        {
            get { return openedList; }
        }

        #region Ctor
        public GlobleRoutePlanData(GlobleMap<T> map)
        {
            this.cellMap = map;
        }
        #endregion
    }
}
