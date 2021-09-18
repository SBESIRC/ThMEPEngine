using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractLabelLine//引线提取
    {
        public DBObjectCollection DbTextCollection { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var DBObjs = spatialIndex.SelectWindowPolygon(polygon.Envelope().ToRectangle());

                DbTextCollection = new DBObjectCollection();

                var bkrCollection = new DBObjectCollection();
                DBObjs.Cast<Entity>()
                    .Where(o => o is Entity)
                    .ForEach(o => bkrCollection.Add(o));
                foreach (var bkr in bkrCollection)
                {
                    if (bkr is Entity ent)
                    {
                        ExplodeLabelLine(ent, DbTextCollection);
                    }
                }

                foreach(var db in DbTextCollection)
                {
                    var line = db as Line;
                    if(line.Length < 10)
                    {
                        ;
                    }
                }
                return DbTextCollection;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-RAIN-NOTE" ||
                   layer.ToUpper() == "W-DRAI-DIMS" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-FRPT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                   layer.ToUpper() == "0" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS";
        }

        
        public List<Line> CreateLabelLineList()
        {
            var LabelPosition = new List<Line>();

            if (DbTextCollection.Count != 0)
            {
                foreach (var db in DbTextCollection)
                {
                    var line = db as Line;
                    var pt1 = new Point3d(line.StartPoint.X, line.StartPoint.Y, 0);
                    var pt2 = new Point3d(line.EndPoint.X, line.EndPoint.Y, 0);
                    LabelPosition.Add(new Line(pt1,pt2));
                }
            }
            LabelPosition = PipeLineList.CleanLaneLines3(LabelPosition);
            return LabelPosition;
        }

        private void ExplodeLabelLine(Entity ent, DBObjectCollection dBObjects)
        {
            if (ent == null) return;

            if (ent is Line line)// Line 直接添加
            {
                if (!line.Layer.ToUpper().Contains("DEFPOINTS"))
                {
                    dBObjects.Add(line);
                }
                return;
            }
            if(ent.GetRXClass().DxfName.StartsWith("TCH") && ent.GetRXClass().DxfName.Contains("PIPE"))
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach(var db in dbObjs)
                {
                    if(db is Line line1)
                    {
                        dBObjects.Add(line1);
                    }
                }
            }
            if (ent is Polyline pline)
            {
                if (pline.Layer.ToUpper().Contains("DEFPOINTS"))
                {
                    return;
                }
            }
            if (ent is AlignedDimension || ent is Arc || ent is DBText || ent is Circle || ent.IsTCHText())//炸出圆 和 天正单行文字 就退出
            {
                return;
            }
            try
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    if (obj is Entity ent1)
                    {
                        ExplodeLabelLine(ent1, dBObjects);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
