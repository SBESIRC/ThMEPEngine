using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
namespace ThMEPWSS.Hydrant.Engine
{
    public class ThFireHydrantButterValveVisitor : ThSpatialElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }
        private void HandleBlockReference(List<ThRawIfcSpatialElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            elements.Add(new ThRawIfcSpatialElementData()
            {
                Data = blkref.GetEffectiveName(),
                Geometry = blkref
            });
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            HandleEntity(elements, dbObj);
        }

        private static void HandleEntity(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            elements.Add(new ThRawIfcSpatialElementData()
            {
                Geometry = dbObj,
            });
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
        }
    }

}