using Linq2Acad;
using System.Linq;
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
                if (polygon.Count > 0)
                {
                    acadDatabase.ModelSpace
                        .OfType<BlockReference>()
                        .Where(b => b.GetEffectiveName() == "楼层框定" && (polygon[0].X - b.Position.X) * (polygon[2].X - b.Position.X) < 0 && (polygon[0].Y - b.Position.Y) * (polygon[2].Y - b.Position.Y) < 0)
                        .ForEach(b => Elements.Add(new ThStoreys(b.ObjectId)));
                }
                else
                {
                    acadDatabase.ModelSpace
                     .OfType<BlockReference>()
                     .Where(b => b.GetEffectiveName() == "楼层框定")
                     .ForEach(b => Elements.Add(new ThStoreys(b.ObjectId)));
                }
            }
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
           
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
        //Recognize by selection set
        //public override void Recognize(Autodesk.AutoCAD.EditorInput.SelectionSet ss)
        //{
        //    //ss->blkrefs


        //    //datas.ForEach(o => Elements.Add(new ThMEPEngineCore.Model.ThIfcSpatialElement()));
        //    //if (polygon.Count > 0)
        //    //{

        //    //}
        //}

    }
}
