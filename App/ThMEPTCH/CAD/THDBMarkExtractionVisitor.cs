using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class THDBMarkExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleElement(dbObj, matrix));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public override bool IsBuildElement(Entity e)
        {
            return e is DBText;
        }

        public override bool CheckLayerValid(Entity e)
        {
            return e.Layer == "S_BEAM_TEXT_VERT";
        }

        private List<ThRawIfcBuildingElementData> HandleElement(Entity entity, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(entity) && CheckLayerValid(entity))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = entity,
                    Data = CreatStructureEntity(entity, matrix),
                });
            }
            return results;
        }

        private THStructureDBText CreatStructureEntity(Entity entity, Matrix3d matrix)
        {
            DBText dBText = entity.Clone() as DBText;
            dBText.TransformBy(matrix);
            var outline = dBText.TextOBB();
            var centerPt = outline.GetRectangleCenterPt();
            return new THStructureDBText()
            {
                Outline = outline,
                TextType = DBTextType.BeamText,
                Content = dBText.TextString,
                Vector = Vector3d.XAxis.RotateBy(dBText.Rotation + Math.PI / 2, Vector3d.ZAxis),
                Point = centerPt,
                Uuid = dBText.Handle.Value,
            };
        }
    }
}
