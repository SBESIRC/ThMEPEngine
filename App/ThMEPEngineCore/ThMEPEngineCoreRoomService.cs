using System.IO;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using System.Collections.Generic;
using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreRoomService
    {
        readonly static string publicRoom = "公共区域";
        readonly static string privateRoom = "私有区域";
        readonly static string outdoorSafeArea = "室外安全区域";
        readonly static string nonFireDetectionArea = "非火灾探测区域";
        readonly static string evacuationInstruction = "疏散指示";
        readonly static string evacuationLighting = "疏散照明";
        readonly static string roomConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "房间名称分类处理.xlsx");
        readonly static string roomNameControl = "房间名称处理";
        readonly static List<string> mustLayoutArea = new List<string> { "楼梯间", "前室" };
        readonly static List<string> cannotLayoutArea = new List<string> { "井道", "存储房间", "设备机房", "无障碍套型", "居住套型" };

        private List<RoomTableTree> Tree { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            ReadRoomConfigTable(roomConfigUrl);
        }

        public List<string> GetLabels(ThIfcRoom room)
        {
            foreach (var roomName in room.Tags)
            {
                var labels = Search(Tree, roomName);
                if (labels.Count != 0)
                {
                    return labels;
                }
            }
            return new List<string>();
        }

        private List<string> Search(List<RoomTableTree> tree, string roomName)
        {
            var listString = new List<string>();
            foreach (var treeNode in tree)
            {
                if (treeNode.nodeName == roomName || RoomConfigTreeService.CompareRoom(treeNode.synonym, roomName))
                {
                    return treeNode.tags;
                }
                else if (treeNode.child.Count > 0)
                {
                    listString = Search(treeNode.child, roomName);
                    if (listString.Count > 0)
                    {
                        return listString;
                    }
                }
            }
            return listString;
        }

        private bool JudgeRoomType(List<RoomTableTree> tree, string roomName, List<string> listString)
        {
            var tag = false;
            foreach (var roomType in listString)
            {
                var roomTableTree = RoomTypeIteration(tree, roomName, roomType);
                tag = JudgeRoomName(roomTableTree, roomName, roomType);
                if (tag)
                {
                    return tag;
                }
            }
            return tag;
        }

        private RoomTableTree RoomTypeIteration(List<RoomTableTree> tree, string roomName, string roomType)
        {
            foreach (var treeNode in tree)
            {
                if (treeNode.nodeName == roomType)
                {
                    return treeNode;
                }
                else if (treeNode.child.Count > 0)
                {
                    var roomTableTree = RoomTypeIteration(treeNode.child, roomName, roomType);
                    if (roomTableTree.nodeName != null)
                    {
                        return roomTableTree;
                    }
                }
            }
            return new RoomTableTree();
        }

        private bool JudgeRoomName(RoomTableTree treeNode, string roomName, string roomType)
        {
            if (treeNode.nodeName == roomName || RoomConfigTreeService.CompareRoom(treeNode.synonym, roomName))
            {
                return true;
            }
            else if (treeNode.child.Count > 0)
            {
                if (JudgeRoomName(treeNode.child, roomName, roomType))
                {
                    return true;
                }
            }
            return false;
        }

        private bool JudgeRoomName(List<RoomTableTree> tree, string roomName, string roomType)
        {
            foreach (var treeNode in tree)
            {
                if (treeNode.nodeName == roomName || RoomConfigTreeService.CompareRoom(treeNode.synonym, roomName))
                {
                    return true;
                }
                else if (treeNode.child.Count > 0)
                {
                    if (JudgeRoomName(treeNode.child, roomName, roomType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为公共区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsPublic(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(publicRoom);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为私有区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsPrivate(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(privateRoom);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为室外安全区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsOutdoorSafeArea(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(outdoorSafeArea);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为非火灾探测区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsNonFireDetectionArea(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(nonFireDetectionArea);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否需要布置疏散指示装置
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsEvacuationInstruction(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(evacuationInstruction);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否需要布置疏散照明装置
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool IsEvacuationLighting(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return false;
                }
                return label.Contains(evacuationLighting);
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为必布区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool MustLayoutArea(ThIfcRoom room)
        {
            foreach (var roomName in room.Tags)
            {
                if (JudgeRoomType(Tree, roomName, mustLayoutArea))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断房间是否为不可布区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool CannotLayoutArea(ThIfcRoom room)
        {
            foreach (var roomName in room.Tags)
            {
                if (JudgeRoomType(Tree, roomName, cannotLayoutArea))
                {
                    return true;
                }
            }
            return false;
        }

        private void ReadRoomConfigTable(string roomConfigUrl)
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var table = dataSet.Tables[roomNameControl];
            if (table != null)
            {
                Tree = RoomConfigTreeService.CreateRoomTree(table);
            }
        }
    }
}
