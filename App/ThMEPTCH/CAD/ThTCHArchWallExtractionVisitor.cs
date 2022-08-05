using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using ThMEPTCH.TCHArchDataConvert;
using ThCADExtension;

namespace ThMEPTCH.CAD
{
    public class ThTCHArchWallExtractionVisitor : ThBuildingElementExtractionVisitor
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
            elements.AddRange(HandleTCHElement(dbObj, matrix));
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
            return e.IsTCHWall();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }

        private List<ThRawIfcBuildingElementData> HandleTCHElement(Entity tch, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(tch) && CheckLayerValid(tch) && tch.Visible && tch.Bounds.HasValue)
            {
                var archWall = tch.Database.LoadWallFromDb(tch.ObjectId);
                var outline = CreateOutline(archWall);
                outline.TransformBy(matrix);
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = archWall,
                    Geometry = outline,
                });
            }
            return results;
        }

        private Polyline CreateOutline(TArchWall archWall)
        {
            var wallEntity = DBToTHEntityCommon.TArchWallToEntityWall(archWall,0,0,0,0,new Vector3d(0,0,0));
            if(wallEntity.OutLine !=null)
            {
                return wallEntity.OutLine.Shell();
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }
    }
}
