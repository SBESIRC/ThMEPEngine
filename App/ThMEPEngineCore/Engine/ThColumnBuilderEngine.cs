﻿using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnBuilderEngine : ThBuildingElementBuilder, IDisposable
    {

        public ThColumnBuilderEngine()
        {
        }
        public void Dispose()
        {
        }    
        
        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var columnExtractor = new ThColumnExtractionEngine();
            columnExtractor.Extract(db);
            var db3ColumnExtractor = new ThDB3ColumnExtractionEngine();
            db3ColumnExtractor.Extract(db);
            columnExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData(){
                                               Geometry=e.Geometry,
                                               Source=DataSource.Raw
                                           }));
            db3ColumnExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData() { 
                                                            Geometry=e.Geometry,
                                                            Source=DataSource.DB3
                                                }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var columnRecognize = new ThColumnRecognitionEngine();
            var db3columnReconize = new ThDB3ColumnRecognitionEngine();
            var res = new List<ThIfcBuildingElement>();
            columnRecognize.Recognize(datas.Where(o=>o.Source==DataSource.Raw).ToList(), pts);
            db3columnReconize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(columnRecognize.Elements);
            res.AddRange(db3columnReconize.Elements);
            return res;
        }

        public override void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var columns = Extract(db);

            // 移动到近原点处
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            columns.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts
                .OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();

            // 识别
            var buildingElements = Recognize(columns, newPts);

            // 后处理
            var handleColumns = Union(buildingElements);

            // 恢复到原位置
            handleColumns.ForEach(c => transformer.Reset(c.Outline));

            // 保存结果
            Elements = handleColumns
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
        public List<ThIfcColumn> Union(List<ThIfcBuildingElement> buildingElements)
        {
            var handleColumns = buildingElements.Select(o => o.Outline).ToCollection();
            handleColumns = Buffer(handleColumns, -BUFFERTOLERANCE);
            handleColumns = Preprocess(handleColumns);
            handleColumns = Buffer(handleColumns, BUFFERTOLERANCE);
            return handleColumns.Cast<Polyline>().Select(o => ThIfcColumn.Create(o)).ToList();
        }
        private DBObjectCollection Preprocess(DBObjectCollection columns)
        {
            var simplifier = new ThPolygonalElementSimplifier();
            var results = columns.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Tessellate(columns);
            results = results.UnionPolygons();
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.MakeValid(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Normalize(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            return results;
        }
        private DBObjectCollection FilterInRange(DBObjectCollection objs, Point3dCollection pts)
        {
            if (pts.Count > 2)
            {
                var results = new DBObjectCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(pts);
                foreach (DBObject filterObj in spatialIndex.SelectCrossingPolygon(pline))
                {
                    results.Add(filterObj);
                }
                return results;
            }
            else
            {
                return objs;
            }
        }
        private DBObjectCollection Buffer(DBObjectCollection objs, double length)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            objs.Cast<Entity>().ForEach(e =>
            {
                var entity = bufferService.Buffer(e, length);
                if (entity != null && GetArea(entity) >= AREATOLERANCE)
                {
                    results.Add(entity);
                }
            });
            return results;
        }
        private double GetArea(Entity polygon)
        {
            return polygon.ToNTSPolygonalGeometry().Area;
        }
    }
}
