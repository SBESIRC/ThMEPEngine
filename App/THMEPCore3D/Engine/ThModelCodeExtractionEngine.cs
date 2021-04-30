using System;
using THMEPCore3D.Model;
using THMEPCore3D.Utils;
using THMEPCore3D.Service;
using THMEPCore3D.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Engine
{
    public class ThModelCodeExtractionEngine : IExtract,IRawDataConvert,IDisposable
    {
        public List<ThModelCode> Results { get; set; }
        public ThModelCodeExtractionEngine()
        {
            Results = new List<ThModelCode>();
        }

        public void Extract(Database db, Point3dCollection range)
        {
            var vistor = new ThModelCodeExtractionVisitor();            
            var extractor = new ThDB3ElementExtractor();
            extractor.Accept(vistor);
            extractor.Extract(db);
            Convert(vistor.Results.Filter(range));
        }

        public void Convert(List<ThDb3ElementRawData> datas)
        {
            datas.ForEach(o => Results.Add(new ThModelCode(o.Data)));
        }

        public void Dispose()
        {
            //
        }
    }
}
