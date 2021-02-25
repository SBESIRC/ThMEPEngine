using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is DBText dBText)
            {
                HandleDbText(dBText, matrix);
            }
        }

        public override void DoXClip(BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                Results.RemoveAll(o =>
                {
                    var annotation = o.Data as ThIfcBeamAnnotation;
                    return !xclip.Contains(annotation.Position.TransformBy(annotation.Matrix));
                });
            }
        }

        public override bool IsBuildElement(Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }

        private void HandleDbText(DBText dBText, Matrix3d matrix)
        {
            if (CheckLayerValid(dBText) &&
                IsBuildElement(dBText) &&
                IsBeamAnnotaion(dBText))
            {
                Results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = new ThIfcBeamAnnotation(dBText, matrix),
                });
            }
        }

        private bool IsBeamAnnotaion(DBText dBText)
        {
            return ThStructureBeamUtils.IsBeamAnnotaion(dBText.TextString);
        }
    }
}
