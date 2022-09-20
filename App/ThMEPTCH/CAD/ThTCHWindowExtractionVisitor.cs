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
            elements.AddRange(HandleTCHElement(dbObj, matrix, 0));
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix, int uid)
        {
            elements.AddRange(HandleTCHElement(dbObj, matrix, uid));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public override bool IsBuildElement(Entity e)
        {
            return e.IsTCHWindow();
        }

        public override bool CheckLayerValid(Entity e)
        {
            return true;
        }

        private List<ThRawIfcBuildingElementData> HandleTCHElement(Entity tch, Matrix3d matrix, int uid)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(tch) && CheckLayerValid(tch))
            {
                var archWindow = tch.Database.LoadWindowFromDb(tch.ObjectId, matrix, uid);
                if(archWindow.IsValid())
                {
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Data = archWindow,
                        Geometry = CreateOutline(archWindow),
                    });
                }
            }
            return results;
        }

        private Polyline CreateOutline(TArchWindow window)
        {
            var doorEntity = DBToTHEntityCommon.TArchWindowToEntityWindow(window);
            if (doorEntity.Outline != null)
            {
                return doorEntity.Outline.Shell();
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }
    }
}
