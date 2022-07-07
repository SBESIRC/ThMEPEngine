using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class RaisePipeConvert
    {
        public static List<CreateBlockInfo> ConvetPipeToBlock(string floorId, List<EquipmentBlockSpace> floorDrainBlcoks) 
        {
            //阳台立管、阳台废水立管、雨水立管、冷凝水立管，在其他地方已经处理了，这里只处理卫生间、厨房立管
            var createBlocks = new List<CreateBlockInfo>();
            string blockName = ThWSSCommon.Layout_PositionRiserBlockName;
            if (SetServicesModel.Instance.drawingScale == EnumDrawingScale.DrawingScale1_150)
                blockName = ThWSSCommon.Layout_PositionRiser150BlockName;
            foreach (var item in floorDrainBlcoks) 
            {
                string dn = "";
                string tag = "";
                string layerName = "";
                switch (item.enumEquipmentType) 
                {
                    case EnumEquipmentType.caissonRiser:
                        //沉箱立管
                        dn = SetServicesModel.Instance.caissonRiserPipeDiameter.ToString();
                        tag = "DL";
                        layerName = ThWSSCommon.Layout_WastWaterPipeLayerName;
                        break;
                    case EnumEquipmentType.ventRiser:
                        //通气立管
                        dn = SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString();
                        tag = "TL";
                        layerName = ThWSSCommon.Layout_WastWaterPipeLayerName;
                        break;
                    case EnumEquipmentType.wastewaterRiser:
                        //废水立管
                        dn = SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString();
                        tag = "FL";
                        if (item.enumRoomType.Equals(EnumRoomType.Balcony))
                            tag = "FyL";
                        else if(item.enumRoomType.Equals(EnumRoomType.Kitchen))
                            tag = "FcL";
                        layerName = ThWSSCommon.Layout_WastWaterPipeLayerName;
                        break;
                    case EnumEquipmentType.sewageWaterRiser:
                        //污水立管
                        dn = SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString();
                        tag = "WL";
                        layerName = ThWSSCommon.Layout_WastWaterPipeLayerName;
                        break;
                    case EnumEquipmentType.sewageWasteRiser:
                        //污废合流立管
                        dn = SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString();
                        tag = "PL";
                        layerName = ThWSSCommon.Layout_WastWaterPipeLayerName;
                        break;
                    case EnumEquipmentType.balconyRiser:
                        dn = SetServicesModel.Instance.balconyRiserPipeDiameter.ToString();
                        tag = "Y2L";
                        layerName = ThWSSCommon.Layout_FloorDrainBlockRainLayerName;
                        break;
                    case EnumEquipmentType.roofRainRiser:
                        dn = SetServicesModel.Instance.roofRainRiserPipeDiameter.ToString();
                        tag = "Y1L";
                        layerName = ThWSSCommon.Layout_FloorDrainBlockRainLayerName;
                        break;
                    case EnumEquipmentType.condensateRiser:
                        dn = SetServicesModel.Instance.condensingRiserPipeDiameter.ToString();
                        tag = "NL";
                        layerName = ThWSSCommon.Layout_FloorDrainBlockRainLayerName;
                        break;
                }
                if (string.IsNullOrEmpty(dn))
                    continue;
                var block = new CreateBlockInfo(floorId, blockName, layerName, item.blockCenterPoint, item.enumEquipmentType,item.uid);
                block.spaceId = item.roomSpaceId;
                block.tag = tag;
                block.dymBlockAttr.Add("可见性1", dn);
                createBlocks.Add(block);
            }
            return createBlocks;
        }
    }
}
