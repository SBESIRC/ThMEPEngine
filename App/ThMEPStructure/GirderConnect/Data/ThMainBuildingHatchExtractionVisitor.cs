﻿using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.GirderConnect.Data
{
    public class ThMainBuildingHatchExtractionVisitor : ThSpatialElementExtractionVisitor
    {
        private const double ArcTessellationLength = 100.0;
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Hatch hatch)
            {
                elements.AddRange(Handle(hatch, matrix));
            }
            else if(dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
            }
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Hatch hatch)
            {
                elements.AddRange(Handle(hatch));
            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        private List<ThRawIfcSpatialElementData> Handle(Hatch hatch, Matrix3d matrix)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(hatch) && CheckLayerValid(hatch))
            {
                var clone = hatch.GetTransformedCopy(matrix) as Hatch;
                results = BuildPolygons(clone);
            }
            return results;
        }

        private List<ThRawIfcSpatialElementData> Handle(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (IsSpatialElement(polyline) && CheckLayerValid(polyline))
            {
                var clone = polyline.GetTransformedCopy(matrix) as Polyline;
                results.Add(new ThRawIfcSpatialElementData()
                {
                    Geometry = clone,
                });
            }
            return results;
        }


        private List<ThRawIfcSpatialElementData> Handle(Hatch hatch)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            if (CheckLayerValid(hatch))
            {
                var clone = hatch.Clone() as Hatch;
                results = BuildPolygons(clone);
            }
            return results;
        }

        private List<ThRawIfcSpatialElementData> BuildPolygons(Hatch hatch)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            var polygons = HatchToPolygons(hatch);
            polygons.ForEach(o =>
            {
                results.Add(new ThRawIfcSpatialElementData()
                {
                    Geometry = o,
                });
            });
            return results;
        }

        public override bool IsSpatialElement(Entity entity)
        {
            return entity is Hatch || entity is Polyline;
        }
        private List<Entity> HatchToPolygons(Hatch hatch)
        {
            using (var ov = new ThCADCoreNTSArcTessellationLength(ArcTessellationLength))
            {
                return hatch.ToPolygons().Select(o=>o.ToDbEntity()).ToList();
            }
        }
    }
}
