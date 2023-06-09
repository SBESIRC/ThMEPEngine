﻿using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class ConnectPipeService
    {
        public List<Entity> ConnectPipe(Polyline polyline, List<BlockReference> connectBlock, List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns,
              List<Line> trunking, List<Polyline> holes, ThEStoreys floor)
        {
            List<Entity> res = new List<Entity>();
            IntrucsionAlarmConnectService connectService = new IntrucsionAlarmConnectService();
            var iaPipes = connectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            var ConnectLines = InsertConnectPipeService.InsertConnectPipe(iaPipes, ThMEPCommon.IA_PIPE_LAYER_NAME, ThMEPCommon.IA_PIPE_LINETYPE);

            AccessControlConnectService accessControlConnectService = new AccessControlConnectService();
            var acPipes = accessControlConnectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            ConnectLines.AddRange(InsertConnectPipeService.InsertConnectPipe(acPipes, ThMEPCommon.AC_PIPE_LAYER_NAME, ThMEPCommon.AC_PIPE_LINETYPE));
            res.AddRange(ConnectLines);
            if (!ThElectricalUIService.Instance.Parameter.withinInGroup)
            {
                var blockModels = ModelClassifyService.ClassifyBlock(connectBlock);
                var connectBlocks = GetConnectBlocks(blockModels);
                var otherBlocks = blockModels.Except(connectBlocks).ToList();

                trunking = trunking.Where(o => o.Length > 400).ToList();
                SystemConnectPipeService systemConnectPipeService = new SystemConnectPipeService();
                var resPolyDic = systemConnectPipeService.Connect(polyline, columns, blockModels, trunking, ConnectLines, connectBlocks, holes);
                resPolyDic = systemConnectPipeService.ChooseTrunking(trunking, resPolyDic);
                var resPolys = systemConnectPipeService.AdjustEndRoute(columns, blockModels.Select(o => o.Boundary).ToList(), resPolyDic);
                //断开有交叉的连线
                var resLines = systemConnectPipeService.DisconnectRoute(resPolys, blockModels.Select(o => o.Boundary).ToList());

                res.AddRange(resLines);
            }    
            return res;
        }

        /// <summary>
        /// 获取需要连接到线槽的块
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private List<BlockModel> GetConnectBlocks(List<BlockModel> blocks)
        {
            List<BlockModel> resBlocks = blocks.Where(x => x is VMGunCamera ||
                x is VMFaceCamera ||
                x is VMPantiltCamera ||
                x is ACCardReader ||
                x is ACIntercom ||
                x is IAControllerModel).ToList();
            return resBlocks;
        }
    }
}
