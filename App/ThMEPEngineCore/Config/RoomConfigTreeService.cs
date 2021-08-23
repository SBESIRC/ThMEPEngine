using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPEngineCore.Config
{
    public static class RoomConfigTreeService
    {
        readonly static string publicRoom = "公共区域";
        /// <summary>
        /// 将房间映射表建成树
        /// </summary>
        /// <returns></returns>
        public static List<RoomTableTree> CreateRoomTree(DataTable RoomInfoMappingTable)
        {
            List<RoomTableTree> resRoomTree = new List<RoomTableTree>();
            if (RoomInfoMappingTable == null) { return resRoomTree; }

            List<string> levelColumns = new List<string>();
            string synonym = "同义词";
            string tags = "标签";
            foreach (DataColumn column in RoomInfoMappingTable.Columns)
            {
                if (column.ColumnName.Contains(synonym))
                {
                    synonym = column.ColumnName;
                }
                else if (column.ColumnName.Contains(tags))
                {
                    tags = column.ColumnName;
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
                        if (!string.IsNullOrEmpty(row[tags].ToString()))
                        {
                            roomTableTree.tags.AddRange(row[tags].ToString().Split('；'));
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
        /// 查找对应的房间名
        /// </summary>
        /// <param name="roomTree"></param>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public static List<string> CalRoomLst(this List<RoomTableTree> roomTree, string roomName)
        {
            List<string> roomInfo = new List<string>();
            foreach (var treeNode in roomTree)
            {
                roomInfo.AddRange(GetNodeInfo(treeNode, roomName, false));
            }

            return roomInfo;
        }

        /// <summary>
        /// 查找对应的房间名
        /// </summary>
        /// <param name="roomTree"></param>
        /// <param name="roomNames"></param>
        /// <returns></returns>
        public static List<string> CalRoomLst(this List<RoomTableTree> roomTree, List<string> roomNames)
        {
            List<string> roomInfo = new List<string>();
            foreach (var treeNode in roomTree)
            {
                foreach (var roomName in roomNames)
                {
                    roomInfo.AddRange(GetNodeInfo(treeNode, roomName, false));
                }
            }

            return roomInfo;
        }

        /// <summary>
        /// 判断是否是公共区域
        /// </summary>
        /// <param name="roomTree"></param>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public static bool IsPublicRoom(this List<RoomTableTree> roomTree, string roomName)
        {
            foreach (var treeNode in roomTree)
            {
                var thisNode = treeNode;
                if (thisNode.nodeName == roomName || CompareRoom(thisNode.synonym, roomName))
                {
                    if (thisNode.tags.Contains(publicRoom))
                    {
                        return true;
                    }
                }

                if (thisNode.child.Count > 0)
                {
                    if (IsPublicRoom(thisNode.child, roomName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 查找目标级别节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static RoomTableTree GetChildNode(RoomTableTree node, int level)
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

        /// <summary>
        /// 判断两个房间名是否相等
        /// </summary>
        /// <param name="roomA"></param>
        /// <param name="roomB"></param>
        /// <returns></returns>
        public static bool CompareRoom(string roomA, string roomB)
        {
            if (roomA == roomB)
            {
                return true;
            }

            if (roomA.Contains("*"))
            {
                string str = roomA;
                if (roomA[0] != '*')
                {
                    str = '^' + str;
                }
                if (roomA[roomA.Length - 1] != '*')
                {
                    str = str + '$';
                }
                str = str.Replace("*", ".*");
                return Regex.IsMatch(roomB, str);
            }

            return false;
        }

        /// <summary>
        /// 判断一个字符串数组中是否包括指定房间名
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public static bool CompareRoom(List<string> roomList, string room)
        {
            foreach(var roomTidal in roomList)
            {
                if (roomTidal == room)
                {
                    return true;
                }

                if (roomTidal.Contains("*"))
                {
                    string str = roomTidal;
                    if (roomTidal[0] != '*')
                    {
                        str = '^' + str;
                    }
                    if (roomTidal[roomTidal.Length - 1] != '*')
                    {
                        str = str + '$';
                    }
                    str = str.Replace("*", ".*");
                    if (Regex.IsMatch(room, str)) 
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 判断是否包含这个节点
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static List<string> GetNodeInfo(RoomTableTree treeNode, string name, bool hasNode = false)
        {
            List<string> nodeInfos = new List<string>();
            if (hasNode || treeNode.nodeName == name || treeNode.synonym.Any(x => x == name))
            {
                nodeInfos.Add(treeNode.nodeName);
                nodeInfos.AddRange(treeNode.synonym);
                hasNode = true;
            }

            foreach (var tn in treeNode.child)
            {
                nodeInfos.AddRange(GetNodeInfo(tn, name, hasNode));
            }

            return nodeInfos;
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
        /// 标签
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        /// 下一层数据
        /// </summary>
        public List<RoomTableTree> child = new List<RoomTableTree>();
    }
}
