using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;



namespace ThMEPLighting.EmgLightConnect.Service
{
    public class GetBlockService
    {
        Dictionary<BlockReference, BlockReference> emgLight = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacR = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacRL = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitE = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitS = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitECeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> exitSCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> enter = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> enterCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacCeiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacR2Ceiling = new Dictionary<BlockReference, BlockReference>();
        Dictionary<BlockReference, BlockReference> evacLR2Ceiling = new Dictionary<BlockReference, BlockReference>();

        public void getBlocksData(Polyline bufferFrame, ThMEPOriginTransformer transformer)
        {
            emgLight = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer);
            evacR = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacRBlockName, transformer);
            evacRL = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLRBlockName, transformer);

            exitE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitEBlockName, transformer);
            exitS = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSBlockName, transformer);
            exitECeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitECeilingBlockName, transformer);
            exitSCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSCeilingBlockName, transformer);

            enter = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterBlockName, transformer);
            enterCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EnterCeilingBlockName, transformer);

            evacCeiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacCeilingBlockName, transformer);
            evacR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacR2LineCeilingBlockName, transformer);
            evacLR2Ceiling = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EvacLR2LineCeilingBlockName, transformer);

            // var ALE = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EPowerLayerName, ThMEPLightingCommon.ALEBlockName, transformer);

        }

        public void getBlockList(Dictionary<EmgConnectCommon.BlockType, List<BlockReference>>  blockList)
        {
            blockList.Add(EmgConnectCommon.BlockType.emgLight, emgLight.Select(x => x.Value).ToList());
            blockList.Add(EmgConnectCommon.BlockType.evac, evacR.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.evac].AddRange(evacRL.Select(x => x.Value).ToList());

            blockList.Add(EmgConnectCommon.BlockType.exit, exitE.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.exit].AddRange(exitS.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.exit].AddRange(exitECeiling.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.exit].AddRange(exitSCeiling.Select(x => x.Value).ToList());

            blockList.Add(EmgConnectCommon.BlockType.enter, enter.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.enter].AddRange(enterCeiling.Select(x => x.Value).ToList());

            blockList.Add(EmgConnectCommon.BlockType.evacCeiling, evacCeiling.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.evacCeiling].AddRange(evacR2Ceiling.Select(x => x.Value).ToList());
            blockList[EmgConnectCommon.BlockType.evacCeiling].AddRange(evacLR2Ceiling.Select(x => x.Value).ToList());

            //blockList.Add(EmgConnectCommon.BlockType.ale, ALE.Select(x => x.Value).ToList());

        }


    }
}
