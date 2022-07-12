using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.IO.JSON;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels.ThSmokeProofMappingModel;

namespace ThMEPHVAC.SmokeProofSystem.Model
{
    public class SmkBlockModel
    {
        public SmkBlockModel(BlockReference block)
        {
            var flexData = FlexDataStoreExtensions.FlexDataStore(block.Id);
            var mainVal = flexData.GetValue(FlexDataKeyType.MianVm.ToString());
            var model = JsonHelper.DeserializeJsonToObject<SmokeCalculateMappingModel>(mainVal);
            Position = block.Position;
            AirVolume = 0; Convert.ToDouble(flexData.GetValue(FlexDataKeyType.Volume.ToString()));
            if (model.AirSupplyTitle == "自然送风")
            {
                BlockType = BlockType.Natural;
            }
            else
            {
                AirVolume = Convert.ToDouble(flexData.GetValue(FlexDataKeyType.Volume.ToString()));
                BlockType = BlockType.Compression;
            }

            if (model.ScenarioTitle == "楼梯间（前室不送风）" || model.ScenarioTitle == "楼梯间（前室送风）")
            {
                RoomType = RoomType.StairRoom;
            }
            else if (model.ScenarioTitle == "消防电梯前室" || model.ScenarioTitle == "独立或合用前室（楼梯间自然）" || 
                model.ScenarioTitle == "独立或合用前室（楼梯间送风）" || model.ScenarioTitle == "避难走道前室")
            {
                RoomType = RoomType.FrontRoom;
            }
        }

        /// <summary>
        /// 基点
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 风量
        /// </summary>
        public double AirVolume { get; set; }

        /// <summary>
        /// 块类型
        /// </summary>
        public BlockType BlockType { get; set; }

        /// <summary>
        /// 块表示的房间类型
        /// </summary>
        public RoomType RoomType { get; set; }
    }

    public enum BlockType
    {
        /// <summary>
        /// 自然
        /// </summary>
        Natural,

        /// <summary>
        /// 加压
        /// </summary>
        Compression,
    }
}
