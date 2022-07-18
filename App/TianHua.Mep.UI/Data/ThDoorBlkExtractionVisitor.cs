using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.Data
{
    internal class ThDoorBlkExtractionVisitor: ThSpatialElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThDoorBlkExtractionVisitor()
        {
            CheckQualifiedLayer = CheckLayerIsValid;
            CheckQualifiedBlockName = CheckBlockNameIsValid;
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, Matrix3d.Identity);
            }
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Polyline));
            }
        }

        public override bool IsSpatialElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }

        public override bool CheckLayerValid(Entity e)
        {
            return CheckQualifiedLayer(e);
        }

        private void HandleBlockReference(List<ThRawIfcSpatialElementData> elements, BlockReference br, Matrix3d matrix)
        {
            if(br.Bounds.HasValue)
            {
                var obb = ToOBB(br);
                if(obb!=null)
                {
                    if(obb.Area>1.0)
                    {
                        obb.TransformBy(matrix);
                        elements.Add(new ThRawIfcSpatialElementData()
                        {
                            Geometry = obb,
                            Data = br.GetEffectiveName(),
                        });
                    }
                    else
                    {
                        obb.Dispose();
                    }
                }
            }
        }

        private bool CheckLayerIsValid(Entity e)
        {
            return base.CheckLayerValid(e);
        }

        private bool CheckBlockNameIsValid(Entity e)
        {
            return false;
        }

        private Polyline ToOBB(BlockReference br)
        {
            var worker = new ThBlockReferenceObbService()
            {
                ChordHeight = 20.0,
                ArcTesslateLength = 50.0,                
            };
            return worker.ToObb(br);
        }
    }
}
