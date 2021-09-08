using System;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDrainageWellExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if(dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
            }
            else if(dbObj is Line line)
            {
                elements.AddRange(Handle(line, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, 
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        private List<ThRawIfcBuildingElementData> Handle(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = polyline.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
        private List<ThRawIfcBuildingElementData> Handle(Line line, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(line) && CheckLayerValid(line))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = line.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
        private List<ThRawIfcBuildingElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(br) && CheckLayerValid(br))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = br.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
    }
    public class ThDrainageWellBlkExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThDrainageWellBlkExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements,
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
           }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                results.Add(new ThRawIfcDistributionElementData()
                {
                    Geometry = br.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }
        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }        
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
    }
}
