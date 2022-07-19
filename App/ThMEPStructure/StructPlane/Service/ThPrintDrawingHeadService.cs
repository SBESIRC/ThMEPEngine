﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.StructPlane.Print;

namespace ThMEPStructure.StructPlane.Service
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
                downLine.Layer = ThPrintLayerManager.HeadTextDownLineLayerName;
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
                var printer = new ThAnnotationPrinter(ThAnnotationPrinter.GetHeadTextConfig(DrawingSacle));
                return printer.Print(database, headText);
            }     
        }
        private ObjectIdCollection PrintScale(Database database)
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
                var printer = new ThAnnotationPrinter(ThAnnotationPrinter.GetHeadTextScaleConfig(DrawingSacle));
                return printer.Print(database, scaleText);
            }            
        }
    }
}
