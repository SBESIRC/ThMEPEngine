using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.FastAStarAlgorithm.FastAStarAlgorithm;

namespace ThMEPEngineCore.Algorithm.FastAStarAlgorithm.Point_PathFinding.Model
{
    public class RoutePlanMap
    {
        private PointMap cellMap;
        /// <summary>
        /// 地图的矩形大小。经过单元格标准处理。
        /// </summary>
        public PointMap CellMap
        {
            get { return cellMap; }
        }

        /// <summary>
        /// 关闭列表，即存放已经遍历处理过的节点。
        /// </summary>
        private List<NodeModel> closedList = new List<NodeModel>();
        public List<NodeModel> ClosedList
        {
            get { return closedList; }
        }

        /// <summary>
        /// OpenedList 开放列表，即存放已经开发但是还未处理的节点。
        /// </summary>
        private MinHeap<NodeModel> openedList = new MinHeap<NodeModel>();
        public MinHeap<NodeModel> OpenedList
        {
            get { return openedList; }
        }

        #region Ctor
        public RoutePlanMap(PointMap map)
        {
            this.cellMap = map;
        }
        #endregion
    }
}
