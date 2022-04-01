using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThSanitarywareExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThSanitarywareExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = CheckBlockNameIsValid;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            //if (!blockTableRecord.Explodable)
            //{
            //    return false;
            //}

            return true;
        }
        public override bool IsDistributionElement(Entity e)
        {
            return CheckQualifiedBlockName(e);
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, 
            BlockReference br, Matrix3d matrix)
        {
            if (IsDistributionElement(br))
            {
                var objs = ExplodeWithVisible(br);
                var newObjs = ExplodeToLines(objs,10.0);
                var obb = ToObb(newObjs.OfType<Line>().Where(o=>o.Length>0.0).ToCollection());
                objs.OfType<Entity>().ForEach(o => o.Dispose());
                newObjs.OfType<Entity>().ForEach(o =>
                {
                    if (!o.IsDisposed)
                    {
                        o.Dispose();
                    }
                });
                if (obb.Area > 1.0)
                {
                    var solid = obb.ToSolid();
                    solid.TransformBy(matrix);
                    var rt = Matrix3d.Rotation(br.Rotation, br.Normal, Point3d.Origin);
                    var data = new BlockInfo
                    {
                        Position = br.Position.TransformBy(matrix),
                        Matrix = matrix.PreMultiplyBy(rt),
                        Name = br.GetEffectiveName(),
                    };
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = solid.ToPolyline(),
                        Data = data,
                    });
                }
            }
        }
        private Polyline ToObb(DBObjectCollection basicCurves)
        {
            if (basicCurves.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(basicCurves);
                transformer.Transform(basicCurves);
                var obb = basicCurves.GetMinimumRectangle();
                transformer.Reset(obb);
                return obb;
            }
            return new Polyline() { Closed = true };
        }
        private DBObjectCollection ExplodeToLines(DBObjectCollection objs,double tesslateLength)
        {
            //理想是炸到Line,Arc,Circle,Ellipse,目前不支持对椭圆的处理，这里不抛出不支持的异常
            var results = new DBObjectCollection();
            objs.Cast<Entity>().Where(o => o is Curve).ForEach(c =>
                {
                    if(c is Line)
                    {
                        results.Add(c);
                    }
                    else if (c is Arc arc)
                    {
                        var poly = arc.TessellateArcWithArc(tesslateLength);
                        var subObjs = new DBObjectCollection();
                        poly.Explode(subObjs);
                        subObjs.Cast<Line>().ForEach(e => results.Add(e));
                    }
                    else if (c is Circle circle)
                    {
                        var poly = circle.TessellateCircleWithArc(tesslateLength);
                        var subObjs = new DBObjectCollection();
                        poly.Explode(subObjs);
                        subObjs.Cast<Line>().ForEach(e => results.Add(e));
                    }
                    else if(c is Ellipse ellipse)
                    {
                        var poly = ellipse.Tessellate(tesslateLength);
                        var subObjs = new DBObjectCollection();
                        poly.Explode(subObjs);
                        subObjs.Cast<Line>().ForEach(e => results.Add(e));
                    }
                    else if(c is Polyline poly)
                    {
                        var newPoly = poly.Tessellate(tesslateLength);
                        var subObjs = new DBObjectCollection();
                        newPoly.Explode(subObjs);
                        subObjs.Cast<Line>().ForEach(e => results.Add(e));
                    }
                    else if(c is Polyline2d poly2d)
                    {
                        var subObjs = new DBObjectCollection();
                        poly2d.Explode(subObjs);
                        subObjs.Cast<Curve>().ForEach(e =>
                        {
                            if(e is Line)
                            {
                                results.Add(e);
                            }
                            else if(e is Arc arc)
                            {
                                var poly = arc.TessellateArcWithArc(tesslateLength);
                                var subObjs = new DBObjectCollection();
                                poly.Explode(subObjs);
                                subObjs.Cast<Line>().ForEach(e => results.Add(e));
                            }
                        });
                    }
                });
            return results;
        }

        private bool CheckBlockNameIsValid(Entity e)
        {
            return false;
        }

        private DBObjectCollection ExplodeWithVisible(BlockReference blockReference)
        {
            var objs = new DBObjectCollection();
            var newObjs = new DBObjectCollection();
            blockReference.Explode(objs);
            objs.OfType<Entity>().ForEach(o =>
            {
                if (o is Curve curve)
                {
                    newObjs.Add(curve);
                }
                else if (o is BlockReference br)
                { 
                    ExplodeWithVisible(br).OfType<Entity>().ForEach(e => newObjs.Add(e));
                }
                else if(o.IsTCHElement())
                {
                   o.ExplodeTCHElement().OfType<Curve>().ForEach(c=> newObjs.Add(c));
                }
            });
            return newObjs.OfType<Entity>()
                .Where(e => e.Visible)
                .Where(e => e.Bounds.HasValue)
                .ToCollection();
        }
    }
    public class BlockInfo
    {
        public Point3d Position { get; set; }
        public Matrix3d Matrix { get; set; }
        public string Name { get; set; }
    }
}
