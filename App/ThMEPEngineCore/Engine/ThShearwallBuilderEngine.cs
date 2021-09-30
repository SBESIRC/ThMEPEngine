using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThShearwallBuilderEngine : ThBuildingElementBuilder ,IDisposable
    {

        public ThShearwallBuilderEngine()
        {
        }

        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            // 获取数据
            //var walls = new DBObjectCollection();
            var res = new List<ThRawIfcBuildingElementData>();
            var shearwallExtractor = new ThShearWallExtractionEngine();
            shearwallExtractor.Extract(db);
            var db3ShearwallExtractor = new ThDB3ShearWallExtractionEngine();
            db3ShearwallExtractor.Extract(db);
            shearwallExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData() 
                                                {
                                                    Geometry=e.Geometry,
                                                    Source=DataSource.Raw
                                                }));
            db3ShearwallExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
                                                 {
                                                     Geometry = e.Geometry,
                                                     Source = DataSource.Raw
                                                 }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var shearwallRecognize = new ThShearWallRecognitionEngine();
            var db3shearwallRecognize = new ThDB3ShearWallRecognitionEngine();
            var res = new List<ThIfcBuildingElement>();
            shearwallRecognize.Recognize(datas.Where(o => o.Source == DataSource.Raw).ToList(), pts);
            db3shearwallRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(shearwallRecognize.Elements);
            res.AddRange(db3shearwallRecognize.Elements);
            return res;
        }

        public override void Build(Database db, Point3dCollection pts)
        {
            var rawdata = Extract(db);
            // 处理极远情况（>1E+10）
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawdata.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts
                .OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();

            // 后处理
            var shearwallElements = Recognize(rawdata, newPts);
            var shearwalls = shearwallElements.Select(o => o.Outline).ToCollection();
            shearwalls = FilterInRange(shearwalls, newPts);
            shearwalls = Preprocess(shearwalls);

            // 回复到原位置
            transformer.Reset(shearwalls);

            // 保存结果
            Elements = shearwalls.OfType<Polyline>()
                .Select(e => ThIfcWall.Create(e))
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
        private DBObjectCollection Preprocess(DBObjectCollection walls)
        {
            var simplifier = new ThShearWallSimplifier();
            var results = walls.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Tessellate(walls);
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
    }
}
