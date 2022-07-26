using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public class ThTCHDoorExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleTCHElement(dbObj, matrix));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public override bool IsBuildElement(Entity e)
        {
            return e.IsTCHOpening() && e.IsDoor();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }

        private List<ThRawIfcBuildingElementData> HandleTCHElement(Entity tch, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(tch) && CheckLayerValid(tch))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = tch.Database.LoadDoorFromDb(tch.ObjectId),
                });
            }
            return results;
        }
    }
}
