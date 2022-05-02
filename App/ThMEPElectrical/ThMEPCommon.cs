using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical
{
    public static class ThMEPCommon
    {
        //广播
        public static readonly string ParkingLineLayer = "AD-SIGN";     //车位线图层
        public static readonly string BroadcastLayerName = "E-FAS-DEVC";     //消防应急广播图层
        public static readonly string BroadcastBlockName = "E-BFAS410-4";     //消防应急广播图块名
        public static readonly string BlindAreaLayer = "E-MQ-XFGB";     //广播盲区图层
        public static readonly string FrameLayer = "AI-房间框线";     //房间框线图层

        //连管
        public static readonly string ConnectPipeLayerName = "E-BRST-WIRE";     //广播连管图层
        public static readonly string ConnectPipeLineType = "ByLayer";//"TH_B";     //广播连管线型

        // 烟感温感
        public const string SENSORLAYERNMAE = "E-FAS-DEVC";
        public const string SMOKE_SENSOR_BLOCK_NAME = "E-BFAS110";
        public const string TEMPERATURE_SENSOR_BLOCK_NAME = "E-BFAS120";
        public const string UCS_COMPASS_BLOCK_NAME = "AI-UCS";
        public const string UCS_COMPASS_LAYER_NAME = "AI-UCS";
        public const string PROTECTAREA_LAYER_NAME = "E-FD-PR";
        public const string BLINDAREA_HATCH_LAYER_NAME = "E-FD-DA";

        //安放平面
        public const string FRAME_LAYER_NAME = "AD-FLOOR-AREA";                         //框线图层
        //----视频监控系统
        public const string VM_LAYER_NAME = "E-VSCS-DEVC";                              //视频监控系统放置图层
        public const string GUNCAMERA_BLOCK_NAME = "E-BVSCS110";                        //枪式摄像机图块
        public const string PANTILTCAMERA_BLOCK_NAME = "E-BVSCS111";                    //云台摄像机图块
        public const string DOMECAMERA_SHILED_BLOCK_NAME = "E-BVSCS210";                //半球摄像机保护罩图块
        public const string GUNCAMERA_SHIELD_BLOCK_NAME = "E-VSCS-DEVC";                //枪式摄像机保护罩图块
        public const string FACERECOGNITIONCAMERA_BLOCK_NAME = "E-BVSCS110-Bio";        //人脸识别摄像机图块
        public static List<string> VM_BLOCK_NAMES = new List<string>()
        {
            GUNCAMERA_BLOCK_NAME,
            PANTILTCAMERA_BLOCK_NAME,
            DOMECAMERA_SHILED_BLOCK_NAME,
            GUNCAMERA_SHIELD_BLOCK_NAME,
            FACERECOGNITIONCAMERA_BLOCK_NAME
        };
        //----视频监控系统连管
        public static readonly string VM_PIPE_LAYER_NAME = "E-VSCS-WIRE";               //视频监控系统连管图层
        public static readonly string VM_PIPE_LINETYPE = "ByLayer";                     //视频监控系统连管线型
        //----出入口控制系统
        public const string AC_LAYER_NAME = "E-ACS-DEVC";                               //出入口控制系统放置图层
        public const string BUTTON_BLOCK_NAME = "E-BACS51";                             //电锁按钮图块
        public const string ELECTRICLOCK_BLOCK_NAME = "E-BACS21";                       //电锁图块
        public const string INTERCOM_BLOCK_NAME = "E-BACS01";                           //出入口对讲门口机图块
        public const string CARDREADER_BLOCK_NAME = "E-BACS41";                         //读卡器图块
        public static List<string> AC_BLOCK_NAMES = new List<string>()
        {
            BUTTON_BLOCK_NAME,
            ELECTRICLOCK_BLOCK_NAME,
            INTERCOM_BLOCK_NAME,
            CARDREADER_BLOCK_NAME,
        };
        //----出入口控制系统连管
        public static readonly string AC_PIPE_LAYER_NAME = "E-ACS-WIRE";               //出入口控制系统连管图层
        public static readonly string AC_PIPE_LINETYPE = "ByLayer";                        //出入口控制系统连管线型
        //----入侵报警系统                                                              
        public const string IA_LAYER_NAME = "E-IAS-DEVC";                               //入侵报警系统放置图层
        public const string CONTROLLER_BLOCK_NAME = "E-BIAS010";                        //入侵报警控制器
        public const string INFRAREDWALLDETECTOR_BLOCK_NAME = "E-BIAS110";              //被动红外幕帘式入侵探测器
        public const string DOUBLEDETECTOR_BLOCK_NAME = "E-BIAS120";                    //被动红外微波双鉴移动探测器(空间)
        public const string INFRAREDHOSITINGDETECTOR_BLOCK_NAME = "E-BIAS111";          //被动红外幕帘式入侵探测器
        public const string DISABLEDALARM_BLOCK_NAME = "E-BIAS200";                     //残卫报警按钮
        public const string SOUNDLIGHTALARM_BLOCK_NAME = "E-BIAS020";                   //残卫声光报警器
        public const string EMERGENCYALARM_BLOCK_NAME = "E-BIAS201";                    //紧急报警按钮
        public static List<string> IA_BLOCK_NAMES = new List<string>()
        {
            CONTROLLER_BLOCK_NAME,
            INFRAREDWALLDETECTOR_BLOCK_NAME,
            DOUBLEDETECTOR_BLOCK_NAME,
            INFRAREDHOSITINGDETECTOR_BLOCK_NAME,
            DISABLEDALARM_BLOCK_NAME,
            SOUNDLIGHTALARM_BLOCK_NAME,
            EMERGENCYALARM_BLOCK_NAME
        };
        //----入侵报警系统 连管
        public static readonly string IA_PIPE_LAYER_NAME = "E-IAS-WIRE";               //入侵报警系统连管图层
        public static readonly string IA_PIPE_LINETYPE = "ByLayer";                        //入侵报警系统连管线型
        //----电子巡更系统
        public const string GT_LAYER_NAME = "E-GTS-DEVC";                               //入侵报警系统放置图层
        public const string TIMERECORDER_BLOCK_NAME = "E-BGTS10";                        //入侵报警控制器
        public static List<string> GT_BLOCK_NAMES = new List<string>()
        {
            TIMERECORDER_BLOCK_NAME
        };
        //----电子巡更系统 连管
        public static readonly string GT_PIPE_LAYER_NAME = "E-GTS-WIRE";               //入侵报警系统连管图层
        public static readonly string GT_PIPE_LINETYPE = "ByLayer";                        //入侵报警系统连管线型
        // 图层
        public const string LANELINE_LAYER_NAME = "E-LANE-CENTER";

        // 常量
        public static readonly Scale3d BlockScale = new Scale3d(100, 100, 100);
        public static readonly Point3d NullPoint3d = new Point3d(double.NaN, double.NaN, 0);
        public static readonly double spacingValue = 4050;

        public static readonly double ShrinkDistance = -500; // 内缩距离
        public static readonly double ShrinkSmallDistance = -3; // 误差内缩距离
        public static readonly double GridPolyExtendLength = 500; // 轴网两端延伸距离

        public static readonly double ExtendBeamLength = 25; // 梁的延伸长度
        public static readonly double WallProfileShrinkDistance = -100; // 用户选择墙线内缩距离
        public static readonly double PolyClosedDistance = 1000; // 多段线视觉认为是闭合多段线的误差距离
        public static readonly double EntityExtendDistance = 1000; // 选择图元延长的距离
        public static readonly double SecondBeamDivideHeight = 600; // 梁高差高度差值大于等于600划分

        public static readonly double ValidBeamLength = 2000; // 有效的梁长度

        public static readonly double NearestDisTolerance = 2; //最近距离容差值

        public static readonly double ProtectAreaScatterLength = 200; // 保护半径离散长度
        public static readonly double PLbufferLength = 1; // pline buffer 宽度
    }
}

