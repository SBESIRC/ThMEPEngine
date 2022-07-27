using System.IO;
using System.Linq;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;

using ThCADExtension;
using ThMEPEngineCore.Service;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using DbPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSTransientService
    {
        private readonly DBObjectCollection _objs;

        private readonly ThNTSBufferService _bufferService;

        private TransientManager Manager => TransientManager.CurrentTransientManager;

        public ThPDSTransientService()
        {
            _objs = new DBObjectCollection();
            _bufferService = new ThNTSBufferService();
        }

        public void AddToTransient(ThPDSProjectGraphNode projectNode)
        {
            AddToTransient(projectNode.Load.Location);
        }

        private void AddToTransient(ThPDSLocation location)
        {
            if (location == null)
            {
                return;
            }
            if (location.BasePoint.EqualsTo(new ThPDSPoint3d(0.01, 0.01)))
            {
                Active.Editor.WriteLine("无法Zoom至指定负载");
                return;
            }
            foreach (Document doc in Application.DocumentManager)
            {
                using (var docLock = doc.LockDocument())
                using (var activeDb = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = Path.GetFileNameWithoutExtension(doc.Database.Filename);
                    if (location.ReferenceDWG.Equals(referenceDWG))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;

                        // Zoom
                        var scaleFactor = 2500.0;
                        var minPoint = new Point3d(location.BasePoint.X - scaleFactor, location.BasePoint.Y - scaleFactor, 0);
                        var maxPoint = new Point3d(location.BasePoint.X + scaleFactor, location.BasePoint.Y + scaleFactor, 0);
                        COMTool.ZoomWindow(minPoint, maxPoint);

                        // 高亮
                        var extents = new Extents3d(location.MinPoint.PDSPoint3dToPoint3d(), location.MaxPoint.PDSPoint3dToPoint3d());
                        ClearTransientGraphics();
                        AddToTransient(extents.ToRectangle());
                    }
                }
            }
        }

        private void AddToTransient(DbPolyline poly)
        {
            var frame = _bufferService.Buffer(poly, 100.0) as DbPolyline;
            if (frame != null)
            {
                frame.SetDatabaseDefaults();
                frame.Color = Color.FromRgb(255, 0, 0);
                frame.ConstantWidth = 50;
            }
            if (frame != null)
            {
                _objs.Add(frame);
                Manager.AddTransient(frame, TransientDrawingMode.Highlight, 1, new IntegerCollection());
            }
        }

        public void ClearTransientGraphics()
        {
            _objs.OfType<DbPolyline>().ForEach(e => Manager.EraseTransient(e, new IntegerCollection()));
        }
    }
}
