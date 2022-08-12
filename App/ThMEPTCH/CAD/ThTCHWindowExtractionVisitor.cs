using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using ThMEPTCH.TCHArchDataConvert;
using ThCADExtension;

namespace ThMEPTCH.CAD
{
    internal class ThTCHWindowExtractionVisitor : ThBuildingElementExtractionVisitor
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
            return e.IsTCHOpening() && e.IsWindow();
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
                var archWindow = tch.Database.LoadWindowFromDb(tch.ObjectId, matrix);
                results.Add(new ThRawIfcBuildingElementData()
                {

                    Data = archWindow,
                    Geometry = CreateOutline(archWindow),
                });
            }
            return results;
        }

        private Polyline CreateOutline(TArchWindow window)
        {
            var doorEntity = DBToTHEntityCommon.TArchWindowToEntityWindow(window, new Vector3d(0, 0, 0));
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
