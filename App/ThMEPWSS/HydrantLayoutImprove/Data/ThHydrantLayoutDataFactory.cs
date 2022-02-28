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
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayoutImprove.Data
{
    public class ThHydrantLayoutDataFactory
    {
        //private ThMEPOriginTransformer Transformer { get; set; }
        //public List<ThExtractorBase> Extractors { get; set; }
        public List<ThIfcTchVPipe> THCVerticalPipe { get; set; } = new List<ThIfcTchVPipe>();
        public List<ThIfcTchVPipe> BlkVerticalPipe { get; set; } = new List<ThIfcTchVPipe>();
        public List<ThIfcTchVPipe> CVerticalPipe { get; set; } = new List<ThIfcTchVPipe>();
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

        }




    }
}

