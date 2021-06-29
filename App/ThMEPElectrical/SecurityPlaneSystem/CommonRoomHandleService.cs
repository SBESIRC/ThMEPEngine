using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Service;

namespace ThMEPElectrical.SecurityPlaneSystem
{
    public static class CommonRoomHandleService
    {
        public static List<string> HandleRoom(string roomName)
        {
            List<string> roomInfo = new List<string>();
            var tableTree = ThElectricalUIService.Instance.Parameter.RoomInfoMappingTree;
            foreach (var treeNode in tableTree)
            {
                roomInfo.AddRange(GetNodeInfo(treeNode, roomName, false));
            }

            return roomInfo;
        }

        /// <summary>
        /// 判断是否包含这个
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
}
