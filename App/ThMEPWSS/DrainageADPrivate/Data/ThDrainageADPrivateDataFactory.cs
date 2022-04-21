using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using Dreambuild.AutoCAD;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;


using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;

namespace ThMEPWSS.DrainageADPrivate.Data
{
    internal class ThDrainageADPrivateDataFactory
    {
        //input
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public List<string> BlockNameValve { get; set; } = new List<string>();
        public List<string> BlockNameTchValve { get; set; } = new List<string>();
        public List<string> LayerFilterTchValve { get; set; } = new List<string>();
        public ThMEPOriginTransformer Transformer { get; set; }

        //output
        public List<ThIfcVirticalPipe> VerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcFlowSegment> TCHPipe { get; set; } = new List<ThIfcFlowSegment>();
        public List<ThIfcDistributionFlowElement> SanitaryTerminal { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<ThIfcDistributionFlowElement> ValveWaterHeater { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> TchValve { get; set; } = new List<Entity>();
        public List<Entity> OpeningSign { get; set; } = new List<Entity>();

        public ThDrainageADPrivateDataFactory()
        { }
        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractVerticalPipe(database, framePts);
            ExtractPipe(database, framePts);
            ExtractSanitaryTerminal(database, framePts);
            ExtractValve(database, framePts);
            ExtractTCHValve(database, framePts);
            ExtractTCHEquipment(database, framePts);
        }

        /// <summary>
        /// 天正立管
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractVerticalPipe(Database database, Point3dCollection framePts)
        {
            var vertical = new ThVerticalPipeExtractService()
            {
                //LayerFilter = new List<string> { ThDrainageADCommon.Layer_EQPM, ThDrainageADCommon.Layer_EQPM_D },
            };
            vertical.Extract(database, framePts);
            VerticalPipe.AddRange(vertical.VerticalPipe);
        }

        /// <summary>
        /// 洁具
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractSanitaryTerminal(Database database, Point3dCollection framePts)
        {
            var terminalBlkNames = new List<string>();
            foreach (var blkName in ThDrainageADCommon.TerminalChineseName)
            {
                terminalBlkNames.AddRange(QueryBlkNames(blkName.Value));
            }
            terminalBlkNames = terminalBlkNames.Distinct().ToList();

            if (terminalBlkNames.Count == 0)
            {
                return;
            }

            var sanitaryTerminalExtractor = new ThSanitaryTerminalRecognitionEngine()
            {
                BlockNameList = terminalBlkNames,
                LayerFilter = new List<string>(),
            };

            sanitaryTerminalExtractor.Recognize(database, framePts);
            //sanitaryTerminalExtractor.Elements.ForEach(x => SanitaryTerminal.Add(x.Outline as BlockReference));
            SanitaryTerminal.AddRange(sanitaryTerminalExtractor.Elements);
        }

        /// <summary>
        /// 天正水平管
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractPipe(Database database, Point3dCollection framePts)
        {
            var TCHPipeRecognize = new ThTCHPipeRecognitionEngine()
            {
                LayerFilter = new List<string> { ThDrainageADCommon.Layer_CoolPipe, ThDrainageADCommon.Layer_HotPipe },
            };
            TCHPipeRecognize.RecognizeMS(database, framePts);
            TCHPipe.AddRange(TCHPipeRecognize.Elements.OfType<ThIfcFlowSegment>().ToList());
        }

        /// <summary>
        /// 热水器 角阀 截止阀 闸阀 止回阀 防污隔断阀 套管
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractValve(Database database, Point3dCollection framePts)
        {
            var extractor = new ThValveRecognitionEngine()
            {
                BlockNameList = BlockNameValve,
            };

            extractor.Recognize(database, framePts);
            ValveWaterHeater.AddRange(extractor.Elements);
        }

        /// <summary>
        /// 天正阀
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractTCHValve(Database database, Point3dCollection framePts)
        {
            var layerFilter = new List<string> { ThDrainageADCommon.Layer_EQPM };

            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => CheckLayerFilter(o.Layer, layerFilter));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(framePts);

                // 天正阀
                dbObjs.OfType<Entity>()
                    .Where(e => e.IsTCHValve())
                    .ForEach(e => TchValve.Add(e));

            }
        }

        /// <summary>
        /// 天正断管阀
        /// </summary>
        /// <param name="database"></param>
        /// <param name="framePts"></param>
        private void ExtractTCHEquipment(Database database, Point3dCollection framePts)
        {
            var layerFilter = new List<string> { ThDrainageADCommon.Layer_EQPM, ThDrainageADCommon.Layer_EQPM_D, ThDrainageADCommon.Layer_DIMS, ThDrainageADCommon.Layer_DIMS_D };

            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => CheckLayerFilter(o.Layer, layerFilter));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(framePts);

                // 天正阀
                dbObjs.OfType<Entity>()
                    .Where(e => e.IsTCHEquipment())
                    .ForEach(e => OpeningSign.Add(e));

            }
        }
       
        private bool CheckLayerFilter(string layerName, List<string> LayerFilter)
        {
            var bReturn = false;
            if (LayerFilter.Count > 0)
            {
                bReturn = LayerFilter.Contains(layerName);
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }

        private List<string> QueryBlkNames(string category)
        {
            var blkName = new List<string>();

            BlockNameDict.TryGetValue(category, out blkName);
            return blkName;
        }
    }
}
