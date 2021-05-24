using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAutoFireAlarmSystemExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThAutoFireAlarmSystemVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            //extractor.Extract(database);    // 提取外参中的块
            extractor.ExtractFromMS(database);// 提取ModelSpace下的块
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
    public class ThAutoFireAlarmSystemRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAutoFireAlarmSystemExtractionEngine();
            engine.Extract(database);
            var originDatas = engine.Results;
            if (polygon.Count > 0)
            {
                var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }

            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }


        public ThBlockNumStatistics FillingBlockNameConfigModel(Entity polygon)
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            if (polygon is Polyline || polygon is MPolygon)
            {
                var dbObjs = Elements.Select(o => o.Outline).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                var Data = Elements.Where(o => dbObjs.Contains(o.Outline)).ToList();
                ThBlockConfigModel.BlockConfig.ForEach(o => BlockDataReturn.BlockStatistics.Add(o.BlockName, Data.Count(x => (x.Outline as BlockReference).Name == o.BlockName)));
            }
            else
            {
                // throw new exception
            }
            return BlockDataReturn;
        }

        public ThBlockNumStatistics FillingBlockNameConfigModel()
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            ThBlockConfigModel.BlockConfig.ForEach(o => BlockDataReturn.BlockStatistics.Add(o.BlockName, Elements.Count(y => (y.Outline as BlockReference).Name == o.BlockName)));
            return BlockDataReturn;
        }

        /// <summary>
        /// 测试用，一会删掉
        /// </summary>
        /// <returns></returns>
        public ThBlockNumStatistics FillingBlockNameConfigModelAll0Test()
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            ThBlockConfigModel.BlockConfig.ForEach(o => BlockDataReturn.BlockStatistics.Add(o.BlockName,0));
            return BlockDataReturn;
        }

        /// <summary>
        /// 测试用，一会删掉
        /// </summary>
        /// <returns></returns>
        public ThBlockNumStatistics FillingBlockNameConfigModelAll1Test()
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            ThBlockConfigModel.BlockConfig.ForEach(o =>
            {
                if (o.BlockName != "E-BFAS031")
                    BlockDataReturn.BlockStatistics.Add(o.BlockName, 1);
                else
                    BlockDataReturn.BlockStatistics.Add(o.BlockName, 0);
            });
            return BlockDataReturn;
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
