using System.Linq;
using System.Collections.Generic;

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
    public class ThBConvertTransientService
    {
        private readonly DBObjectCollection _objs;

        private readonly ThNTSBufferService _bufferService;

        private TransientManager Manager => TransientManager.CurrentTransientManager;

        public ThBConvertTransientService()
        {
            _objs = new DBObjectCollection();
            _bufferService = new ThNTSBufferService();
        }

        public bool AddTransient(ThBConvertCompareModel model, double scale)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            {
                if (model.Database == activeDb.Database)
                {
                    if (model.SourceId != ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        return Highlight(activeDb, new List<ObjectId> { model.SourceId, model.TargetId }, scale);
                    }
                    else if (model.SourceId != ObjectId.Null && model.TargetId == ObjectId.Null)
                    {
                        return Highlight(activeDb, new List<ObjectId> { model.SourceId }, scale);
                    }
                    else if (model.SourceId == ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        return Highlight(activeDb, new List<ObjectId> { model.TargetId }, scale);
                    }
                    else if (model.TargetIdList.Count > 0)
                    {
                        return Highlight(activeDb, model.TargetIdList, scale);
                    }
                }
                return false;
            }
        }

        private bool Highlight(AcadDatabase activeDb, List<ObjectId> objectIds, double scale)
        {
            ClearTransientGraphics();
            for (var i = 0; i < objectIds.Count; i++)
            {
                var obb = ThBConvertObbService.BlockObb(activeDb, objectIds[i], scale);
                if (obb.Area > 1.0)
                {
                    var buffer = obb.Buffer(100.0).OfType<DbPolyline>().First().GeometricExtents;

                    // 高亮
                    var extents = new Extents3d(buffer.MinPoint, buffer.MaxPoint).ToRectangle();
                    AddToTransient(extents);
                }
                else
                {
                    return false;
                }
            }
            return true;
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
