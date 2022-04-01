﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThVerticalPipeExtractService
    {
        //---input
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<double> Radius { get; set; } = new List<double>();

        //public List<string> LayerName { get; set; } = new List<string>();
        //public ThMEPOriginTransformer Transformer { get; set; }
        //---output
        public List<ThIfcVirticalPipe> VerticalPipe { get; protected set; }
 
        public ThVerticalPipeExtractService()
        {
            VerticalPipe = new List<ThIfcVirticalPipe>();
            LayerFilter = new List<string>() { ThHydrantCommon.Layer_Vertical };
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var TCHResult = ExtractTCHVPipe(pts);
            var blkResult = ExtractBlkVPipe(pts);
            var cResult = ExtractCVPipe(pts);
            VerticalPipe.AddRange(TCHResult);
            VerticalPipe.AddRange(blkResult);
            VerticalPipe.AddRange(cResult);
        }

        private List<ThIfcVirticalPipe> ExtractTCHVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var TCHPipeRecognize = new ThTCHVPipeRecognitionEngine()
                {
                    LayerFilter = LayerFilter,
                };
                TCHPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var TCHResult = TCHPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList();

                return TCHResult;
            }
        }
        private List<ThIfcVirticalPipe> ExtractBlkVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkPipeRecognize = new ThBlockVPipeRecognitionEngine()
                {
                    LayerFilter = LayerFilter,
                };
                blkPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var blkResult = blkPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList();

                return blkResult;
            }
        }

        private List<ThIfcVirticalPipe> ExtractCVPipe(Point3dCollection pts)
        {
         //   var radiusList = new List<double>() { 100 / 2, 150 / 2 };

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var cPipeRecognize = new ThCircleVPipeRecognitionEngine()
                {
                    LayerFilter = LayerFilter,
                    Radius = Radius,
                };
                cPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var cResult = cPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList();

                return cResult;
            }
        }

    }
}
