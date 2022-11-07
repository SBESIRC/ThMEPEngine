using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertElementExtractionEngine : ThDistributionElementExtractionEngine
    {
        /// <summary>
        /// 块名
        /// </summary>
        public List<string> NameFilter { get; set; }

        /// <summary>
        /// 专业
        /// </summary>
        public ConvertCategory Category { get; set; }

        public override void Extract(Database database)
        {
            var visitor = new ThBConvertElementExtractionVisitor()
            {
                NameFilter = NameFilter,
                Category = Category,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
}
