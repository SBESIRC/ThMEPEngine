using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public static class ModelClassifyService
    {
        public static List<BlockModel> ClassifyBlock(List<BlockReference> blocks)
        {
            List<BlockReference> cBlocks = new List<BlockReference>(blocks);
            List<BlockModel> models = new List<BlockModel>();
            //分类视频监控系统系统块
            var vmBlocks = ClassifyVMBlock(cBlocks);
            //分类紧急报警系统块
            cBlocks = cBlocks.Except(vmBlocks.Select(x => x.blockModel)).ToList();
            var iaBlocks = ClassifyIABlock(cBlocks);
            //分类出入口控制系统块
            cBlocks = cBlocks.Except(iaBlocks.Select(x => x.blockModel)).ToList();
            var acBlocks = ClassifyACBlock(cBlocks);

            return models;
        }

        /// <summary>
        /// 分类视频监控系统系统块
        /// </summary>
        /// <param name="IABlock"></param>
        /// <returns></returns>
        public static List<VMModel> ClassifyVMBlock(List<BlockReference> IABlock)
        {
            List<VMModel> models = new List<VMModel>();
            foreach (var block in IABlock)
            {
                if (block.Name == ThMEPCommon.GUNCAMERA_BLOCK_NAME) models.Add(new VMGunCamera(block));
                if (block.Name == ThMEPCommon.PANTILTCAMERA_BLOCK_NAME) models.Add(new VMPantiltCamera(block));
                if (block.Name == ThMEPCommon.FACERECOGNITIONCAMERA_BLOCK_NAME) models.Add(new VMFaceCamera(block));
            }

            return models;
        }

        /// <summary>
        /// 分类紧急报警系统块
        /// </summary>
        /// <param name="IABlock"></param>
        /// <returns></returns>
        public static List<IAModel> ClassifyIABlock(List<BlockReference> IABlock)
        {
            List<IAModel> models = new List<IAModel>();
            foreach (var block in IABlock)
            {
                if (block.Name == ThMEPCommon.CONTROLLER_BLOCK_NAME) models.Add(new IAControllerModel(block));
                if (block.Name == ThMEPCommon.INFRAREDWALLDETECTOR_BLOCK_NAME) models.Add(new IAInfraredWallDetectorModel(block));
                if (block.Name == ThMEPCommon.DOUBLEDETECTOR_BLOCK_NAME) models.Add(new IADoubleDetectorModel(block));
                if (block.Name == ThMEPCommon.INFRAREDHOSITINGDETECTOR_BLOCK_NAME) models.Add(new IAInfraredHositingDetectorModel(block));
                if (block.Name == ThMEPCommon.DISABLEDALARM_BLOCK_NAME) models.Add(new IADisabledAlarmButtun(block));
                if (block.Name == ThMEPCommon.SOUNDLIGHTALARM_BLOCK_NAME) models.Add(new IASoundLightAlarm(block));
                if (block.Name == ThMEPCommon.EMERGENCYALARM_BLOCK_NAME) models.Add(new IAEmergencyAlarmButton(block));
            }

            return models;
        }

        /// <summary>
        /// 分类出入口控制系统块
        /// </summary>
        /// <param name="IABlock"></param>
        /// <returns></returns>
        public static List<ACModel> ClassifyACBlock(List<BlockReference> IABlock)
        {
            List<ACModel> models = new List<ACModel>();
            foreach (var block in IABlock)
            {
                if (block.Name == ThMEPCommon.BUTTON_BLOCK_NAME) models.Add(new ACButtun(block));
                if (block.Name == ThMEPCommon.ELECTRICLOCK_BLOCK_NAME) models.Add(new ACElectricLock(block));
                if (block.Name == ThMEPCommon.CARDREADER_BLOCK_NAME) models.Add(new ACCardReader(block));
                if (block.Name == ThMEPCommon.INTERCOM_BLOCK_NAME) models.Add(new ACIntercom(block));
            }

            return models;
        }
    }
}
