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
                    // 用ABB 方式判断
                    var minX = polygon.Cast<Point3d>().Select(o => o.X).Min();
                    var maxX = polygon.Cast<Point3d>().Select(o => o.X).Max();

                    var minY = polygon.Cast<Point3d>().Select(o => o.Y).Min();
                    var maxY = polygon.Cast<Point3d>().Select(o => o.Y).Max();

                    acadDatabase.ModelSpace
                        .OfType<BlockReference>()
                        .Where(b => !b.BlockTableRecord.IsNull 
                        && b.GetEffectiveName() == "楼层框定" 
                        && (b.Position.X > minX && b.Position.X < maxX)
                        && (b.Position.Y > minY && b.Position.Y < maxY))
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
