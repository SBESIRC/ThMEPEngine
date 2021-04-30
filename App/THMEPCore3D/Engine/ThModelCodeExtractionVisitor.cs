using System;
using ThCADExtension;
using THMEPCore3D.Model;
using THMEPCore3D.Service;
using THMEPCore3D.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Engine
{
    public class ThModelCodeExtractionVisitor : ThDB3ElementExtractionVisitor
    {
        public override void DoExtract(List<ThDb3ElementRawData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThDb3ElementRawData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();            
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private void HandleBlockReference(List<ThDb3ElementRawData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsBuildElement(blkref) && CheckLayerValid(blkref))
            {
                var data = Parse(blkref);
                if(data.Count>0)
                {
                    elements.Add(new ThDb3ElementRawData()
                    {
                        Data = data,
                        Geometry = blkref.GetTransformedCopy(matrix),
                    });
                }
            }
        }

        private Dictionary<string, string> Parse(Entity entity)
        {
            var instance = new ThHyperlinkParseService();
            instance.Parse(entity.Hyperlinks[0].Name);
            return instance.Properties;
        }

        public override bool IsBuildElement(Entity entity)
        {
            if(entity is BlockReference br)
            {
                string name = br.GetEffectiveName().ToUpper();
                if(name.Contains("ProjectView_Plane_".ToUpper()) || 
                    name.Contains("PVFL_Plane_".ToUpper()))
                {
                    return entity.Hyperlinks.Count > 0;
                }
            }
            return false;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {                
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
