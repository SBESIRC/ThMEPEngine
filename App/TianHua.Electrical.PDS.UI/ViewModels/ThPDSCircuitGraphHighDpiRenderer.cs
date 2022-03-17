using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphHighDpiRenderer : ThPDSCircuitGraphWpfRenderer
    {
        static Point ToPoint(Point2d pt) => new(pt.X, pt.Y);
        static Rect ToRect(GRect r) => new(r.MinX, r.MinY, r.Width, r.Height);
        public void Render(Canvas canvas, AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            var shapeOutlinePen = new Pen(Brushes.Black, 1);
            shapeOutlinePen.Freeze();
            var dGroup = new DrawingGroup();
            using (var dc = dGroup.Open())
            {
                foreach (var info in PDSBlockInfos)
                {
                    foreach (var ct in info.CTexts)
                    {
                        dc.DrawText(new FormattedText(ct.Text, CultureInfo.GetCultureInfo("zh-cn"), FlowDirection.LeftToRight, new Typeface("Consolas"), 32, Brushes.Black, VisualTreeHelper.GetDpi(canvas).PixelsPerDip), ToPoint(ct.Boundary.LeftTop));
                    }
                    foreach (var line in info.Lines)
                    {
                        dc.DrawLine(shapeOutlinePen, ToPoint(line.StartPoint), ToPoint(line.EndPoint));
                    }
                    foreach (var c in info.Circles)
                    {
                        dc.DrawEllipse(Brushes.White, shapeOutlinePen, ToPoint(c.Center), c.Radius, c.Radius);
                    }
                    dc.DrawRectangle(Brushes.White, shapeOutlinePen, ToRect(info.Boundary));
                }
            }

            //// Display the drawing using an image control.
            //Image theImage = new Image();
            //DrawingImage dImageSource = new DrawingImage(dGroup);
            //theImage.Source = dImageSource;

            var br = new DrawingBrush(dGroup);
            canvas.Background = br;
        }
    }
}
