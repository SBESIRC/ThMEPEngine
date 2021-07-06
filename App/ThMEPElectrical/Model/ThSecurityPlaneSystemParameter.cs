using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class ThSecurityPlaneSystemParameter
    {
        public readonly string VideoMonitoringSystem = "视频监控系统";
        public readonly string IntrusionAlarmSystem = "入侵报警系统";
        public readonly string AccessControlSystem = "出入口控制系统";
        public readonly string GuardTourSystem = "电子巡更系统";
        public readonly string RoomNameControl = "房间名称处理";
        /// <summary>
        /// 摄像机均布间距
        /// </summary>
        public double videoDistance { get; set; }

        /// <summary>
        /// 摄像机纵向盲区距离
        /// </summary>
        public double videoBlindArea { get; set; }

        /// <summary>
        /// 摄像机最大成像距离
        /// </summary>
        public double videaMaxArea { get; set; }

        /// <summary>
        /// 出入口控制系统配置表
        /// </summary>
        public DataTable accessControlSystemTable { get; set; }

        /// <summary>
        /// 视频监控系统配置表
        /// </summary>
        public DataTable videoMonitoringSystemTable { get; set; }

        /// <summary>
        /// 入侵报警系统配置表
        /// </summary>
        public DataTable intrusionAlarmSystemTable { get; set; }

        /// <summary>
        /// 电子巡更系统配置表
        /// </summary>
        public DataTable guardTourSystemTable { get; set; }

        /// <summary>
        /// 房间名称映射处理表
        /// </summary>
        public DataTable RoomInfoMappingTable { get; set; }

        /// <summary>
        /// 房间名称映射处理表树状结构
        /// </summary>
        public List<RoomTableTree> RoomInfoMappingTree
        {
            get { return CreateRoomTree(); }
        }

        /// <summary>
        /// 将房间映射表建成树
        /// </summary>
        /// <returns></returns>
        private List<RoomTableTree> CreateRoomTree()
        {
            List<RoomTableTree> resRoomTree = new List<RoomTableTree>();
            if (RoomInfoMappingTable == null) { return resRoomTree; }

            List<string> levelColumns = new List<string>();
            string synonym = "同义词";
            foreach (DataColumn column in RoomInfoMappingTable.Columns)
            {
                if (column.ColumnName.Contains(synonym))
                {
                    synonym = column.ColumnName;
                }
                else
                {
                    levelColumns.Add(column.ColumnName);
                }
            }

            for (int i = 0; i < RoomInfoMappingTable.Rows.Count; i++)
            {
                DataRow row = RoomInfoMappingTable.Rows[i];
                for (int j = 0; j < levelColumns.Count; j++)
                {
                    if (!string.IsNullOrEmpty(row[levelColumns[j]].ToString()))
                    {
                        RoomTableTree roomTableTree = new RoomTableTree();
                        roomTableTree.nodeName = row[levelColumns[j]].ToString();
                        roomTableTree.nodeLevel = j;
                        if (!string.IsNullOrEmpty(row[synonym].ToString()))
                        {
                            roomTableTree.synonym.AddRange(row[synonym].ToString().Split('，'));
                        }

                        if (j == 0)
                        {
                            resRoomTree.Add(roomTableTree);
                        }
                        else
                        {
                            var parentNode = GetChildNode(resRoomTree.Last(), j - 1);
                            if (parentNode != null)
                            {
                                parentNode.child.Add(roomTableTree);
                            }
                        }
                    }
                }
            }

            return resRoomTree;
        }

        /// <summary>
        /// 查找目标级别节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private RoomTableTree GetChildNode(RoomTableTree node, int level)
        {
            if (node.nodeLevel == level)
            {
                return node;
            }
            if (node == null || node.child.Count <= 0 || level < node.nodeLevel)
            {
                return null;
            }

            return GetChildNode(node.child.Last(), level);
        }
    }

    public class RoomTableTree
    {
        /// <summary>
        /// 节点房间名
        /// </summary>
        public string nodeName { get; set; }

        /// <summary>
        /// 节点级
        /// </summary>
        public int nodeLevel { get; set; }

        /// <summary>
        /// 同义词
        /// </summary>
        public List<string> synonym = new List<string>();

        /// <summary>
        /// 下一层数据
        /// </summary>
        public List<RoomTableTree> child = new List<RoomTableTree>();
    }
}