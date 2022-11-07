using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Geom;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.ImagePlot.Service
{
    public class ThCADImagePlotService
    {
        static bool IsDarkLayer(string layer) => false;

        public class ThBasicBuildingElementExtractionVisitor : ThBuildingElementExtractionVisitor
        {
            public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
            {
                // 忽略图纸空间和匿名块
                if (blockTableRecord.IsLayout)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public override bool IsBuildElementBlockReference(BlockReference blockReference)
            {
                // 添加要筛选的块名规则
                return base.IsBuildElementBlockReference(blockReference);
            }

            public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
            {
                if (dbObj is Curve curve)
                {
                    elements.AddRange(HandleCurve(curve, matrix));
                }
            }

            public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
            {
                var xclip = blockReference.XClipInfo();
                if (xclip.IsValid)
                {
                    xclip.TransformBy(matrix);
                    if (!xclip.Inverted)
                    {
                        elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
                    }
                    else
                    {
                        elements.RemoveAll(o => xclip.Contains(o.Geometry as Curve));
                    }
                }
            }
            private List<ThRawIfcBuildingElementData> HandleCurve(Curve curve, Matrix3d matrix)
            {
                var results = new List<ThRawIfcBuildingElementData>();
                if (IsBuildElement(curve) && CheckLayerValid(curve) && curve.Visible)
                {
                    var clone = curve.WashClone();
                    if (clone != null)
                    {
                        try
                        {
                            clone.TransformBy(matrix);
                            results.Add(new ThRawIfcBuildingElementData()
                            {
                                Geometry = clone,
                            });
                        }
                        catch
                        {
                        }
                    }
                }
                return results;
            }
        }
        public static DBObjectCollection ExtractBaseElements(Database database)
        {
            var validLayers = ThDbLayerManager.Layers(database)
                .Where(o => !IsDarkLayer(o))
                .ToList();
            var visitor = new ThBasicBuildingElementExtractionVisitor()
            {
                LayerFilter = validLayers.ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            return visitor.Results.Select(o => o.Geometry).ToCollection();
        }

        public static void ImageOut()
        {
            try
            {
                var r1 = Active.Editor.GetString("\nstep 1: Select output type [bmp, png, jpg]");
                if (r1.Status != PromptStatus.OK) return;
                var r2 = Active.Editor.GetDouble("\nstep2: input mm to pixel scale (default is .25): ");
                if (r2.Status != PromptStatus.OK) return;
                if (!double.TryParse(r2.StringResult, out var scale)) scale = .25;
                var r3 = Active.Editor.GetString("\nstep3 : select output folder.");
                if (r3.Status != PromptStatus.OK) return;
                var dir = r3.StringResult;
                if (dir.EndsWith(":")) dir += "\\";
                var imgFmt = r1.StringResult;
                var imgFormat = imgFmt switch
                {
                    "bmp" => ImageFormat.Bmp,
                    "png" => ImageFormat.Png,
                    "jpg" => ImageFormat.Jpeg,
                    "jpeg" => ImageFormat.Jpeg,
                    _ => throw new NotSupportedException(),
                };
                var tk = DateTime.Now.Ticks;
                var t0 = DateTime.Now;
                using (Active.Document.LockDocument())
                using (var adb = AcadDatabase.Active())
                {
                    var color = Color.FromArgb(127, 0, 0, 0);
                    if (imgFormat != ImageFormat.Png) color = Color.White;
                    using var brush = new SolidBrush(color);
                    using var pen = new Pen(brush);
                    var ents = ExtractBaseElements(adb.Database).OfType<Entity>().ToList();
                    foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(x => x.BlockId.IsValid && x.GetEffectiveName() is "THAPE_A1L_inner"))
                    {
                        var rect = br.Bounds.Value.ToGRect();
                        var v = -rect.LeftButtom.ToVector2d();
                        var m = Matrix3d.Displacement(v.ToVector3d() + new Vector3d(0, -rect.Height, 0)).PreMultiplyBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, 0, 0), new Vector3d(1, 0, 0)))).PreMultiplyBy(Matrix3d.Scaling(scale, default));
                        var width = rect.Width * scale;
                        var height = rect.Height * scale;

                        if (width <= 0) return;
                        if (height <= 0) return;

                        using var bmp = new Bitmap(Convert.ToInt32(width), Convert.ToInt32(height));
                        using var g = Graphics.FromImage(bmp);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);
                        foreach (var ent in ents)
                        {
                            if (ent is Line line)
                            {
                                var seg = line.ToGLineSegment().TransformBy(m);
                                g.DrawLine(pen, seg.StartPoint.ToPoint(), seg.EndPoint.ToPoint());
                            }
                            else if (ent is Polyline pl)
                            {
                                var n = pl.NumberOfVertices;
                                if (n > 0)
                                {
                                    var st = pl.GetPoint3dAt(0).TransformBy(m).ToPointF();
                                    var pt = st;
                                    PointF ed = default;
                                    for (int i = 1; i < n; i++)
                                    {
                                        ed = pl.GetPoint3dAt(i).TransformBy(m).ToPointF();
                                        g.DrawLine(pen, st, ed);
                                        st = ed;
                                    }
                                    if (pl.Closed) g.DrawLine(pen, pt, ed);
                                }
                            }
                            else if (ent is Circle circle)
                            {
                                var c = circle.Center.TransformBy(m).ToPoint();
                                var r = Convert.ToInt32(circle.Radius * scale);
                                g.DrawEllipse(pen, new Rectangle(c.X - r, c.Y - r, 2 * r, 2 * r));
                            }
                            else if (ent is Curve curve)
                            {
                                try
                                {
                                    var pts = new List<Point3d>(16);
                                    var pr = curve.StartParam + .01;
                                    while (pr < curve.EndParam)
                                    {
                                        var ed = curve.GetPointAtParameter(pr);
                                        pts.Add(ed);
                                        pr += .1;
                                    }
                                    if (pts.Count > 1)
                                    {
                                        g.DrawCurve(pen, pts.Select(x => x.TransformBy(m).ToPointF()).ToArray());
                                    }
                                }
                                catch { }
                            }
                        }
                        var temp_file = Path.Combine(dir, ++tk + "." + imgFmt);
                        bmp.Save(temp_file, imgFormat);
                    }
                    Console.WriteLine(DateTime.Now - t0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
