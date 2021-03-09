using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorMarkExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is DBText dbText)
            {
                elements.AddRange(HandleDbText(dbText, matrix));
            }
            else if (dbObj is MText mText)
            {
                elements.AddRange(HandleMText(mText, matrix));
            }
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

        private List<ThRawIfcBuildingElementData> HandleDbText(DBText dbText, Matrix3d matrix)
        {
            var texts = new List<DBText>();
            if (IsBuildElement(dbText) && IsDoorMark(dbText))
            {               
                var clone = dbText.Clone() as DBText;
                clone.TransformBy(matrix);
                texts.Add(clone);
            }
            return texts.Select(o => CreateBuildingElementData(o)).ToList();
        }
        private List<ThRawIfcBuildingElementData> HandleMText(MText mText, Matrix3d matrix)
        {
            var texts = new List<MText>();
            if (IsBuildElement(mText) && IsDoorMark(mText))
            {
                var clone = mText.Clone() as MText;
                clone.TransformBy(matrix);
                texts.Add(clone);
            }
            return texts.Select(o => CreateBuildingElementData(o)).ToList();
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(DBText dbText)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = dbText.TextOBB(),
                Data= dbText
            };
        }
        private ThRawIfcBuildingElementData CreateBuildingElementData(MText mText)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = mText.TextOBB(),
                Data = mText
            };
        }
        private new bool IsBuildElement(Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }
        private bool IsDoorMark(Entity entity)
        {
            var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
            return thPropertySet.IsDoor;
        }
    }
}
