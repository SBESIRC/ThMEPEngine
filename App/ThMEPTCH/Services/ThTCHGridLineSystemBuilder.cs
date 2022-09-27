using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPTCH.CAD;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.Services
{
    internal class ThTCHGridLineSystemBuilder
    {
        private List<FloorCurveEntity> _axisLines;
        private List<FloorCurveEntity> _gridDimensions;
        private List<FloorCurveEntity> _continuousDimensions;
        public ThTCHGridLineSystemBuilder(List<FloorCurveEntity> axisDatas)
        {
            Init(axisDatas);
        }
        private void Init(List<FloorCurveEntity> axisDatas)
        {
            _axisLines = new List<FloorCurveEntity>();
            _gridDimensions = new List<FloorCurveEntity>();
            _continuousDimensions = new List<FloorCurveEntity>();
            axisDatas.ForEach(o =>
            {
                if (o.Property is TCHAxisProperty property)
                {
                    switch (property.Category)
                    {
                        case ThTCHAxisLineExtractionVisitor.CategoryValue:
                            _axisLines.Add(o);
                            break;
                        case ThTCHAxisGridDimensionExtractionVisitor.CategoryValue:
                            _gridDimensions.Add(o);
                            break;
                        case ThTCHAxisContinuousDimensionExtractionVisitor.CategoryValue:
                            _continuousDimensions.Add(o);
                            break;
                        default:
                            break;
                    }
                }
            });
        }

        public ThGridLineSyetemData Build()
        {
            var data = new ThGridLineSyetemData();
            // 轴网数据
            var gridLines = CreateGridLines(_axisLines);
            gridLines.ForEach(o => data.GridLines.Add(o));

            // 连续标注
            var dimensionGroups = CreateDimensionGroups(_continuousDimensions);
            dimensionGroups.ForEach(o => data.DimensionGroups.Add(o));

            // 轴网标注
            var circleLableGroups = CreateCircleLableGroups(_gridDimensions);
            circleLableGroups.ForEach(o => data.CircleLableGroups.Add(o));
            return data;
        }

        private List<ThTCHPolyline> CreateGridLines(List<FloorCurveEntity> axisLines)
        {
            var gridLines = new List<ThTCHPolyline>();
            axisLines.ForEach(o =>
            {
                var tchPolyline = new ThTCHPolyline();
                var property =  o.Property as TCHAxisProperty;
                if (o.EntityCurve is Line line)
                {
                    gridLines.Add(line.ToTCHPolyline());
                }
                else if (o.EntityCurve is Arc arc)
                {
                    //
                }
                else if(o.EntityCurve is Polyline polyline)
                {
                    gridLines.Add(polyline.ToTCHPolyline());
                }
            });
            return gridLines;
        }

        private List<ThDimensionGroupData> CreateDimensionGroups(List<FloorCurveEntity> continuousDimensions)
        {
            var dimensionGroups = new List<ThDimensionGroupData>();
            continuousDimensions.ForEach(o =>
            {
                var groupData = new ThDimensionGroupData();
                var objs = o.EntityCurve.ExplodeTCHElement();
                objs.OfType<AlignedDimension>().ForEach(dimension =>
                {
                    groupData.Dimensions.Add(ConvertTo(dimension));
                });
                dimensionGroups.Add(groupData);
            });
            return dimensionGroups;
        }

        private List<ThCircleLableGroupData> CreateCircleLableGroups(List<FloorCurveEntity> gridDimensions)
        {
            var circleLableGroups = new List<ThCircleLableGroupData>();
            gridDimensions.ForEach(o =>
            {
                var groupData = new ThCircleLableGroupData();
                //var objs = o.EntityCurve.ExplodeTCHElement();
                //var circleLabelGroups = GroupCircleLabel(objs);
                var circleLabelGroups = GroupCircleLabel(o.EntityCurve);
                circleLabelGroups.ForEach(g =>
                {
                    var circleLable = new CircleLable()
                    {
                        Mark = g.Text.TextString,
                        Circle = g.Circle.ToTCHCircle(),
                    };
                    var leaders = ConvertTo(g.Path);
                    leaders.ForEach(l => circleLable.Leaders.Add(l));
                    groupData.CircleLables.Add(circleLable);
                });
                circleLableGroups.Add(groupData);
            });
            return circleLableGroups;
        }

        private List<ThTCHLine> ConvertTo(List<Curve> curves)
        {
            var results = new List<ThTCHLine>();
            curves.ForEach(c =>
            {
                if (c is Line line)
                {
                    results.Add(line.ToTCHLine());
                }
                else if(c is Polyline poly)
                {
                    for(int i=0;i<poly.NumberOfVertices;i++)
                    {
                        var st = poly.GetSegmentType(i);
                        if(st== SegmentType.Line)
                        {
                            var lineSeg = poly.GetLineSegmentAt(i);
                            results.Add(new ThTCHLine()
                            {
                                StartPt = lineSeg.StartPoint.ToTCHPoint(),
                                EndPt = lineSeg.EndPoint.ToTCHPoint()
                            });
                        }
                        else if(st == SegmentType.Arc)
                        {
                            var arcSeg = poly.GetArcSegmentAt(i);
                            results.Add(new ThTCHLine()
                            {
                                StartPt = arcSeg.StartPoint.ToTCHPoint(),
                                EndPt = arcSeg.EndPoint.ToTCHPoint()
                            });                            
                        }
                        else
                        {
                            //
                        }
                    }
                }
            });
            return results;
        }

        private List<CircleLableGroup> GroupCircleLabel(Entity entity)
        {
            //按照天正炸的顺序构建
            var groups = new List<CircleLableGroup>();
            var entitySet = new DBObjectCollection();
            entity.Explode(entitySet);
            for(int i=0;i<entitySet.Count;i++)
            {
                if(entitySet[i] is Line)
                {
                    var curves = new List<Curve>();
                    var circleLabels = new DBObjectCollection();
                    int j = i;
                    for (;j< entitySet.Count;j++)
                    {
                        var current = entitySet[j] as Entity;
                        if (current is Line || current is Polyline)
                        {
                            curves.Add(current as Curve);
                        }
                        else if(current.IsTCHElement())
                        {
                            current.Explode(circleLabels);
                            break;
                        }
                    }
                    if(circleLabels.Count==2)
                    {
                        var circles = circleLabels.OfType<Circle>();
                        var dbTexts = circleLabels.OfType<DBText>();
                        if(circles.Count()==1 && dbTexts.Count()==1)
                        {
                            groups.Add(new CircleLableGroup(curves, circles.First(), dbTexts.First()));
                        }
                    }
                    i = j;
                }
            }            
            return groups;
        }

        private List<CircleLableGroup> GroupCircleLabel(DBObjectCollection circleLabels)
        {
            //按照逻辑构建
            var transformer = new ThMEPOriginTransformer(circleLabels);
            transformer.Transform(circleLabels);
            var grouper = new ThCircleLableGrouper(circleLabels);
            grouper.Group();
            transformer.Reset(circleLabels);
            return grouper.Groups;
        }

        private ThAlignedDimension ConvertTo(AlignedDimension dimension)
        {
            var xLine1Point = dimension.XLine1Point;
            var xLine2Point = dimension.XLine2Point;
            var dimLinePoint = dimension.DimLinePoint;
            var projectionPt = dimLinePoint.GetProjectPtOnLine(xLine1Point, xLine2Point);
            var dir = projectionPt.GetVectorTo(dimLinePoint);
            var xLine1PointExtend = xLine1Point + dir;
            var xLine2PointExtend = xLine2Point + dir;

            var alignedDimension = new ThAlignedDimension()
            {
                XLine1Point = xLine1Point.ToTCHPoint(),
                XLine2Point = xLine2Point.ToTCHPoint(),
                DimLinePoint = dimLinePoint.ToTCHPoint(),
                Mark = dimension.DimensionText,
            };

            var xLine1 = new ThTCHLine()
            {
                StartPt = xLine1Point.ToTCHPoint(),
                EndPt = xLine1PointExtend.ToTCHPoint()
            };            
            alignedDimension.DimLines.Add(xLine1);
            var xLine2 = new ThTCHLine()
            {
                StartPt = xLine2Point.ToTCHPoint(),
                EndPt = xLine2PointExtend.ToTCHPoint(),
            };
            alignedDimension.DimLines.Add(xLine2);
            var dimLine = new ThTCHLine()
            {
                StartPt = xLine1Point.ToTCHPoint(),
                EndPt = xLine2Point.ToTCHPoint(),
            };
            alignedDimension.DimLines.Add(dimLine);
            return alignedDimension;
        }
    }
}
