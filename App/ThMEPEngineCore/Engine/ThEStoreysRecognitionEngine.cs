using System;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.Engine
{
    public class ThEStoreysRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blkrefs = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull && b.GetEffectiveName() == "AI-楼层框定E");
                if (polygon.Count > 0)
                {
                    var envelope = polygon.Envelope();

                    blkrefs.Where(o => envelope.ToExtents2d().IsPointIn(o.Position.ToPoint2D()))
                        .ForEach(b => Elements.Add(new ThEStoreys(b.ObjectId)));
                }
                else
                {
                    blkrefs.ForEach(b => Elements.Add(new ThEStoreys(b.ObjectId)));
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

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotSupportedException();
        }
    }
}
