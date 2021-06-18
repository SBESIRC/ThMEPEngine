using System;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Engine
{
    public class ThStoreysRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blkrefs = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull && b.GetEffectiveName() == "楼层框定");
                if (polygon.Count > 0)
                {
                    var envelope = polygon.Envelope();
                    
                    blkrefs.Where(o => envelope.IsPointIn(o.Position))
                        .ForEach(b => Elements.Add(new ThStoreys(b.ObjectId)));
                }
                else
                {
                    blkrefs.ForEach(b => Elements.Add(new ThStoreys(b.ObjectId)));
                }
            }
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
