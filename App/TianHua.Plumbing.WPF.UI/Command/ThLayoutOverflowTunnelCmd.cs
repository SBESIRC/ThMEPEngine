using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;

namespace TianHua.Plumbing.WPF.UI.Command
{
    public class ThLayoutOverflowTunnelCmd : ThMEPBaseCommand, IDisposable
    {
        private string layerName = "";
        public ThLayoutOverflowTunnelCmd(string _layerName)
        {
            ActionName = "放置过水洞";
            CommandName = "THCRGSD";
            layerName = _layerName;
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                ImportLayers();
                OpenLayer();
                UserInteract();
            }
        }

        private void ImportLayers()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), true);
            }
        }

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(layerName);
            }
        }

        private void UserInteract()
        {
            FocusToCAD();
            while (true)
            {
                var ppo = Active.Editor.GetPoint("\n选择第一个点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);

                    PromptCornerOptions optins = new PromptCornerOptions("\n选择第二个点", wcsPt);
                    var secPpo = Active.Editor.GetCorner(optins);
                    if (secPpo.Status == PromptStatus.OK)
                    {
                        var secWcsPt = secPpo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                        InsertBlock(wcsPt, secWcsPt);
                    }
                    else break;
                }
                else
                {
                    break;
                }
            }
        }

        private void InsertBlock(Point3d pt1, Point3d pt2)
        {
            var minX = Math.Min(pt1.X, pt2.X);
            var maxX = Math.Max(pt1.X, pt2.X);
            var minY = Math.Min(pt1.Y, pt2.Y);
            var maxY = Math.Max(pt1.Y, pt2.Y);
            if (minX == maxX || minY == maxY)
            {
                return;
            }

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);

            Line line1 = new Line(new Point3d(minX, minY, 0), new Point3d(maxX, maxY, 0));
            Line line2 = new Line(new Point3d(maxX, minY, 0), new Point3d(minX, maxY, 0));

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(
                        blockDb.Layers.ElementOrDefault(layerName), false);
                polyline.Layer = layerName;
                polyline.ColorIndex = 256;
                acadDatabase.ModelSpace.Add(polyline);

                line1.Layer = layerName;
                line1.ColorIndex = 256;
                acadDatabase.ModelSpace.Add(line1);

                line2.Layer = layerName;
                line2.ColorIndex = 256;
                acadDatabase.ModelSpace.Add(line2);
            }
        }

        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
