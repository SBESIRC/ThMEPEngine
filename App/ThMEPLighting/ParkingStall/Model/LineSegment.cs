using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public enum LineLengthType
    {
        SHORT_TYPE,
        LONG_TYPE,
    }

    /// <summary>
    /// 
    /// </summary>
    public class LineSegment
    {
        public Line SegmentLine;

        public LineLengthType SegmentLineLengthType;

        /// <summary>
        /// 通过具体的某一条边与周围矩形相连的信息
        /// </summary>
        public List<PolylineNode> IntersectPolyNodes
        {
            get;
            set;
        } = new List<PolylineNode>();

        public LineSegment(Line line, LineLengthType lineLengthType)
        {
            SegmentLine = line;
            SegmentLineLengthType = lineLengthType;
        }
    }

    /// <summary>
    /// 一个车位信息
    /// </summary>
    public class PolylineNode
    {
        public Polyline SrcPoly;
        public List<LineSegment> LineSegments;

        public List<PolylineNode> RelatedPolylineNodes = new List<PolylineNode>();

        public bool IsUse;

        public PolylineNode(Polyline polyline, List<LineSegment> lineSegments)
        {
            SrcPoly = polyline;
            LineSegments = lineSegments;
            IsUse = false;
        }
    }
}
