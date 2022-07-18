using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertZoomService
    {
        public ThBConvertZoomService()
        {

        }

        public void Zoom(ThBConvertCompareModel model)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            {
                if (model.Database == activeDb.Database)
                {
                    if (model.SourceId != ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Zoom(model.SourceId);
                    }
                    else if (model.SourceId != ObjectId.Null && model.TargetId == ObjectId.Null)
                    {
                        Zoom(model.SourceId);
                    }
                    else if (model.SourceId == ObjectId.Null && model.TargetId != ObjectId.Null)
                    {
                        Zoom(model.TargetId);
                    }
                    else if (model.TargetIdList.Count > 0)
                    {
                        Zoom(model.TargetIdList[0]);
                    }
                }
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
    }
}