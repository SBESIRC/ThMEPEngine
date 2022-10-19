using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ThMEPHVAC.FanConnect.Model
{
    public enum PIPELEVEL
    {
        LEVEL1,//第一级管（干管）
        LEVEL2,//第二级管（支干管）
        LEVEL3,//第三级管（支管）
        LEVEL4 //第四级管（连接最风机最后一段线）
    }
    public enum PIPETYPE
    {
        R,
        C,
        CS,
        CR,
        HS,
        HR,
        CHS,
        CHR,
        CSCR,
        HSHR,
    }

    public class ThFanPipeModel
    {
        public int WayCount { set; get; }//是否是四通连接点
        public bool IsFlag { set; get; }//标识位，表示是否反向ExPline顺序
        public bool IsValve { set; get; }//是否添加了阀门
        public bool IsConnect { set; get; }//与父结点是否连接
        public bool IsContacted { set; get; }//是否绘制了连接点
        public double PipeWidth { set; get; }//水管宽度
        public PIPELEVEL PipeLevel { set; get; }//主体级别
        public Vector3d CroVector { set; get; }//与父结点叉乘方向
        public Line PLine { set; get; }//主体
        public List<Line> ExPline { set; get; }//扩展线
        public List<Point3d> ExPoint { set; get; }//扩展线的连接点
        public ThFanPipeModel BrotherItem { set; get; }//共结点

        public ThFanCUModel ConnectFan { get; set; }
        public ThFanPipeModel(Line line, PIPELEVEL level = PIPELEVEL.LEVEL1, double width = 200)
        {
            WayCount = 2;
            IsValve = false;
            IsContacted = false;
            IsFlag = false;
            IsConnect = false;
            PipeWidth = width;
            PipeLevel = level;
            PLine = line;
            ExPoint = new List<Point3d>();
            CroVector = new Vector3d(0.0, 0.0, 1.0);
        }

    }
    public class ThFanPointModel
    {
        public bool IsFlag { set; get; }//标识位，表示是否反向ExPline顺序
        public bool IsCondMarked { set; get; }//是否已标记冷凝水管
        public bool IsCoolHotMarked { set; get; }//是否已标记冷热水
        public bool IsCrossPoint { set; get; }//是否是交叉点
        public double CoolCapa { set; get; }//制冷量
        public double CoolFlow { set; get; }//制冷流量值
        public double HotFlow { set; get; }//制热流量值
        public Point3d CntPoint { set; get; }//连接点
        public double MarkSpace { set; get; }//标记间隔
        public PIPELEVEL Level { set; get; }//主体级别
        public ThFanPointModel()
        {
            IsFlag = false;
            IsCondMarked = false;
            IsCoolHotMarked = false;
            IsCrossPoint = false;
            CoolCapa = 0.0;
            CoolFlow = 0.0;
            HotFlow = 0.0;
            MarkSpace = 200.0;
            Level = PIPELEVEL.LEVEL1;
        }
    }


    public class ThFanPointModelNew
    {
        public int Level { get; set; }//node级别
        public Point3d BasePt { set; get; }//点位，所属属性均为 this=》parent的line
        public double CoolCapa { set; get; }//制冷量
        public double CoolFlow { set; get; }//制冷流量值
        public double HotFlow { set; get; }//制热流量值
        public int CoolCapaDim { get; set; }//冷凝管径
        public Tuple<double, double> ACPipeDim { get; set; }//冷媒管径
        public int CoolDim { get; set; }//冷管径
        public int HotDim { get; set; }//热管径
        public bool IsLevelChangeMark { get; set; }//层级管径标注位
        public bool IsCoolHotChangeMark { get; set; }//四管制，冷or热变化管径标注位
        public bool IsCoolHotMaxChangeMark { get; set; }//两管制，冷热取大变化管径标注位
        public bool IsCapaChangeMarked { get; set; }//冷凝变化管径标注位
        public bool IsACChangeMarked { get; set; }//冷媒变化管径标注位
        public int IsFlag { get; set; }//标识位，表示是否反向ExPline顺序 : -1没匹配过，0/1 ：匹配ThFanPipeModel
        public bool IsFirstPart { get; set; }//是否是第一段
        public ThFanPointModelNew(Point3d pt)
        {
            CoolCapa = 0.0;
            CoolFlow = 0.0;
            HotFlow = 0.0;
            Level = 0;
            IsLevelChangeMark = false;
            IsCoolHotChangeMark = false;
            IsCoolHotMaxChangeMark = false;
            IsCapaChangeMarked = false;
            IsACChangeMarked = false;
            IsFlag = -1;
            IsFirstPart = false;

            BasePt = pt;
        }
    }

}
