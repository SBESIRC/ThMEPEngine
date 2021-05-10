using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCleanToolsVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                var name = reference.GetEffectiveName();
                return ThCleanToolsManager.IsCleanToolBlockName(name);
            }
            return false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            elements.Add(new ThRawIfcDistributionElementData()
            {
                Data = blkref.GetEffectiveName(),
                Geometry = blkref.GetTransformedCopy(matrix),
            });
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public class ThWCleanToolsRecongnitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDistributionElementExtractor();
            var cleanToolVisitor = new ThWCleanToolsVisitor();
            engine.Accept(cleanToolVisitor);

            engine.Extract(database);

            var dbObjs = cleanToolVisitor.Results.Select(o => o.Geometry).ToCollection();

            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }

            Elements.AddRange(dbObjs.Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));
        }
    }
}
