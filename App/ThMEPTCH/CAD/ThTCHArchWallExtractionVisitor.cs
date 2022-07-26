using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public class ThTCHArchWallExtractionVisitor : ThBuildingElementExtractionVisitor
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
            return e.IsTCHWall();
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
                    Data = tch.Database.LoadWallFromDb(tch.ObjectId),
                });
            }
            return results;
        }
    }
}
