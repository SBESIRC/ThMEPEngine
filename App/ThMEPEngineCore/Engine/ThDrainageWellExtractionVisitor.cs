using System;
using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

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
        private ThBlockReferenceObbService BlkObbService { get; set; }
        public ThDrainageWellBlkExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
            BlkObbService = new ThBlockReferenceObbService()
            {
                ArcTesslateLength = 50.0,
                ChordHeight = 20.0,
            };
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
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
                var obb = BlkObbService.ToObb(br);
                if(obb!=null)
                {
                    obb.TransformBy(matrix);
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = obb,
                    });
                }
            }
            return results;
        }
        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else if(ent is Polyline polyline)
            {
                return xclip.Contains(polyline);
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
