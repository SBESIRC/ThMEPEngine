using ThCADExtension;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.CAD
{
    public class ThTCHDoorExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleTCHElement(dbObj, matrix, 0));
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix, int uid)
        {
            elements.AddRange(HandleTCHElement(dbObj, matrix, uid));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        public override bool IsBuildElement(Entity e)
        {
            return e.IsTCHOpening() && e.IsDoor();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }

        private List<ThRawIfcBuildingElementData> HandleTCHElement(Entity tch, Matrix3d matrix, int uid)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(tch) && CheckLayerValid(tch) && tch.Visible && tch.Bounds.HasValue)
            {
                var archDoor = tch.Database.LoadDoorFromDb(tch.ObjectId, matrix, uid);
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = archDoor,
                    Geometry = CreateOutline(archDoor),
                });
            }
            return results;
        }

        private Polyline CreateOutline(TArchDoor archDoor)
        {
            var doorEntity = DBToTHEntityCommon.TArchDoorToEntityDoor(archDoor, new Vector3d(0, 0, 0));
            if (doorEntity.OutLine != null)
            {
                return doorEntity.OutLine.Shell();
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }
    }
}
