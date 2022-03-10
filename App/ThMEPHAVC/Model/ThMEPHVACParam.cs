using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public enum EndCompType
    {
        None,
        VerticalPipe,
        RainProofShutter,
        DownFlip45,
    }
    public enum GenerationStyle
    {
        Auto,
        GenerationByPort,
        GenerationWithPortVolume
    }
    public enum ElevationAlignStyle
    {
        Top,
        Bottom,
        Center
    }
    public enum TeeType
    {
        BRANCH_COLLINEAR_WITH_OTTER,
        BRANCH_VERTICAL_WITH_OTTER
    };
    public class PortInfo
    {
        public ObjectId id;
        public string effectiveName;
        public bool haveLeft = true;
        public bool haveRight = true;
        public string ductSize; // 用于绘制endline管段时的判别
        public Point3d position;// 确定墙点的位置后再分配风口的位置
        public double portAirVolume;
    };
    public class SegInfo
    {
        public Line l;
        public Vector3d horizontalVec;
        public string ductSize; //只用于主管段绘制, endline由ports控制大小
        public double srcShrink;
        public double dstShrink;
        public double airVolume;//进入管段时的风量，主管段为整个管段的风量，endline为入口处的风量
        public string elevation;
        public Line GetShrinkedLine()
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(l);
            var sp = l.StartPoint + dirVec * srcShrink;
            var ep = l.EndPoint - dirVec * dstShrink;
            return new Line(sp, ep);
        }
        public SegInfo() { }
        public SegInfo (SegInfo seg)
        {
            l = new Line(seg.l.StartPoint, seg.l.EndPoint);
            ductSize = seg.ductSize;
            srcShrink = seg.srcShrink;
            dstShrink = seg.dstShrink;
            airVolume = seg.airVolume;
            elevation = seg.elevation;
        }
    }
    public class EndlineSegInfo
    {
        public int portNum;
        public SegInfo seg;
        //portNum==portsPosition.Count()先根据生成方式确定风口个数，确定变径等信息后再确定风口位置
        public List<PortInfo> portsInfo;
        public Point3d dirAlignPoint = Point3d.Origin;// 为原点时不做Dimension插入
    };    
    public class EndlineInfo
    {
        // 截至到每一个三四通的endline
        public double totalAirVolume;//endline出端口时的风量，用于计算主管段风量
        public Point3d verAlignPoint = Point3d.Origin;// 垂直对其只对最后最后一个管段的最后一个风口
        public Dictionary<int, EndlineSegInfo> endlines;
    };
    public class PortParam
    {
        public Point3d srtPoint;
        public double portInterval; // 0->自动间距
        public ThMEPHVACParam param;
        public bool verticalPipeEnable;
        public EndCompType endCompType;
        public GenerationStyle genStyle;
        public DBObjectCollection centerLines;
        public Vector3d srtDisVec = Point3d.Origin.GetAsVector(); // 有上下翻时起始点需要偏移的距离
    }
    public class FanParam
    {
        public bool roomEnable;
        public bool notRoomEnable;
        public bool isRoomReCommandSize;// 用于UI上切换风机显示管径推荐值还是自定义值
        public bool isNotRoomReCommandSize;// 用于UI上切换风机显示管径推荐值还是自定义值
        public int portNum;
        public string scale;
        public string portSize;
        public string portName;
        public string scenario;
        public string portRange;
        public string bypassSize;
        public string bypassPattern; // 在退出UI界面时填充
        public string roomDuctSize;
        public string notRoomDuctSize;
        public string roomElevation;
        public string notRoomElevation;
        public double airSpeed;
        public double airVolume;
        public double airHighVolume;
        public double portInterval; // 0->自动间距
        public Line lastNotRoomLine;
        public DBObjectCollection centerLines; // 在退出UI界面时搜索
        public DBObjectCollection bypassLines;
        public ElevationAlignStyle roomElevationStyle;
        public ElevationAlignStyle notRoomElevationStyle;
    }
    public class SpecialGraphInfo
    {
        //图形的中心点为lines[0].startpoint
        public List<Line> lines; //lines[0]为in_line 其余为out_lines
        public List<double> everyPortWidth;
        public SpecialGraphInfo(List<Line> lines, List<double> everyPortWidth)
        {
            this.lines = lines;
            this.everyPortWidth = everyPortWidth;
        }
    };
    public class ThMEPHVACParam
    {
        public int portNum;
        public double airSpeed;
        public double airVolume;
        public double highAirVolume;
        public double elevation;
        public double mainHeight;
        public double portBottomEle;// 风口底标高
        public string scale;
        public string scenario;
        public string portSize;
        public string portName;
        public string portRange;
        public string inDuctSize;
    }
    public class TransInfo
    {
        public bool flip = false;//三通和四通有时必须绕y轴翻转
        public double rotateAngle;
        public Point3d centerPoint;
    }
    public class ReducingInfo
    {
        public Line l;
        public string bigSize;
        public string smallSize;
    }
    public class ElbowInfo
    {
        public double openAngle;
        public string ductWidth;
        public TransInfo trans;
    }
    public class TeeInfo
    {
        public string mainWidth;
        public string branch;
        public string other;
        public TransInfo trans;
    }
    public class CrossInfo
    {
        public string iWidth;
        public string innerWidth;
        public string coWidth;
        public string outterWidth;
        public TransInfo trans;
    }
    public class SidePortInfo
    {
        public bool isLeft;
        public List<Handle> portHandles;
        public SidePortInfo(bool isLeft, List<Handle> portHandles)
        {
            this.isLeft = isLeft;
            this.portHandles = portHandles;
        }
    }
    public class TextAlignLine
    {
        public Line l;
        public bool isRoom;
        public string ductSize;
    }
}
