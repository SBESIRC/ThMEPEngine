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
        public readonly string VideoMonitoringSystem = "视屏监控系统";
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
            int thirdIndex = 2;
            int fourthIndex = 3;
            int synonymIndex = 4;
            int startRowIndex = -1;
            RoomTableTree parentRoom = null;
            for (int i = 0; i < RoomInfoMappingTable.Rows.Count; i++)
            {
                DataRow row = RoomInfoMappingTable.Rows[i];
                if (row[thirdIndex] != null)
                {
                    RoomTableTree roomTableTree = new RoomTableTree();
                    roomTableTree.nodeName = row[thirdIndex].ToString();
                    roomTableTree.synonym.AddRange(row[synonymIndex].ToString().Split(','));

                    resRoomTree.Add(roomTableTree);
                    parentRoom = roomTableTree;
                    startRowIndex = i;
                }

                if (startRowIndex >= 0)
                {
                    RoomTableTree roomTableTree = new RoomTableTree();
                    roomTableTree.nodeName = row[fourthIndex].ToString();
                    roomTableTree.synonym.AddRange(row[synonymIndex].ToString().Split(','));

                    parentRoom.child.Add(roomTableTree);
                }
            }

            return resRoomTree;
        }
    }

    public class RoomTableTree
    {
        /// <summary>
        /// 节点房间名
        /// </summary>
        public string nodeName { get; set; }

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