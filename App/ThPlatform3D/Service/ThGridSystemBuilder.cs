using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.CAD;

namespace ThPlatform3D.Service
{
    internal class ThGridSystemBuilder
    {
        private ThGridLineSyetemData _gridSystemData;
        private DBObjectCollection _gridLines;
        private List<DBObjectCollection> _dimensionGroups;
        private List<List<DBObjectCollection>> _circleLabelGroups;

        public DBObjectCollection GridLines => _gridLines;
        public List<DBObjectCollection> DimensionGroups => _dimensionGroups;
        public List<List<DBObjectCollection>> CircleLabelGroups => _circleLabelGroups;

        private ThGridSystemBuilder()
        {
            _gridLines = new DBObjectCollection();
            _dimensionGroups = new List<DBObjectCollection>();
            _circleLabelGroups = new List<List<DBObjectCollection>>();
        }

        public ThGridSystemBuilder(ThGridLineSyetemData gridSystemData):this()
        {
            _gridSystemData = gridSystemData;
        }

        public void Build()
        {
            if(_gridSystemData==null)
            {
                return;
            }

            // 创建轴网线
            _gridLines = BuildGridLines();

            // 创建对齐标注
            _dimensionGroups = BuildDimensionGroups();

            // 创建轴网编号
            _circleLabelGroups = BuildCircleLabelGroups();
        }

        private DBObjectCollection BuildGridLines()
        {
            var results  = new DBObjectCollection();
            _gridSystemData
                .GridLines
                .OfType<ThTCHPolyline>()
                .ForEach(p =>
            {
                if(p.Points.Count==2)
                {
                    var line = new Line(p.Points[0].ToPoint3d(), p.Points[1].ToPoint3d());
                    results.Add(line);
                }
                else if(p.Points.Count > 2)
                {
                    var polyline = p.ToPolyline();
                    results.Add(polyline);
                }
            });
            return results;
        }

        private List<DBObjectCollection> BuildDimensionGroups()
        {
            var groups = new List<DBObjectCollection>();
            _gridSystemData
                .DimensionGroups
                .OfType<ThDimensionGroupData>()
                .ForEach(g =>
            {
                var group = g.Dimensions
                    .OfType<ThAlignedDimension>()
                    .Select(a => CreateAlignedDimension(a))
                    .ToCollection();
                groups.Add(group);
            });
            return groups;
        }

        private AlignedDimension CreateAlignedDimension(ThAlignedDimension alignedDimension)
        {
            return new AlignedDimension()
            {
                XLine1Point = alignedDimension.XLine1Point.ToPoint3d(),
                XLine2Point = alignedDimension.XLine2Point.ToPoint3d(),
                DimLinePoint = alignedDimension.DimLinePoint.ToPoint3d(),
            };
        }

        private List<List<DBObjectCollection>> BuildCircleLabelGroups()
        {
            var allGroup = new List<List<DBObjectCollection>>();
            _gridSystemData
                .CircleLableGroups
                .OfType<ThCircleLableGroupData>()
                .ForEach(g =>
                {
                    var singleGroup = new List<DBObjectCollection>();
                    g.CircleLables
                        .OfType<CircleLable>()
                        .ForEach(cl => singleGroup.Add(CreateCircleLabel(cl)));
                    allGroup.Add(singleGroup);
                });
            return allGroup;
        }

        private DBObjectCollection CreateCircleLabel(CircleLable circleLable)
        {
            var results = new DBObjectCollection();
            // 引线
            circleLable.Leaders
                .OfType<ThTCHLine>()
                .ForEach(o =>
                {
                    var line = new Line()
                    {
                        StartPoint = o.StartPt.ToPoint3d(),
                        EndPoint = o.EndPt.ToPoint3d(),
                    };
                    results.Add(line);
                });

            // 圆圈
            var circle = new Circle()
            {
                Center = circleLable.Circle.Center.ToPoint3d(),
                Radius = circleLable.Circle.Radius,
                Normal = Autodesk.AutoCAD.Geometry.Vector3d.ZAxis,
            };
            results.Add(circle);

            // 文字
            var text = new DBText();
            text.Height = circle.Diameter * 0.7;
            text.WidthFactor = 0.7;
            text.Position = circle.Center;            
            text.HorizontalMode = TextHorizontalMode.TextMid;
            text.VerticalMode = TextVerticalMode.TextVerticalMid;
            text.TextString = circleLable.Mark;
            text.AlignmentPoint = text.Position;
            results.Add(text);
            return results;
        }
    }
}
