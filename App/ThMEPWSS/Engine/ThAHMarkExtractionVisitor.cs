using System;
using System.Linq;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Engine
{

    public class ThAHMarkExtractionVisitor: ThBuildingElementExtractionVisitor
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
                elements.RemoveAll(o => !xclip.Contains(GetTextPosition(o.Geometry)));
            }
        }

        private List<ThRawIfcBuildingElementData> HandleDbText(DBText dbText, Matrix3d matrix)
        {
            var texts = new List<DBText>();
            if (IsBuildElement(dbText) && IsAHMark(dbText.TextString))
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
            if (IsBuildElement(mText) && IsAHMark(mText.Contents))
            {
                var clone = mText.Clone() as MText;
                clone.TransformBy(matrix);
                texts.Add(clone);
            }
            return texts.Select(o => CreateBuildingElementData(o)).ToList();
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Entity text)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = text,
            };
        }
        private bool IsAHMark(string content)
        {
            // etc
            return content.ToLower().Contains("ah1") || content.ToLower().Contains("ah2");
        }
        private Point3d GetTextPosition(object ent)
        {
            if (ent is DBText dbText)
            {
                return dbText.Position;
            }
            else if (ent is MText mText)
            {
                return mText.Location;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
