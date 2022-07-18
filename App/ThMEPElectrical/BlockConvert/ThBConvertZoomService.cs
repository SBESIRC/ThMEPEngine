using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;

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

        public void Zoom(ThBConvertCompareModel model, double scale)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            {
                if (model.Database == activeDb.Database)
                {
                    if (model.SourceId != ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Zoom(model.SourceId);
                        Highlight(activeDb, new List<ObjectId> { model.SourceId, model.TargetId }, scale);
                    }
                    else if (model.SourceId != ObjectId.Null && model.TargetId == ObjectId.Null)
                    {
                        Zoom(model.SourceId);
                        Highlight(activeDb, new List<ObjectId> { model.SourceId }, scale);
                    }
                    else if (model.SourceId == ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Zoom(model.TargetId);
                        Highlight(activeDb, new List<ObjectId> { model.TargetId }, scale);
                    }
                    else if (model.TargetIdList.Count > 0)
                    {
                        Zoom(model.TargetIdList[0]);
                        Highlight(activeDb, model.TargetIdList, scale);
                    }
                }
            }
        }

        private void Highlight(AcadDatabase activeDb, List<ObjectId> objectIds, double scale)
        {
            ClearTransientGraphics();
            for (var i = 0; i < objectIds.Count; i++)
            {
                var obb = ThBConvertObbService.BlockObb(activeDb, objectIds[i], scale);
                var buffer = obb.Buffer(100.0).OfType<DbPolyline>().First().GeometricExtents;

                // 高亮
                var extents = new Extents3d(buffer.MinPoint, buffer.MaxPoint).ToRectangle();
                AddToTransient(extents);
            }
        }

        private void Zoom(ObjectId objectId)
        {
            var position = objectId.GetBlockPosition();

            // Zoom
            var scaleFactor = 2500.0;
            var minPoint = new Point3d(position.X - scaleFactor, position.Y - scaleFactor, 0);
            var maxPoint = new Point3d(position.X + scaleFactor, position.Y + scaleFactor, 0);
            COMTool.ZoomWindow(minPoint, maxPoint);
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
            _objs.Clear();
        }
    }
}