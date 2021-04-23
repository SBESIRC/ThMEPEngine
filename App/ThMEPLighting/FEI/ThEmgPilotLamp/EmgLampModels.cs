using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    public class LightLayout
    {
        /// <summary>
        /// 点在车道线上的投影点
        /// </summary>
        public Point3d linePoint { get; }
        /// <summary>
        /// 布置处的点，墙或柱的外侧
        /// </summary>
        public Point3d pointInOutSide { get; }
        /// <summary>
        /// 布置点所在线的方向
        /// </summary>
        public Vector3d directionSide { get; }
        /// <summary>
        /// 所属疏散路径
        /// </summary>
        public Line hostLine { get; }
        public double disToHostLineSp { get; }
        /// <summary>
        /// 最近的疏散点，不一定是终点
        /// </summary>
        public GraphNode nearNode { get; }
        /// <summary>
        /// 所属墙或柱的轮廓线
        /// </summary>
        public Polyline wallOrColumns { get; set; }
        /// <summary>
        /// 是否是吊装
        /// </summary>
        public bool isHoisting { get; }
        /// <summary>
        /// 出口类型
        /// </summary>
        public int endType { get; set; }
        /// <summary>
        /// 在线的那一侧
        /// </summary>
        public Vector3d sideLineDir { get; }
        /// <summary>
        /// 疏散的方向(这个是和线方向平行，排布方向要根据柱墙的实际方向进行修正)
        /// </summary>
        public Vector3d direction { get; }
        /// <summary>
        /// 是否是双面指示灯(只有吊装的才有是否是双面的)
        /// </summary>
        public bool isTwoSide { get; set; }
        /// <summary>
        /// 是否是双向指示灯(目前给吊装指示灯)
        /// </summary>
        public bool isTwoExitDir { get; set; }
        /// <summary>
        /// 壁装时该节点是否需要检查删除
        /// </summary>
        public bool isCheckDelete { get; set; }
        /// <summary>
        /// 疏散指示灯放置信息
        /// </summary>
        /// <param name="point">所在线上的点</param>
        /// <param name="sidePoint">生成点</param>
        /// <param name="line">所属线</param>
        /// <param name="lineSide">线上点到生产点的反向，用于确定在线的那一侧，因为线的方向不定，可以根据这个来确定</param>
        /// <param name="exitDir">该点前往疏散口的方向</param>
        /// <param name="sideCreateDir">创建点实际的指向</param>
        /// <param name="node">所属节点</param>
        /// <param name="hoisting">是否是吊装</param>
        public LightLayout(Point3d point,Point3d sidePoint, Line line, Vector3d lineSide,Vector3d exitDir,Vector3d sideCreateDir,GraphNode node,bool hoisting=false)
        {
            this.linePoint = point;
            this.pointInOutSide = sidePoint;
            this.hostLine = line;
            if (null != hostLine) 
                this.disToHostLineSp = hostLine.StartPoint.DistanceTo(point);
            this.directionSide = sideCreateDir;
            this.sideLineDir = lineSide;
            this.nearNode = node;
            this.isHoisting = hoisting;
            this.direction = exitDir;
        }
    }
    /// <summary>
    /// 节点的相关信息
    /// </summary>
    public class NodeDirection
    {
        /// <summary>
        /// 在线上的点
        /// </summary>
        public Point3d nodePointInLine { get; }
        /// <summary>
        /// 具体节点
        /// </summary>
        public GraphNode graphNode { get; }
        /// <summary>
        /// 节点到出口处的距离
        /// </summary>
        public double distanceToExit { get; }
        /// <summary>
        /// 该节点的疏散进入方向
        /// 进入------->|
        ///             |
        ///             |<---------进入
        ///             |
        ///             |-->----出
        ///             |
        /// </summary>
        public List<Vector3d> inDirection { get; }
        /// <summary>
        /// 该点的指向疏散口的方向，沿着线的方向
        /// </summary>
        public Vector3d outDirection { get; }
        public NodeDirection(Point3d linePoint, GraphRoute route,double disToExit, Vector3d exitDir,GraphNode node)
        {
            this.nodePointInLine = linePoint;
            this.distanceToExit = disToExit;
            this.graphNode =node;
            this.outDirection = exitDir;
            this.inDirection = new List<Vector3d>();
        }
    }
    public class LineColumn
    {
        public Polyline column { get; }
        public Point3d pointInLine { get; }
        public Point3d centerPoint { get; }
        public GraphRoute nearRoute { get; }
        public Vector3d directionToExit { get; }
        public bool isTwoDirection { get; set; }
        public LineColumn(Polyline polyline, GraphRoute nodeRoute, Point3d linePoint, bool isTwoWay = false)
        {
            column = polyline;
            pointInLine = linePoint;
            nearRoute = nodeRoute;
            directionToExit = (nodeRoute.node.nodePoint - linePoint).GetNormal();
            isTwoDirection = isTwoWay;
            var allPts = column.GetPoints().ToList();
            var sumX = allPts.Sum(c => c.X);
            var sumY = allPts.Sum(c => c.Y);
            centerPoint = new Point3d(sumX / allPts.Count, sumY / allPts.Count, 0);
        }
    }
    /// <summary>
    /// 节点处的线的方向和实际疏散方向
    /// </summary>
    public class LineGraphNode
    {
        public Line line { get; }
        public Vector3d lineDir { get; }
        public Vector3d layoutLineSide { get; set; }
        public List<LineColumn> lineColumus { get; }
        public List<NodeDirection> nodeDirections { get; }
        public LineGraphNode(Line li)
        {
            this.line = li;
            if (null != line)
                lineDir = (line.EndPoint - line.StartPoint).GetNormal();
            this.nodeDirections = new List<NodeDirection>();
            this.lineColumus = new List<LineColumn>();
        }
    }
    public class IndicatorLight
    {
        /// <summary>
        /// 出口处连接主要疏散路径线(Ⅰ类线)
        /// </summary>
        public List<Curve> exitLines { get; set; }
        /// <summary>
        /// 车道中心线（Ⅱ类线）
        /// </summary>
        public List<Curve> mainLines { get; set; }
        /// <summary>
        /// 辅助疏散路径（Ⅲ类线,壁装）
        /// </summary>
        public List<Curve> assistLines { get; set; }
        /// <summary>
        /// 辅助疏散路径（Ⅲ类线,吊装）
        /// </summary>
        public List<Curve> assistHostLines { get; set; }
        /// <summary>
        /// 所有构造的节点
        /// </summary>
        public List<GraphNode> allNodes { get; set; }
        /// <summary>
        /// 所有节点到出口的路径信息
        /// </summary>
        public List<GraphRoute> allNodeRoutes { get; set; }
        /// <summary>
        /// 所有的可用疏散路径
        /// </summary>
        public List<Curve> allLines { get; set; }
        /// <summary>
        /// 指示灯最大间距（壁装）
        /// </summary>
        public double spaceWallMount { get; set; }
        /// <summary>
        /// 指示灯吊装最大间距
        /// </summary>
        public double spaceHoisting { get; set; }
        public IndicatorLight()
        {
            mainLines = new List<Curve>();
            exitLines = new List<Curve>();
            assistLines = new List<Curve>();
            allLines = new List<Curve>();
            allNodes = new List<GraphNode>();
            allNodeRoutes = new List<GraphRoute>();
            assistHostLines = new List<Curve>();
        }
    }
    /// <summary>
    /// 图节点
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// 点
        /// </summary>
        public Point3d nodePoint { get; set; }
        /// <summary>
        /// 是否是结束点
        /// </summary>
        public bool isExit { get; set; }
        /// <summary>
        /// 节点类型（扩展用）
        /// </summary>
        public int nodeType { get; set; }
    }
    /// <summary>
    /// 图中点到出口点的行走路线
    /// </summary>
    public class GraphRoute 
    {
        public GraphNode node { get; set; }
        public double weightToNextNode { get; set; }
        public GraphRoute nextRoute { get; set; }
    }
}
