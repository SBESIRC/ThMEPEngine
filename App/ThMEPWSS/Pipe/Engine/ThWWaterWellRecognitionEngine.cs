using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWWaterWellVisitor : ThDistributionElementExtractionVisitor
    {
        WaterWellIdentifyConfigInfo ConfigInfo = null;//配置信息
        public ThWWaterWellVisitor(WaterWellIdentifyConfigInfo configInfo)
        {
            ConfigInfo = configInfo;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                var name = reference.GetEffectiveName();
                foreach (string label in ConfigInfo.WhiteList)
                {
                    if (name.EndsWith(label) && label != "")
                    {
                        //将该空间添加到list中
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            //var outline = blkref.ToOBB(blkref.BlockTransform.PreMultiplyBy(matrix));
            var curves = Explode(blkref).OfType<Curve>()
                .Where(o=> o is Line || o is Polyline)
                .Where(c=>c.Visible).ToCollection();
            var outline = curves.GetMinimumRectangle().GetTransformedCopy(matrix) as Polyline;

            var elementInfo = new WWaterWellElementInfo()
            {
                Outline = outline,
                BlkEffectiveName = blkref.GetEffectiveName(),
            };
            elements.Add(new ThRawIfcDistributionElementData()
            {
                Data = elementInfo,
                Geometry = blkref.GetTransformedCopy(matrix),
            });
        }

        private DBObjectCollection Explode(BlockReference br)
        {
            var results = new DBObjectCollection();
            var objs = new DBObjectCollection();
            br.Explode(objs);
            foreach(Entity entity in objs)
            {
                if(entity is BlockReference br1)
                {
                    var subObjs = Explode(br1);
                    subObjs.OfType<Entity>().ForEach(e => results.Add(e));
                }
                else
                {
                    results.Add(entity);
                }
            }
            return results;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public class WWaterWellElementInfo
    {
        public string BlkEffectiveName { get; set; }
        public Polyline Outline { get; set; }
        public WWaterWellElementInfo()
        {
            BlkEffectiveName = "";
        }
    }
    public class ThWWaterWellExtractionEngine : ThDistributionElementExtractionEngine
    {
        public WaterWellIdentifyConfigInfo ConfigInfo { get; set; }//配置信息
        public override void Extract(Database database)
        {
            var waterWellVisitor = new ThWWaterWellVisitor(ConfigInfo);
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(waterWellVisitor);
            extractor.Extract(database); //从块和外参里提取元素
            Results.AddRange(waterWellVisitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var waterWellVisitor = new ThWWaterWellVisitor(ConfigInfo);
            var extractor = new ThDistributionElementExtractor();            
            extractor.Accept(waterWellVisitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(waterWellVisitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
    public class ThWWaterWellRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        WaterWellIdentifyConfigInfo ConfigInfo = null;//配置信息
        public List<ThRawIfcDistributionElementData> Datas { get; set; }

        public ThWWaterWellRecognitionEngine(WaterWellIdentifyConfigInfo configInfo)
        {
            ConfigInfo = configInfo;
            Datas = new List<ThRawIfcDistributionElementData>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThWWaterWellExtractionEngine()
            { 
                ConfigInfo = this.ConfigInfo,
            };
            extractionEngine.Extract(database);
            Recognize(extractionEngine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThWWaterWellExtractionEngine()
            {
                ConfigInfo = this.ConfigInfo,
            };
            extractionEngine.ExtractFromMS(database);
            Recognize(extractionEngine.Results, polygon);
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> datas,Point3dCollection polygon)
        {
            var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }

            var tmpData = datas.Where(o => dbObjs.Contains(o.Geometry)).ToList();

            Datas.AddRange(tmpData);
            Elements.AddRange(tmpData.Select(o => o.Geometry).Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));
        }
    }
}
