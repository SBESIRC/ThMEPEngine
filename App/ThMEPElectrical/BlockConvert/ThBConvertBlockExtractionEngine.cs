using System;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertBlockExtractionEngine : ThDistributionElementExtractionEngine
    {
        public List<string> NameFilter { get; set; }

        public override void Extract(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromMS(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(e => !e.BlockTableRecord.IsNull)
                    .ForEach(e =>
                    {
                        if (!e.IsErased && NameFilter.Contains(e.GetEffectiveName()))
                        {
                            var elementData = new ThRawIfcDistributionElementData
                            {
                                Geometry = e.GetBlockOBB(),
                                Data = new ThBlockReferenceData(e.Id),
                            };
                            Results.Add(elementData);
                        }
                    });
            }
        }
    }
}
