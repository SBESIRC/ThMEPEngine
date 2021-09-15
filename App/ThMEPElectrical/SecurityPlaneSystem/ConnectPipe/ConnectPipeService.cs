using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class ConnectPipeService
    {
        public void ConnectPipe(Polyline polyline, List<BlockReference> connectBlock, List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns,
              List<Line> trunking, List<Polyline> holes, ThEStoreys floor)
        {
            IntrucsionAlarmConnectService connectService = new IntrucsionAlarmConnectService();
            var iaPipes = connectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            var ConnectLines = InsertConnectPipeService.InsertConnectPipe(iaPipes, ThMEPCommon.IA_PIPE_LAYER_NAME, ThMEPCommon.IA_PIPE_LINETYPE);

            AccessControlConnectService accessControlConnectService = new AccessControlConnectService();
            var acPipes = accessControlConnectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            ConnectLines.AddRange(InsertConnectPipeService.InsertConnectPipe(acPipes, ThMEPCommon.AC_PIPE_LAYER_NAME, ThMEPCommon.AC_PIPE_LINETYPE));

            var blockModels = ModelClassifyService.ClassifyBlock(connectBlock);
            var connectBlocks = GetConnectBlocks(blockModels);
            var otherBlocks = blockModels.Except(connectBlocks).ToList();

            SystemConnectPipeService systemConnectPipeService = new SystemConnectPipeService();
            var resPolyDic = systemConnectPipeService.Conenct(polyline, columns, blockModels, trunking, ConnectLines, connectBlocks, holes);
            var resPolys = systemConnectPipeService.AdjustEndRoute(columns, blockModels.Select(o => o.Boundary).ToList(), resPolyDic);
            //断开有交叉的连线
            var resLines = systemConnectPipeService.DisconnectRoute(resPolys, blockModels.Select(o => o.Boundary).ToList());
            using (AcadDatabase db = AcadDatabase.Active())
            {
                foreach (var polys in resLines)
                {
                    db.ModelSpace.Add(polys);
                }
            }
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
