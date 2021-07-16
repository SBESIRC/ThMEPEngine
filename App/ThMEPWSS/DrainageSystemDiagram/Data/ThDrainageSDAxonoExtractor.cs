using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetARX;
using Dreambuild.AutoCAD;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDAxonoExtractor : ThExtractorBase
    {
        private List<DBPoint> Points { get; set; }
        public List<ThIfcSanitaryTerminalToilate> SanTmnList { get; private set; }
        public ThMEPOriginTransformer Transfer { get; set; }

        public ThDrainageSDAxonoExtractor()
        {
            Category = BuiltInCategory.WaterSupplyPoint.ToString();
            SanTmnList = new List<ThIfcSanitaryTerminalToilate>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var ExtractEngine = new ThDrainageSDExtractionEngine();
            ExtractEngine.Extract(database);
            ExtractEngine.ExtractFromMS(database);
            var originDatas = ExtractEngine.Results;

            List<ThRawIfcDistributionElementData> transData = new List<ThRawIfcDistributionElementData>();

            //transfer originData
            if (Transfer != null)
            {
                foreach (var oriD in originDatas)
                {

                }
            }
            else
            {
                transData.AddRange(originDatas);
            }

            //recogition Engine
            using (var recEngine = new ThDrainageSDRecognitionEngine())
            {
                recEngine.Recognize(transData, pts);

                foreach (var element in recEngine.Elements)
                {
                    var toModel = element as ThIfcSanitaryTerminalToilate;
                    SanTmnList.Add(toModel);
                }
            }
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            //results.AddRange(BuildToilateDrains());
            return results;
        }

    }

    public class ThDrainageSDAxonoExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public ThDrainageSDAxonoExtractionEngine()
        {
        }
        public void Dispose()
        {
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThDrainageSDAxonoVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThDrainageSDAxonoVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }
    }

    public class ThDrainageSDAxonoRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public ThDrainageSDAxonoRecognitionEngine()
        {

        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> originDatas, Point3dCollection polygon)
        {
            //var engine = new ThDrainageSDExtractionEngine();
            //engine.Extract(database);
            //engine.ExtractFromMS(database);
            //var originDatas = engine.Results;

            if (polygon.Count > 0)
            {
                var dbObjs = originDatas.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }

            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcSanitaryTerminalToilate(x.Geometry, x.Data as string)));
        }
    }

    public class ThDrainageSDAxonoVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> toiBlkNames { get; set; }
        public List<string> valveBlkName { get; set; }

        public bool BlockObbSwitch { get; set; }
        public ThDrainageSDAxonoVisitor()
        {
            toiBlkNames = new List<string>() { "A-Toilet-1", "A-Toilet-2", "A-Toilet-3",
                                            "A-Toilet-4", "A-Kitchen-3", "A-Kitchen-4",
                                            "小便器", "A-Toilet-5", "蹲便器",
                                            "A-Kitchen-9", "A-Toilet-6", "A-Toilet-7",
                                            "A-Toilet-8", "A-Toilet-9", "儿童坐便器",
                                            "儿童洗脸盆", "儿童小便器"  };

            valveBlkName = new List<string>() {"$VALVE$00000333","截止阀",
                                            "给水角阀平面",
                                            "水表1","进户水表","室内水表详图",
                                            "给水管径50"};

            LayerFilter = new HashSet<string>()
            {
                "W-WSUP-EQPM",
                "W-WSUP-COOL-PIPE-AI",
                "W-WSUP-DIMS"
            };

            BlockObbSwitch = false;
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsDistributionElement(blkref))
            {
                if (BlockObbSwitch)
                {
                    var rec = blkref.ToOBB(blkref.BlockTransform.PreMultiplyBy(matrix));
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = blkref.GetEffectiveName(),
                        Geometry = rec
                    });
                }
                else
                {
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = blkref.GetEffectiveName(),
                        Geometry = blkref
                    });
                }

            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public override bool IsDistributionElement(Entity entity)
        {
            var bReturn = false;
            if (entity is BlockReference br)
            {
                var blkName = br.GetEffectiveName();
                bReturn = IsExisted(blkName, toiBlkNames);
                bReturn = bReturn || IsExisted(blkName, valveBlkName);
            }
            if (entity is Curve )
            {
                bReturn = true; 
            }
            return bReturn;
        }

        private bool IsExisted(string blkName, List<string> blkNames)
        {
            return blkNames.Where(o => blkName.ToUpper().Contains(o.ToUpper())).Any();
        }

        public override bool CheckLayerValid(Entity curve)
        {

            return LayerFilter.Count == 0 ? true : LayerFilter.Contains(curve.Layer);

        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            return true;
        }

    }
}
