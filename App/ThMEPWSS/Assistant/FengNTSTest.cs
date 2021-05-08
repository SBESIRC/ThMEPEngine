//这个文件仅用于测试
//请勿引用该命名空间下的所有类！！！


namespace qsqbdc
{
    using System;
    using System.Collections.Generic;
    using NetTopologySuite.Geometries;
    using NetTopologySuite;
    using NetTopologySuite.Geometries.Prepared;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using System.Linq;
    using System.Collections;

    public class CADCoreNTSServiceConfig
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly CADCoreNTSServiceConfig _default = new CADCoreNTSServiceConfig() { PrecisionReduce = false };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static CADCoreNTSServiceConfig() { }
        internal CADCoreNTSServiceConfig() { }
        public static CADCoreNTSServiceConfig Default { get { return _default; } }
        //-------------SINGLETON-----------------
        public bool PrecisionReduce { get; set; }

        public double ArcTessellationLength { get; set; } = 1000.0;

        private GeometryFactory geometryFactory;
        private GeometryFactory defaultGeometryFactory;
        public GeometryFactory GeometryFactory
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (geometryFactory == null)
                    {
                        geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(PrecisionModel);
                    }
                    return geometryFactory;

                }
                else
                {
                    if (defaultGeometryFactory == null)
                    {
                        defaultGeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
                    }
                    return defaultGeometryFactory;
                }
            }
        }

        private PreparedGeometryFactory preparedGeometryFactory;
        public PreparedGeometryFactory PreparedGeometryFactory
        {
            get
            {
                if (preparedGeometryFactory == null)
                {
                    preparedGeometryFactory = new PreparedGeometryFactory();
                }
                return preparedGeometryFactory;
            }
        }

        private Lazy<PrecisionModel> precisionModel;
        public PrecisionModel PrecisionModel
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (precisionModel == null)
                    {
                        precisionModel = PrecisionModel.Fixed;
                    }
                    return precisionModel.Value;
                }
                else
                {
                    return NtsGeometryServices.Instance.DefaultPrecisionModel;
                }
            }
        }
    }
    public static class FengNTSTest
    {
        public static LineString ToNTSLineString(Point3d start, Point3d end)
        {
            var points = new Coordinate[]
            {
                ToNTSCoordinate(start),
                ToNTSCoordinate(end)
            };
            return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points);
        }
        public static Polyline TessellatePolylineWithArc(Polyline poly, double length)
        {
            var segments = new PolylineSegmentCollection(poly);
            var tessellateSegments = new PolylineSegmentCollection();
            foreach (var s in segments)
            {
                tessellateSegments.AddRange(s.TessellateSegmentWithArc(length));
            }
            return tessellateSegments.ToPolyline();
        }
        public static LineString ToNTSLineString(Polyline poly)
        {
            var points = new List<Coordinate>();
            var arcLength = CADCoreNTSServiceConfig.Default.ArcTessellationLength;
            var polyLine = poly.HasBulges ? TessellatePolylineWithArc(poly, arcLength) : poly;
            for (int i = 0; i < polyLine.NumberOfVertices; i++)
            {
                points.Add(ToNTSCoordinate(polyLine.GetPoint3dAt(i)));
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (polyLine.Closed && !points[0].Equals(points[points.Count - 1]))
            {
                points.Add(points[0]);
            }

            if (points[0].Equals(points[points.Count - 1]))
            {
                // 三个点，其中起点和终点重合
                // 多段线退化成一根线段
                if (points.Count == 3)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points.ToArray());
                }

                // 二个点，其中起点和终点重合
                // 多段线退化成一个点
                if (points.Count == 2)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString();
                }

                // 一个点
                // 多段线退化成一个点
                if (points.Count == 1)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString();
                }

                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLinearRing(points.ToArray());
            }
            else
            {
                // 首尾端点不一致的情况
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points.ToArray());
            }
        }
        public static Coordinate ToNTSCoordinate(Point3d point)
        {
            return new Coordinate(
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.X),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.Y)
                    );

        }
        public static Coordinate ToNTSCoordinate(Point2d point)
        {
            return new Coordinate(
                CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.X),
                CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.Y)
                );
        }
        /// <summary>
        /// 根据弧长分割PolylineSegment
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection TessellateSegmentWithArc(this PolylineSegment segment, double length)
        {
            var segments = new PolylineSegmentCollection();
            if (segment.IsLinear)
            {
                // 分割线是直线
                segments.Add(segment);
            }
            else
            {
                // 分割线是弧线
                var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
                // 排除分割长度大于弧的周长的情况
                if (length >= 2 * Math.PI * circulararc.Radius)
                {
                    segments.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
                }
                else
                {
                    var angle = length / circulararc.Radius;
                    segments.AddRange(DoTessellate(segment, angle));
                }
            }
            return segments;
        }
        /// <summary>
        /// 根据角度分割弧段(保证起始点终止点不变)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection DoTessellate(PolylineSegment segment, double angle)
        {
            var TessellateArc = new PolylineSegmentCollection();
            var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
            var angleRange = 4 * Math.Atan(segment.Bulge);
            // 判断弧线是否是顺时针方向
            int IsClockwise = (segment.Bulge < 0.0) ? -1 : 1;
            if (angle >= (angleRange * IsClockwise))
            {
                TessellateArc.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
            }
            else
            {
                // 如果方向向量与y轴正方向的角度 小于等于90° 则方向向量在一三象限或x轴上，此时方向向量与x轴的角度不需要变化，否则需要 2PI - 与x轴角度
                double StartAng = (circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)));

                double EndAng = (circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)));
                int num = Convert.ToInt32(Math.Floor(angleRange * IsClockwise / angle)) + 1;

                for (int i = 1; i <= num; i++)
                {
                    var startAngle = StartAng + (i - 1) * angle * IsClockwise;
                    var endAngle = StartAng + i * angle * IsClockwise;
                    if (i == num)
                    {
                        endAngle = EndAng;
                    }
                    startAngle = (startAngle > 8 * Math.Atan(1)) ? startAngle - 8 * Math.Atan(1) : startAngle;
                    startAngle = (startAngle < 0.0) ? startAngle + 8 * Math.Atan(1) : startAngle;
                    endAngle = (endAngle > 8 * Math.Atan(1)) ? endAngle - 8 * Math.Atan(1) : endAngle;
                    endAngle = (endAngle < 0.0) ? endAngle + 8 * Math.Atan(1) : endAngle;
                    // Arc的构建方向是逆时针的，所以如果是顺时针的弧段，需要反向构建
                    if (segment.Bulge < 0.0)
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, endAngle, startAngle);
                        TessellateArc.Add(new PolylineSegment(arc.EndPoint.ToPoint2d(), arc.StartPoint.ToPoint2d()));
                    }
                    else
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, startAngle, endAngle);
                        TessellateArc.Add(new PolylineSegment(arc.StartPoint.ToPoint2d(), arc.EndPoint.ToPoint2d()));
                    }
                }
            }
            return TessellateArc;
        }
    }
   
   
    /// <summary>
    /// Represents a Polyline segment.
    /// </summary>
    public class PolylineSegment
    {
        #region Fields

        private Point2d _startPoint, _endPoint;
        private double _bulge, _startWidth, _endWidth;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the segment start point.
        /// </summary>
        public Point2d StartPoint
        {
            get { return _startPoint; }
            set { _startPoint = value; }
        }

        /// <summary>
        /// Gets or sets the segment end point.
        /// </summary>
        public Point2d EndPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        /// <summary>
        /// Gets or sets the segment bulge.
        /// </summary>
        public double Bulge
        {
            get { return _bulge; }
            set { _bulge = value; }
        }

        /// <summary>
        /// Gets or sets the segment start width.
        /// </summary>
        public double StartWidth
        {
            get { return _startWidth; }
            set { _startWidth = value; }
        }

        /// <summary>
        /// Gets or sets the segment end width.
        /// </summary>
        public double EndWidth
        {
            get { return _endWidth; }
            set { _endWidth = value; }
        }

        /// <summary>
        /// Gets true if the segment is linear.
        /// </summary>
        public bool IsLinear
        {
            get { return _bulge == 0.0; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of PolylineSegment from two points.
        /// </summary>
        /// <param name="startPoint">The start point of the segment.</param>
        /// <param name="endPoint">The end point of the segment.</param>
        public PolylineSegment(Point2d startPoint, Point2d endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _bulge = 0.0;
            _startWidth = 0.0;
            _endWidth = 0.0;
        }

        /// <summary>
        /// Creates a new instance of PolylineSegment from two points and a bulge.
        /// </summary>
        /// <param name="startPoint">The start point of the segment.</param>
        /// <param name="endPoint">The end point of the segment.</param>
        /// <param name="bulge">The bulge of the segment.</param>
        public PolylineSegment(Point2d startPoint, Point2d endPoint, double bulge)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _bulge = bulge;
            _startWidth = 0.0;
            _endWidth = 0.0;
        }

        /// <summary>
        /// Creates a new instance of PolylineSegment from two points, a bulge and a constant width.
        /// </summary>
        /// <param name="startPoint">The start point of the segment.</param>
        /// <param name="endPoint">The end point of the segment.</param>
        /// <param name="bulge">The bulge of the segment.</param>
        /// <param name="constantWidth">The constant width of the segment.</param>
        public PolylineSegment(Point2d startPoint, Point2d endPoint, double bulge, double constantWidth)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _bulge = bulge;
            _startWidth = constantWidth;
            _endWidth = constantWidth;
        }

        /// <summary>
        /// Creates a new instance of PolylineSegment from two points, a bulge, a start width and an end width.
        /// </summary>
        /// <param name="startPoint">The start point of the segment.</param>
        /// <param name="endPoint">The end point of the segment.</param>
        /// <param name="bulge">The bulge of the segment.</param>
        /// <param name="startWidth">The start width of the segment.</param>
        /// <param name="endWidth">The end width of the segment.</param>
        public PolylineSegment(Point2d startPoint, Point2d endPoint, double bulge, double startWidth, double endWidth)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _bulge = bulge;
            _startWidth = startWidth;
            _endWidth = endWidth;
        }

        /// <summary>
        /// Creates a new instance of PolylineSegment from a LineSegment2d
        /// </summary>
        /// <param name="line">A LineSegment2d instance.</param>
        public PolylineSegment(LineSegment2d line)
        {
            _startPoint = line.StartPoint;
            _endPoint = line.EndPoint;
            _bulge = 0.0;
            _startWidth = 0.0;
            _endWidth = 0.0;
        }

        /// <summary>
        /// Creates a new instance of PolylineSegment from a CircularArc2d
        /// </summary>
        /// <param name="arc">A CircularArc2d instance.</param>
        public PolylineSegment(CircularArc2d arc)
        {
            _startPoint = arc.StartPoint;
            _endPoint = arc.EndPoint;
            _bulge = Math.Tan((arc.EndAngle - arc.StartAngle) / 4.0);
            if (arc.IsClockWise) _bulge = -_bulge;
            _startWidth = 0.0;
            _endWidth = 0.0;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a copy of the PolylineSegment
        /// </summary>
        /// <returns>A new PolylineSegment instance which is a copy of the instance this method applies to.</returns>
        public PolylineSegment Clone()
        {
            return new PolylineSegment(this.StartPoint, this.EndPoint, this.Bulge, this.StartWidth, this.EndWidth);
        }

        /// <summary>
        /// Returns the parameter value of point.
        /// </summary>
        /// <param name="pt">The Point 2d whose get the PolylineSegment parameter at.</param>
        /// <returns>A double between 0.0 and 1.0, or -1.0 if the point does not lie on the segment.</returns>
        public double GetParameterOf(Point2d pt)
        {
            if (IsLinear)
            {
                LineSegment2d line = ToLineSegment();
                return line.IsOn(pt) ? _startPoint.GetDistanceTo(pt) / line.Length : -1.0;
            }
            else
            {
                CircularArc2d arc = ToCircularArc();
                return arc.IsOn(pt) ?
                    arc.GetLength(arc.GetParameterOf(_startPoint), arc.GetParameterOf(pt)) /
                    arc.GetLength(arc.GetParameterOf(_startPoint), arc.GetParameterOf(_endPoint)) :
                    -1.0;
            }
        }

        /// <summary>
        /// Inverses the segment.
        /// </summary>
        public void Inverse()
        {
            Point2d tmpPoint = this.StartPoint;
            double tmpWidth = this.StartWidth;
            _startPoint = this.EndPoint;
            _endPoint = tmpPoint;
            _bulge = -this.Bulge;
            _startWidth = this.EndWidth;
            _endWidth = tmpWidth;
        }

        /// <summary>
        /// Converts the PolylineSegment into a LineSegment2d.
        /// </summary>
        /// <returns>A new LineSegment2d instance or null if the PolylineSegment bulge is not equal to 0.0.</returns>
        public LineSegment2d ToLineSegment()
        {
            return IsLinear ? new LineSegment2d(_startPoint, _endPoint) : null;
        }

        /// <summary>
        /// Converts the PolylineSegment into a CircularArc2d.
        /// </summary>
        /// <returns>A new CircularArc2d instance or null if the PolylineSegment bulge is equal to 0.0.</returns>
        public CircularArc2d ToCircularArc()
        {
            return IsLinear ? null : new CircularArc2d(_startPoint, _endPoint, _bulge, false);
        }

        /// <summary>
        /// Converts the PolylineSegment into a Curve2d.
        /// </summary>
        /// <returns>A new Curve2d instance.</returns>
        public Curve2d ToCurve2d()
        {
            return IsLinear ?
                (Curve2d)(new LineSegment2d(_startPoint, _endPoint)) :
                (Curve2d)(new CircularArc2d(_startPoint, _endPoint, _bulge, false));
        }

        /// <summary>
        /// Determines whether the specified PolylineSegment instances are considered equal. 
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>true if the objects are considered equal; otherwise, nil.</returns>
        public override bool Equals(object obj)
        {
            PolylineSegment seg = obj as PolylineSegment;
            if (seg == null) return false;
            if (seg.GetHashCode() != this.GetHashCode()) return false;
            if (!_startPoint.IsEqualTo(seg.StartPoint)) return false;
            if (!_endPoint.IsEqualTo(seg.EndPoint)) return false;
            if (_bulge != seg.Bulge) return false;
            if (_startWidth != seg.StartWidth) return false;
            if (_endWidth != seg.EndWidth) return false;
            return true;
        }

        /// <summary>
        /// Serves as a hash function for the PolylineSegment type. 
        /// </summary>
        /// <returns>A hash code for the current PolylineSegemnt.</returns>
        public override int GetHashCode()
        {
            return _startPoint.GetHashCode() ^
                _endPoint.GetHashCode() ^
                _bulge.GetHashCode() ^
                _startWidth.GetHashCode() ^
                _endWidth.GetHashCode();
        }

        /// <summary>
        /// Applies ToString() to each property and concatenate the results separted with commas.
        /// </summary>
        /// <returns>A string containing the current PolylineSegemnt properties separated with commas.</returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}",
                _startPoint.ToString(),
                _endPoint.ToString(),
                _bulge.ToString(),
                _startWidth.ToString(),
                _endWidth.ToString());
        }

        #endregion
    }
    /// <summary>
    /// Represents a PolylineSegment collection.
    /// </summary>
    public class PolylineSegmentCollection : IList<PolylineSegment>
    {
        private List<PolylineSegment> _contents = new List<PolylineSegment>();

        /// <summary>
        /// Gets the first PolylineSegment StartPoint
        /// </summary>
        public Point2d StartPoint
        {
            get { return _contents[0].StartPoint; }
        }

        /// <summary>
        /// Gets the last PolylineSegment EndPoint
        /// </summary>
        public Point2d EndPoint
        {
            get { return _contents[Count - 1].EndPoint; }
        }

        #region Constructors

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection.
        /// </summary>
        public PolylineSegmentCollection() { }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from a PolylineSegment collection (IEnumerable).
        /// </summary>
        /// <param name="segments">A PolylineSegment collection.</param>
        public PolylineSegmentCollection(IEnumerable<PolylineSegment> segments)
        {
            _contents.AddRange(segments);
        }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from a PolylineSegment array.
        /// </summary>
        /// <param name="segments">A PolylineSegment array.</param>
        public PolylineSegmentCollection(params PolylineSegment[] segments)
        {
            _contents.AddRange(segments);
        }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from a Polyline.
        /// </summary>
        /// <param name="pline">A Polyline instance.</param>
        public PolylineSegmentCollection(Polyline pline)
        {
            int n = pline.NumberOfVertices - 1;
            for (int i = 0; i < n; i++)
            {
                _contents.Add(new PolylineSegment(
                    pline.GetPoint2dAt(i),
                    pline.GetPoint2dAt(i + 1),
                    pline.GetBulgeAt(i),
                    pline.GetStartWidthAt(i),
                    pline.GetEndWidthAt(i)));
            }
            if (pline.Closed == true)
            {
                _contents.Add(new PolylineSegment(
                    pline.GetPoint2dAt(n),
                    pline.GetPoint2dAt(0),
                    pline.GetBulgeAt(n),
                    pline.GetStartWidthAt(n),
                    pline.GetEndWidthAt(n)));
            }
        }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from a Polyline2d.
        /// </summary>
        /// <param name="pline">A Polyline2d instance.</param>
        public PolylineSegmentCollection(Vertex2d[] vertices,bool closed)
        {
            int n = vertices.Length - 1;
            for (int i = 0; i < n; i++)
            {
                Vertex2d vertex = vertices[i];
                _contents.Add(new PolylineSegment(
                    vertex.Position.Convert2d(),
                    vertices[i + 1].Position.Convert2d(),
                    vertex.Bulge,
                    vertex.StartWidth,
                    vertex.EndWidth));
            }
            if (closed == true)
            {
                Vertex2d vertex = vertices[n];
                _contents.Add(new PolylineSegment(
                    vertex.Position.Convert2d(),
                    vertices[0].Position.Convert2d(),
                    vertex.Bulge,
                    vertex.StartWidth,
                    vertex.EndWidth));
            }
        }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from a Circle.
        /// </summary>
        /// <param name="circle">A Circle instance.</param>
        public PolylineSegmentCollection(Circle circle)
        {
            Plane plane = new Plane(Point3d.Origin, circle.Normal);
            Point2d cen = circle.Center.Convert2d(plane);
            Vector2d vec = new Vector2d(circle.Radius, 0.0);
            _contents.Add(new PolylineSegment(cen + vec, cen - vec, 1.0));
            _contents.Add(new PolylineSegment(cen - vec, cen + vec, 1.0));
        }

        /// <summary>
        /// Creates a new instance of PolylineSegmentCollection from an Ellipse.
        /// </summary>
        /// <param name="ellipse">An Ellipse instance.</param>
        public PolylineSegmentCollection(Ellipse ellipse)
        {
            // PolylineSegmentCollection figuring the closed ellipse
            double pi = Math.PI;
            Plane plane = new Plane(Point3d.Origin, ellipse.Normal);
            Point3d cen3d = ellipse.Center;
            Point3d pt3d0 = cen3d + ellipse.MajorAxis;
            Point3d pt3d4 = cen3d + ellipse.MinorAxis;
            Point3d pt3d2 = ellipse.GetPointAtParameter(pi / 4.0);
            Point2d cen = cen3d.Convert2d(plane);
            Point2d pt0 = pt3d0.Convert2d(plane);
            Point2d pt2 = pt3d2.Convert2d(plane);
            Point2d pt4 = pt3d4.Convert2d(plane);
            Line2d line01 = new Line2d(pt0, (pt4 - cen).GetNormal() + (pt2 - pt0).GetNormal());
            Line2d line21 = new Line2d(pt2, (pt0 - pt4).GetNormal() + (pt0 - pt2).GetNormal());
            Line2d line23 = new Line2d(pt2, (pt4 - pt0).GetNormal() + (pt4 - pt2).GetNormal());
            Line2d line43 = new Line2d(pt4, (pt0 - cen).GetNormal() + (pt2 - pt4).GetNormal());
            Line2d majAx = new Line2d(cen, pt0);
            Line2d minAx = new Line2d(cen, pt4);
            Point2d pt1 = line01.IntersectWith(line21)[0];
            Point2d pt3 = line23.IntersectWith(line43)[0];
            Point2d pt5 = pt3.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt6 = pt2.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt7 = pt1.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt8 = pt0.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt9 = pt7.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt10 = pt6.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt11 = pt5.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt12 = pt4.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt13 = pt3.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt14 = pt2.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt15 = pt1.TransformBy(Matrix2d.Mirroring(majAx));
            double bulge1 = Math.Tan((pt4 - cen).GetAngleTo(pt1 - pt0) / 2.0);
            double bulge2 = Math.Tan((pt1 - pt2).GetAngleTo(pt0 - pt4) / 2.0);
            double bulge3 = Math.Tan((pt4 - pt0).GetAngleTo(pt3 - pt2) / 2.0);
            double bulge4 = Math.Tan((pt3 - pt4).GetAngleTo(pt0 - cen) / 2.0);
            _contents.Add(new PolylineSegment(pt0, pt1, bulge1));
            _contents.Add(new PolylineSegment(pt1, pt2, bulge2));
            _contents.Add(new PolylineSegment(pt2, pt3, bulge3));
            _contents.Add(new PolylineSegment(pt3, pt4, bulge4));
            _contents.Add(new PolylineSegment(pt4, pt5, bulge4));
            _contents.Add(new PolylineSegment(pt5, pt6, bulge3));
            _contents.Add(new PolylineSegment(pt6, pt7, bulge2));
            _contents.Add(new PolylineSegment(pt7, pt8, bulge1));
            _contents.Add(new PolylineSegment(pt8, pt9, bulge1));
            _contents.Add(new PolylineSegment(pt9, pt10, bulge2));
            _contents.Add(new PolylineSegment(pt10, pt11, bulge3));
            _contents.Add(new PolylineSegment(pt11, pt12, bulge4));
            _contents.Add(new PolylineSegment(pt12, pt13, bulge4));
            _contents.Add(new PolylineSegment(pt13, pt14, bulge3));
            _contents.Add(new PolylineSegment(pt14, pt15, bulge2));
            _contents.Add(new PolylineSegment(pt15, pt0, bulge1));

            // if the ellipse is an elliptical arc:
            if (!ellipse.Closed)
            {
                double startParam, endParam;
                Point2d startPoint = ellipse.StartPoint.Convert2d(plane);
                Point2d endPoint = ellipse.EndPoint.Convert2d(plane);

                // index of the PolylineSegment closest to the ellipse start point
                int startIndex = GetClosestSegmentIndexTo(startPoint);
                // start point on the PolylineSegment
                Point2d pt = _contents[startIndex].ToCurve2d().GetClosestPointTo(startPoint).Point;
                // if the point is equal to the PolylineSegment end point, jump the next segment in collection
                if (pt.IsEqualTo(_contents[startIndex].EndPoint))
                {
                    if (startIndex == 15)
                        startIndex = 0;
                    else
                        startIndex++;
                    startParam = 0.0;
                }
                // else get the 'parameter' at point on the PolylineSegment
                else
                {
                    startParam = _contents[startIndex].GetParameterOf(pt);
                }

                // index of the PolylineSegment closest to the ellipse end point
                int endIndex = GetClosestSegmentIndexTo(endPoint);
                // end point on the PolylineSegment
                pt = _contents[endIndex].ToCurve2d().GetClosestPointTo(endPoint).Point;
                // if the point is equals to the PolylineSegment startPoint, jump to the previous segment
                if (pt.IsEqualTo(_contents[endIndex].StartPoint))
                {
                    if (endIndex == 0)
                        endIndex = 15;
                    else
                        endIndex--;
                    endParam = 1.0;
                }
                // else get the 'parameter' at point on the PolylineSegment
                else
                {
                    endParam = _contents[endIndex].GetParameterOf(pt);
                }

                // if the parameter at start point is not equal to 0.0, calculate the bulge
                if (startParam != 0.0)
                {
                    _contents[startIndex].StartPoint = startPoint;
                    _contents[startIndex].Bulge = _contents[startIndex].Bulge * (1.0 - startParam);
                }

                // if the parameter at end point is not equal to 1.0, calculate the bulge
                if (endParam != 1.0) //(endParam != 0.0)
                {
                    _contents[endIndex].EndPoint = endPoint;
                    _contents[endIndex].Bulge = _contents[endIndex].Bulge * (endParam);
                }

                // if both points are on the same segment
                if (startIndex == endIndex)
                {
                    PolylineSegment segment = _contents[startIndex];
                    _contents.Clear();
                    _contents.Add(segment);
                }

                else if (startIndex < endIndex)
                {
                    _contents.RemoveRange(endIndex + 1, 15 - endIndex);
                    _contents.RemoveRange(0, startIndex);
                }
                else
                {
                    _contents.AddRange(_contents.GetRange(0, endIndex + 1));
                    _contents.RemoveRange(0, startIndex);
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, 
        /// and returns the zero-based index of the first occurrence within the entire collection.
        /// </summary>
        /// <param name="match">The Predicate delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
        public int FindIndex(Predicate<PolylineSegment> match)
        {
            return _contents.FindIndex(match);
        }

        /// <summary>
        /// Returns the zero-based index of the closest segment to the input point.
        /// </summary>
        /// <param name="pt">The Point2d from which the distances to segments are compared.</param>
        /// <returns>The zero-based index of the segment in the PolylineSegmentCollection.</returns>
        public int GetClosestSegmentIndexTo(Point2d pt)
        {
            int result = 0;
            double dist = _contents[0].ToCurve2d().GetDistanceTo(pt);
            for (int i = 1; i < Count; i++)
            {
                double tmpDist = _contents[i].ToCurve2d().GetDistanceTo(pt);
                if (tmpDist < dist)
                {
                    result = i;
                    dist = tmpDist;
                }
            }
            return result;
        }

        /// <summary>
        /// Inserts a segments collection into the collection at the specified index. 
        /// </summary>
        /// <param name="index">The zero-based index at which collection should be inserted</param>
        /// <param name="collection">The collection to insert</param>
        public void InsertRange(int index, IEnumerable<PolylineSegment> collection)
        {
            _contents.InsertRange(index, collection);
        }

        /// <summary>
        /// Joins the contiguous segments into one or more PolylineSegment collections.
        /// Start point and end point of each segment are compared  using the global tolerance.
        /// </summary>
        /// <returns>A List of PolylineSegmentCollection instances.</returns>
        public List<PolylineSegmentCollection> Join()
        {
            return Join(Tolerance.Global);
        }

        /// <summary>
        /// Joins the contiguous segments into one or more PolylineSegment collections.
        /// Start point and end point of each segment are compared  using the specified tolerance.
        /// </summary>
        /// <param name="tol">The tolerance to use while comparing segments startand end points</param>
        /// <returns>A List of PolylineSegmentCollection instances.</returns>
        public List<PolylineSegmentCollection> Join(Tolerance tol)
        {
            List<PolylineSegmentCollection> result = new List<PolylineSegmentCollection>();
            PolylineSegmentCollection clone = new PolylineSegmentCollection(_contents);
            while (clone.Count > 0)
            {
                PolylineSegmentCollection newCol = new PolylineSegmentCollection();
                PolylineSegment seg = clone[0];
                newCol.Add(seg);
                Point2d start = seg.StartPoint;
                Point2d end = seg.EndPoint;
                clone.RemoveAt(0);
                while (true)
                {
                    int i = clone.FindIndex(s => s.StartPoint.IsEqualTo(end, tol));
                    if (i >= 0)
                    {
                        seg = clone[i];
                        newCol.Add(seg);
                        end = seg.EndPoint;
                        clone.RemoveAt(i);
                        continue;
                    }
                    i = clone.FindIndex(s => s.EndPoint.IsEqualTo(end, tol));
                    if (i >= 0)
                    {
                        seg = clone[i];
                        seg.Inverse();
                        newCol.Add(seg);
                        end = seg.EndPoint;
                        clone.RemoveAt(i);
                        continue;
                    }
                    i = clone.FindIndex(s => s.EndPoint.IsEqualTo(start, tol));
                    if (i >= 0)
                    {
                        seg = clone[i];
                        newCol.Insert(0, seg);
                        start = seg.StartPoint;
                        clone.RemoveAt(i);
                        continue;
                    }
                    i = clone.FindIndex(s => s.StartPoint.IsEqualTo(start, tol));
                    if (i >= 0)
                    {
                        seg = clone[i];
                        seg.Inverse();
                        newCol.Insert(0, seg);
                        start = seg.StartPoint;
                        clone.RemoveAt(i);
                        continue;
                    }
                    break;
                }
                result.Add(newCol);
            }
            return result;
        }

        /// <summary>
        /// Removes a range of segments from the collection.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of segments to remove.</param>
        /// <param name="count">The number of segments to remove.</param>
        public void RemoveRange(int index, int count)
        {
            _contents.RemoveRange(index, count);
        }

        /// <summary>
        /// Reverses the collection order and all PolylineSegments
        /// </summary>
        public void Reverse()
        {
            for (int i = 0; i < Count; i++)
            {
                _contents[i].Inverse();
            }
            _contents.Reverse();
        }

        /// <summary>
        /// Creates a new Polyline from the PolylineSegment collection.
        /// </summary>
        /// <returns>A Polyline instance.</returns>
        public Polyline ToPolyline()
        {
            Polyline pline = new Polyline();
            for (int i = 0; i < _contents.Count; i++)
            {
                PolylineSegment seg = _contents[i];
                pline.AddVertexAt(i, seg.StartPoint, seg.Bulge, seg.StartWidth, seg.EndWidth);
            }
            int j = _contents.Count;
            pline.AddVertexAt(j, this[j - 1].EndPoint, 0.0, _contents[j - 1].EndWidth, _contents[0].StartWidth);
            if (pline.GetPoint2dAt(0).IsEqualTo(pline.GetPoint2dAt(j)))
            {
                pline.RemoveVertexAt(j);
                pline.Closed = true;
            }
            return pline;
        }

        #endregion

        #region IList<PolylineSegment> Members

        /// <summary>
        /// Returns the zero-based index of the first occurrence of a value in the collection.
        /// </summary>
        /// <param name="item">The segment to locate in the collection.</param>
        /// <returns>The zero-based index of the first occurrence of item within the entire List, if found; otherwise, –1.</returns>
        public int IndexOf(PolylineSegment item)
        {
            return _contents.IndexOf(item);
        }

        /// <summary>
        /// Inserts a segment into the collection at the specified index. 
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The segment to insert.</param>
        public void Insert(int index, PolylineSegment item)
        {
            _contents.Insert(index, item);
        }

        /// <summary>
        /// Removes the element at the specified index of the collection. 
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            _contents.RemoveAt(index);
        }

        /// <summary>
        /// Gets or sets the element at the specified index. 
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public PolylineSegment this[int index]
        {
            get { return _contents[index]; }
            set { _contents[index] = value; }
        }

        #endregion

        #region ICollection<PolylineSegment> Members

        /// <summary>
        /// Adds a segment to the end of the collection.
        /// </summary>
        /// <param name="item">The segment to be added to the end of the collection.</param>
        public void Add(PolylineSegment item)
        {
            _contents.Add(item);
        }

        /// <summary>
        /// Adds the segments of the specified collection to the end of the collection.
        /// </summary>
        /// <param name="range">The collection whose elements should be added to the end of this collection.</param>
        public void AddRange(IEnumerable<PolylineSegment> range)
        {
            _contents.AddRange(range);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            _contents.Clear();
        }

        /// <summary>
        /// Determines whether a segment is in the collection.
        /// </summary>
        /// <param name="item">The segment to locate in the collection.</param>
        /// <returns>true if item is found in the collection; otherwise, false.</returns>
        public bool Contains(PolylineSegment item)
        {
            return _contents.Contains(item);
        }

        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from collection.</param>
        /// <param name="index">The zero-based index in array at which copying begin.s</param>
        public void CopyTo(PolylineSegment[] array, int index)
        {
            _contents.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements actually contained in the collection.
        /// </summary>
        public int Count
        {
            get { return _contents.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">The segment to remove.</param>
        /// <returns>The segment to remove from the collection.</returns>
        public bool Remove(PolylineSegment item)
        {
            return _contents.Remove(item);
        }

        #endregion

        #region IEnumerable<PolylineSegment> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An IEnumerable&lt;PolylineSegment&gt; enumerator for the PolylineSegmentCollection.</returns>
        public IEnumerator<PolylineSegment> GetEnumerator()
        {
            foreach (PolylineSegment seg in _contents) yield return seg;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
    /// <summary>
    /// Various algorithms.
    /// </summary>
    public static class Algorithms
    {
        #region Curve algorithms

        /// <summary>
        /// Gets the distance between a point and a curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="point">The point.</param>
        /// <param name="extend">Whether to extend curve if needed.</param>
        /// <returns>The distance.</returns>
        public static double GetDistToPoint(this Curve cv, Point3d point, bool extend = false)
        {
            return cv.GetClosestPointTo(point, extend).DistanceTo(point);
        }

        /// <summary>
        /// Gets the parameter at a specified distance on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="dist">The distance.</param>
        /// <returns>The paramter.</returns>
        public static double GetParamAtDist(this Curve cv, double dist)
        {
            if (dist < 0)
            {
                dist = 0;
            }
            else if (dist > cv.GetDistanceAtParameter(cv.EndParam))
            {
                dist = cv.GetDistanceAtParameter(cv.EndParam);
            }
            return cv.GetParameterAtDistance(dist);
        }

        /// <summary>
        /// Gets the distance at a specified parameter on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>The distance.</returns>
        public static double GetDistAtParam(this Curve cv, double param)
        {
            if (param < 0)
            {
                param = 0;
            }
            else if (param > cv.EndParam)
            {
                param = cv.EndParam;
            }
            return cv.GetDistanceAtParameter(param);
        }

        /// <summary>
        /// Gets the point at a specified parameter on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>The point.</returns>
        public static Point3d GetPointAtParam(this Curve cv, double param)
        {
            if (param < 0)
            {
                param = 0;
            }
            else if (param > cv.EndParam)
            {
                param = cv.EndParam;
            }
            return cv.GetPointAtParameter(param);
        }

        /// <summary>
        /// Gets the point at a specified distance on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="dist">The distance.</param>
        /// <returns>The point.</returns>
        public static Point3d GetPointAtDistX(this Curve cv, double dist)
        {
            if (dist < 0)
            {
                dist = 0;
            }
            else if (dist > cv.GetDistanceAtParameter(cv.EndParam))
            {
                dist = cv.GetDistanceAtParameter(cv.EndParam);
            }
            return cv.GetPointAtDist(dist);
        }

        /// <summary>
        /// Gets the distance at a specified point on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="point">The point.</param>
        /// <returns>The distance.</returns>
        public static double GetDistAtPointX(this Curve cv, Point3d point)
        {
            if (point.DistanceTo(cv.StartPoint) < Consts.Epsilon)
            {
                return 0.0;
            }
            else if (point.DistanceTo(cv.EndPoint) < Consts.Epsilon)
            {
                return cv.GetDistAtPoint(cv.EndPoint);
            }
            else
            {
                try
                {
                    return cv.GetDistAtPoint(point);
                }
                catch
                {
                    return cv.GetDistAtPoint(cv.GetClosestPointTo(point, false));
                }
            }
        }

        /// <summary>
        /// Gets the parameter at a specified point on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="point">The point.</param>
        /// <returns>The parameter.</returns>
        public static double GetParamAtPointX(this Curve cv, Point3d point)
        {
            if (point.DistanceTo(cv.StartPoint) < Consts.Epsilon)
            {
                return 0.0;
            }
            else if (point.DistanceTo(cv.EndPoint) < Consts.Epsilon)
            {
                return cv.GetParameterAtPoint(cv.EndPoint);
            }
            else
            {
                try
                {
                    return cv.GetParameterAtPoint(point);
                }
                catch
                {
                    return cv.GetParameterAtPoint(cv.GetClosestPointTo(point, false));
                }
            }
        }

        /// <summary>
        /// Gets subcurve from curve by distance interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in distance.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurve(this Curve curve, Interv interval) // todo: Remove complex type from API
        {
            if (curve is Line)
            {
                curve = (curve as Line).ToPolyline();
            }
            else if (curve is Arc)
            {
                curve = (curve as Arc).ToPolyline();
            }
            double start = curve.GetParamAtDist(interval.Start);
            double end = curve.GetParamAtDist(interval.End);
            return curve.GetSubCurveByParams(new Interv(start, end));
        }

        /// <summary>
        /// Gets subcurve from curve by distance interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in distance.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurve(this Curve curve, Tuple<double, double> interval)
        {
            return Algorithms.GetSubCurve(curve, new Interv(interval.Item1, interval.Item2));
        }

#if R23
        /// <summary>
        /// Gets subcurve from curve by distance interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in distance.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurve(this Curve curve, (double, double) interval)
        {
            return Algorithms.GetSubCurve(curve, new Interv(interval));
        }
#endif

        /// <summary>
        /// Gets subcurve from curve by parameter interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in parameter.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurveByParams(this Curve curve, Interv interval)
        {
            if (curve is Line)
            {
                curve = (curve as Line).ToPolyline();
            }
            else if (curve is Arc)
            {
                curve = (curve as Arc).ToPolyline();
            }
            double start = interval.Start;
            double end = interval.End;
            double startDist = curve.GetDistAtParam(start);
            double endDist = curve.GetDistAtParam(end);

            //LogManager.Write("total", curve.EndParam);
            //LogManager.Write("start", start);
            //LogManager.Write("end", end);
            //LogManager.Write("type", curve.GetType());

            DBObjectCollection splits = curve.GetSplitCurves(new DoubleCollection(new double[] { start, end }));
            if (splits.Count == 3)
            {
                return splits[1] as Curve;
            }
            else
            {
                if (startDist == endDist)
                {
                    Point3d p = curve.GetPointAtParameter(start);
                    if (curve is Line)
                    {
                        return new Line(p, p);
                    }
                    else if (curve is Arc)
                    {
                        return new Arc(p, 0, 0, 0);
                    }
                    else if (curve is Polyline)
                    {
                        Polyline poly = new Polyline();
                        poly.AddVertexAt(0, p.ToPoint2d(), 0, 0, 0);
                        poly.AddVertexAt(0, p.ToPoint2d(), 0, 0, 0);
                        return poly;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (splits.Count == 2)
                    {
                        if (start == 0)
                        {
                            return splits[0] as Curve;
                        }
                        else
                        {
                            return splits[1] as Curve;
                        }
                    }
                    else // Count == 1
                    {
                        return splits[0] as Curve;
                    }
                }
            }
        }

        /// <summary>
        /// Gets subcurve from curve by parameter interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in parameter.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurveByParams(this Curve curve, Tuple<double, double> interval)
        {
            return Algorithms.GetSubCurveByParams(curve, new Interv(interval.Item1, interval.Item2));
        }

#if R23
        /// <summary>
        /// Gets subcurve from curve by parameter interval.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="interval">The subcurve interval in parameter.</param>
        /// <returns>The subcurve.</returns>
        public static Curve GetSubCurveByParams(this Curve curve, (double, double) interval)
        {
            return Algorithms.GetSubCurveByParams(curve, new Interv(interval));
        }
#endif

        /// <summary>
        /// Gets all points on curve whose parameters are an arithmetic sequence starting from 0.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="paramDelta">The parameter increment. Th default is 1, in which case the method returns all points on curve whose parameters are integres.</param>
        /// <returns>The points.</returns>
        public static IEnumerable<Point3d> GetPoints(this Curve cv, double paramDelta = 1)
        {
            for (var param = 0d; param <= cv.EndParam; param += paramDelta)
            {
                yield return cv.GetPointAtParam(param);
            }
        }

        /// <summary>
        /// Gets all points on curve whose distances (from start) are an arithmetic sequence starting from 0.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="distDelta">The dist increment.</param>
        /// <returns>The points.</returns>
        public static IEnumerable<Point3d> GetPointsByDist(this Curve cv, double distDelta)
        {
            for (var dist = 0d; dist <= cv.GetDistAtParam(cv.EndParam); dist += distDelta)
            {
                yield return cv.GetPointAtDistX(dist);
            }
        }

        /// <summary>
        /// Gets points that equally divide (parameter wise) a curve into `divs` (number of) segments.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="divs">The number of divisions.</param>
        /// <returns>The points.</returns>
        [Obsolete("This method has a design defect and will be removed.")]
        public static IEnumerable<Point3d> GetPoints(this Curve cv, int divs)
        {
            double div = cv.EndParam / divs;
            for (double i = 0; i < cv.EndParam + div; i += div)
            {
                yield return cv.GetPointAtParam(i);
            }
        }

        private static IEnumerable<Point3d> GetPolylineFitPointsImp(this Curve cv, int divsWhenArc)
        {
            var poly = cv as Polyline;
            if (poly == null)
            {
                yield return cv.StartPoint;
                yield return cv.EndPoint;
            }
            else
            {
                for (int i = 0; i < poly.EndParam - Consts.Epsilon; i++) // mod 20111101
                {
                    if (poly.GetBulgeAt(i) == 0)
                    {
                        yield return poly.GetPointAtParameter(i);
                    }
                    else
                    {
                        int divs = divsWhenArc == 0 ? (int)((Math.Atan(Math.Abs(poly.GetBulgeAt(i))) * 4) / (Math.PI / 18) + 4) : divsWhenArc;
                        // adding 4 in case extra small arcs, whose lengths might be huge.
                        // TODO: this is a design defect. We probably need to use fixed dist.
                        for (int j = 0; j < divs; j++)
                        {
                            yield return poly.GetPointAtParam(i + (double)j / divs);
                        }
                    }
                }
                yield return poly.GetPointAtParameter(poly.EndParam);
            }
        }

        /// <summary>
        /// Gets polyline fit points (in case of arcs).
        /// </summary>
        /// <param name="cv">The polyline.</param>
        /// <param name="divsWhenArc">Number of divisions for arcs. The default is 0 (smart).</param>
        /// <returns>The points.</returns>
        public static IEnumerable<Point3d> GetPolylineFitPoints(this Curve cv, int divsWhenArc = 0)
        {
            try
            {
                return Algorithms.GetPolylineFitPointsImp(cv, divsWhenArc).ToArray();
            }
            catch
            {
                throw new Exception("PolylineNeedsCleanup");
            }
        }

        /// <summary>
        /// Gets subcurves by dividing a curve on points whose parameters are an arithmetic sequence starting from 0.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="paramDelta">The parameter increment. Th default is 1, in which case the method divides the curve on points whose parameters are integers.</param>
        /// <returns>The result curves.</returns>
        public static IEnumerable<Curve> GetSegments(this Curve cv, double paramDelta = 1)
        {
            for (var param = 0d; param < cv.EndParam; param += paramDelta)
            {
                yield return cv.GetSubCurveByParams(Tuple.Create(param, param + paramDelta));
            }
        }

        /// <summary>
        /// Gets subcurves by dividing a curve on points whose distances (from start) are an arithmetic sequence starting from 0.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="distDelta">The dist increment.</param>
        /// <returns>The result curves.</returns>
        public static IEnumerable<Curve> GetSegmentsByDist(this Curve cv, double distDelta)
        {
            for (var dist = 0d; dist < cv.GetDistAtParam(cv.EndParam); dist += distDelta)
            {
                yield return cv.GetSubCurve(Tuple.Create(dist, dist + distDelta)); // TODO: unify patterns of using "Param" and "Dist".
            }
        }

        /// <summary>
        /// Gets the minimum distance between two curves.
        /// </summary>
        /// <param name="cv1">The curve 1.</param>
        /// <param name="cv2">The curve 2.</param>
        /// <param name="divs">The number of divisions per curve used for calculating.</param>
        /// <returns>The distance.</returns>
        [Obsolete("The desgin has defect and the implementation is not optimized.")]
        public static double GetDistOfTwoCurve(Curve cv1, Curve cv2, int divs = 100)
        {
            var pts1 = cv1.GetPoints(divs);
            var pts2 = cv2.GetPoints(divs);
            return pts1.Min(p1 => pts2.Min(p2 => p1.DistanceTo(p2)));
        }

        #endregion

        #region Range algorithms

        /// <summary>
        /// Gets entity extents.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>The result extents.</returns>
        public static Extents3d GetExtents(this IEnumerable<Entity> entities)
        {
            var extent = entities.First().GeometricExtents;
            foreach (var ent in entities)
            {
                extent.AddExtents(ent.GeometricExtents);
            }
            return extent;
        }

        /// <summary>
        /// Gets the center of an Extents3d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <returns>The center.</returns>
        public static Point3d GetCenter(this Extents3d extents)
        {
            return Point3d.Origin + 0.5 * (extents.MinPoint.GetAsVector() + extents.MaxPoint.GetAsVector());
        }

        /// <summary>
        /// Gets the center of an Extents2d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <returns>The center.</returns>
        public static Point2d GetCenter(this Extents2d extents)
        {
            return Point2d.Origin + 0.5 * (extents.MinPoint.GetAsVector() + extents.MaxPoint.GetAsVector());
        }

        /// <summary>
        /// Scales an Extents3d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <param name="factor">The scale factor.</param>
        /// <returns>The result.</returns>
        public static Extents3d Expand(this Extents3d extents, double factor)
        {
            var center = extents.GetCenter();
            return new Extents3d(center + factor * (extents.MinPoint - center), center + factor * (extents.MaxPoint - center));
        }

        /// <summary>
        /// Inflates an Point3d into an Extents3d.
        /// </summary>
        /// <param name="center">The point.</param>
        /// <param name="size">The inflation size.</param>
        /// <returns>The result.</returns>
        public static Extents3d Expand(this Point3d center, double size) // newly 20130201
        {
            Vector3d move = new Vector3d(size / 2, size / 2, size / 2);
            return new Extents3d(center - move, center + move);
        }

        /// <summary>
        /// Scales an Extents2d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <param name="factor">The scale factor.</param>
        /// <returns>The result.</returns>
        public static Extents2d Expand(this Extents2d extents, double factor)
        {
            var center = extents.GetCenter();
            return new Extents2d(center + factor * (extents.MinPoint - center), center + factor * (extents.MaxPoint - center));
        }

        /// <summary>
        /// Inflates an Point2d into an Extents2d.
        /// </summary>
        /// <param name="center">The point.</param>
        /// <param name="size">The inflation size.</param>
        /// <returns>The result.</returns>
        public static Extents2d Expand(this Point2d center, double size)
        {
            var move = new Vector2d(size / 2, size / 2);
            return new Extents2d(center - move, center + move);
        }

        /// <summary>
        /// Determines if a point is in an extents.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <param name="point">The point.</param>
        /// <returns>The result.</returns>
        public static bool IsPointIn(this Extents3d extents, Point3d point)
        {
            return point.X >= extents.MinPoint.X && point.X <= extents.MaxPoint.X
                && point.Y >= extents.MinPoint.Y && point.Y <= extents.MaxPoint.Y
                && point.Z >= extents.MinPoint.Z && point.Z <= extents.MaxPoint.Z;
        }

        /// <summary>
        /// Determines if a point is in an extents.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <param name="point">The point.</param>
        /// <returns>The result.</returns>
        public static bool IsPointIn(this Extents2d extents, Point2d point)
        {
            return point.X >= extents.MinPoint.X && point.X <= extents.MaxPoint.X
                && point.Y >= extents.MinPoint.Y && point.Y <= extents.MaxPoint.Y;
        }

        /// <summary>
        /// Converts Extents3d to Extents2d.
        /// </summary>
        /// <param name="extents">The Extents3d.</param>
        /// <param name="x">The X value selector.</param>
        /// <param name="y">The Y value selector.</param>
        /// <returns>The result Extents2d.</returns>
        public static Extents2d ToExtents2d(
            this Extents3d extents,
            Func<Point3d, double> x = null,
            Func<Point3d, double> y = null)
        {
            if (x == null)
            {
                x = p => p.X;
            }

            if (y == null)
            {
                y = p => p.Y;
            }

            return new Extents2d(
                x(extents.MinPoint),
                y(extents.MinPoint),
                x(extents.MaxPoint),
                y(extents.MaxPoint));
        }

        /// <summary>
        /// Converts Extents2d to Extents3d.
        /// </summary>
        /// <param name="extents">The Extents2d.</param>
        /// <param name="x">The X value selector.</param>
        /// <param name="y">The Y value selector.</param>
        /// <param name="z">The Z value selector.</param>
        /// <returns>The result Extents3d.</returns>
        public static Extents3d ToExtents3d(
            this Extents2d extents,
            Func<Point2d, double> x = null,
            Func<Point2d, double> y = null,
            Func<Point2d, double> z = null)
        {
            if (x == null)
            {
                x = p => p.X;
            }

            if (y == null)
            {
                y = p => p.Y;
            }

            if (z == null)
            {
                z = p => 0;
            }

            var minPoint = new Point3d(x(extents.MinPoint), y(extents.MinPoint), z(extents.MinPoint));
            var maxPoint = new Point3d(x(extents.MaxPoint), y(extents.MaxPoint), z(extents.MaxPoint));
            return new Extents3d(minPoint, maxPoint);
        }

        /// <summary>
        /// Gets the center of multiple entities.
        /// </summary>
        /// <param name="ents">The entities.</param>
        /// <returns>The center.</returns>
        public static Point3d GetCenter(this IEnumerable<Entity> ents)
        {
            return ents.GetExtents().GetCenter();
        }

        /// <summary>
        /// Gets the center of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The center.</returns>
        public static Point3d GetCenter(this Entity entity)
        {
            return entity.GeometricExtents.GetCenter();
        }

        /// <summary>
        /// Gets the volume of an Extents3d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <returns>The volume.</returns>
        public static double GetVolume(this Extents3d extents)
        {
            return (extents.MaxPoint.X - extents.MinPoint.X) * (extents.MaxPoint.Y - extents.MinPoint.Y) * (extents.MaxPoint.Z - extents.MinPoint.Z);
        }

        /// <summary>
        /// Gets the area of an Extents3d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <returns>The area.</returns>
        [Obsolete("Use `.ToExtents2d().GetArea()` instead.")]
        public static double GetArea(this Extents3d extents) // newly 20130514
        {
            return (extents.MaxPoint.X - extents.MinPoint.X) * (extents.MaxPoint.Y - extents.MinPoint.Y);
        }

        /// <summary>
        /// Gets the area of an Extents2d.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <returns>The area.</returns>
        public static double GetArea(this Extents2d extents) // newly 20130514
        {
            return (extents.MaxPoint.X - extents.MinPoint.X) * (extents.MaxPoint.Y - extents.MinPoint.Y);
        }

        #endregion

        #region Point algorithms

        /// <summary>
        /// Gets an empty Point3d
        /// </summary>
        public static Point3d NullPoint3d { get; } = new Point3d(double.NaN, double.NaN, double.NaN);

        /// <summary>
        /// Determines if a Point3d is empty.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>The result.</returns>
        public static bool IsNull(this Point3d p)
        {
            return double.IsNaN(p.X);
        }

        /// <summary>
        /// Converts Point3d to Point2d.
        /// </summary>
        /// <param name="point">The Point3d.</param>
        /// <returns>A Point2d.</returns>
        public static Point2d ToPoint2d(this Point3d point)
        {
            return new Point2d(point.X, point.Y);
        }

        /// <summary>
        /// Converts Point2d to Point3d.
        /// </summary>
        /// <param name="point">The Point2d.</param>
        /// <returns>A Point3d.</returns>
        public static Point3d ToPoint3d(this Point2d point)
        {
            return new Point3d(point.X, point.Y, 0);
        }

        /// <summary>
        /// Gets the convex hull of multiple points.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <returns>The convex hull.</returns>
        public static List<Point3d> GetConvexHull(List<Point3d> source)
        {
            var points = new List<Point3d>();
            var collection = new List<Point3d>();
            var num = 0;
            source.Sort((p1, p2) => (p1.X - p2.X == 0) ? (int)(p1.Y - p2.Y) : (int)(p1.X - p2.X));

            points.Add(source[0]);
            points.Add(source[1]);
            for (num = 2; num <= (source.Count - 1); num++)
            {
                points.Add(source[num]);
                while ((points.Count >= 3) && !IsTurnRight(points[points.Count - 3], points[points.Count - 2], points[points.Count - 1]))
                {
                    points.RemoveAt(points.Count - 2);
                }
            }
            collection.Add(source[source.Count - 1]);
            collection.Add(source[source.Count - 2]);
            for (num = source.Count - 2; num >= 0; num--)
            {
                collection.Add(source[num]);
                while ((collection.Count >= 3) && !IsTurnRight(collection[collection.Count - 3], collection[collection.Count - 2], collection[collection.Count - 1]))
                {
                    collection.RemoveAt(collection.Count - 2);
                }
            }
            collection.RemoveAt(collection.Count - 1);
            collection.RemoveAt(0);
            points.AddRange(collection);
            return points;
        }

        private static bool IsTurnRight(Point3d px, Point3d py, Point3d pz)
        {
            double num = 0;
            num = ((pz.Y - py.Y) * (py.X - px.X)) - ((py.Y - px.Y) * (pz.X - py.X));
            return (num < 0f);
        }

        #endregion

        #region Vector algorithms

        /// <summary>
        /// Converts Vector2d to Vector3d.
        /// </summary>
        /// <param name="point">The Vector2d.</param>
        /// <returns>A Vector3d.</returns>
        public static Vector3d ToVector3d(this Vector2d point)
        {
            return new Vector3d(point.X, point.Y, 0);
        }

        /// <summary>
        /// Converts Vector3d to Vector2d.
        /// </summary>
        /// <param name="point">The Vector3d.</param>
        /// <returns>A Vector2d.</returns>
        public static Vector2d ToVector2d(this Vector3d point)
        {
            return new Vector2d(point.X, point.Y);
        }

        /// <summary>
        /// Gets the pseudo cross product (a.k.a. 'kross') of two Vector2ds.
        /// </summary>
        /// <param name="v1">The vector 1.</param>
        /// <param name="v2">The vector 2.</param>
        /// <returns>The pseudo cross product (a.k.a. 'kross').</returns>
        public static double Kross(this Vector2d v1, Vector2d v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        /// <summary>
        /// Gets the angle between two vectors within [0, Pi].
        /// </summary>
        /// <param name="v0">The vector 0.</param>
        /// <param name="v1">The vector 1.</param>
        /// <returns>The result.</returns>
        public static double ZeroToPiAngleTo(this Vector2d v0, Vector2d v1)
        {
            return v0.GetAngleTo(v1);
        }

        /// <summary>
        /// Gets the heading (direction) angle of a vector within [0, 2Pi].
        /// </summary>
        /// <param name="v0">The vector.</param>
        /// <returns>The result.</returns>
        public static double DirAngleZeroTo2Pi(this Vector2d v0)
        {
            double angle = v0.ZeroToPiAngleTo(Vector2d.XAxis);
            if (v0.Y < 0)
            {
                angle = 2 * Math.PI - angle;
            }
            return angle;
        }

        /// <summary>
        /// Gets the angle between two vectors within [0, 2Pi].
        /// </summary>
        /// <param name="v0">The vector 0.</param>
        /// <param name="v1">The vector 1.</param>
        /// <returns>The result.</returns>
        public static double ZeroTo2PiAngleTo(this Vector2d v0, Vector2d v1)
        {
            double angle0 = v0.DirAngleZeroTo2Pi();
            double angle1 = v1.DirAngleZeroTo2Pi();
            double angleDelta = angle1 - angle0;
            if (angleDelta < 0) angleDelta = angleDelta + 2 * Math.PI;
            return angleDelta;
        }

        /// <summary>
        /// Gets the angle between two vectors within [-Pi, Pi].
        /// </summary>
        /// <param name="v0">The vector 0.</param>
        /// <param name="v1">The vector 1.</param>
        /// <returns>The result.</returns>
        public static double MinusPiToPiAngleTo(this Vector2d v0, Vector2d v1)
        {
            double angle0 = v0.DirAngleZeroTo2Pi();
            double angle1 = v1.DirAngleZeroTo2Pi();
            double angleDelta = angle1 - angle0;
            if (angleDelta < -Math.PI) angleDelta = angleDelta + 2 * Math.PI;
            else if (angleDelta > Math.PI) angleDelta = angleDelta - 2 * Math.PI;
            return angleDelta;
        }

        #endregion

        #region Polyline algorithms

        /// <summary>
        /// Gets all vertices of a polyline.
        /// </summary>
        /// <remarks>
        /// For a polyline, the difference between this method and `GetPoints()` is when `IsClosed=true`.
        /// </remarks>
        /// <param name="poly">The polyline.</param>
        /// <returns>The points.</returns>
        public static IEnumerable<Point3d> GetPolyPoints(this Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                yield return poly.GetPoint3dAt(i);
            }
        }

        /// <summary>
        /// Determines if the polyline is self-intersecting.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <returns>The result.</returns>
        public static bool IsSelfIntersecting(this Polyline poly) // newly by WY 20130202
        {
            var points = poly.GetPolyPoints().ToList();
            for (int i = 0; i < points.Count - 3; i++)
            {
                var a1 = points[i].ToPoint2d();
                var a2 = points[i + 1].ToPoint2d();
                for (var j = i + 2; j < points.Count - 1; j++)
                {
                    var b1 = points[j].ToPoint2d();
                    var b2 = points[j + 1].ToPoint2d();
                    if (IsLineSegIntersect(a1, a2, b1, b2))
                    {
                        if (i == 0 && j == points.Count - 2)
                        {
                            // NOTE: If they happen to be the first and the last, check if polyline is closed. A closed polyline is not considered self-intersecting.
                            if (points.First().DistanceTo(points.Last()) > Consts.Epsilon)
                            {
                                return true;
                            }
                            continue;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if two line segments intersect.
        /// </summary>
        /// <param name="a1">Line a point 1.</param>
        /// <param name="a2">Line a point 2.</param>
        /// <param name="b1">Line b point 1.</param>
        /// <param name="b2">Line b point 2.</param>
        /// <returns>The result.</returns>
        public static bool IsLineSegIntersect(Point2d a1, Point2d a2, Point2d b1, Point2d b2)
        {
            if ((a1 - a2).Kross(b1 - b2) == 0)
            {
                return false;
            }

            double lambda = 0;
            double miu = 0;

            if (b1.X == b2.X)
            {
                lambda = (b1.X - a1.X) / (a2.X - b1.X);
                double Y = (a1.Y + lambda * a2.Y) / (1 + lambda);
                miu = (Y - b1.Y) / (b2.Y - Y);
            }
            else if (a1.X == a2.X)
            {
                miu = (a1.X - b1.X) / (b2.X - a1.X);
                double Y = (b1.Y + miu * b2.Y) / (1 + miu);
                lambda = (Y - a1.Y) / (a2.Y - Y);
            }
            else if (b1.Y == b2.Y)
            {
                lambda = (b1.Y - a1.Y) / (a2.Y - b1.Y);
                double X = (a1.X + lambda * a2.X) / (1 + lambda);
                miu = (X - b1.X) / (b2.X - X);
            }
            else if (a1.Y == a2.Y)
            {
                miu = (a1.Y - b1.Y) / (b2.Y - a1.Y);
                double X = (b1.X + miu * b2.X) / (1 + miu);
                lambda = (X - a1.X) / (a2.X - X);
            }
            else
            {
                lambda = (b1.X * a1.Y - b2.X * a1.Y - a1.X * b1.Y + b2.X * b1.Y + a1.X * b2.Y - b1.X * b2.Y) / (-b1.X * a2.Y + b2.X * a2.Y + a2.X * b1.Y - b2.X * b1.Y - a2.X * b2.Y + b1.X * b2.Y);
                miu = (-a2.X * a1.Y + b1.X * a1.Y + a1.X * a2.Y - b1.X * a2.Y - a1.X * b1.Y + a2.X * b1.Y) / (a2.X * a1.Y - b2.X * a1.Y - a1.X * a2.Y + b2.X * a2.Y + a1.X * b2.Y - a2.X * b2.Y); // from Mathematica
            }

            bool result = false;
            if (lambda >= 0 || double.IsInfinity(lambda))
            {
                if (miu >= 0 || double.IsInfinity(miu))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Cuts a closed polyline into two closed halves with a straight line
        /// </summary>
        /// <param name="loop">The closed polyline.</param>
        /// <param name="cut">The cutting line.</param>
        /// <returns>The result.</returns>
        public static Polyline[] CutLoopToHalves(Polyline loop, Line cut)
        {
            if (loop.EndPoint != loop.StartPoint)
            {
                return new Polyline[0];
            }
            var points = new Point3dCollection();
            loop.IntersectWith3264(cut, Autodesk.AutoCAD.DatabaseServices.Intersect.ExtendArgument, points);
            if (points.Count != 2)
            {
                return new Polyline[0];
            }
            double a, b;
            if (loop.GetParamAtPointX(points[0]) < loop.GetParamAtPointX(points[1]))
            {
                a = loop.GetParamAtPointX(points[0]);
                b = loop.GetParamAtPointX(points[1]);
            }
            else
            {
                a = loop.GetParamAtPointX(points[1]);
                b = loop.GetParamAtPointX(points[0]);
            }
            var poly1 = new Polyline();
            var poly2 = new Polyline();

            // The half without the polyline start/end point.
            poly2.AddVertexAt(0, loop.GetPointAtParameter(a).ToPoint2d(), loop.GetBulgeBetween(a, Math.Ceiling(a)), 0, 0);
            int i = 1;
            for (int n = (int)Math.Ceiling(a); n < b - 1; n++)
            {
                poly2.AddVertexAt(i, loop.GetPointAtParameter(n).ToPoint2d(), loop.GetBulgeAt(n), 0, 0);
                i++;
            }
            poly2.AddVertexAt(i, loop.GetPointAtParameter(Math.Floor(b)).ToPoint2d(), loop.GetBulgeBetween(Math.Floor(b), b), 0, 0);
            poly2.AddVertexAt(i + 1, loop.GetPointAtParameter(b).ToPoint2d(), 0, 0, 0);
            poly2.AddVertexAt(i + 2, loop.GetPointAtParameter(a).ToPoint2d(), 0, 0, 0);

            // The half with the polyline start/end point.
            poly1.AddVertexAt(0, loop.GetPointAtParameter(b).ToPoint2d(), loop.GetBulgeBetween(b, Math.Ceiling(b)), 0, 0);
            int j = 1;
            for (int n = (int)Math.Ceiling(b); n < loop.EndParam; n++)
            {
                poly1.AddVertexAt(j, loop.GetPointAtParameter(n).ToPoint2d(), loop.GetBulgeAt(n), 0, 0);
                j++;
            }
            for (int n = 0; n < a - 1; n++)
            {
                poly1.AddVertexAt(j, loop.GetPointAtParameter(n).ToPoint2d(), loop.GetBulgeAt(n), 0, 0);
                j++;
            }
            poly1.AddVertexAt(j, loop.GetPointAtParameter(Math.Floor(a)).ToPoint2d(), loop.GetBulgeBetween(Math.Floor(a), a), 0, 0);
            poly1.AddVertexAt(j + 1, loop.GetPointAtParameter(a).ToPoint2d(), 0, 0, 0);
            poly1.AddVertexAt(j + 2, loop.GetPointAtParameter(b).ToPoint2d(), 0, 0, 0);

            return new Polyline[] { poly1, poly2 };
        }

        /// <summary>
        /// Gets the bulge between two parameters within the same arc segment of a polyline.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="startParam">The start parameter.</param>
        /// <param name="endParam">The end parameter.</param>
        /// <returns>The bulge.</returns>
        public static double GetBulgeBetween(this Polyline poly, double startParam, double endParam)
        {
            double total = poly.GetBulgeAt((int)Math.Floor(startParam));
            return (endParam - startParam) * total;
        }

        /// <summary>
        /// For a polyline with Closed=True, changes the value to False and closes it by adding a point.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <returns>The result polyline.</returns>
        public static Polyline TrulyClose(this Polyline poly)
        {
            if (poly.Closed == false)
            {
                return poly;
            }
            var result = poly.Clone() as Polyline;
            result.Closed = false;
            if (result.EndPoint != result.StartPoint)
            {
                result.AddVertexAt(poly.NumberOfVertices, poly.StartPoint.ToPoint2d(), 0, 0, 0);
            }
            return result;
        }

        /// <summary>
        /// Offsets a polyline.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="offsets">The offsets for each segments.</param>
        /// <returns>The result polyline.</returns>
        public static Polyline OffsetPoly(this Polyline poly, double[] offsets)
        {
            poly = poly.TrulyClose();

            var bulges = new List<double>();
            var segs1 = new List<Polyline>();
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var subPoly = new Polyline();
                subPoly.AddVertexAt(0, poly.GetPointAtParameter(i).ToPoint2d(), poly.GetBulgeAt(i), 0, 0);
                subPoly.AddVertexAt(1, poly.GetPointAtParameter(i + 1).ToPoint2d(), 0, 0, 0);
                var temp = subPoly.GetOffsetCurves(offsets[i]);
                if (temp.Count > 0)
                {
                    segs1.Add(temp[0] as Polyline);
                    bulges.Add(poly.GetBulgeAt(i));
                }
            }

            var points = new Point3dCollection();
            Enumerable.Range(0, segs1.Count).ForEach(x =>
            {
                int count = points.Count;
                int y = x + 1 < segs1.Count ? x + 1 : 0;
                segs1[x].IntersectWith3264(segs1[y], Autodesk.AutoCAD.DatabaseServices.Intersect.ExtendBoth, points);
                if (points.Count - count > 1) // This is an arc - more than 1 intersection point.
                {
                    var a = points[points.Count - 2];
                    var b = points[points.Count - 1];
                    if (segs1[x].EndPoint.DistanceTo(a) > segs1[x].EndPoint.DistanceTo(b))
                    {
                        points.Remove(a);
                    }
                    else
                    {
                        points.Remove(b);
                    }
                }
            });
            var result = new Polyline();

            int j = 0;
            points.Cast<Point3d>().ForEach(point =>
            {
                double bulge = j + 1 < points.Count ? bulges[j + 1] : 0;
                result.AddVertexAt(j, point.ToPoint2d(), bulge, 0, 0);
                j++;
            });

            if (poly.StartPoint == poly.EndPoint) // Closed polyline: add intersection to result.
            {
                result.AddVertexAt(0, points[points.Count - 1].ToPoint2d(), bulges[0], 0, 0);
            }

            else // Open polyline: add 2 offset points rather than the intersection to result.
            {
                result.AddVertexAt(0, segs1[0].StartPoint.ToPoint2d(), bulges[0], 0, 0);
                result.AddVertexAt(result.NumberOfVertices, segs1.Last().EndPoint.ToPoint2d(), 0, 0, 0);
                if (result.NumberOfVertices > 3)
                {
                    result.RemoveVertexAt(result.NumberOfVertices - 2); // Cannot be put before add - geometry will degrade.
                }
            }

            return result;
        }

        /// <summary>
        /// Gets arc bulge.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <param name="start">The start point.</param>
        /// <returns>The bulge.</returns>
        public static double GetArcBulge(this Arc arc, Point3d start)
        {
            double bulge;
            double angle = arc.EndAngle - arc.StartAngle;
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            if (arc.Normal.Z > 0)
            {
                bulge = Math.Tan(angle / 4);
            }
            else
            {
                bulge = -Math.Tan(angle / 4);
            }
            if (start == arc.EndPoint)
            {
                bulge = -bulge;
            }
            return bulge;
        }

        /// <summary>
        /// Converts line to polyline.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Line line)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, line.StartPoint.ToPoint2d(), 0, 0, 0);
            poly.AddVertexAt(1, line.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }

        /// <summary>
        /// Converts arc to polyline.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Arc arc)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, arc.StartPoint.ToPoint2d(), arc.GetArcBulge(arc.StartPoint), 0, 0);
            poly.AddVertexAt(1, arc.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }

        /// <summary>
        /// Cleans up a polyline by removing duplicate points.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <returns>The number of points removed.</returns>
        public static int PolyClean_RemoveDuplicatedVertex(Polyline poly)
        {
            var points = poly.GetPolyPoints().ToArray();
            var dupIndices = new List<int>();
            for (int i = points.Length - 2; i >= 0; i--)
            {
                if (points[i].DistanceTo(points[i + 1]) < Consts.Epsilon)
                {
                    dupIndices.Add(i);
                }
            }
            dupIndices.ForEach(index => poly.RemoveVertexAt(index));
            return dupIndices.Count;
        }

        /// <summary>
        /// Cleans up a polyline by removing approximate points.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="epsilon">The eplison.</param>
        /// <returns>The number of points removed.</returns>
        public static int PolyClean_ReducePoints(Polyline poly, double epsilon)
        {
            var points = poly.GetPolyPoints().ToArray();
            var cleanList = new List<int>();
            int j = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].DistanceTo(points[j]) < epsilon)
                {
                    cleanList.Add(i);
                }
                else
                {
                    j = i;
                }
            }
            cleanList.Reverse();
            cleanList.ForEach(index => poly.RemoveVertexAt(index));
            return cleanList.Count;
        }

        /// <summary>
        /// Cleans up a polyline by removing extra collinear points. 
        /// </summary>
        /// <param name="poly">The polyline.</param>
        public static void PolyClean_RemoveColinearPoints(Polyline poly)
        {
            // TODO: implement this.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cleans up a polyline by setting topo direction.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="dir">The direction.</param>
        /// <returns>A value indicating if a reversion is done.</returns>
        public static bool PolyClean_SetTopoDirection(Polyline poly, Direction dir)
        {
            if (poly.StartPoint == poly.EndPoint)
            {
                return false;
            }

            var reversed = false;
            var delta = poly.EndPoint - poly.StartPoint;
            switch (dir)
            {
                case Direction.West:
                    if (delta.X > 0)
                    {
                        poly.ReverseCurve();
                        reversed = true;
                    }
                    break;
                case Direction.North:
                    if (delta.Y < 0)
                    {
                        poly.ReverseCurve();
                        reversed = true;
                    }
                    break;
                case Direction.East:
                    if (delta.X < 0)
                    {
                        poly.ReverseCurve();
                        reversed = true;
                    }
                    break;
                case Direction.South:
                    if (delta.Y > 0)
                    {
                        poly.ReverseCurve();
                        reversed = true;
                    }
                    break;
                default:
                    break;
            }
            return reversed;
        }

        /// <summary>
        /// The direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// None.
            /// </summary>
            None,
            /// <summary>
            /// West.
            /// </summary>
            West,
            /// <summary>
            /// North.
            /// </summary>
            North,
            /// <summary>
            /// East.
            /// </summary>
            East,
            /// <summary>
            /// South.
            /// </summary>
            South
        }

        /// <summary>
        /// Connects polylines.
        /// </summary>
        /// <param name="poly">The base polyline.</param>
        /// <param name="poly1">The other polyline.</param>
        public static void JoinPolyline(this Polyline poly, Polyline poly1)
        {
            int index = poly.GetPolyPoints().Count();
            int index1 = 0;
            poly1.GetPoints().ForEach(point =>
            {
                poly.AddVertexAt(index, point.ToPoint2d(), poly1.GetBulgeAt(index1), 0, 0);
                index++;
                index1++;
            });
        }

        /// <summary>
        /// Connects polylines.
        /// </summary>
        /// <param name="poly1">The polyline 1.</param>
        /// <param name="poly2">The polyline 2.</param>
        /// <returns>The result polyline.</returns>
        public static Polyline PolyJoin(this Polyline poly1, Polyline poly2)
        {
            int index = 0;
            var poly = new Polyline();
            for (int i = 0; i < poly1.NumberOfVertices; i++)
            {
                if (i == poly1.NumberOfVertices - 1 && poly1.EndPoint == poly2.StartPoint)
                {
                }
                else
                {
                    poly.AddVertexAt(index, poly1.GetPoint2dAt(i), poly1.GetBulgeAt(i), 0, 0);
                    index++;
                }
            }
            for (int i = 0; i < poly2.NumberOfVertices; i++)
            {
                poly.AddVertexAt(index, poly2.GetPoint2dAt(i), poly2.GetBulgeAt(i), 0, 0);
                index++;
            }
            return poly;
        }

        /// <summary>
        /// Connects polyline.
        /// </summary>
        /// <param name="polys">The polylines.</param>
        /// <returns>The result polyline.</returns>
        public static Polyline PolyJoin(this List<Polyline> polys) // newly 20130807
        {
            return polys.Aggregate(Algorithms.PolyJoin);
        }

        /// <summary>
        /// Connects polylines, ignoring intermediate vertices.
        /// </summary>
        /// <param name="polys">The polylines.</param>
        /// <returns>The result polyline.</returns>
        public static Polyline PolyJoinIgnoreMiddleVertices(this List<Polyline> polys) // newly 20130807
        {
            var points = polys
                .SelectMany(p => new[] { p.StartPoint, p.EndPoint })
                .Distinct()
                .ToList();
            var globalWidth = 0;
            var vertices = points.Select(point => Tuple.Create(point, 0d)).ToList();
            var poly = new Polyline();
            Enumerable
                .Range(0, vertices.Count)
                .ForEach(index => poly.AddVertexAt(
                    index: index,
                    pt: vertices[index].Item1.ToPoint2d(),
                    bulge: vertices[index].Item2,
                    startWidth: globalWidth,
                    endWidth: globalWidth));
            return poly;
        }

        /// <summary>
        /// Determines if a point is in a polygon (defined by a polyline).
        /// </summary>
        /// <param name="poly">The polygon.</param>
        /// <param name="p">The point.</param>
        /// <returns>The result.</returns>
        public static bool IsPointIn(this Polyline poly, Point3d p)
        {
            double temp = 0;
            var points = poly.GetPoints().ToList();
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i < points.Count - 1) ? (i + 1) : 0;
                var v1 = points[i].ToPoint2d() - p.ToPoint2d();
                var v2 = points[j].ToPoint2d() - p.ToPoint2d();
                temp += v1.MinusPiToPiAngleTo(v2);
            }
            if (Math.Abs(Math.Abs(temp) - 2 * Math.PI) < 0.1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the centroid of a polyline.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <returns>The centroid.</returns>
        public static Point3d Centroid(this Polyline poly) // newly 20130801
        {
            var points = poly.GetPoints().ToList();
            if (points.Count == 1)
            {
                return points[0];
            }
            else
            {
                var temp = Point3d.Origin;
                double areaTwice = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    int j = (i < points.Count - 1) ? (i + 1) : 0;
                    var v1 = points[i].GetAsVector();
                    var v2 = points[j].GetAsVector();
                    temp += v1.CrossProduct(v2).Z / 3.0 * (v1 + v2);
                    areaTwice += v1.CrossProduct(v2).Z;
                }
                return (1.0 / areaTwice) * temp;
            }
        }

        #endregion

        #region Miscellaneous algorithms

        /// <summary>
        /// Gets the transformation matrix of world coordinates to viewport coordinates
        /// </summary>
        /// <param name="viewCenter">The view center.</param>
        /// <param name="viewportCenter">The viewport center.</param>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        public static Matrix3d WorldToViewport(Point3d viewCenter, Point3d viewportCenter, double scale)
        {
            return Matrix3d
                .Scaling(1.0 / scale, viewportCenter)
                .PostMultiplyBy(Matrix3d.Displacement(viewportCenter - viewCenter));
        }

        /// <summary>
        /// Intersects entities.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entOther">The other entity.</param>
        /// <param name="intersectType">The type.</param>
        /// <param name="points">The intersection points output.</param>
        internal static void IntersectWith3264(this Entity entity, Entity entOther, Intersect intersectType, Point3dCollection points)
        {
            // NOTE: Use runtime binding for difference between 32- and 64-bit APIs.
            var methodInfo = typeof(Entity).GetMethod("IntersectWith",
                new Type[] { typeof(Entity), typeof(Intersect), typeof(Point3dCollection), typeof(long), typeof(long) });
            if (methodInfo == null) // 32-bit AutoCAD
            {
                methodInfo = typeof(Entity).GetMethod("IntersectWith",
                new Type[] { typeof(Entity), typeof(Intersect), typeof(Point3dCollection), typeof(int), typeof(int) });
            }
            methodInfo.Invoke(entity, new object[] { entOther, intersectType, points, 0, 0 });
        }

        /// <summary>
        /// Intersects entities.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entOther">The other entity.</param>
        /// <param name="intersectType">The type.</param>
        /// <returns>The intersection points.</returns>
        public static List<Point3d> Intersect(this Entity entity, Entity entOther, Intersect intersectType)
        {
            var points = new Point3dCollection();
            Algorithms.IntersectWith3264(entity, entOther, intersectType, points);
            return points.Cast<Point3d>().ToList();
        }

        /// <summary>
        /// Double sequence generator.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="step">The step.</param>
        /// <returns>The result.</returns>
        public static IEnumerable<double> Range(double start, double end, double step = 1)
        {
            for (double x = start; x <= end; x += step)
            {
                yield return x;
            }
        }

        /// <summary>
        /// For each loop.
        /// </summary>
        /// <typeparam name="T">The element type of source.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }

        /// <summary>
        /// Gets the total length of the part of curves within a polygon.
        /// </summary>
        /// <param name="curves">The curves.</param>
        /// <param name="poly">The polygon.</param>
        /// <returns>The result.</returns>
        public static double GetCurveTotalLengthInPolygon(IEnumerable<Curve> curves, Polyline poly) // newly 20130514
        {
            return Algorithms.GetCurveTotalLength(curves, p => poly.IsPointIn(p));
        }

        /// <summary>
        /// Gets the total length of the part of curves within extents.
        /// </summary>
        /// <param name="curves">The curves.</param>
        /// <param name="extents">The extents.</param>
        /// <returns>The result.</returns>
        public static double GetCurveTotalLengthInExtents(IEnumerable<Curve> curves, Extents3d extents) // newly 20130514
        {
            return Algorithms.GetCurveTotalLength(curves, p => extents.IsPointIn(p));
        }

        private static double GetCurveTotalLength(IEnumerable<Curve> curves, Func<Point3d, bool> isIn, int divs = 100) // newly 20130514
        {
            double totalLength = 0;
            foreach (var curve in curves)
            {
                double length = curve.GetDistAtParam(curve.EndParam);
                double divLength = length / divs;
                var points = Enumerable.Range(0, divs + 1).Select(i => curve.GetPointAtParam(i * divLength)).ToList();
                for (int i = 0; i < divs; i++)
                {
                    if (isIn(points[i]) && isIn(points[i + 1]))
                    {
                        totalLength += points[i].DistanceTo(points[i + 1]);
                    }
                }
            }
            return totalLength;
        }

        /// <summary>
        /// Converts hatch to polyline.
        /// </summary>
        /// <param name="hatch">The hatch.</param>
        /// <returns>The result polylines.</returns>
        public static List<Polyline> HatchToPline(Hatch hatch) // newly 20130729
        {
            var plines = new List<Polyline>();
            int loopCount = hatch.NumberOfLoops;
            //System.Diagnostics.Debug.Write(loopCount);
            for (int index = 0; index < loopCount;)
            {
                if (hatch.GetLoopAt(index).IsPolyline)
                {
                    var loop = hatch.GetLoopAt(index).Polyline;
                    var p = new Polyline();
                    int i = 0;
                    loop.Cast<BulgeVertex>().ForEach(y =>
                    {
                        p.AddVertexAt(i, y.Vertex, y.Bulge, 0, 0);
                        i++;
                    });
                    plines.Add(p);
                    break;
                }
                else
                {
                    var loop = hatch.GetLoopAt(index).Curves;
                    var p = new Polyline();
                    int i = 0;
                    loop.Cast<Curve2d>().ForEach(y =>
                    {
                        p.AddVertexAt(i, y.StartPoint, 0, 0, 0);
                        i++;
                        if (y == loop.Cast<Curve2d>().Last())
                        {
                            p.AddVertexAt(i, y.EndPoint, 0, 0, 0);
                        }
                    });
                    plines.Add(p);
                    break;
                }
            }
            return plines;
        }

        #endregion
    }
    /// <summary>
    /// Constants.
    /// </summary>
    public static class Consts
    {
        /// <summary>
        /// Universal tolerance.
        /// </summary>
        public const double Epsilon = 0.001;
        /// <summary>
        /// Default text style.
        /// </summary>
        public const string TextStyleName = "AutoCADCodePackTextStyle";
        /// <summary>
        /// FXD AppName for code.
        /// </summary>
        public const string AppNameForCode = "TongJiCode"; // like HTML tag name
        /// <summary>
        /// FXD AppName for ID.
        /// </summary>
        public const string AppNameForID = "TongJiID"; // like HTML id
        /// <summary>
        /// FXD AppName for name.
        /// </summary>
        public const string AppNameForName = "TongJiName"; // like HTML id or name
        /// <summary>
        /// FXD AppName for tags.
        /// </summary>
        public const string AppNameForTags = "TongJiTags"; // like HTML class
    }
    /// <summary>
    /// Interval.
    /// </summary>
    public class Interv
    {
        /// <summary>
        /// The lower limit.
        /// </summary>
        public double Start { get; }

        /// <summary>
        /// The upper limit.
        /// </summary>
        public double End { get; }

        /// <summary>
        /// The length.
        /// </summary>
        public double Length => this.End - this.Start;

        /// <summary>
        /// Creates an interval by specifying start and end.
        /// </summary>
        /// <param name="start">The lower limit.</param>
        /// <param name="end">The uppper limit.</param>
        public Interv(double start, double end)
        {
            this.Start = start;
            this.End = end;
        }

#if R23
        /// <summary>
        /// Creates an interval from a C# 7.0 tuple.
        /// </summary>
        /// <param name="tuple">The tuple.</param>
        public Interv((double, double) tuple)
            : this(tuple.Item1, tuple.Item2)
        {
        }
#endif

        /// <summary>
        /// Adds point to interval.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The result (new interval).</returns>
        public Interv AddPoint(double point)
        {
            if (point > End)
            {
                return new Interv(Start, point);
            }
            else if (point < Start)
            {
                return new Interv(point, End);
            }

            return this;
        }

        /// <summary>
        /// Adds interval to interval.
        /// </summary>
        /// <param name="interval">The added interval.</param>
        /// <returns>The result (new interval).</returns>
        public Interv AddInterval(Interv interval)
        {
            return this.AddPoint(interval.Start).AddPoint(interval.End);
        }

        /// <summary>
        /// Determines if a point is in the interval.
        /// </summary>
        /// <param name="point">The value.</param>
        /// <returns>A value indicating whether the point is in.</returns>
        public bool IsPointIn(double point)
        {
            return point >= this.Start && point <= this.End;
        }
    }
    /// <summary>
    /// Provides extension methods for the Point3d type.
    /// </summary>
    public static class Point3dExtensions
    {
        /// <summary>
        /// Converts a 3d point into a 2d point (projection on XY plane).
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <returns>The corresponding 2d point.</returns>
        public static Point2d Convert2d(this Point3d pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        /// <summary>
        /// Projects the point on the WCS XY plane.
        /// </summary>
        /// <param name="pt">The point to be projected.</param>
        /// <returns>The projected point</returns>
        public static Point3d Flatten(this Point3d pt)
        {
            return new Point3d(pt.X, pt.Y, 0.0);
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on the segment defined by two points.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="p1">The segment start point.</param>
        /// <param name="p2">The segment end point.</param>
        /// <returns>true if the point is on the segment; otherwise, false.</returns>
        public static bool IsBetween(this Point3d pt, Point3d p1, Point3d p2)
        {
            return p1.GetVectorTo(pt).GetNormal().Equals(pt.GetVectorTo(p2).GetNormal());
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on the segment defined by two points.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="p1">The segment start point.</param>
        /// <param name="p2">The segment end point.</param>
        /// <param name="tol">The tolerance used in comparisons.</param>
        /// <returns>true if the point is on the segment; otherwise, false.</returns>
        public static bool IsBetween(this Point3d pt, Point3d p1, Point3d p2, Tolerance tol)
        {
            return p1.GetVectorTo(pt).GetNormal(tol).Equals(pt.GetVectorTo(p2).GetNormal(tol));
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is inside the extents.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="extents">The extents 3d supposed to contain the point.</param>
        /// <returns>true if the point is inside the extents; otherwise, false.</returns>
        public static bool IsInside(this Point3d pt, Extents3d extents)
        {
            return
                pt.X >= extents.MinPoint.X &&
                pt.Y >= extents.MinPoint.Y &&
                pt.Z >= extents.MinPoint.Z &&
                pt.X <= extents.MaxPoint.X &&
                pt.Y <= extents.MaxPoint.Y &&
                pt.Z <= extents.MaxPoint.Z;
        }

        /// <summary>
        /// Defines a point with polar coordinates from an origin point.
        /// </summary>
        /// <param name="org">The instance to which the method applies.</param>
        /// <param name="angle">The angle about the X axis.</param>
        /// <param name="distance">The distance from the origin</param>
        /// <returns>The new 3d point.</returns>
        public static Point3d Polar(this Point3d org, double angle, double distance)
        {
            return new Point3d(
                org.X + (distance * Math.Cos(angle)),
                org.Y + (distance * Math.Sin(angle)),
                org.Z);
        }
    }
    /// <summary>
    /// Provides extension methods for the Polyline2d type.
    /// </summary>
    public static class Polyline2dExtensions
    {
        /// <summary>
        /// Gets the linear 3d segment of the polyline 2d at specified index.
        /// </summary>
        /// <param name="pl">The instance to which the method applies.</param>
        /// <param name="index">The segment index.</param>
        /// <returns>A copy of the segment (WCS coordinates).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown the index is out of range.</exception>
        public static LineSegment3d GetLineSegmentAt(this Polyline2d pl, int index)
        {
            try
            {
                return new LineSegment3d(
                    pl.GetPointAtParameter(index),
                    pl.GetPointAtParameter(index + 1));
            }
            catch
            {
                throw new ArgumentOutOfRangeException("Out of range index");
            }
        }

        /// <summary>
        /// Gets the linear 2d segment of the polyline 2d at specified index.
        /// </summary>
        /// <param name="pl">The instance to which the method applies.</param>
        /// <param name="index">The segment index.</param>
        /// <returns>A copy of the segment (OCS coordinates).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown the index is out of range.</exception>
        public static LineSegment2d GetLineSegment2dAt(this Polyline2d pl, int index)
        {
            try
            {
                Matrix3d WCS2ECS = pl.Ecs.Inverse();
                return new LineSegment2d(
                    pl.GetPointAtParameter(index).TransformBy(WCS2ECS).Convert2d(),
                    pl.GetPointAtParameter(index + 1.0).TransformBy(WCS2ECS).Convert2d());
            }
            catch
            {
                throw new ArgumentOutOfRangeException("Out of range index");
            }
        }

        /// <summary>
        /// Gets the arc 3d segment of the polyline 2d at specified index.
        /// </summary>
        /// <param name="pl">The instance to which the method applies.</param>
        /// <param name="index">The segment index.</param>
        /// <returns>A copy of the segment (WCS coordinates).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown the index is out of range.</exception>
        public static CircularArc3d GetArcSegmentAt(this Polyline2d pl, int index)
        {
            try
            {
                return new CircularArc3d(
                    pl.GetPointAtParameter(index),
                    pl.GetPointAtParameter(index + 0.5),
                    pl.GetPointAtParameter(index + 1));
            }
            catch
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }

        /// <summary>
        /// Gets the arc 2d segment of the polyline 2d at specified index.
        /// </summary>
        /// <param name="pl">The instance to which the method applies.</param>
        /// <param name="index">The segment index.</param>
        /// <returns>A copy of the segment (OCS coordinates).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown the index is out of range.</exception>
        public static CircularArc2d GetArcSegment2dAt(this Polyline2d pl, int index)
        {
            try
            {
                Matrix3d WCS2ECS = pl.Ecs.Inverse();
                return new CircularArc2d(
                    pl.GetPointAtParameter(index).TransformBy(WCS2ECS).Convert2d(),
                    pl.GetPointAtParameter(index + 0.5).TransformBy(WCS2ECS).Convert2d(),
                    pl.GetPointAtParameter(index + 1.0).TransformBy(WCS2ECS).Convert2d());
            }
            catch
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }
    }
    /// <summary>
    /// Provides methods for the derived classes.
    /// </summary>
    /// <typeparam name="T">The type of elements in the triangle.</typeparam>
    public abstract class Triangle<T> : IEnumerable<T>
    {
        #region Fields

        /// <summary>
        /// The first triangle element (origin).
        /// </summary>
        protected T _pt0;

        /// <summary>
        /// The second triangle element.
        /// </summary>
        protected T _pt1;

        /// <summary>
        /// The third triangle element.
        /// </summary>
        protected T _pt2;

        /// <summary>
        /// An array containing the three triangle elements.
        /// </summary>
        protected T[] _pts = new T[3];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Triangle &lt;T&gt; that is empty.
        /// </summary>
        protected internal Triangle()
        {
        }

        /// <summary>
        /// Initializes a new instance of Triangle &lt;T&gt; that contains elements copied from the specified array.
        /// </summary>
        /// <param name="pts">The array whose elements are copied to the new Triangle.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown if the array do not contains three items.</exception>
        protected internal Triangle(T[] pts)
        {
            if (pts.Length != 3)
                throw new ArgumentOutOfRangeException("The array must contain 3 items");
            _pts[0] = _pt0 = pts[0];
            _pts[1] = _pt1 = pts[1];
            _pts[2] = _pt2 = pts[2];
        }

        /// <summary>
        /// Initializes a new instance of Triangle &lt;T&gt; that contains the specified elements.
        /// </summary>
        /// <param name="a">First element to be copied in the new Triangle.</param>
        /// <param name="b">Second element to be copied in the new Triangle.</param>
        /// <param name="c">Third element to be copied in the new Triangle.</param>
        protected internal Triangle(T a, T b, T c)
        {
            _pts[0] = _pt0 = a;
            _pts[1] = _pt1 = b;
            _pts[2] = _pt2 = c;
        }

        #endregion

        #region Indexor

        /// <summary>
        /// Item
        /// </summary>
        /// <param name="i">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// IndexOutOfRangeException is throw if index is less than 0 or more than 2.</exception>
        public T this[int i]
        {
            get
            {
                if (i > 2 || i < 0)
                    throw new IndexOutOfRangeException("Index out of range");
                return _pts[i];
            }
            set
            {
                if (i > 2 || i < 0)
                    throw new IndexOutOfRangeException("Index out of range");
                _pts[i] = value;
                this.Set(_pts);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the specified Triangle&lt;T&gt; derived types instances are considered equal.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>true if every corresponding element in both Triangle&lt;T&gt; are considered equal; otherwise, nil.</returns>
        public override bool Equals(object obj)
        {
            Triangle<T> trgl = obj as Triangle<T>;
            return
                trgl != null &&
                trgl.GetHashCode() == this.GetHashCode() &&
                trgl[0].Equals(_pt0) &&
                trgl[1].Equals(_pt1) &&
                trgl[2].Equals(_pt2);
        }

        /// <summary>
        /// Serves as a hash function for Triangle&lt;T&gt; derived types.
        /// </summary>
        /// <returns>A hash code for the current Triangle&lt;T&gt;.</returns>
        public override int GetHashCode()
        {
            return _pt0.GetHashCode() ^ _pt1.GetHashCode() ^ _pt2.GetHashCode();
        }

        /// <summary>
        /// Reverses the order without changing the origin (swaps the 2nd and 3rd elements)
        /// </summary>
        public void Inverse()
        {
            this.Set(_pt0, _pt2, _pt1);
        }

        /// <summary>
        /// Sets the elements of the triangle.
        /// </summary>
        /// <param name="pts">The array whose elements are copied to the Triangle.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// ArgumentOutOfRangeException is thrown if the array do not contains three items.</exception>
        public void Set(T[] pts)
        {
            if (pts.Length != 3)
                throw new IndexOutOfRangeException("The array must contain 3 items");
            _pts[0] = _pt0 = pts[0];
            _pts[1] = _pt1 = pts[1];
            _pts[2] = _pt2 = pts[2];
        }

        /// <summary>
        /// Sets the elements of the triangle.
        /// </summary>
        /// <param name="a">First element to be copied in the Triangle.</param>
        /// <param name="b">Second element to be copied in the Triangle.</param>
        /// <param name="c">Third element to be copied in the Triangle.</param>
        public void Set(T a, T b, T c)
        {
            _pts[0] = _pt0 = a;
            _pts[1] = _pt1 = b;
            _pts[2] = _pt2 = c;
        }

        /// <summary>
        /// Converts the triangle into an array.
        /// </summary>
        /// <returns>An array of three elements.</returns>
        public T[] ToArray()
        {
            return _pts;
        }

        /// <summary>
        /// Applies ToString() to each element and concatenate the results separted with commas.
        /// </summary>
        /// <returns>A string containing the three elements separated with commas.</returns>
        public override string ToString()
        {
            return string.Format("{0},{1},{2}", _pt0, _pt1, _pt2);
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the triangle.
        /// </summary>
        /// <returns>An IEnumerable&lt;T&gt; enumerator for the Triangle&lt;T&gt;.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (T item in _pts) yield return item;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
    /// <summary>
    /// Represents a triangle in a 2d plane. It can be viewed as a structure consisting of three Point2d.
    /// </summary>
    public class Triangle2d : Triangle<Point2d>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Triangle2d that is empty.
        /// </summary>
        public Triangle2d() : base() { }

        /// <summary>
        /// Initializes a new instance of Triangle2d that contains elements copied from the specified array.
        /// </summary>
        /// <param name="pts">The Point2d array whose elements are copied to the new Triangle2d.</param>
        public Triangle2d(Point2d[] pts) : base(pts) { }

        /// <summary>
        /// Initializes a new instance of Triangle2d that contains the specified elements.
        /// </summary>
        /// <param name="a">The first vertex of the new Triangle2d (origin).</param>
        /// <param name="b">The second vertex of the new Triangle2d (2nd vertex).</param>
        /// <param name="c">The third vertex of the new Triangle2d (3rd vertex).</param>
        public Triangle2d(Point2d a, Point2d b, Point2d c) : base(a, b, c) { }

        /// <summary>
        /// Initializes a new instance of Triangle2d according to an origin and two vectors.
        /// </summary>
        /// <param name="org">The origin of the Triangle2d (1st vertex).</param>
        /// <param name="v1">The vector from origin to the second vertex.</param>
        /// <param name="v2">The vector from origin to the third vertex.</param>
        public Triangle2d(Point2d org, Vector2d v1, Vector2d v2)
        {
            _pts[0] = _pt0 = org;
            _pts[1] = _pt1 = org + v1;
            _pts[2] = _pt2 = org + v2;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the triangle signed area.
        /// </summary>
        public double SignedArea
        {
            get
            {
                return (((_pt1.X - _pt0.X) * (_pt2.Y - _pt0.Y)) -
                    ((_pt2.X - _pt0.X) * (_pt1.Y - _pt0.Y))) / 2.0;
            }
        }

        /// <summary>
        /// Gets the triangle centroid.
        /// </summary>
        public Point2d Centroid
        {
            get { return (_pt0 + _pt1.GetAsVector() + _pt2.GetAsVector()) / 3.0; }
        }

        /// <summary>
        /// Gets the circumscribed circle.
        /// </summary>
        public CircularArc2d CircumscribedCircle
        {
            get
            {
                Line2d l1 = this.GetSegmentAt(0).GetBisector();
                Line2d l2 = this.GetSegmentAt(1).GetBisector();
                Point2d[] inters = l1.IntersectWith(l2);
                if (inters == null)
                    return null;
                return new CircularArc2d(inters[0], inters[0].GetDistanceTo(this._pt0));
            }
        }

        /// <summary>
        /// Gets the inscribed circle.
        /// </summary>
        public CircularArc2d InscribedCircle
        {
            get
            {
                Vector2d v1 = _pt0.GetVectorTo(_pt1).GetNormal();
                Vector2d v2 = _pt0.GetVectorTo(_pt2).GetNormal();
                Vector2d v3 = _pt1.GetVectorTo(_pt2).GetNormal();
                if (v1.IsEqualTo(v2) || v2.IsEqualTo(v3))
                    return null;
                Line2d l1 = new Line2d(_pt0, v1 + v2);
                Line2d l2 = new Line2d(_pt1, v1.Negate() + v3);
                Point2d[] inters = l1.IntersectWith(l2);
                return new CircularArc2d(inters[0], this.GetSegmentAt(0).GetDistanceTo(inters[0]));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the triangle vertices are clockwise.
        /// </summary>
        public bool IsClockwise
        {
            get { return (this.SignedArea < 0.0); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts the triangle into a Triangle3d according to the specified plane.
        /// </summary>
        /// <param name="plane">Plane of the Triangle3d.</param>
        /// <returns>The new Triangle3d.</returns>
        public Triangle3d Convert3d(Plane plane)
        {
            return new Triangle3d(
                Array.ConvertAll<Point2d, Point3d>(_pts, x => x.Convert3d(plane)));
        }

        /// <summary>
        /// Converts the triangle into a Triangle3d according to the plane defined by its normal and elevation.
        /// </summary>
        /// <param name="normal">The normal vector of the plane.</param>
        /// <param name="elevation">The elevation of the plane.</param>
        /// <returns>The new Triangle3d.</returns>
        public Triangle3d Convert3d(Vector3d normal, double elevation)
        {
            return new Triangle3d(
                Array.ConvertAll<Point2d, Point3d>(_pts, x => x.Convert3d(normal, elevation)));
        }

        /// <summary>
        /// Gets the angle between the two segments at specified vertex.
        /// </summary>.
        /// <param name="index">The vertex index.</param>
        /// <returns>The angle expressed in radians.</returns>
        public double GetAngleAt(int index)
        {
            double pi = 3.141592653589793;
            double ang =
                this[index].GetVectorTo(this[(index + 1) % 3]).GetAngleTo(
                this[index].GetVectorTo(this[(index + 2) % 3]));
            if (ang > pi * 2)
                return pi * 2 - ang;
            else
                return ang;
        }

        /// <summary>
        /// Gets the segment at specified index.
        /// </summary>
        /// <param name="index">The segment index.</param>
        /// <returns>The segment 3d.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// IndexOutOfRangeException is thrown if index is less than 0 or more than 2.</exception>
        public LineSegment2d GetSegmentAt(int index)
        {
            if (index > 2)
                throw new IndexOutOfRangeException("Index out of range");
            return new LineSegment2d(this[index], this[(index + 1) % 3]);
        }

        /// <summary>
        /// Gets the intersection points between the triangle and the line.
        /// </summary>
        /// <param name="le2d">The line with which intersections are searched.</param>
        /// <returns>The intersection points list (an empty list if none).</returns>
        public List<Point2d> IntersectWith(LinearEntity2d le2d)
        {
            List<Point2d> result = new List<Point2d>();
            for (int i = 0; i < 3; i++)
            {
                Point2d[] inters = le2d.IntersectWith(this.GetSegmentAt(i));
                if (inters != null && inters.Length != 0 && !result.Contains(inters[0]))
                    result.Add(inters[0]);
            }

            return result;
        }

        /// <summary>
        /// Gets the intersection points between the triangle and the line.
        /// </summary>
        /// <param name="le2d">The line with which intersections are searched.</param>
        /// <param name="tol">The tolerance used in comparisons.</param>
        /// <returns>The intersection points list (an empty list if none).</returns>
        public List<Point2d> IntersectWith(LinearEntity2d le2d, Tolerance tol)
        {
            List<Point2d> result = new List<Point2d>();
            for (int i = 0; i < 3; i++)
            {
                Point2d[] inters = le2d.IntersectWith(this.GetSegmentAt(i), tol);
                if (inters != null && inters.Length != 0 && !result.Contains(inters[0]))
                    result.Add(inters[0]);
            }
            return result;
        }

        /// <summary>
        /// Checks if the distance between every respective Point2d in both Triangle2d is less than or equal to the Tolerance.Global.EqualPoint value.
        /// </summary>
        /// <param name="t2d">The triangle2d to compare.</param>
        /// <returns>true if the condition is met; otherwise, false.</returns>
        public bool IsEqualTo(Triangle2d t2d)
        {
            return this.IsEqualTo(t2d, Tolerance.Global);
        }

        /// <summary>
        /// Checks if the distance between every respective Point2d in both Triangle2d is less than or equal to the Tolerance.EqualPoint value of the specified tolerance.
        /// </summary>
        /// <param name="t2d">The triangle2d to compare.</param>
        /// <param name="tol">The tolerance used in points comparisons.</param>
        /// <returns>true if the condition is met; otherwise, false.</returns>
        public bool IsEqualTo(Triangle2d t2d, Tolerance tol)
        {
            return t2d[0].IsEqualTo(_pt0, tol) && t2d[1].IsEqualTo(_pt1, tol) && t2d[2].IsEqualTo(_pt2, tol);
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is strictly inside the triangle.
        /// </summary>
        /// <param name="pt">The point to be evaluated.</param>
        /// <returns>true if the point is inside; otherwise, false.</returns>
        public bool IsPointInside(Point2d pt)
        {
            if (this.IsPointOn(pt))
                return false;
            List<Point2d> inters = this.IntersectWith(new Ray2d(pt, Vector2d.XAxis));
            if (inters.Count != 1)
                return false;
            Point2d p = inters[0];
            return !p.IsEqualTo(this[0]) && !p.IsEqualTo(this[1]) && !p.IsEqualTo(this[2]);
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on a triangle segment.
        /// </summary>
        /// <param name="pt">The point to be evaluated.</param>
        /// <returns>Ttrue if the point is on a segment; otherwise, false.</returns>
        public bool IsPointOn(Point2d pt)
        {
            return
                pt.IsEqualTo(this[0]) ||
                pt.IsEqualTo(this[1]) ||
                pt.IsEqualTo(this[2]) ||
                pt.IsBetween(this[0], this[1]) ||
                pt.IsBetween(this[1], this[2]) ||
                pt.IsBetween(this[2], this[0]);
        }

        /// <summary>
        /// Sets the elements of the triangle using an origin and two vectors.
        /// </summary>
        /// <param name="org">The origin of the Triangle3d (1st vertex).</param>
        /// <param name="v1">The vector from origin to the second vertex.</param>
        /// <param name="v2">The vector from origin to the third vertex.</param>
        public void Set(Point2d org, Vector2d v1, Vector2d v2)
        {
            _pts[0] = _pt0 = org;
            _pts[1] = _pt1 = org + v1;
            _pts[2] = _pt2 = org + v2;
        }

        /// <summary>
        /// Transforms a Triangle2d with a transformation matrix
        /// </summary>
        /// <param name="mat">The 2d transformation matrix.</param>
        /// <returns>The new Triangle2d.</returns>
        public Triangle2d TransformBy(Matrix2d mat)
        {
            return new Triangle2d(Array.ConvertAll<Point2d, Point2d>(
                _pts, new Converter<Point2d, Point2d>(p => p.TransformBy(mat))));
        }

        #endregion
    }
    /// <summary>
    /// Provides extension methods for the Point2d type.
    /// </summary>
    public static class Point2dExtensions
    {
        /// <summary>
        /// Converts a 2d point into a 3d point with Z coodinate equal to 0.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <returns>The corresponding 3d point.</returns>
        public static Point3d Convert3d(this Point2d pt)
        {
            return new Point3d(pt.X, pt.Y, 0.0);
        }

        /// <summary>
        /// Converts a 2d point into a 3d point according to the specified plane.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="plane">The plane which the point lies on.</param>
        /// <returns>The corresponding 3d point</returns>
        public static Point3d Convert3d(this Point2d pt, Plane plane)
        {
            return new Point3d(pt.X, pt.Y, 0.0).TransformBy(Matrix3d.PlaneToWorld(plane));
        }

        /// <summary>
        /// Converts a 2d point into a 3d point according to the plane defined by
        /// the specified normal vector and elevation.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="normal">The normal vector of the plane which the point lies on.</param>
        /// <param name="elevation">The elevation of the plane which the point lies on.</param>
        /// <returns>The corresponding 3d point</returns>
        public static Point3d Convert3d(this Point2d pt, Vector3d normal, double elevation)
        {
            return new Point3d(pt.X, pt.Y, elevation).TransformBy(Matrix3d.PlaneToWorld(normal));
        }

        /// <summary>
        /// Projects the point on the WCS XY plane.
        /// </summary>
        /// <param name="pt">The point 2d to project.</param>
        /// <param name="normal">The normal vector of the entity which owns the point 2d.</param>
        /// <returns>The transformed Point2d.</returns>
        public static Point2d Flatten(this Point2d pt, Vector3d normal)
        {
            return new Point3d(pt.X, pt.Y, 0.0)
                .TransformBy(Matrix3d.PlaneToWorld(normal))
                .Convert2d(new Plane());
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on the segment defined by two points.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="p1">The segment start point.</param>
        /// <param name="p2">The segment end point.</param>
        /// <returns>true if the point is on the segment; otherwise, false.</returns>
        public static bool IsBetween(this Point2d pt, Point2d p1, Point2d p2)
        {
            return p1.GetVectorTo(pt).GetNormal().Equals(pt.GetVectorTo(p2).GetNormal());
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on the segment defined by two points.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="p1">The segment start point.</param>
        /// <param name="p2">The segment end point.</param>
        /// <param name="tol">The tolerance used in comparisons.</param>
        /// <returns>true if the point is on the segment; otherwise, false.</returns>
        public static bool IsBetween(this Point2d pt, Point2d p1, Point2d p2, Tolerance tol)
        {
            return p1.GetVectorTo(pt).GetNormal(tol).Equals(pt.GetVectorTo(p2).GetNormal(tol));
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is inside the extents.
        /// </summary>
        /// <param name="pt">The instance to which the method applies.</param>
        /// <param name="extents">The extents 2d supposed to contain the point.</param>
        /// <returns>true if the point is inside the extents; otherwise, false.</returns>
        public static bool IsInside(this Point2d pt, Extents2d extents)
        {
            return
                pt.X >= extents.MinPoint.X &&
                pt.Y >= extents.MinPoint.Y &&
                pt.X <= extents.MaxPoint.X &&
                pt.Y <= extents.MaxPoint.Y;
        }

        /// <summary>
        /// Defines a point with polar coordinates from an origin point.
        /// </summary>
        /// <param name="org">The instance to which the method applies.</param>
        /// <param name="angle">The angle about the X axis.</param>
        /// <param name="distance">The distance from the origin</param>
        /// <returns>The new 2d point.</returns>
        public static Point2d Polar(this Point2d org, double angle, double distance)
        {
            return new Point2d(
                org.X + (distance * Math.Cos(angle)),
                org.Y + (distance * Math.Sin(angle)));
        }
    }
    /// <summary>
    /// Represents a triangle in the 3d space. It can be viewed as a structure consisting of three Point3d.
    /// </summary>
    public class Triangle3d : Triangle<Point3d>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Triangle3d; that is empty.
        /// </summary>
        public Triangle3d() : base() { }


        /// <summary>
        /// Initializes a new instance of Triangle3d that contains elements copied from the specified array.
        /// </summary>
        /// <param name="pts">The Point3d array whose elements are copied to the new Triangle3d.</param>
        public Triangle3d(Point3d[] pts) : base(pts) { }

        /// <summary>
        /// Initializes a new instance of Triangle3d that contains the specified elements.
        /// </summary>
        /// <param name="a">The first vertex of the new Triangle3d (origin).</param>
        /// <param name="b">The second vertex of the new Triangle3d (2nd vertex).</param>
        /// <param name="c">The third vertex of the new Triangle3d (3rd vertex).</param>
        public Triangle3d(Point3d a, Point3d b, Point3d c) : base(a, b, c) { }

        /// <summary>
        /// Initializes a new instance of Triangle3d according to an origin and two vectors.
        /// </summary>
        /// <param name="org">The origin of the Triangle3d (1st vertex).</param>
        /// <param name="v1">The vector from origin to the second vertex.</param>
        /// <param name="v2">The vector from origin to the third vertex.</param>
        public Triangle3d(Point3d org, Vector3d v1, Vector3d v2)
        {
            _pts[0] = _pt0 = org;
            _pts[0] = _pt1 = org + v1;
            _pts[0] = _pt2 = org + v2;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the triangle area.
        /// </summary>
        public double Area
        {
            get
            {
                return Math.Abs(
                    (((_pt1.X - _pt0.X) * (_pt2.Y - _pt0.Y)) -
                    ((_pt2.X - _pt0.X) * (_pt1.Y - _pt0.Y))) / 2.0);
            }
        }

        /// <summary>
        /// Gets the triangle centroid.
        /// </summary>
        public Point3d Centroid
        {
            get { return (_pt0 + _pt1.GetAsVector() + _pt2.GetAsVector()) / 3.0; }
        }

        /// <summary>
        /// Gets the circumscribed circle.
        /// </summary>
        public CircularArc3d CircumscribedCircle
        {
            get
            {
                CircularArc2d ca2d = this.Convert2d().CircumscribedCircle;
                if (ca2d == null)
                    return null;
                return new CircularArc3d(ca2d.Center.Convert3d(this.GetPlane()), this.Normal, ca2d.Radius);
            }
        }

        /// <summary>
        /// Gets the triangle plane elevation.
        /// </summary>
        public double Elevation
        {
            get { return _pt0.TransformBy(Matrix3d.WorldToPlane(this.Normal)).Z; }
        }

        /// <summary>
        /// Gets the unit vector of the triangle plane greatest slope.
        /// </summary>
        public Vector3d GreatestSlope
        {
            get
            {
                Vector3d norm = this.Normal;
                if (norm.IsParallelTo(Vector3d.ZAxis))
                    return new Vector3d(0.0, 0.0, 0.0);
                if (norm.Z == 0.0)
                    return Vector3d.ZAxis.Negate();
                return new Vector3d(-norm.Y, norm.X, 0.0).CrossProduct(norm).GetNormal();
            }
        }

        /// <summary>
        /// Gets the unit horizontal vector of the triangle plane.
        /// </summary>
        public Vector3d Horizontal
        {
            get
            {
                Vector3d norm = this.Normal;
                if (norm.IsParallelTo(Vector3d.ZAxis))
                    return Vector3d.XAxis;
                return new Vector3d(-norm.Y, norm.X, 0.0).GetNormal();
            }
        }

        /// <summary>
        /// Gets the inscribed circle.
        /// </summary>
        public CircularArc3d InscribedCircle
        {
            get
            {
                CircularArc2d ca2d = this.Convert2d().InscribedCircle;
                if (ca2d == null)
                    return null;
                return new CircularArc3d(ca2d.Center.Convert3d(this.GetPlane()), this.Normal, ca2d.Radius);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the triangle plane is horizontal.
        /// </summary>
        public bool IsHorizontal
        {
            get { return _pt0.Z == _pt1.Z && _pt0.Z == _pt2.Z; }
        }

        /// <summary>
        /// Gets the normal vector of the triangle plane.
        /// </summary>
        public Vector3d Normal
        {
            get { return (_pt1 - _pt0).CrossProduct(_pt2 - _pt0).GetNormal(); }
        }

        /// <summary>
        /// Gets the percent slope of the triangle plane.
        /// </summary>
        public double SlopePerCent
        {
            get
            {
                Vector3d norm = this.Normal;
                if (norm.Z == 0.0)
                    return Double.PositiveInfinity;
                return Math.Abs(100.0 * (Math.Sqrt(Math.Pow(norm.X, 2.0) + Math.Pow(norm.Y, 2.0))) / norm.Z);
            }
        }

        /// <summary>
        /// Gets the triangle coordinates system 
        /// (origin = centroid, X axis = horizontal vector, Y axis = negated geatest slope vector).
        /// </summary>
        public Matrix3d SlopeUCS
        {
            get
            {
                Point3d origin = this.Centroid;
                Vector3d zaxis = this.Normal;
                Vector3d xaxis = this.Horizontal;
                Vector3d yaxis = zaxis.CrossProduct(xaxis).GetNormal();
                return new Matrix3d(new double[]{
                    xaxis.X, yaxis.X, zaxis.X, origin.X,
                    xaxis.Y, yaxis.Y, zaxis.Y, origin.Y,
                    xaxis.Z, yaxis.Z, zaxis.Z, origin.Z,
                    0.0, 0.0, 0.0, 1.0});
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts a Triangle3d into a Triangle2d according to the Triangle3d plane.
        /// </summary>
        /// <returns>The resulting Triangle2d.</returns>
        public Triangle2d Convert2d()
        {
            return new Triangle2d(Array.ConvertAll(_pts, x => x.Convert2d(this.GetPlane())));
        }

        /// <summary>
        /// Projects a Triangle3d on the WCS XY plane.
        /// </summary>
        /// <returns>The resulting Triangle2d.</returns>
        public Triangle2d Flatten()
        {
            return new Triangle2d(
                new Point2d(this[0].X, this[0].Y),
                new Point2d(this[1].X, this[1].Y),
                new Point2d(this[2].X, this[2].Y));
        }

        /// <summary>
        /// Gets the angle between the two segments at specified vertex.
        /// </summary>.
        /// <param name="index">The vertex index.</param>
        /// <returns>The angle expressed in radians.</returns>
        public double GetAngleAt(int index)
        {
            return this[index].GetVectorTo(this[(index + 1) % 3]).GetAngleTo(
                this[index].GetVectorTo(this[(index + 2) % 3]));
        }

        /// <summary>
        /// Gets the bounded plane defined by the triangle.
        /// </summary>
        /// <returns>The bouned plane.</returns>
        public BoundedPlane GetBoundedPlane()
        {
            return new BoundedPlane(this[0], this[1], this[2]);
        }

        /// <summary>
        /// Gets the unbounded plane defined by the triangle.
        /// </summary>
        /// <returns>The unbouned plane.</returns>
        public Plane GetPlane()
        {
            Point3d origin =
                new Point3d(0.0, 0.0, this.Elevation).TransformBy(Matrix3d.PlaneToWorld(this.Normal));
            return new Plane(origin, this.Normal);
        }

        /// <summary>
        /// Gets the segment at specified index.
        /// </summary>
        /// <param name="index">The segment index.</param>
        /// <returns>The segment 3d</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// IndexOutOfRangeException is throw if index is less than 0 or more than 2.</exception>
        public LineSegment3d GetSegmentAt(int index)
        {
            if (index > 2)
                throw new IndexOutOfRangeException("Index out of range");
            return new LineSegment3d(this[index], this[(index + 1) % 3]);
        }

        /// <summary>
        /// Checks if the distance between every respective Point3d in both Triangle3d is less than or equal to the Tolerance.Global.EqualPoint value.
        /// </summary>
        /// <param name="t3d">The triangle3d to compare.</param>
        /// <returns>true if the condition is met; otherwise, false.</returns>
        public bool IsEqualTo(Triangle3d t3d)
        {
            return this.IsEqualTo(t3d, Tolerance.Global);
        }

        /// <summary>
        /// Checks if the distance between every respective Point3d in both Triangle3d is less than or equal to the Tolerance.EqualPoint value of the specified tolerance.
        /// </summary>
        /// <param name="t3d">The triangle3d to compare.</param>
        /// <param name="tol">The tolerance used in points comparisons.</param>
        /// <returns>true if the condition is met; otherwise, false.</returns>
        public bool IsEqualTo(Triangle3d t3d, Tolerance tol)
        {
            return t3d[0].IsEqualTo(_pt0, tol) && t3d[1].IsEqualTo(_pt1, tol) && t3d[2].IsEqualTo(_pt2, tol);
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is strictly inside the triangle.
        /// </summary>
        /// <param name="pt">The point to be evaluated.</param>
        /// <returns>true if the point is inside; otherwise, false.</returns>
        public bool IsPointInside(Point3d pt)
        {
            Tolerance tol = new Tolerance(1e-9, 1e-9);
            Vector3d v1 = pt.GetVectorTo(_pt0).CrossProduct(pt.GetVectorTo(_pt1)).GetNormal();
            Vector3d v2 = pt.GetVectorTo(_pt1).CrossProduct(pt.GetVectorTo(_pt2)).GetNormal();
            Vector3d v3 = pt.GetVectorTo(_pt2).CrossProduct(pt.GetVectorTo(_pt0)).GetNormal();
            return (v1.IsEqualTo(v2, tol) && v2.IsEqualTo(v3, tol));
        }

        /// <summary>
        /// Gets a value indicating whether the specified point is on a triangle segment.
        /// </summary>
        /// <param name="pt">The point to be evaluated.</param>
        /// <returns>true if the point is on a segment; otherwise, false.</returns>
        public bool IsPointOn(Point3d pt)
        {
            Tolerance tol = new Tolerance(1e-9, 1e-9);
            Vector3d v0 = new Vector3d(0.0, 0.0, 0.0);
            Vector3d v1 = pt.GetVectorTo(_pt0).CrossProduct(pt.GetVectorTo(_pt1));
            Vector3d v2 = pt.GetVectorTo(_pt1).CrossProduct(pt.GetVectorTo(_pt2));
            Vector3d v3 = pt.GetVectorTo(_pt2).CrossProduct(pt.GetVectorTo(_pt0));
            return (v1.IsEqualTo(v0, tol) || v2.IsEqualTo(v0, tol) || v3.IsEqualTo(v0, tol));
        }

        /// <summary>
        /// Sets the elements of the triangle using an origin and two vectors.
        /// </summary>
        /// <param name="org">The origin of the Triangle3d (1st vertex).</param>
        /// <param name="v1">The vector from origin to the second vertex.</param>
        /// <param name="v2">The vector from origin to the third vertex.</param>
        public void Set(Point3d org, Vector3d v1, Vector3d v2)
        {
            _pt0 = org;
            _pt1 = org + v1;
            _pt2 = org + v2;
            _pts = new Point3d[3] { _pt0, _pt1, _pt2 };
        }

        /// <summary>
        /// Transforms a Triangle3d with a transformation matrix
        /// </summary>
        /// <param name="mat">The 3d transformation matrix.</param>
        /// <returns>The new Triangle3d.</returns>
        public Triangle3d Transformby(Matrix3d mat)
        {
            return new Triangle3d(Array.ConvertAll<Point3d, Point3d>(
                _pts, new Converter<Point3d, Point3d>(p => p.TransformBy(mat))));
        }

        #endregion
    }
    /// <summary>
    /// Provides extension methods for the CircularArc2dType
    /// </summary>
    public static class CircularArc2dExtensions
    {
        /// <summary>
        /// Gets the signed area of the circular arc.
        /// </summary>
        /// <param name="arc">The instance to which the method applies.</param>
        /// <returns>The signed area.</returns>
        public static double SignedArea(this CircularArc2d arc)
        {
            double rad = arc.Radius;
            double ang = arc.IsClockWise ?
                arc.StartAngle - arc.EndAngle :
                arc.EndAngle - arc.StartAngle;
            return rad * rad * (ang - Math.Sin(ang)) / 2.0;
        }

        /// <summary>
        /// Gets the centroid of the circular arc.
        /// </summary>
        /// <param name="arc">The instance to which the method applies.</param>
        /// <returns>The centroid of the arc.</returns>
        public static Point2d Centroid(this CircularArc2d arc)
        {
            Point2d start = arc.StartPoint;
            Point2d end = arc.EndPoint;
            double area = arc.SignedArea();
            double chord = start.GetDistanceTo(end);
            double angle = (end - start).Angle;
            return arc.Center.Polar(angle - (Math.PI / 2.0), (chord * chord * chord) / (12.0 * area));
        }

        /// <summary>
        /// Returns the tangents between the active CircularArc2d instance complete circle and a point.
        /// </summary>
        /// <remarks>
        /// Tangents start points are on the object to which this method applies, end points on the point passed as argument.
        /// Tangents are always returned in the same order: the tangent on the left side of the line from the circular arc center
        /// to the point before the other one. 
        /// </remarks>
        /// <param name="arc">The instance to which this method applies.</param>
        /// <param name="pt">The Point2d to which tangents are searched</param>
        /// <returns>An array of LineSegement2d representing the tangents (2) or null if there is none.</returns>
        public static LineSegment2d[] GetTangentsTo(this CircularArc2d arc, Point2d pt)
        {
            // check if the point is inside the circle
            Point2d center = arc.Center;
            if (pt.GetDistanceTo(center) <= arc.Radius)
                return null;

            Vector2d vec = center.GetVectorTo(pt) / 2.0;
            CircularArc2d tmp = new CircularArc2d(center + vec, vec.Length);
            Point2d[] inters = arc.IntersectWith(tmp);
            if (inters == null)
                return null;
            LineSegment2d[] result = new LineSegment2d[2];
            Vector2d v1 = inters[0] - center;
            Vector2d v2 = inters[1] - center;
            int i = vec.X * v1.Y - vec.Y - v1.X > 0 ? 0 : 1;
            int j = i ^ 1;
            result[i] = new LineSegment2d(inters[0], pt);
            result[j] = new LineSegment2d(inters[1], pt);
            return result;
        }

        /// <summary>
        /// Returns the tangents between the active CircularArc2d instance complete circle and another one.
        /// </summary>
        /// <remarks>
        /// Tangents start points are on the object to which this method applies, end points on the one passed as argument.
        /// Tangents are always returned in the same order: outer tangents before inner tangents, and for both,
        /// the tangent on the left side of the line from this circular arc center to the other one before the other one.
        /// </remarks>
        /// <param name="arc">The instance to which this method applies.</param>
        /// <param name="other">The CircularArc2d to which searched for tangents.</param>
        /// <param name="flags">An enum value specifying which type of tangent is returned.</param>
        /// <returns>An array of LineSegment2d representing the tangents (maybe 2 or 4) or null if there is none.</returns>
        public static LineSegment2d[] GetTangentsTo(this CircularArc2d arc, CircularArc2d other, TangentType flags)
        {
            // check if a circle is inside the other
            double dist = arc.Center.GetDistanceTo(other.Center);
            if (dist - Math.Abs(arc.Radius - other.Radius) <= Tolerance.Global.EqualPoint)
                return null;

            // check if circles overlap
            bool overlap = arc.Radius + other.Radius >= dist;
            if (overlap && flags == TangentType.Inner)
                return null;

            CircularArc2d tmp1, tmp2;
            Point2d[] inters;
            Vector2d vec1, vec2, vec = other.Center - arc.Center;
            int i, j;
            LineSegment2d[] result = new LineSegment2d[(int)flags == 3 && !overlap ? 4 : 2];

            // outer tangents
            if ((flags & TangentType.Outer) > 0)
            {
                if (arc.Radius == other.Radius)
                {
                    Line2d perp = new Line2d(arc.Center, vec.GetPerpendicularVector());
                    inters = arc.IntersectWith(perp);
                    if (inters == null)
                        return null;
                    vec1 = (inters[0] - arc.Center).GetNormal();
                    vec2 = (inters[1] - arc.Center).GetNormal();
                    i = vec.X * vec1.Y - vec.Y - vec1.X > 0 ? 0 : 1;
                    j = i ^ 1;
                    result[i] = new LineSegment2d(inters[0], inters[0] + vec);
                    result[j] = new LineSegment2d(inters[1], inters[1] + vec);
                }
                else
                {
                    Point2d center = arc.Radius < other.Radius ? other.Center : arc.Center;
                    tmp1 = new CircularArc2d(center, Math.Abs(arc.Radius - other.Radius));
                    tmp2 = new CircularArc2d(arc.Center + vec / 2.0, dist / 2.0);
                    inters = tmp1.IntersectWith(tmp2);
                    if (inters == null)
                        return null;
                    vec1 = (inters[0] - center).GetNormal();
                    vec2 = (inters[1] - center).GetNormal();
                    i = vec.X * vec1.Y - vec.Y - vec1.X > 0 ? 0 : 1;
                    j = i ^ 1;
                    result[i] = new LineSegment2d(arc.Center + vec1 * arc.Radius, other.Center + vec1 * other.Radius);
                    result[j] = new LineSegment2d(arc.Center + vec2 * arc.Radius, other.Center + vec2 * other.Radius);
                }
            }

            // inner tangents
            if ((flags & TangentType.Inner) > 0 && !overlap)
            {
                double ratio = (arc.Radius / (arc.Radius + other.Radius)) / 2.0;
                tmp1 = new CircularArc2d(arc.Center + vec * ratio, dist * ratio);
                inters = arc.IntersectWith(tmp1);
                if (inters == null)
                    return null;
                vec1 = (inters[0] - arc.Center).GetNormal();
                vec2 = (inters[1] - arc.Center).GetNormal();
                i = vec.X * vec1.Y - vec.Y - vec1.X > 0 ? 2 : 3;
                j = i == 2 ? 3 : 2;
                result[i] = new LineSegment2d(arc.Center + vec1 * arc.Radius, other.Center + vec1.Negate() * other.Radius);
                result[j] = new LineSegment2d(arc.Center + vec2 * arc.Radius, other.Center + vec2.Negate() * other.Radius);
            }
            return result;
        }
    }
    /// <summary>
    /// AutoCAD coordinate systems enumeration.
    /// </summary>
    public enum CoordSystem
    {
        /// <summary>
        /// World Coordinate System.
        /// </summary>
        WCS = 0,

        /// <summary>
        /// Current User Coordinate System. 
        /// </summary>
        UCS,

        /// <summary>
        /// Display Coordinate System of the current viewport.
        /// </summary>
        DCS,

        /// <summary>
        /// Paper Space Display Coordinate System.
        /// </summary>
        PSDCS
    }

    /// <summary>
    /// Tangent type enum
    /// </summary>
    [Flags]
    public enum TangentType
    {
        /// <summary>
        /// Inner tangents of two circles.
        /// </summary>
        Inner = 1,

        /// <summary>
        /// Outer tangents of two circles.
        /// </summary>
        Outer = 2
    }
}



namespace qsqhzy
{
    using qsqbdc;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Autodesk.AutoCAD.BoundaryRepresentation;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using NetTopologySuite.Algorithm;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Geometries.Utilities;
    using NetTopologySuite.IO;
    using NetTopologySuite.Operation.Overlay;
    using NetTopologySuite.Operation.OverlayNG;
    using NetTopologySuite.Utilities;
    using Newtonsoft.Json;

    public class ThCADCoreNTSDbGeoJsonReader : GeoJsonReader
    {

    }
    public class ThCADCoreNTSDbGeoJsonWriter : GeoJsonWriter
    {
        public string Write(Curve curve)
        {
            return base.Write(curve.ToNTSGeometry());
        }
    }
    public static class ThCADCoreNTSGeometryWriter
    {
        public static string ToGeoJSON(this Geometry geo)
        {
            using (StringWriter geoJson = new StringWriter())
            using (JsonTextWriter writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                var geoJsonWriter = new GeoJsonWriter();
                geoJsonWriter.Write(geo, writer);
                return geoJson.ToString();
            }
        }
    }


    public static class ThCADCoreNTSEntityExtension
    {
        public static Geometry ToNTSGeometry(this Entity obj)
        {
            if (obj is Curve curve)
            {
                return curve.ToNTSLineString();
            }
            else if (obj is DBPoint point)
            {
                return point.ToNTSPoint();
            }
            else if (obj is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else if (obj is Region region)
            {
                return region.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polygon ToNTSPolygon(this Entity entity)
        {
            if (entity is Polyline polyline)
            {
                return polyline.ToNTSPolygon();
            }
            else if (entity is Circle circle)
            {
                return circle.ToNTSPolygon();
            }
            else if (entity is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static LineString ToNTSLineString(this Curve curve)
        {
            if (curve is Line line)
            {
                return line.ToNTSLineString();
            }
            else if (curve is Polyline polyline)
            {
                return polyline.ToNTSLineString();
            }
            else if (curve is Polyline2d poly2d)
            {
                return poly2d.ToNTSLineString();
            }
            else if (curve is Circle circle)
            {
                return circle.ToNTSLineString();
            }
            else if (curve is Arc arc)
            {
                return arc.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
    public static class ThCADCoreNTSDbExtension
    {
        public static Polyline ToDbPolyline(this LineString lineString)
        {
            var pline = new Polyline()
            {
                Closed = lineString.IsClosed,
            };
            pline.CreatePolyline(lineString.Coordinates.ToAcGePoint3ds());
            return pline;
        }

        public static Line ToDbline(this LineString lineString)
        {
            var line = new Line
            {
                StartPoint = lineString.StartPoint.ToAcGePoint3d(),
                EndPoint = lineString.EndPoint.ToAcGePoint3d()
            };
            return line;
        }

        public static List<Polyline> ToDbPolylines(this Polygon polygon)
        {
            var plines = new List<Polyline>();
            plines.Add(polygon.Shell.ToDbPolyline());
            foreach (LinearRing hole in polygon.Holes)
            {
                plines.Add(hole.ToDbPolyline());
            }
            return plines;
        }

        public static MPolygon ToDbMPolygon(this Polygon polygon)
        {
            List<Curve> holes = new List<Curve>();
            var shell = polygon.Shell.ToDbPolyline();
            polygon.Holes.ForEach(o => holes.Add(o.ToDbPolyline()));
            return ThMPolygonTool.CreateMPolygon(shell, holes);
        }

        public static Entity ToDbEntity(this Polygon polygon)
        {
            if (polygon.NumInteriorRings > 0)
            {
                return polygon.ToDbMPolygon();
            }
            else
            {
                return polygon.Shell.ToDbPolyline();
            }
        }

        public static List<Polyline> ToDbPolylines(this MultiLineString geometries)
        {
            var plines = new List<Polyline>();
            foreach (var geometry in geometries.Geometries)
            {
                if (geometry is LineString lineString)
                {
                    plines.Add(lineString.ToDbPolyline());
                }
                else if (geometry is LinearRing linearRing)
                {
                    plines.Add(linearRing.ToDbPolyline());
                }
                else if (geometry is Polygon polygon)
                {
                    plines.AddRange(polygon.ToDbPolylines());
                }
                else if (geometry is MultiLineString multiLineString)
                {
                    plines.AddRange(multiLineString.ToDbPolylines());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return plines;
        }

        public static Line ToDbLine(this LineSegment segment)
        {
            return new Line()
            {
                StartPoint = segment.P0.ToAcGePoint3d(),
                EndPoint = segment.P1.ToAcGePoint3d()
            };
        }

        public static List<Region> ToDbRegions(this MultiPolygon mPolygon)
        {
            var regions = new List<Region>();
            foreach (Polygon polygon in mPolygon.Geometries)
            {
                regions.Add(polygon.ToDbRegion());
            }
            return regions;
        }

        public static List<Polyline> ToDbPolylines(this MultiPolygon mPolygon)
        {
            var plines = new List<Polyline>();
            foreach (Polygon polygon in mPolygon.Geometries)
            {
                plines.Add(polygon.Shell.ToDbPolyline());
            }
            return plines;
        }

        public static List<DBObject> ToDbObjects(this Geometry geometry, bool keepHoles = false)
        {
            var objs = new List<DBObject>();
            if (geometry.IsEmpty)
            {
                return objs;
            }
            if (geometry is LineString lineString)
            {
                objs.Add(lineString.ToDbPolyline());
            }
            else if (geometry is LinearRing linearRing)
            {
                objs.Add(linearRing.ToDbPolyline());
            }
            else if (geometry is Polygon polygon)
            {
                if (keepHoles)
                {
                    objs.Add(polygon.ToDbMPolygon());
                }
                else
                {
                    objs.AddRange(polygon.ToDbPolylines());
                }
            }
            else if (geometry is MultiLineString lineStrings)
            {
                lineStrings.Geometries.ForEach(g => objs.AddRange(g.ToDbObjects(keepHoles)));
            }
            else if (geometry is MultiPolygon polygons)
            {
                polygons.Geometries.ForEach(g => objs.AddRange(g.ToDbObjects(keepHoles)));
            }
            else if (geometry is GeometryCollection geometries)
            {
                geometries.Geometries.ForEach(g => objs.AddRange(g.ToDbObjects(keepHoles)));
            }
            else if (geometry is Point point)
            {
                objs.Add(point.ToDbPoint());
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }

        public static LineString ToNTSLineString(this Polyline poly)
        {
            var points = new List<Coordinate>();
            var arcLength = CADCoreNTSServiceConfig.Default.ArcTessellationLength;
            var polyLine = poly.HasBulges ? poly.TessellatePolylineWithArc(arcLength) : poly;
            for (int i = 0; i < polyLine.NumberOfVertices; i++)
            {
                points.Add(polyLine.GetPoint3dAt(i).ToNTSCoordinate());
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (polyLine.Closed && !points[0].Equals(points[points.Count - 1]))
            {
                points.Add(points[0]);
            }

            if (points[0].Equals(points[points.Count - 1]))
            {
                // 三个点，其中起点和终点重合
                // 多段线退化成一根线段
                if (points.Count == 3)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points.ToArray());
                }

                // 二个点，其中起点和终点重合
                // 多段线退化成一个点
                if (points.Count == 2)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString();
                }

                // 一个点
                // 多段线退化成一个点
                if (points.Count == 1)
                {
                    return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString();
                }

                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLinearRing(points.ToArray());
            }
            else
            {
                // 首尾端点不一致的情况
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points.ToArray());
            }
        }

        public static LineString ToNTSLineString(this Polyline2d poly2d)
        {
            var poly = new Polyline();
            poly.ConvertFrom(poly2d, false);
            return poly.ToNTSLineString();
        }

        public static Polygon ToNTSPolygon(this Polyline polyLine)
        {
            var geometry = polyLine.ToNTSLineString();
            if (geometry is LinearRing ring)
            {
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePolygon(ring);
            }
            else
            {
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePolygon();
            }
        }

        public static Polygon ToNTSPolygon(this MPolygon mPolygon)
        {
            Polyline shell = null;
            List<Polyline> holes = new List<Polyline>();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                LoopDirection direction = mPolygon.GetLoopDirection(i);
                MPolygonLoop mPolygonLoop = mPolygon.GetMPolygonLoopAt(i);
                Polyline polyline = new Polyline()
                {
                    Closed = true
                };
                for (int j = 0; j < mPolygonLoop.Count; j++)
                {
                    var bulgeVertex = mPolygonLoop[j];
                    polyline.AddVertexAt(j, bulgeVertex.Vertex, bulgeVertex.Bulge, 0, 0);
                }
                if (LoopDirection.Exterior == direction)
                {
                    shell = polyline;
                }
                else if (LoopDirection.Interior == direction)
                {
                    holes.Add(polyline);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            if (shell == null && holes.Count == 1)
            {
                return holes[0].ToNTSPolygon();
            }
            else if (shell != null && holes.Count == 0)
            {
                return shell.ToNTSPolygon();
            }
            else if (shell != null && holes.Count > 0)
            {
                List<LinearRing> holeRings = new List<LinearRing>();
                holes.ForEach(o =>
                {
                    holeRings.Add(o.ToNTSLineString() as LinearRing);
                });
                LinearRing shellLinearRing = shell.ToNTSLineString() as LinearRing;
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePolygon(shellLinearRing, holeRings.ToArray());
            }
            else
            {
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePolygon();
            }
        }

        public static Polygon ToNTSPolygon(this Circle circle)
        {
            var length = CADCoreNTSServiceConfig.Default.ArcTessellationLength;
            var circum = 2 * Math.PI * circle.Radius;
            int num = (int)Math.Ceiling(circum / length);
            if (num >= 3)
            {
                return circle.ToNTSPolygon(num);
            }
            else
            {
                return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePolygon();
            }
        }

        public static LineString ToNTSLineString(this Circle circle)
        {
            return circle.ToNTSPolygon().Shell;
        }

        public static Polygon ToNTSPolygon(this Circle circle, int numPoints)
        {
            // 获取圆的外接矩形
            var shapeFactory = new GeometricShapeFactory(CADCoreNTSServiceConfig.Default.GeometryFactory)
            {
                NumPoints = numPoints,
                Size = 2 * circle.Radius,
                Centre = circle.Center.ToNTSCoordinate(),
            };
            return shapeFactory.CreateCircle();
        }

        public static LineString ToNTSLineString(this Arc arc)
        {
            var arcLength = CADCoreNTSServiceConfig.Default.ArcTessellationLength;
            return arc.TessellateArcWithArc(arcLength).ToNTSLineString();
        }

        public static LineString ToNTSLineString(this Line line)
        {
            var points = new List<Coordinate>
            {
                line.StartPoint.ToNTSCoordinate(),
                line.EndPoint.ToNTSCoordinate()
            };
            return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString(points.ToArray());
        }

        public static LineSegment ToNTSLineSegment(this Line line)
        {
            return new LineSegment(line.StartPoint.ToNTSCoordinate(), line.EndPoint.ToNTSCoordinate());
        }

        public static Point ToNTSPoint(this DBPoint point)
        {
            return CADCoreNTSServiceConfig.Default.GeometryFactory.CreatePoint(point.Position.ToNTSCoordinate());
        }

        public static DBPoint ToDbPoint(this Point point)
        {
            return new DBPoint(point.ToAcGePoint3d());
        }

        public static bool IsCCW(this Polyline pline)
        {
            return Orientation.IsCCW(pline.ToNTSLineString().Coordinates);
        }
    }
    public static class ThPolylineExtension
    {
        /// <summary>
        /// 多段线顶点集合（不支持圆弧段）
        /// </summary>
        /// <param name="pLine"></param>
        /// <returns></returns>
        public static Point3dCollection Vertices(this Polyline pLine)
        {
            //https://keanw.com/2007/04/iterating_throu.html
            Point3dCollection vertices = new Point3dCollection();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                // 暂时不考虑“圆弧”的情况
                vertices.Add(pLine.GetPoint3dAt(i));
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (pLine.Closed && !vertices[0].IsEqualTo(vertices[vertices.Count - 1]))
            {
                vertices.Add(vertices[0]);
            }

            return vertices;
        }

        /// <summary>
        /// 多段线顶点集合（支持圆弧段）
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Point3dCollection VerticesEx(this Polyline poly, double length)
        {
            if (poly.HasBulges)
            {
                return poly.TessellatePolylineWithArc(length).Vertices();
            }
            else
            {
                return poly.Vertices();
            }
        }

        public static double[] Coordinates2D(this Polyline pLine)
        {
            return pLine.Vertices().Cast<Point3d>().Select(o => o.ToPoint2d().ToArray()).SelectMany(o => o).ToArray();
        }

        public static Polyline CreateRectangle(Point3d pt1, Point3d pt2, Point3d pt3, Point3d pt4)
        {
            var points = new Point3dCollection()
            {
                pt1,
                pt2,
                pt3,
                pt4
            };
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(points);
            return pline;
        }

        public static Polyline CreateTriangle(Point2d pt1, Point2d pt2, Point2d pt3)
        {
            var points = new Point2dCollection()
            {
                pt1,
                pt2,
                pt3,
            };
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(points);
            return pline;
        }

        public static Polyline ToRectangle(this Extents3d extents)
        {
            Point3d pt1 = extents.MinPoint;
            Point3d pt3 = extents.MaxPoint;
            Point3d pt2 = new Point3d(pt3.X, pt1.Y, pt1.Z);
            Point3d pt4 = new Point3d(pt1.X, pt3.Y, pt1.Z);
            return CreateRectangle(pt1, pt2, pt3, pt4);
        }

        /// <summary>
        /// 根据弦长分割Polyline
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Polyline TessellatePolylineWithChord(this Polyline poly, double chord)
        {
            var segments = new PolylineSegmentCollection(poly);
            var tessellateSegments = new PolylineSegmentCollection();
            segments.ForEach(s => tessellateSegments.AddRange(s.TessellateSegmentWithChord(chord)));
            return tessellateSegments.ToPolyline();
        }

        /// <summary>
        /// 根据弦长分割Arc
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Polyline TessellateArcWithChord(this Arc arc, double chord)
        {
            var segment = new PolylineSegment(
                arc.StartPoint.ToPoint2D(),
                arc.EndPoint.ToPoint2D(),
                arc.BulgeFromCurve(false));
            return segment.TessellateSegmentWithChord(chord).ToPolyline();
        }

        /// <summary>
        /// 根据弦长分割PolylineSegment
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection TessellateSegmentWithChord(this PolylineSegment segment, double chord)
        {
            var segments = new PolylineSegmentCollection();
            if (segment.IsLinear)
            {
                // 分割段是直线
                segments.Add(segment);
            }
            else
            {
                // 分割线是弧线
                var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
                // 排除弦长大于弧直径的情况
                if (chord > 2 * circulararc.Radius)
                {
                    segments.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
                }
                else
                {
                    var angle = 2 * Math.Asin(chord / (2 * circulararc.Radius));
                    segments.AddRange(segment.DoTessellate(angle));
                }
            }
            return segments;
        }

        /// <summary>
        /// 根据弧长分割Polyline
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline TessellatePolylineWithArc(this Polyline poly, double length)
        {
            var segments = new PolylineSegmentCollection(poly);
            var tessellateSegments = new PolylineSegmentCollection();
            segments.ForEach(s => tessellateSegments.AddRange(s.TessellateSegmentWithArc(length)));
            return tessellateSegments.ToPolyline();
        }

        /// <summary>
        /// 根据弧长分割Arc
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline TessellateArcWithArc(this Arc arc, double length)
        {
            var segment = new PolylineSegment(
                arc.StartPoint.ToPoint2D(),
                arc.EndPoint.ToPoint2D(),
                arc.BulgeFromCurve(false));
            return segment.TessellateSegmentWithArc(length).ToPolyline();
        }

        /// <summary>
        /// 根据弧长分割PolylineSegment
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection TessellateSegmentWithArc(this PolylineSegment segment, double length)
        {
            var segments = new PolylineSegmentCollection();
            if (segment.IsLinear)
            {
                // 分割线是直线
                segments.Add(segment);
            }
            else
            {
                // 分割线是弧线
                var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
                // 排除分割长度大于弧的周长的情况
                if (length >= 2 * Math.PI * circulararc.Radius)
                {
                    segments.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
                }
                else
                {
                    var angle = length / circulararc.Radius;
                    segments.AddRange(segment.DoTessellate(angle));
                }
            }
            return segments;
        }

        public static Polyline Tessellate(this Circle circle, double length)
        {
            if (length >= 2 * Math.PI * circle.Radius)
            {
                return circle.ToTriangle();
            }
            else
            {
                Plane plane = new Plane(circle.Center, circle.Normal);
                Matrix3d planeToWorld = Matrix3d.PlaneToWorld(plane);
                Arc firstArc = new Arc(Point3d.Origin, circle.Radius, 0.0, Math.PI);
                Arc secondArc = new Arc(Point3d.Origin, circle.Radius, Math.PI, Math.PI * 2.0);
                firstArc.TransformBy(planeToWorld);
                secondArc.TransformBy(planeToWorld);
                Polyline firstPolyline = firstArc.TessellateArcWithArc(length);
                Polyline secondPolyline = secondArc.TessellateArcWithArc(length);
                var firstSegmentCollection = new PolylineSegmentCollection(firstPolyline);
                var secondSegmentCollection = new PolylineSegmentCollection(secondPolyline);
                var segmentCollection = new PolylineSegmentCollection();
                firstSegmentCollection.ForEach(o => segmentCollection.Add(o));
                secondSegmentCollection.ForEach(o => segmentCollection.Add(o));
                return segmentCollection.ToPolyline();
            }
        }
        private static Polyline ToTriangle(this Circle circle)
        {
            Plane plane = new Plane(circle.Center, circle.Normal);
            Matrix3d planeToWorld = Matrix3d.PlaneToWorld(plane);
            Point3d firstPt = new Point3d(0, circle.Radius, 0);
            double xLen = circle.Radius * Math.Cos(Math.PI / 6.0);
            double yLen = circle.Radius * Math.Sin(Math.PI / 6.0);
            Point3d secondPt = new Point3d(-xLen, -yLen, 0);
            Point3d thirdPt = new Point3d(xLen, -yLen, 0);
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(firstPt.X, firstPt.Y), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(secondPt.X, secondPt.Y), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(thirdPt.X, thirdPt.Y), 0, 0, 0);
            polyline.TransformBy(planeToWorld);
            plane.Dispose();
            return polyline;
        }
        /// <summary>
        /// 根据角度分割弧段(保证起始点终止点不变)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection DoTessellate(this PolylineSegment segment, double angle)
        {
            var TessellateArc = new PolylineSegmentCollection();
            var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
            var angleRange = 4 * Math.Atan(segment.Bulge);
            // 判断弧线是否是顺时针方向
            int IsClockwise = (segment.Bulge < 0.0) ? -1 : 1;
            if (angle >= (angleRange * IsClockwise))
            {
                TessellateArc.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
            }
            else
            {
                // 如果方向向量与y轴正方向的角度 小于等于90° 则方向向量在一三象限或x轴上，此时方向向量与x轴的角度不需要变化，否则需要 2PI - 与x轴角度
                double StartAng = (circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)));

                double EndAng = (circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)));
                int num = Convert.ToInt32(Math.Floor(angleRange * IsClockwise / angle)) + 1;

                for (int i = 1; i <= num; i++)
                {
                    var startAngle = StartAng + (i - 1) * angle * IsClockwise;
                    var endAngle = StartAng + i * angle * IsClockwise;
                    if (i == num)
                    {
                        endAngle = EndAng;
                    }
                    startAngle = (startAngle > 8 * Math.Atan(1)) ? startAngle - 8 * Math.Atan(1) : startAngle;
                    startAngle = (startAngle < 0.0) ? startAngle + 8 * Math.Atan(1) : startAngle;
                    endAngle = (endAngle > 8 * Math.Atan(1)) ? endAngle - 8 * Math.Atan(1) : endAngle;
                    endAngle = (endAngle < 0.0) ? endAngle + 8 * Math.Atan(1) : endAngle;
                    // Arc的构建方向是逆时针的，所以如果是顺时针的弧段，需要反向构建
                    if (segment.Bulge < 0.0)
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, endAngle, startAngle);
                        TessellateArc.Add(new PolylineSegment(arc.EndPoint.ToPoint2d(), arc.StartPoint.ToPoint2d()));
                    }
                    else
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, startAngle, endAngle);
                        TessellateArc.Add(new PolylineSegment(arc.StartPoint.ToPoint2d(), arc.EndPoint.ToPoint2d()));
                    }
                }
            }
            return TessellateArc;
        }
    }
    public static class ThCADCoreNTSGeExtension
    {
        public static Point3d ToAcGePoint3d(this Coordinate coordinate)
        {
            if (!double.IsNaN(coordinate.Z))
            {
                return new Point3d(
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.X),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.Y),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.Z)
                    );
            }
            else
            {
                return new Point3d(
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.X),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.Y),
                    0);
            }
        }

        public static Point3d ToAcGePoint3d(this Point point)
        {
            return point.Coordinate.ToAcGePoint3d();
        }

        public static Point2d ToAcGePoint2d(this Coordinate coordinate)
        {
            return new Point2d(
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.X),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(coordinate.Y)
                );
        }

        public static Point2d ToAcGePoint2d(this Point point)
        {
            return point.Coordinate.ToAcGePoint2d();
        }

        public static Coordinate ToNTSCoordinate(this Point3d point)
        {
            return new Coordinate(
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.X),
                    CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.Y)
                    );

        }

        public static Coordinate ToNTSCoordinate(this Point2d point)
        {
            return new Coordinate(
                CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.X),
                CADCoreNTSServiceConfig.Default.PrecisionModel.MakePrecise(point.Y)
                );
        }

        public static Coordinate[] ToNTSCoordinates(this Point3dCollection points)
        {
            var coordinates = new List<Coordinate>();
            foreach (Point3d pt in points)
            {
                coordinates.Add(pt.ToNTSCoordinate());
            }
            return coordinates.ToArray();
        }

        public static Point3dCollection ToAcGePoint3ds(this Coordinate[] coordinates)
        {
            var points = new Point3dCollection();
            foreach (var coordinate in coordinates)
            {
                points.Add(coordinate.ToAcGePoint3d());
            }
            return points;
        }

        public static bool IsReflex(Point3d p1, Point3d p2, Point3d p3)
        {
            return (p3.Y - p1.Y) * (p2.X - p1.X) - (p3.X - p1.X) * (p2.Y - p1.Y) < 0;
        }

        public static bool IsConvex(Point3d p1, Point3d p2, Point3d p3)
        {
            return (p3.Y - p1.Y) * (p2.X - p1.X) - (p3.X - p1.X) * (p2.Y - p1.Y) > 0;
        }

        public static bool IsCollinear(Point3d p1, Point3d p2, Point3d p3)
        {
            var coordinate1 = p1.ToNTSCoordinate();
            var coordinate2 = p2.ToNTSCoordinate();
            var coordinate3 = p3.ToNTSCoordinate();
            return Orientation.Index(coordinate1, coordinate2, coordinate3) == OrientationIndex.Collinear;
        }
    }
    public static class ThMPolygonTool
    {
        public static MPolygon CreateMPolygon(Curve external, List<Curve> innerCurves)
        {
            MPolygon mPolygon = new MPolygon();
            if (external is Polyline polyline)
            {
                if (polyline.Area > 0.0)
                {
                    mPolygon.AppendLoopFromBoundary(polyline, false, 0.0);
                }
            }
            else if (external is Polyline2d polyline2d)
            {
                mPolygon.AppendLoopFromBoundary(polyline2d, false, 0.0);
            }
            else if (external is Circle circle)
            {
                mPolygon.AppendLoopFromBoundary(circle, false, 0.0);
            }
            else
            {
                throw new NotSupportedException();
            }

            innerCurves.ForEach(o =>
            {
                if (o is Polyline innerPolyline)
                {
                    mPolygon.AppendLoopFromBoundary(innerPolyline, false, 0.0);
                }
                else if (o is Polyline2d innerPolyline2d)
                {
                    mPolygon.AppendLoopFromBoundary(innerPolyline2d, false, 0.0);
                }
                else if (o is Circle innerCircle)
                {
                    mPolygon.AppendLoopFromBoundary(innerCircle, false, 0.0);
                }
                else
                {
                    throw new NotSupportedException();
                }
            });

            mPolygon.SetLoopDirection(0, LoopDirection.Exterior);
            if (innerCurves.Count > 0)
            {
                for (int i = 1; i <= innerCurves.Count; i++)
                {
                    mPolygon.SetLoopDirection(i, LoopDirection.Interior);
                }
            }
            return mPolygon;
        }
    }
    public static class ThCurveExtension
    {
        /// <summary>
        /// 获取曲线的中点
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static Point3d GetMidpoint(this Curve curve)
        {
            double d1 = curve.GetDistanceAtParameter(curve.StartParam);
            double d2 = curve.GetDistanceAtParameter(curve.EndParam);
            return curve.GetPointAtDist(d1 + ((d2 - d1) / 2.0));
        }


        // Make sure the pt1 and pt2 are on the Curve before calling this method.
        //  https://spiderinnet1.typepad.com/blog/2012/10/autocad-net-isonpoint3d-curvegetclosestpointto-curvegetparameteratpoint.html
        public static double GetLength(this Curve ent, Point3d pt1, Point3d pt2)
        {
            double dist1 = ent.GetDistanceAtParameter(ent.GetParameterAtPoint(ent.GetClosestPointTo(pt1, false)));
            double dist2 = ent.GetDistanceAtParameter(ent.GetParameterAtPoint(ent.GetClosestPointTo(pt2, false)));

            return Math.Abs(dist1 - dist2);
        }

        // https://www.keanw.com/2012/01/testing-whether-a-point-is-on-any-autocad-curve-using-net.html
        public static bool IsPointOnCurve(this Curve curve, Point3d pt, Tolerance tolerance)
        {
            return pt.IsEqualTo(curve.GetClosestPointTo(pt, false), tolerance);
        }

        public static double BulgeFromCurve(this Curve cv, bool clockwise)
        {
            double bulge = 0.0;
            Arc a = cv as Arc;
            if (a != null)
            {
                double newStart;
                // The start angle is usually greater than the end,
                // as arcs are all counter-clockwise.
                // (If it isn't it's because the arc crosses the
                // 0-degree line, and we can subtract 2PI from the
                // start angle.)
                if (a.StartAngle > a.EndAngle)
                    newStart = a.StartAngle - 8 * Math.Atan(1);
                else
                    newStart = a.StartAngle;

                // Bulge is defined as the tan of
                // one fourth of the included angle
                bulge = Math.Tan((a.EndAngle - newStart) / 4);
                // If the curve is clockwise, we negate the bulge
                if (clockwise)
                    bulge = -bulge;

            }
            return bulge;
        }

        public static LinearEntity3d ToGeLine(this Line line)
        {
            return new Line3d(line.StartPoint, line.EndPoint);
        }
    }
    /// <summary>
    /// 多段线操作类
    /// </summary>
    public static class PolylineTools
    {
        /// <summary>
        /// 通过三维点集合创建多段线
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="pts">多段线的顶点</param>
        public static void CreatePolyline(this Polyline pline, Point3dCollection pts)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                //添加多段线的顶点
                pline.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
        }

        /// <summary>
        /// 通过二维点集合创建多段线
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="pts">多段线的顶点</param>
        public static void CreatePolyline(this Polyline pline, Point2dCollection pts)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                //添加多段线的顶点
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
        }

        /// <summary>
        /// 通过不固定的点创建多段线
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="pts">多段线的顶点</param>
        public static void CreatePolyline(this Polyline pline, params Point2d[] pts)
        {
            pline.CreatePolyline(new Point2dCollection(pts));
        }

        /// <summary>
        /// 创建矩形
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="pt1">矩形的角点</param>
        /// <param name="pt2">矩形的角点</param>
        public static void CreateRectangle(this Polyline pline, Point2d pt1, Point2d pt2)
        {
            //设置矩形的4个顶点
            double minX = Math.Min(pt1.X, pt2.X);
            double maxX = Math.Max(pt1.X, pt2.X);
            double minY = Math.Min(pt1.Y, pt2.Y);
            double maxY = Math.Max(pt1.Y, pt2.Y);
            Point2dCollection pts = new Point2dCollection();
            pts.Add(new Point2d(minX, minY));
            pts.Add(new Point2d(minX, maxY));
            pts.Add(new Point2d(maxX, maxY));
            pts.Add(new Point2d(maxX, minY));
            pline.CreatePolyline(pts);
            pline.Closed = true;//闭合多段线以形成矩形
        }

        /// <summary>
        /// 创建多边形
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="centerPoint">多边形中心点</param>
        /// <param name="number">边数</param>
        /// <param name="radius">外接圆半径</param>
        public static void CreatePolygon(this Polyline pline, Point2d centerPoint, int number, double radius)
        {
            Point2dCollection pts = new Point2dCollection(number);
            double angle = 2 * Math.PI / number;//计算每条边对应的角度
            //计算多边形的顶点
            for (int i = 0; i < number; i++)
            {
                Point2d pt = new Point2d(centerPoint.X + radius * Math.Cos(i * angle), centerPoint.Y + radius * Math.Sin(i * angle));
                pts.Add(pt);
            }
            pline.CreatePolyline(pts);
            pline.Closed = true;//闭合多段线以形成多边形
        }

        /// <summary>
        /// 创建多段线形式的圆
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="centerPoint">圆心</param>
        /// <param name="radius">半径</param>
        public static void CreatePolyCircle(this Polyline pline, Point2d centerPoint, double radius)
        {
            //计算多段线的顶点
            Point2d pt1 = new Point2d(centerPoint.X + radius, centerPoint.Y);
            Point2d pt2 = new Point2d(centerPoint.X - radius, centerPoint.Y);
            Point2d pt3 = new Point2d(centerPoint.X + radius, centerPoint.Y);
            Point2dCollection pts = new Point2dCollection();
            //添加多段线的顶点
            pline.AddVertexAt(0, pt1, 1, 0, 0);
            pline.AddVertexAt(1, pt2, 1, 0, 0);
            pline.AddVertexAt(2, pt3, 1, 0, 0);
            pline.Closed = true;//闭合曲线以形成圆
        }

        /// <summary>
        /// 创建多段线形式的圆弧
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="centerPoint">圆弧的圆心</param>
        /// <param name="radius">圆弧的半径</param>
        /// <param name="startAngle">起始角度</param>
        /// <param name="endAngle">终止角度</param>
        public static void CreatePolyArc(this Polyline pline, Point2d centerPoint, double radius, double startAngle, double endAngle)
        {
            //计算多段线的顶点
            Point2d pt1 = new Point2d(centerPoint.X + radius * Math.Cos(startAngle),
                                    centerPoint.Y + radius * Math.Sin(startAngle));
            Point2d pt2 = new Point2d(centerPoint.X + radius * Math.Cos(endAngle),
                                    centerPoint.Y + radius * Math.Sin(endAngle));
            //添加多段线的顶点
            pline.AddVertexAt(0, pt1, Math.Tan((endAngle - startAngle) / 4), 0, 0);
            pline.AddVertexAt(1, pt2, 0, 0, 0);
        }
    }
    public static class ThCADCoreNTSRegionExtension
    {
        public static Polygon ToNTSPolygon(this Region region)
        {
            // 暂时不支持"复杂面域"
            var plines = region.ToPolylines();
            if (plines.Count != 1)
            {
                throw new NotSupportedException();
            }

            // 返回由面域外轮廓线封闭的多边形区域
            var pline = plines[0] as Polyline;
            return pline.ToNTSPolygon();
        }

        public static Region ToDbRegion(this Polygon polygon)
        {
            try
            {
                // 暂时不考虑有“洞”的情况
                var curves = new DBObjectCollection
                {
                    polygon.Shell.ToDbPolyline()
                };
                return Region.CreateFromCurves(curves)[0] as Region;
            }
            catch
            {
                // 未知错误
                return null;
            }
        }

        public static Region Union(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算共用部分
            var rGeometry = pGeometry.Union(sGeometry);
            if (rGeometry is Polygon polygon)
            {
                return polygon.ToDbRegion();
            }

            return null;
        }

        public static Region Intersection(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算相交部分
            var rGeometry = pGeometry.Intersection(sGeometry);
            if (rGeometry is Polygon polygon)
            {
                return polygon.ToDbRegion();
            }

            return null;
        }

        public static List<Polyline> Difference(this Region pRegion, Region sRegion)
        {
            var regions = new List<Polyline>();
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return regions;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return regions;
            }

            // 若相交，则计算在pRegion，但不在sRegion的部分
            var rGeometry = pGeometry.Difference(sGeometry);
            if (rGeometry is Polygon polygon)
            {
                regions.Add(polygon.Shell.ToDbPolyline());
            }
            else if (rGeometry is MultiPolygon mPolygon)
            {
                regions.AddRange(mPolygon.ToDbPolylines());
            }
            else
            {
                // 为止情况，抛出异常
                throw new NotSupportedException();
            }
            return regions;
        }

        public static Geometry Intersect(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算相交部分
            return pGeometry.Intersection(sGeometry);
        }

        public static List<Polyline> Difference(this Region pRegion, DBObjectCollection sRegions)
        {
            var regions = new List<Polyline>();
            try
            {
                var pGeometry = pRegion.ToNTSPolygon();
                var sGeometry = sRegions.ToNTSMultiPolygon();
                if (pGeometry == null || sGeometry == null)
                {
                    return regions;
                }

                // 检查是否相交
                if (!pGeometry.Intersects(sGeometry))
                {
                    return regions;
                }

                // 若相交，则计算在pRegion，但不在sRegion的部分
                var rGeometry = pGeometry.Difference(sGeometry);
                if (rGeometry is Polygon polygon)
                {
                    regions.Add(polygon.Shell.ToDbPolyline());
                }
                else if (rGeometry is MultiPolygon mPolygon)
                {
                    regions.AddRange(mPolygon.ToDbPolylines());
                }
                else
                {
                    // 为止情况，抛出异常
                    throw new NotSupportedException();
                }
            }
            catch
            {
                // 在某些情况下，NTS会抛出异常
                // 这里只捕捉异常，不做特殊的处理
            }
            return regions;
        }

        public static List<Polyline> Differences(this Region pRegion, DBObjectCollection sRegions)
        {
            var pGeometrys = new List<Polygon>();
            try
            {
                pGeometrys.Add(pRegion.ToNTSPolygon());
                foreach (DBObject sGe in sRegions)
                {
                    var sGeometry = new DBObjectCollection() { sGe }.ToNTSMultiPolygon();
                    foreach (var pGeometry in pGeometrys)
                    {
                        if (pGeometry == null || sGeometry == null)
                        {
                            continue;
                        }

                        // 检查是否相交
                        if (!pGeometry.Intersects(sGeometry))
                        {
                            continue;
                        }

                        // 若相交，则计算在pRegion，但不在sRegion的部分
                        var rGeometry = pGeometry.Difference(sGeometry);
                        if (rGeometry is Polygon polygon)
                        {
                            pGeometrys = new List<Polygon>() { polygon };
                        }
                        else if (rGeometry is MultiPolygon mPolygon)
                        {
                            pGeometrys = new List<Polygon>();
                            foreach (Polygon rPolygon in mPolygon.Geometries)
                            {
                                pGeometrys.Add(rPolygon);
                            }
                        }
                        else
                        {
                            // 为止情况，抛出异常
                            throw new NotSupportedException();
                        }
                    }
                }
            }
            catch
            {
                // 在某些情况下，NTS会抛出异常
                // 这里只捕捉异常，不做特殊的处理
            }
            return pGeometrys.Select(x => x.Shell.ToDbPolyline()).ToList();
        }
    }
    public static class ThCADCoreNTSDbObjCollectionExtension
    {
        public static MultiPolygon ToNTSMultiPolygon(this DBObjectCollection objs)
        {
            var polygons = objs.Cast<Entity>().Select(o => o.ToNTSPolygon());
            return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static MultiLineString ToMultiLineString(this DBObjectCollection objs)
        {
            var lineStrings = objs.Cast<Curve>().Select(o => o.ToNTSLineString());
            return CADCoreNTSServiceConfig.Default.GeometryFactory.CreateMultiLineString(lineStrings.ToArray());
        }

        public static Geometry ToNTSNodedLineStrings(this MultiLineString linestrings)
        {
            // UnaryUnionOp.Union()有Robust issue
            // 会抛出"non-noded intersection" TopologyException
            // OverlayNGRobust.Union()在某些情况下仍然会抛出TopologyException (NTS 2.2.0)
            Geometry lineString = CADCoreNTSServiceConfig.Default.GeometryFactory.CreateLineString();
            return OverlayNGRobust.Overlay(linestrings, lineString, SpatialFunction.Union);
        }

        public static Geometry ToNTSNodedLineStrings(this DBObjectCollection objs)
        {
            return objs.ToMultiLineString().ToNTSNodedLineStrings();
        }

        public static Geometry UnionGeometries(this DBObjectCollection curves)
        {
            return OverlayNGRobust.Union(curves.ToNTSMultiPolygon());
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves)
        {
            return curves.UnionGeometries().ToDbCollection();
        }

        public static Geometry Intersection(this DBObjectCollection curves, Curve curve)
        {
            return OverlayNGRobust.Overlay(
                curves.ToMultiLineString(),
                curve.ToNTSGeometry(),
                SpatialFunction.Intersection);
        }

        public static Polyline GetMinimumRectangle(this DBObjectCollection curves)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var geometry = curves.Combine();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geometry);
            if (rectangle is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Geometry Combine(this DBObjectCollection curves)
        {
            return GeometryCombiner.Combine(curves.ToMultiLineString().Geometries);
        }

        public static DBObjectCollection ToDbCollection(this Geometry geometry, bool keepHoles = false)
        {
            return geometry.ToDbObjects(keepHoles).ToCollection();
        }
    }
    /// <summary>
    /// 集合扩展类
    /// </summary>
    public static class CollectionEx
    {
        /// <summary>
        /// 对象id迭代器转换为集合
        /// </summary>
        /// <param name="ids">对象id的迭代器</param>
        /// <returns>对象id集合</returns>
        public static ObjectIdCollection ToCollection(this IEnumerable<ObjectId> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }
            ObjectIdCollection idCol = new ObjectIdCollection();
            foreach (ObjectId id in ids)
                idCol.Add(id);
            return idCol;
        }

        /// <summary>
        /// 实体迭代器转换为集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="objs">实体对象的迭代器</param>
        /// <returns>实体集合</returns>
        public static DBObjectCollection ToCollection<T>(this IEnumerable<T> objs) where T : DBObject
        {
            if (objs == null)
            {
                throw new ArgumentNullException(nameof(objs));
            }
            DBObjectCollection objCol = new DBObjectCollection();
            foreach (T obj in objs)
                objCol.Add(obj);
            return objCol;
        }

        /// <summary>
        /// double 数值迭代器转换为 double 数值集合
        /// </summary>
        /// <param name="doubles">double 数值迭代器</param>
        /// <returns>double 数值集合</returns>
        public static DoubleCollection ToCollection(this IEnumerable<double> doubles)
        {
            DoubleCollection doubleCol = new DoubleCollection();
            foreach (double d in doubles)
                doubleCol.Add(d);
            return doubleCol;
        }

        /// <summary>
        /// 二维点迭代器转换为二维点集合
        /// </summary>
        /// <param name="pts">二维点迭代器</param>
        /// <returns>二维点集合</returns>
        public static Point2dCollection ToCollection(this IEnumerable<Point2d> pts)
        {
            Point2dCollection ptCol = new Point2dCollection();
            foreach (Point2d pt in pts)
                ptCol.Add(pt);
            return ptCol;
        }

        /// <summary>
        /// 三维点迭代器转换为二维点集合
        /// </summary>
        /// <param name="pts">三维点迭代器</param>
        /// <returns>三维点集合</returns>
        public static Point3dCollection ToCollection(this IEnumerable<Point3d> pts)
        {
            Point3dCollection ptCol = new Point3dCollection();
            foreach (Point3d pt in pts)
                ptCol.Add(pt);
            return ptCol;
        }

        /// <summary>
        /// 对象id集合转换为对象id列表
        /// </summary>
        /// <param name="ids">对象id集合</param>
        /// <returns>对象id列表</returns>
        public static List<ObjectId> GetObjectIds(this ObjectIdCollection ids)
        {
            return ids.Cast<ObjectId>().ToList();
        }
    }
    public static class ThRegionTool
    {
        ///<summary>
        /// Returns whether a Region contains a Point3d.
        ///</summary>
        ///<param name="pt">A points to test against the Region.</param>
        ///<returns>A Boolean indicating whether the Region contains
        /// the point.
        /// </returns>
        public static bool ContainsPoint(this Region reg, Point3d pt)
        {
            using (var brep = new Brep(reg))
            {
                var pc = new PointContainment();
                using (var brepEnt = brep.GetPointContainment(pt, out pc))
                {
                    return pc != PointContainment.Outside;
                }
            }
        }

        ///<summary>
        /// Returns whether a Region contains a set of Point3ds.
        ///</summary>
        ///<param name="pts">An array of points to test against the Region.</param>
        ///<returns>A Boolean indicating whether the Region contains
        /// all the points.
        /// </returns>
        public static bool ContainsPoints(this Region reg, Point3dCollection ptc)
        {
            var pts = new Point3d[ptc.Count];
            ptc.CopyTo(pts, 0);
            return reg.ContainsPoints(pts);
        }

        ///<summary>
        /// Returns whether a Region contains a set of Point3ds.
        ///</summary>
        ///<param name="pts">An array of points to test against the Region.</param>
        ///<returns>A Boolean indicating whether the Region contains
        /// all the points.
        /// </returns>
        public static bool ContainsPoints(this Region reg, Point3d[] pts)
        {
            using (var brep = new Brep(reg))
            {
                foreach (var pt in pts)
                {
                    var pc = new PointContainment();
                    using (var brepEnt = brep.GetPointContainment(pt, out pc))
                    {
                        if (pc == PointContainment.Outside)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 获取Region的顶点
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Point3dCollection Vertices(this Region region)
        {
            var vertices = new Point3dCollection();
            if (!region.IsNull)
            {
                using (var brepRegion = new Brep(region))
                {
                    foreach (var face in brepRegion.Faces)
                    {
                        foreach (var loop in face.Loops)
                        {
                            foreach (var vertex in loop.Vertices)
                            {
                                vertices.Add(vertex.Point);
                            }
                        }
                    }
                }
            }
            return vertices;
        }

        /// <summary>
        /// 从Region获取Polylines
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        /// https://www.keanw.com/2008/08/creating-a-seri.html
        public static DBObjectCollection ToPolylines(this Region reg)
        {
            // We will return a collection of entities
            // (should include closed Polylines and other
            // closed curves, such as Circles)
            DBObjectCollection res = new DBObjectCollection();
            // Explode Region -> collection of Curves / Regions
            DBObjectCollection cvs = new DBObjectCollection();
            reg.Explode(cvs);

            // Create a plane to convert 3D coords
            // into Region coord system
            Plane pl = new Plane(new Point3d(0, 0, 0), reg.Normal);
            using (pl)
            {
                bool finished = false;
                while (!finished && cvs.Count > 0)
                {
                    // Count the Curves and the non-Curves, and find
                    // the index of the first Curve in the collection
                    int cvCnt = 0, nonCvCnt = 0, fstCvIdx = -1;
                    for (int i = 0; i < cvs.Count; i++)
                    {
                        Curve tmpCv = cvs[i] as Curve;
                        if (tmpCv == null)
                            nonCvCnt++;
                        else
                        {
                            // Closed curves can go straight into the
                            // results collection, and aren't added
                            // to the Curve count
                            if (tmpCv.Closed)
                            {
                                res.Add(tmpCv);
                                cvs.Remove(tmpCv);
                                // Decrement, so we don't miss an item
                                i--;
                            }
                            else
                            {
                                cvCnt++;
                                if (fstCvIdx == -1)
                                    fstCvIdx = i;
                            }
                        }
                    }

                    if (fstCvIdx >= 0)
                    {
                        // For the initial segment take the first
                        // Curve in the collection
                        Curve fstCv = (Curve)cvs[fstCvIdx];
                        // The resulting Polyline
                        Polyline p = new Polyline();
                        // Set common entity properties from the Region
                        p.SetPropertiesFrom(reg);

                        // Add the first two vertices, but only set the
                        // bulge on the first (the second will be set
                        // retroactively from the second segment)
                        // We also assume the first segment is counter-
                        // clockwise (the default for arcs), as we're
                        // not swapping the order of the vertices to
                        // make them fit the Polyline's order
                        p.AddVertexAt(
                          p.NumberOfVertices,
                          fstCv.StartPoint.Convert2d(pl),
                          fstCv.BulgeFromCurve(false), 0, 0

                        );
                        p.AddVertexAt(
                          p.NumberOfVertices,
                          fstCv.EndPoint.Convert2d(pl),
                          0, 0, 0
                        );

                        cvs.Remove(fstCv);
                        // The next point to look for
                        Point3d nextPt = fstCv.EndPoint;
                        // We no longer need the curve
                        fstCv.Dispose();
                        // Find the line that is connected to
                        // the next point
                        // If for some reason the lines returned were not
                        // connected, we could loop endlessly.
                        // So we store the previous curve count and assume
                        // that if this count has not been decreased by
                        // looping completely through the segments once,
                        // then we should not continue to loop.
                        // Hopefully this will never happen, as the curves
                        // should form a closed loop, but anyway...
                        // Set the previous count as artificially high,
                        // so that we loop once, at least.
                        int prevCnt = cvs.Count + 1;
                        while (cvs.Count > nonCvCnt && cvs.Count < prevCnt)
                        {
                            prevCnt = cvs.Count;
                            foreach (DBObject obj in cvs)
                            {
                                Curve cv = obj as Curve;
                                if (cv != null)
                                {
                                    // If one end of the curve connects with the
                                    // point we're looking for...
                                    if (cv.StartPoint == nextPt || cv.EndPoint == nextPt)
                                    {
                                        // Calculate the bulge for the curve and
                                        // set it on the previous vertex
                                        double bulge = cv.BulgeFromCurve(cv.EndPoint == nextPt);
                                        if (bulge != 0.0)
                                            p.SetBulgeAt(p.NumberOfVertices - 1, bulge);

                                        // Reverse the points, if needed
                                        if (cv.StartPoint == nextPt)
                                            nextPt = cv.EndPoint;
                                        else
                                            // cv.EndPoint == nextPt
                                            nextPt = cv.StartPoint;

                                        // Add out new vertex (bulge will be set next
                                        // time through, as needed)
                                        p.AddVertexAt(
                                          p.NumberOfVertices,
                                          nextPt.Convert2d(pl),
                                          0, 0, 0
                                        );

                                        // Remove our curve from the list, which
                                        // decrements the count, of course
                                        cvs.Remove(cv);
                                        cv.Dispose();

                                        break;
                                    }
                                }
                            }
                        }


                        // Once we have added all the Polyline's vertices,
                        // transform it to the original region's plane
                        p.TransformBy(Matrix3d.PlaneToWorld(pl));
                        res.Add(p);

                        if (cvs.Count == nonCvCnt)
                            finished = true;
                    }


                    // If there are any Regions in the collection,
                    // recurse to explode and add their geometry
                    if (nonCvCnt > 0 && cvs.Count > 0)
                    {
                        foreach (DBObject obj in cvs)
                        {
                            Region subReg = obj as Region;
                            if (subReg != null)
                            {
                                DBObjectCollection subRes = subReg.ToPolylines();
                                foreach (DBObject o in subRes)
                                    res.Add(o);

                                cvs.Remove(subReg);
                                subReg.Dispose();
                            }
                        }
                    }

                    if (cvs.Count == 0)
                        finished = true;
                }
            }
            return res;
        }
    }
}