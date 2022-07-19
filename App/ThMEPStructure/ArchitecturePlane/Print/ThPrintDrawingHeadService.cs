using System;
using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.Common;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThPrintDrawingHeadService
    {
        #region ---------- config ----------
        public double LineUpTextInterval { get; set; } = 200.0;
        public double LineRightTextInterval { get; set; } = 500.0;      
        public string Head { get; set; } = "";
        public string DrawingSacle { get; set; } = "";
        public Point3d BasePt { get; set; } = Point3d.Origin;
        #endregion
        public ThPrintDrawingHeadService()
        {
        }
        public ObjectIdCollection Print(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                var results = new ObjectIdCollection();    
                var headTextIds = PrintHead(database);
                var scaleTextIds = PrintScale(database);
                headTextIds.OfType<ObjectId>().ForEach(o => results.Add(o));
                scaleTextIds.OfType<ObjectId>().ForEach(o => results.Add(o));
                if (headTextIds.Count==0)
                {
                    return results;
                }
                var downLineWidth = 80.0;
                // 创建文字
                var headText = acadDb.Element<DBText>(headTextIds[0],true);
                var textLength = headText.GeometricExtents.MaxPoint.X - 
                    headText.GeometricExtents.MinPoint.X;
                var textMoveMt1 = Matrix3d.Displacement(new Vector3d(-textLength / 2.0, 
                    LineUpTextInterval+Math.Abs(headText.GeometricExtents.MinPoint.Y)+ downLineWidth/2.0, 0));
                headText.TransformBy(textMoveMt1);

                // 创建线 
                var downLineLength = textLength * 1.05;
                var downLine = new Polyline();           
                downLine.AddVertexAt(0, new Point2d(-downLineLength / 2.0, 0), 0.0, downLineWidth, downLineWidth);
                downLine.AddVertexAt(1, new Point2d(downLineLength / 2.0, 0), 0.0, downLineWidth, downLineWidth);
                downLine.Layer = ThArchPrintLayerManager.DEFPOINTS;
                downLine.ColorIndex = (int)ColorIndex.BYLAYER;
                results.Add(acadDb.ModelSpace.Add(downLine));

                // 调整比例文字
                if(scaleTextIds.Count>0)
                {
                    var scaleText = acadDb.Element<DBText>(scaleTextIds[0], true);
                    var textMoveMt2 = Matrix3d.Displacement(new Vector3d(downLineLength/2.0+ LineRightTextInterval,0,0));
                    scaleText.TransformBy(textMoveMt2);
                }

                // 移动
                var mt = Matrix3d.Displacement(BasePt - Point3d.Origin);
                results.OfType<ObjectId>().ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o,true);
                    entity.TransformBy(mt);
                });

                return results;
            }
        }

        private ObjectIdCollection PrintHead(Database database)
        {
            var results = new ObjectIdCollection();
            if (!string.IsNullOrEmpty(Head))
            {
                var headText = new DBText()
                {
                    Position = Point3d.Origin,
                    TextString = Head,
                    Height = 100,
                };
                var config = GetHeadTextConfig(DrawingSacle);
                var textId = headText.Print(database, config);
                results.Add(textId);
            }
            return results;
        }
        private ObjectIdCollection PrintScale(Database database)
        {
            var results = new ObjectIdCollection();
            if (!string.IsNullOrEmpty(DrawingSacle))
            {
                var scaleText = new DBText()
                {
                    Position = Point3d.Origin,
                    TextString = DrawingSacle,
                    Height = 100,
                };
                var config = GetHeadTextScaleConfig(DrawingSacle);
                var textId = scaleText.Print(database, config);
                results.Add(textId);
            }
            return results;
        }
        private AnnotationPrintConfig GetHeadTextConfig(string drawingScale)
        {
            var config = GetHeadTextConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private AnnotationPrintConfig GetHeadTextConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThArchPrintLayerManager.CommonLayer,
                Height = 8,
                WidthFactor = 0.8,
                TextStyleName = ThArchPrintStyleManager.THSTYLE3,
            };
        }
        private AnnotationPrintConfig GetHeadTextScaleConfig(string drawingScale)
        {
            var config = GetHeadTextScaleConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private AnnotationPrintConfig GetHeadTextScaleConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThArchPrintLayerManager.CommonLayer,
                Height = 6,
                WidthFactor = 0.8,
                TextStyleName = ThArchPrintStyleManager.THSTYLE3,
            };
        }
    }
}
