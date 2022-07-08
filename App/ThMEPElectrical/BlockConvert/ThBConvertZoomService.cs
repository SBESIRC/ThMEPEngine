using System.Linq;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Service;
using DbPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertZoomService
    {
        private readonly DBObjectCollection _objs;

        private readonly ThNTSBufferService _bufferService;

        private TransientManager Manager => TransientManager.CurrentTransientManager;

        public ThBConvertZoomService()
        {
            _objs = new DBObjectCollection();
            _bufferService = new ThNTSBufferService();
        }

        public void Zoom(ThBConvertCompareModel model)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            {
                if (model.Database.Equals(activeDb.Database))
                {
                    if (model.SourceId != ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Highlight(activeDb, model.SourceId, true);
                        Highlight(activeDb, model.TargetId);
                    }
                    else if (model.SourceId != ObjectId.Null && model.TargetId == ObjectId.Null)
                    {
                        Highlight(activeDb, model.SourceId, true);
                    }
                    else if (model.SourceId == ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Highlight(activeDb, model.TargetId, true);
                    }
                    else if (model.TargetIdList.Count > 0)
                    {
                        Highlight(activeDb, model.TargetIdList[0], true);
                        for (var i = 1; i < model.TargetIdList.Count; i++)
                        {
                            Highlight(activeDb, model.TargetIdList[i]);
                        }
                    }
                }
            }
        }

        private void Highlight(AcadDatabase activeDb, ObjectId objectId, bool zoom = false)
        {
            var obb = ThBConvertObbService.BlockObb(activeDb, objectId);
            var buffer = obb.Buffer(100.0).OfType<DbPolyline>().First().GeometricExtents;

            // 高亮
            var extents = new Extents3d(buffer.MinPoint, buffer.MaxPoint);
            ClearTransientGraphics();
            AddToTransient(extents.ToRectangle());

            if (zoom)
            {
                // Zoom
                var scaleFactor = 2500.0;
                var minPoint = new Point3d(buffer.MinPoint.X - scaleFactor, buffer.MinPoint.Y - scaleFactor, 0);
                var maxPoint = new Point3d(buffer.MaxPoint.X + scaleFactor, buffer.MaxPoint.Y + scaleFactor, 0);
                COMTool.ZoomWindow(minPoint, maxPoint);
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
