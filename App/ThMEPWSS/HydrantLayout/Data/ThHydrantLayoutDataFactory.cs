using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.Hydrant.Engine;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThHydrantLayoutDataFactory
    {
        //private ThMEPOriginTransformer Transformer { get; set; }
        //public List<ThExtractorBase> Extractors { get; set; }
        public List<ThIfcVirticalPipe> THCVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> BlkVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> CVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();

        public List<ThIfcDistributionFlowElement> Hydrant { get; set; } = new List<ThIfcDistributionFlowElement>();
        public ThHydrantLayoutDataFactory()
        {

        }
        //public void SetTransformer(ThMEPOriginTransformer transformer)
        //{
        //    Transformer = transformer;
        //}
        public void GetElements(Database database, Point3dCollection collection)
        {
            //    Extractors = new List<ThExtractorBase>()
            //    {
            //        new ThTchVerticalPipeExtractService()
            //        {
            //            LayerName = new List<string>(){ThHydrantCommon.Layer_Vertical},
            //            Transformer = Transformer,
            //        },
            //        new ThBlkVerticalPipeExtractor()
            //        {
            //            LayerName = ThHydrantCommon.Layer_Vertical ,
            //            BlkNameList = new List<string>(){ThHydrantCommon.BlkName_Vertical, ThHydrantCommon.BlkName_Vertical150},
            //            Transformer = Transformer,
            //        },
            //        new ThCircleVerticalPipeExtractor()
            //        {
            //            LayerName = ThHydrantCommon.Layer_Vertical,
            //            Radius = new List<double>(){100,150},
            //            Transformer = Transformer,
            //        },

            //     };

            //    Extractors.ForEach(o => o.Extract(database, collection));

            //    //////移回原位
            //    Extractors.ForEach(o =>
            //    {
            //        if (o is ITransformer iTransformer)
            //        {
            //            iTransformer.Reset();
            //        }
            //    });
            //}


            var thcVertical = new ThTchVerticalPipeExtractService();
            thcVertical.Extract(database, collection);
            THCVerticalPipe = thcVertical.TCHVerticalPipe;

            var blkVertical = new ThBlkVerticalPipeExtractService();
            blkVertical.Extract(database, collection);
            BlkVerticalPipe = blkVertical.VerticalPipe;

            var cVertical = new ThCircleVerticalPipeExtractService();
            cVertical.Extract(database, collection);
            CVerticalPipe = cVertical.VerticalPipe;


            var hydrantVisitor = new ThHydrantExtractionVisitor()
            {
                BlkNames = new List<string> { ThHydrantCommon.BlkName_Hydrant, ThHydrantCommon.BlkName_Hydrant_Extinguisher },
            };
            var hydrantRecog = new ThHydrantRecognitionEngine(hydrantVisitor);
            hydrantRecog.RecognizeMS(database, collection);
            Hydrant.AddRange(hydrantRecog.Elements);





        }




    }
}

