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
        const string publicRoom = "公共区域";
        const string privateRoom = "私有区域";
        const string outdoorSafeArea = "室外安全区域";
        const string nonFireDetectionArea = "非火灾探测区域";
        const string evacuationInstruction = "疏散指示";
        const string evacuationLighting = "疏散照明";
        const string roomNameControl = "房间名称处理";

        public List<RoomTableTree> Tree { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            ReadRoomConfigTable(ThCADCommon.RoomConfigPath());
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

        public bool JudgeRoomType(List<RoomTableTree> tree, string roomName, List<string> listString)
        {
            foreach (var roomType in listString)
            {
                var roomTableTree = RoomTypeIteration(tree, roomName, roomType);
                if (JudgeRoomName(roomTableTree, roomName, roomType))
                {
                    return true;
                }
            }
            return false;
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
        //public virtual bool MustLayoutArea(ThIfcRoom room)
        //{
        //    var names = new List<string> { 
        //        "楼梯间", 
        //        "前室",
        //    };
        //    foreach (var roomName in room.Tags)
        //    {
        //        if (JudgeRoomType(Tree, roomName, names))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// 判断房间是否为不可布区域
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        //public virtual bool CannotLayoutArea(ThIfcRoom room)
        //{
        //    var names = new List<string> {
        //        "井道",
        //        "存储房间",
        //        "设备机房",
        //        "无障碍套型",
        //        "居住套型"
        //    };
        //    foreach (var roomName in room.Tags)
        //    {
        //        if (JudgeRoomType(Tree, roomName, names))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}


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
