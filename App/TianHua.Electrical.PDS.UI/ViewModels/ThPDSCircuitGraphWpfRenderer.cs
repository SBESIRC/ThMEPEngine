using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphWpfRenderer : ThPDSCircuitGraphRenderer
    {
        public string Left;
        public List<string> Rights;
        public List<PDSBlockInfo> PDSBlockInfos;
        public Canvas Canvas;
        public override void Render(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph, ThPDSProjectGraphNode node, ThPDSCircuitGraphRenderContext context)
        {
            var infos = PDSBlockInfos;
            using var bmp = new System.Drawing.Bitmap(1000, 1000);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            var pen = new System.Drawing.Pen(System.Drawing.Brushes.Black);

            var left = Left;
            Point3d basePoint = new Point3d(0, -50, 0);
            var data = new DrawingData
            {
                LeftItem = new LeftItem() { Type = left, },
            };
            var rits = data.RightItems = new List<RightItem>();
            //new string[] { "常规", "漏电", "接触器控制", "热继电器保护", "配电计量（上海CT）", "配电计量（上海直接表）", "配电计量（CT表在前）", "配电计量（直接表在前）", "配电计量（CT表在后）", "配电计量（直接表在后）", "电动机（分立元件）", "电动机（CPS）", "电动机（分立元件星三角启动）", "电动机（CPS星三角启动）", "双速电动机（分立元件 D-YY）", "双速电动机（分立元件 Y-Y）", "双速电动机（CPS D-YY）", "双速电动机（CPS Y-Y）", "分支小母排", "小母排分支", "消防应急照明回路（WFEL）", "控制（从属接触器）", "控制（从属CPS）", "SPD附件" }
            if (Rights != null)
            {
                foreach (var name in Rights)
                {
                    rits.Add(new RightItem() { Type = name, });
                }
            }
            Point3d basePt;
            Point3d firstBusPt = default;
            Point3d lastBusPt = default;
            {
                var info = infos.FirstOrDefault(x => x.BlockName == data.LeftItem.Type);
                if (info is null) return;
                drawInfo(info, basePoint);
                basePt = basePoint.OffsetXY(info.Boundary.Width, 0);
                firstBusPt = basePt;
                lastBusPt = basePt;
            }
            var clickCtrls = new List<System.Windows.Controls.Canvas>(4096);
            foreach (var name in rits.Select(x => x.Type))
            {
                var info = infos.FirstOrDefault(x => x.BlockName == name);
                if (info is null) return;
                drawInfo(info, basePt);
                {
                    var cvs = new System.Windows.Controls.Canvas()
                    {
                        Width = info.Boundary.Width,
                        Height = info.Boundary.Height,
                    };
                    var v3 = basePt - info.BasePoint;
                    var v2 = v3.ToVector2d();
                    System.Windows.Controls.Canvas.SetLeft(cvs, (info.Boundary.LeftTop + v2).X);
                    System.Windows.Controls.Canvas.SetTop(cvs, -(info.Boundary.LeftTop + v2).Y);
                    cvs.Background = System.Windows.Media.Brushes.Transparent;
                    cvs.MouseEnter += (s, e) =>
                    {

                        cvs.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 50));
                    };
                    cvs.MouseLeave += (s, e) =>
                    {
                        cvs.Background = System.Windows.Media.Brushes.Transparent;
                    };
                    clickCtrls.Add(cvs);
                }
                basePt = basePt.OffsetXY(0, -info.Boundary.Height);
                lastBusPt = basePt;
            }

            {
                var m = new System.Drawing.Drawing2D.Matrix();
                m.Scale(1, -1);
                g.Transform = m;
                //var seg = new GLineSegment(firstBusPt.OffsetY(30), lastBusPt.OffsetY(-30));
                var seg = new GLineSegment(firstBusPt, lastBusPt);
                var _pen = new System.Drawing.Pen(System.Drawing.Brushes.Black, 5);
                g.DrawLine(_pen, (seg.StartPoint).ToPoint(), (seg.EndPoint).ToPoint());

                {
                    var cvs = new System.Windows.Controls.Canvas()
                    {
                        Width = 50,
                        Height = Math.Abs(seg.StartPoint.Y - seg.EndPoint.Y),
                    };
                    System.Windows.Controls.Canvas.SetLeft(cvs, seg.StartPoint.X - 25);
                    System.Windows.Controls.Canvas.SetTop(cvs, -seg.StartPoint.Y);
                    cvs.Background = System.Windows.Media.Brushes.Transparent;
                    cvs.MouseEnter += (s, e) =>
                    {
                        cvs.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 50));
                    };
                    cvs.MouseLeave += (s, e) =>
                    {
                        cvs.Background = System.Windows.Media.Brushes.Transparent;
                    };
                    clickCtrls.Add(cvs);
                }
            }

            static System.Windows.Media.Imaging.BitmapImage ToBitmapImage(System.Drawing.Image bitmap)
            {
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                var result = new System.Windows.Media.Imaging.BitmapImage();
                result.BeginInit();
                result.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }

            //var canvas = new System.Windows.Controls.Canvas();
            var canvas = (context as ThPDSCircuitWpfGraphRenderContext)?.Canvas ?? Canvas;
            if (canvas is null) return;
            canvas.Children.Clear();
            var imgCtrl = new System.Windows.Controls.Image()
            {
                Source = ToBitmapImage(bmp),
                Stretch = System.Windows.Media.Stretch.None,
            };
            canvas.Children.Add(imgCtrl);
            foreach (var ctrl in clickCtrls)
            {
                canvas.Children.Add(ctrl);
            }
            //var w = new System.Windows.Window()
            //{
            //    Content = canvas,
            //    Topmost = true,
            //    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            //};
            //w.Show();

            void drawInfo(PDSBlockInfo info, Point3d basePt)
            {
                var m = new System.Drawing.Drawing2D.Matrix();
                g.Transform = m;
                var v3 = basePt - info.BasePoint;
                var v2 = v3.ToVector2d();
                var fontsize = 10;
                var font = new System.Drawing.Font("宋体", fontsize);
                foreach (var ct in info.CTexts)
                {

                    var leftTop = (ct.Boundary.LeftTop + v2).ToPoint();
                    leftTop.Y = -leftTop.Y;
                    g.DrawString(ct.Text, font, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftTop, new System.Drawing.SizeF(10000, 10000)));
                }
                m = new System.Drawing.Drawing2D.Matrix();
                m.Scale(1, -1);
                g.Transform = m;
                foreach (var seg in info.Lines)
                {
                    g.DrawLine(pen, (seg.StartPoint + v2).ToPoint(), (seg.EndPoint + v2).ToPoint());
                }
                foreach (var c in info.Circles)
                {
                    g.DrawArc(pen, Convert.ToSingle(c.X + v2.X - c.Radius), Convert.ToSingle(c.Y + v2.Y - c.Radius), Convert.ToSingle(c.Radius * 2), Convert.ToSingle(c.Radius * 2), 0, 360);
                }
                foreach (var a in info.Arcs)
                {
                    g.DrawArc(pen, Convert.ToSingle(a.X + v2.X - a.Radius), Convert.ToSingle(a.Y + v2.Y - a.Radius), Convert.ToSingle(a.Radius * 2), Convert.ToSingle(a.Radius * 2), Convert.ToSingle(a.StartAngle.AngleToDegree()), Convert.ToSingle(a.EndAngle.AngleToDegree()));
                }
            }

        }
    }
    public static class PointConvertExtensions
    {
        public static Point ToPoint(this Autodesk.AutoCAD.Geometry.Point2d pt) => new(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
    }
}