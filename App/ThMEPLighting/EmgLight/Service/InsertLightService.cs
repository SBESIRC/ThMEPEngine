using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;

namespace ThMEPLighting.EmgLight.Service
{
    public static class InsertLightService
    {
        private static double scaleNum = 100;
        public static void InsertSprayBlock(Dictionary<Polyline, (Point3d, Vector3d)> insertPtInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel();
                foreach (var ptInfo in insertPtInfo)
                {

                    db.Database.InsertModel(ptInfo.Value.Item1 + ptInfo.Value.Item2 * scaleNum * 1.5, -ptInfo.Value.Item2, new Dictionary<string, string>() { });

                }
            }
        }

        public static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, Dictionary<string, string> attNameValues)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir);
            //控制旋转角度
            if (layoutDir.DotProduct(-Vector3d.XAxis) < 0)
            {
                rotateAngle = -rotateAngle;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPLightingCommon.EmgLightLayerName,
                    ThMEPLightingCommon.EmgLightBlockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle,
                    attNameValues);
            }
        }

        public static void ImportModel(this Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LightingEmgLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThMEPLightingCommon.EmgLightBlockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.EmgLightLayerName), false);
            }
        }

        //private static string BlockDwgPath()
        //{
        //    return System.IO.Path.Combine(ThCADCommon.SupportPath(), ThMEPCommon.BroadcastDwgName);
        //}

        //for debug
        public static void ShowGeometry(List<Polyline> Polylines, int ci, LineWeight lw = LineWeight.LineWeight025)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (Polyline pl in Polylines)
                {
                    var showPl = pl.Clone() as Polyline;
                    showPl.ColorIndex = ci;
                    showPl.LineWeight = lw;
                    acdb.ModelSpace.Add(showPl);
                }
            }
        }

        public static void ShowGeometry(Polyline Polyline, int ci, LineWeight lw = LineWeight.LineWeight025)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                var showPl = Polyline.Clone() as Polyline;
                showPl.ColorIndex = ci;
                showPl.LineWeight = lw;
                acdb.ModelSpace.Add(showPl);

            }
        }
        public static void ShowGeometry(Line line, int ci, LineWeight lw = LineWeight.LineWeight025)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                var showPl = line.Clone() as Line;
                showPl.ColorIndex = ci;
                showPl.LineWeight = lw;
                acdb.ModelSpace.Add(showPl);

            }
        }

        public static void ShowGeometry(List<Line> lines, int ci, LineWeight lw = LineWeight.LineWeight025)
        {

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (Line line in lines)
                {

                    var showPl = line.Clone() as Line;
                    showPl.ColorIndex = ci;
                    showPl.LineWeight = lw;
                    acdb.ModelSpace.Add(showPl);
                }
            }
        }

        public static void ShowGeometry(Point3d pt, int ci, LineWeight lw = LineWeight.LineWeight025)
        {

            using (AcadDatabase acdb = AcadDatabase.Active())
            {


                var pointC = new Circle(pt, new Vector3d(0, 0, 1), 200);
                pointC.ColorIndex = ci;
                pointC.LineWeight = lw;
                acdb.ModelSpace.Add(pointC);

            }
        }

        public static void ShowGeometry(Point3d pt, string s, int ci, LineWeight lw = LineWeight.LineWeight025)
        {

            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                DBText text = new DBText();
                text.Position = pt;
                text.ColorIndex = ci;
                text.LineWeight = lw;
                text.TextString = s;
                text.Rotation = 0;
                text.Height = 1000;
                text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLEP5");
                acdb.ModelSpace.Add(text);

            }
        }

        //public static void ShowGeometry(List<Entity> lines, int ci, LineWeight lw = LineWeight.LineWeight025)
        //{

        //    using (AcadDatabase acdb = AcadDatabase.Active())
        //    {
        //        foreach (Entity line in lines)
        //        {

        //            var showPl = line.Clone() as Entity;
        //            showPl.ColorIndex = ci;
        //            showPl.LineWeight = lw;
        //            acdb.ModelSpace.Add(showPl);
        //        }
        //    }
        //}
        //public static void ShowGeometry(Entity line, int ci, LineWeight lw = LineWeight.LineWeight025)
        //{
        //    using (AcadDatabase acdb = AcadDatabase.Active())
        //    {

        //        var showPl = line.Clone() as Entity;
        //        showPl.ColorIndex = ci;
        //        showPl.LineWeight = lw;
        //        acdb.ModelSpace.Add(showPl);

        //    }
        //}
    }
}
