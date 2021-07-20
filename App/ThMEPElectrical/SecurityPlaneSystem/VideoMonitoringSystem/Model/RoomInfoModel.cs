using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model
{
    public class RoomInfoModel
    {
        /// <summary>
        /// 房间a
        /// </summary>
        public List<string> roomA = new List<string>();

        /// <summary>
        /// 房间b
        /// </summary>
        public List<string> roomB = new List<string>();

        /// <summary>
        /// 楼层限制
        /// </summary>
        public string floorName { get; set; }

        /// <summary>
        /// 房间a采取的措施
        /// </summary>
        public LayoutType roomAHandle { get; set; }

        /// <summary>
        /// 房间b采取的措施
        /// </summary>
        public LayoutType roomBHandle { get; set; }

        /// <summary>
        /// 判断房间应该的连接类型
        /// </summary>
        public ConnectType connectType { get; set; }
    }

    public enum LayoutType
    {
        /// <summary>
        /// 不需要布置
        /// </summary>
        Nothing,

        /// <summary>
        /// 沿线布置枪式摄像机
        /// </summary>
        AlongLineGunCamera,

        /// <summary>
        /// 沿线布置云台摄像机
        /// </summary>
        AlongLinePanTiltCamera,

        /// <summary>
        /// 沿线布置半球摄像机
        /// </summary>
        AlongLineDomeCamera,

        /// <summary>
        /// 沿线布置枪式摄像机(带室内保护罩)
        /// </summary>
        AlongLineGunCameraWithShield,

        /// <summary>
        /// 入口覆盖枪式摄像机
        /// </summary>
        EntranceGunCamera,

        /// <summary>
        /// 入口覆盖枪式摄像机（翻转）
        /// </summary>
        EntranceGunCameraFlip,

        /// <summary>
        /// 入口覆盖半球摄像机
        /// </summary>
        EntranceDomeCamera,

        /// <summary>
        /// 入口覆盖半球摄像机（翻转）
        /// </summary>
        EntranceDomeCameraFlip,

        /// <summary>
        /// 入口覆盖枪式摄像机(带室内保护罩)
        /// </summary>
        EntranceGunCameraWithShield,

        /// <summary>
        /// 入口覆盖枪式摄像机(带室内保护罩)（翻转）
        /// </summary>
        EntranceGunCameraWithShieldFlip,

        /// <summary>
        /// 入口覆盖人脸识别摄像机
        /// </summary>
        EntranceFaceRecognitionCamera,

        /// <summary>
        /// 入口覆盖人脸识别摄像机（翻转）
        /// </summary>
        EntranceFaceRecognitionCameraFlip,
    }

    public enum ConnectType
    {
        /// <summary>
        /// 正常房间连接
        /// </summary>
        Normal,

        /// <summary>
        /// 无
        /// </summary>
        NoCennect,

        /// <summary>
        /// All
        /// </summary>
        AllConnect,
    }
}
