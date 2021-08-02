using System;
using System.Collections.Generic;
using System.Linq;

using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDAxonoExtractor : ThExtractorBase
    {
        public List<ThTerminalToilet> SanTmnList { get; private set; }
        public List<Line> pipes { get; set; }

        public List<Circle> stack { get; set; }
        public List<ThDrainageSDADValve> ValveList { get; set; }

        public List<string> toiBlkNames { get; private set; }

        public List<string> valveBlkName { get; private set; }

        public Dictionary<string, string> curveType { get; private set; }

        public ThMEPOriginTransformer Transfer { get; set; }

        public ThDrainageSDAxonoExtractor()
        {
            Category = "CoolSupplyWaterAxonometric";
            SanTmnList = new List<ThTerminalToilet>();
            ValveList = new List<ThDrainageSDADValve>();
            pipes = new List<Line>();
            stack = new List<Circle>();

            toiBlkNames = ThDrainageADCommon.toiBlkNames;
            valveBlkName = ThDrainageADCommon.valveBlkName;
            curveType = ThDrainageADCommon.curveType;

        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var ExtractEngine = new ThDrainageSDAxonoExtractionEngine(toiBlkNames, valveBlkName, curveType);
            ExtractEngine.Extract(database);
            ExtractEngine.ExtractFromMS(database);
            var originDatas = ExtractEngine.Results;

            List<ThRawIfcSpatialElementData> transData = new List<ThRawIfcSpatialElementData>();

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
            using (var recEngine = new ThDrainageSDAxonoRecognitionEngine())
            {
                recEngine.Recognize(transData, pts);

                foreach (var element in recEngine.Elements)
                {
                    var toModel = element as ThDrainageSDAxonoData;
                    if (toModel.Type == curveType["Line"])
                    {
                        var l = toModel.Outline as Line;
                        pipes.Add(l.Clone() as Line);
                    }

                    if (toModel.Type == curveType["Circle"] || toModel.Type == ThDrainageADCommon.blkName_stack)
                    {
                        if (toModel.Outline is Circle)
                        {
                            var c = toModel.Outline as Circle;
                            stack.Add(c);
                        }
                        if (toModel.Outline is BlockReference)
                        {
                            var c = toModel.Outline as BlockReference;
                            var objId = c.ObjectId;
                            var thBlk = new ThBlockReferenceData(objId);
                            var visibility = thBlk.CurrentVisibilityStateValue();
                            var scale = Math.Abs(c.ScaleFactors.X);

                            var r = ThDrainageADCommon.blkSize_stack[visibility] * scale;

                            var newC = new Circle(c.Position, new Vector3d(0, 0, 1), r);
                            stack.Add(newC);
                        }
                    }

                    if (toiBlkNames.Contains(toModel.Type))
                    {
                        SanTmnList.Add(new ThTerminalToilet(toModel.Outline, toModel.Type));
                    }

                    if (valveBlkName.Contains(toModel.Type) && toModel.Type != ThDrainageADCommon.blkName_stack)
                    {
                        ValveList.Add(new ThDrainageSDADValve(toModel.Outline, toModel.Type));
                    }
                }
            }
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            //results.AddRange(BuildToiletDrains());
            return results;
        }
    }

    public class ThDrainageSDAxonoExtractionEngine : ThSpatialElementExtractionEngine, IDisposable
    {
        public List<string> toiBlkNames { get; private set; }
        public List<string> valveBlkName { get; private set; }
        public Dictionary<string, string> pipeType { get; private set; }

        public ThDrainageSDAxonoExtractionEngine(List<string> toiBlkNames, List<string> valveBlkName, Dictionary<string, string> pipeType)
        {
            this.toiBlkNames = toiBlkNames;
            this.valveBlkName = valveBlkName;
            this.pipeType = pipeType;
        }
        public void Dispose()
        {
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThDrainageSDAxonoVisitor(toiBlkNames, valveBlkName, pipeType);

            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThDrainageSDAxonoVisitor(toiBlkNames, valveBlkName, pipeType);
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }
        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }

    public class ThDrainageSDAxonoRecognitionEngine : ThSpatialElementRecognitionEngine
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
        public override void Recognize(List<ThRawIfcSpatialElementData> originDatas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = originDatas.Where(x => x.Geometry is not Circle).Select(o => o.Geometry).ToCollection();
                var stackData = new Dictionary <ThRawIfcSpatialElementData,DBPoint>();
                originDatas.ForEach(x => {
                    if (x.Geometry is Circle c)
                    {
                        var obj = new DBPoint(c.Center);
                        stackData.Add(x,obj);
                        dbObjs.Add(obj);
                    } });

                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
                originDatas.AddRange(stackData.Where(x => dbObjs.Contains(x.Value)).Select(x => x.Key));

            }

            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThDrainageSDAxonoData(x.Geometry, x.Data as string)));
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }

    public class ThDrainageSDAxonoVisitor : ThSpatialElementExtractionVisitor
    {
        public List<string> toiBlkNames { get; private set; }
        public List<string> valveBlkName { get; private set; }
        public Dictionary<string, string> curveType { get; private set; }
        public bool BlockObbSwitch { get; set; }
        public ThDrainageSDAxonoVisitor(List<string> toiBlkNames, List<string> valveBlkName, Dictionary<string, string> pipeType)
        {
            LayerFilter = ThDrainageADCommon.LayerFilter;

            this.toiBlkNames = toiBlkNames;
            this.valveBlkName = valveBlkName;
            this.curveType = pipeType;

            BlockObbSwitch = false;
        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {

            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }

        }

        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is Line l)
            {
                elements.Add(new ThRawIfcSpatialElementData()
                {
                    Data = curveType["Line"],
                    Geometry = l
                });
            }

            if (dbObj is Circle c)
            {
                if (c.Radius < ThDrainageADCommon.tol_StackR)
                {
                    elements.Add(new ThRawIfcSpatialElementData()
                    {
                        Data = curveType["Circle"],
                        Geometry = new Circle(new Point3d(c.Center.X, c.Center.Y, 0), new Vector3d(0, 0, 1), c.Radius)
                    });
                }
            }


            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, Matrix3d.Identity);
            }

        }


        private void HandleBlockReference(List<ThRawIfcSpatialElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsDistributionElement(blkref))
            {
                if (BlockObbSwitch)
                {
                    var rec = blkref.ToOBB(blkref.BlockTransform.PreMultiplyBy(matrix));
                    elements.Add(new ThRawIfcSpatialElementData()
                    {
                        Data = blkref.GetEffectiveName(),
                        Geometry = rec
                    });
                }
                else
                {
                    elements.Add(new ThRawIfcSpatialElementData()
                    {
                        Data = blkref.GetEffectiveName(),
                        Geometry = blkref
                    });
                }

            }
        }

        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public bool IsDistributionElement(Entity entity)
        {
            var bReturn = false;
            if (entity is BlockReference br)
            {
                var blkName = br.GetEffectiveName();
                bReturn = IsExisted(blkName, toiBlkNames);
                bReturn = bReturn || IsExisted(blkName, valveBlkName);
            }
            if (entity is Curve)
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
            var bReturn = false;
            if (LayerFilter.Count == 0)
            {
                bReturn = true;
            }

            if (bReturn == false)
            {
                if (curve is Line || curve is Circle)
                {
                    if (LayerFilter.Contains(curve.Layer))
                    {
                        bReturn = true;
                    }
                }
                else
                {
                    bReturn = true;
                }

            }

            return bReturn;

        }

        public override bool IsSpatialElementBlock(BlockTableRecord blockTableRecord)
        {
            return true;
        }

    }
}
