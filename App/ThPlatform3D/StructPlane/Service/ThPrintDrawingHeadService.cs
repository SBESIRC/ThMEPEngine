using System;
using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.StructPlane.Print;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThPrintDrawingHeadService
    {
        #region ---------- config ----------
        public double LineUpTextInterval { get; set; } = 200.0;
        public double LineRightTextInterval { get; set; } = 500.0;
        public double MarkBottomDistanceToHeadTextTop = 1000;
        public double MarkGapDistance = 10.0; // （1F~10F,Floor1）与(时间)之间的间距
        public string Head { get; set; } = "";
        public string DrawingSacle { get; set; } = "";
        public Point3d BasePt { get; set; } = Point3d.Origin;
        public Tuple<string, string, string> StdFlrInfo { get; set; }
        #endregion
        public ThPrintDrawingHeadService()
        {
        }
        public ObjectIdCollection Print(AcadDatabase acadDb)
        {
            var results = new ObjectIdCollection();
            var mark1ObjIds =  new ObjectIdCollection();
            var mark2ObjIds = new ObjectIdCollection();
            // 打印标准层信息+时间戳
            var mark1 = BuildMark1();
            if (!string.IsNullOrEmpty(mark1))
            {
                var mark2 = BuildMark2();
                mark1ObjIds = PrintMark(acadDb, mark1);
                mark2ObjIds = PrintMark(acadDb, mark2);
            }
            var headTextIds = PrintHead(acadDb);
            var scaleTextIds = PrintScale(acadDb);
            headTextIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            scaleTextIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            mark1ObjIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            mark2ObjIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            if (headTextIds.Count == 0)
            {
                return results;
            }
            var downLineWidth = 80.0;
            // 创建文字
            var headText = acadDb.Element<DBText>(headTextIds[0], true);
            var textLength = headText.GeometricExtents.MaxPoint.X -
                headText.GeometricExtents.MinPoint.X;
            var textMoveMt1 = Matrix3d.Displacement(new Vector3d(-textLength / 2.0,
                LineUpTextInterval + Math.Abs(headText.GeometricExtents.MinPoint.Y) + downLineWidth / 2.0, 0));
            headText.TransformBy(textMoveMt1);

            // 创建线 
            var downLineLength = textLength * 1.05;
            var downLine = new Polyline();
            downLine.AddVertexAt(0, new Point2d(-downLineLength / 2.0, 0), 0.0, downLineWidth, downLineWidth);
            downLine.AddVertexAt(1, new Point2d(downLineLength / 2.0, 0), 0.0, downLineWidth, downLineWidth);
            downLine.Layer = ThPrintLayerManager.HeadTextDownLineLayerName;
            downLine.ColorIndex = (int)ColorIndex.BYLAYER;
            results.Add(acadDb.ModelSpace.Add(downLine));

            // 调整比例文字
            if (scaleTextIds.Count > 0)
            {
                var scaleText = acadDb.Element<DBText>(scaleTextIds[0], true);
                var textMoveMt2 = Matrix3d.Displacement(new Vector3d(downLineLength / 2.0 + LineRightTextInterval, 0, 0));
                scaleText.TransformBy(textMoveMt2);
            }

            // 调整标注文字
            if(mark1ObjIds.Count==1 && mark2ObjIds.Count==1)
            {                
                var mark1Text = acadDb.Element<DBText>(mark1ObjIds[0], true);
                var mark2Text = acadDb.Element<DBText>(mark2ObjIds[0], true);

                var mark2NewTopPt = new Point3d(
                    (mark1Text.GeometricExtents.MinPoint.X+ mark1Text.GeometricExtents.MaxPoint.X)/2.0, 
                    mark1Text.GeometricExtents.MinPoint.Y- MarkGapDistance,0);

                var mark2OldTopPt = new Point3d(
                    (mark2Text.GeometricExtents.MinPoint.X + mark2Text.GeometricExtents.MaxPoint.X) / 2.0,
                    mark2Text.GeometricExtents.MaxPoint.Y,0);

                var mark2Displacement1 = Matrix3d.Displacement(mark2NewTopPt - mark2OldTopPt);
                mark2Text.TransformBy(mark2Displacement1);

                var headTopPt = new Point3d((headText.GeometricExtents.MaxPoint.X +
                headText.GeometricExtents.MinPoint.X) / 2.0, headText.GeometricExtents.MaxPoint.Y, 0.0);
                var mark2NewBottomPt = headTopPt + new Vector3d(0, MarkBottomDistanceToHeadTextTop, 0);
                var mark2OldBottomPt = new Point3d(
                    (mark2Text.GeometricExtents.MinPoint.X + mark2Text.GeometricExtents.MaxPoint.X) / 2.0,
                    mark2Text.GeometricExtents.MinPoint.Y, 0);

                var mark2Displacement2 = Matrix3d.Displacement(mark2NewBottomPt - mark2OldBottomPt);
                mark1Text.TransformBy(mark2Displacement2);
                mark2Text.TransformBy(mark2Displacement2);
            }

            // 移动
            var mt = Matrix3d.Displacement(BasePt - Point3d.Origin);
            results.OfType<ObjectId>().ForEach(o =>
            {
                var entity = acadDb.Element<Entity>(o, true);
                entity.TransformBy(mt);
            });
            return results;
        }

        private string BuildMark1()
        {
            if(StdFlrInfo!=null)
            {
                return "(" + StdFlrInfo.Item1 + "~" + StdFlrInfo.Item2 + "," + StdFlrInfo.Item3 + ")";
            }
            else
            {
                return "";
            }
        }

        private string BuildMark2()
        {
            return "(" + DateTime.Now.Year.ToString()+ DateTime.Now.Month.ToString()+ DateTime.Now.Day.ToString() +
                DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() +")";
        }

        private ObjectIdCollection PrintMark(AcadDatabase database,string mark)
        {
            var markText = new DBText()
            {
                Position = Point3d.Origin,
                TextString = mark,
                Height = 100,
            };
            var config = ThAnnotationPrinter.GetMarkTextConfig(DrawingSacle);
            return ThAnnotationPrinter.Print(database, markText, config);
        }

        private ObjectIdCollection PrintHead(AcadDatabase database)
        {
            if(string.IsNullOrEmpty(Head))
            {
                return new ObjectIdCollection();
            }
            else
            {
                var headText = new DBText()
                {
                    Position = Point3d.Origin,
                    TextString = Head,
                    Height = 100,
                };
                var config = ThAnnotationPrinter.GetHeadTextConfig(DrawingSacle);
                return ThAnnotationPrinter.Print(database, headText, config);
            }     
        }
        private ObjectIdCollection PrintScale(AcadDatabase database)
        {
            if(string.IsNullOrEmpty(DrawingSacle))
            {
                return new ObjectIdCollection();
            }
            else
            {
                var scaleText = new DBText()
                {
                    Position = Point3d.Origin,
                    TextString = DrawingSacle,
                    Height = 100,
                };
                var config = ThAnnotationPrinter.GetHeadTextScaleConfig(DrawingSacle);
                return ThAnnotationPrinter.Print(database, scaleText, config);
            }            
        }
    }
}
