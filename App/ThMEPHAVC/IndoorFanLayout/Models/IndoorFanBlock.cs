using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System.Linq;
using System.Text.RegularExpressions;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout.Models
{
    class IndoorFanBlock
    {
        public ObjectId FanBlockId { get; }
        public string BlockName { get; }
        public string FanName { get; set; }
        public double CoolLoad { get; set; }
        public double HotLoad { get; set; }
        public double FanLength { get; set; }
        public Point3d BlockPosion { get; }
        public EnumFanType FanType { get; }
        public BlockReference FanBlock { get; }
        public IndoorFanBlock(ObjectId blockId,string blockName, BlockReference block)
        {
            if (blockName == IndoorFanBlockServices.CoilFanFourBlackName)
            {
                FanType = EnumFanType.FanCoilUnitFourControls;
            }
            else if (blockName == IndoorFanBlockServices.CoilFanTwoBlackName)
            {
                FanType = EnumFanType.FanCoilUnitTwoControls;
            }
            else if (blockName == IndoorFanBlockServices.VRFFanBlackName)
            {
                FanType = EnumFanType.VRFConditioninConduit;
            }
            else if (blockName == IndoorFanBlockServices.VRFFanFourSideBlackName)
            {
                FanType = EnumFanType.VRFConditioninFourSides;
            }
            else if (blockName == IndoorFanBlockServices.AirConditionFanBlackName)
            {
                FanType = EnumFanType.IntegratedAirConditionin;
            }
            BlockName = blockName;
            FanBlock = block;
            var point = block.Position;
            BlockPosion = new Point3d(point.X, point.Y, 0);
            this.FanBlockId = blockId;
        }
    }

    class IndoorFanVentBlock
    {
        public ObjectId VentBlockId { get; }
        public string BlockName { get; }
        public string VentName { get; set; }
        public Point3d BlockPosion { get; }
        public BlockReference VentBlock { get; }
        public IndoorFanVentBlock(ObjectId blockId,string blockName, BlockReference block)
        {
            BlockName = blockName;
            VentBlock = block;
            var point = block.Position;
            BlockPosion = new Point3d(point.X, point.Y, 0);
            this.VentBlockId = blockId;
        }
    }
}
