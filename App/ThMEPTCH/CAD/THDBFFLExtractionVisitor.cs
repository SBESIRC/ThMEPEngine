using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPTCH.Services;

namespace ThMEPTCH.CAD
{
    /// <summary>
    /// Finish Floor（建筑楼板） 
    /// </summary>
    public class THDBFFLExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(Handle(dbObj, matrix, 0));
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix, int uid)
        {
            elements.AddRange(Handle(dbObj, matrix, uid));
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

        private List<ThRawIfcBuildingElementData> Handle(Entity e, Matrix3d matrix, int uid)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(e) && CheckLayerValid(e))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = CreateFloorCurveEntity(e, matrix, uid),
                });
            }
            return results;
        }

        public override bool IsBuildElement(Entity e)
        {
            return e is Polyline;
        }

        private FloorCurveEntity CreateFloorCurveEntity(Entity e, Matrix3d matrix, int uid)
        {
            return new FloorCurveEntity(GetUniqueId(e, matrix, uid), GetOutline(e, matrix), "楼板");
        }

        private Entity GetOutline(Entity e, Matrix3d matrix)
        {
            return e.GetTransformedCopy(matrix);
        }

        private ulong GetUniqueId(Entity e, Matrix3d matrix, int uid)
        {
            return (ulong)ThMEPDbUniqueIdService.UniqueId(e.ObjectId, uid, matrix);
        }
    }
}
