using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;
using System.Collections.Generic;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThBeamPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb, Curve curve, PrintConfig config)
        {
            var results = new ObjectIdCollection();
            var beamId = curve.Print(acadDb, config);
            if(beamId!=ObjectId.Null)
            {
                results.Add(beamId);
            }            
            return results;
        }
        public static PrintConfig GetBeamConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.BeamLayerName,
            };
        }

        public static AnnotationPrintConfig GetBeamTextConfig(string drawingScale)
        {
            var config = GetBeamTextConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }

        private static AnnotationPrintConfig GetBeamTextConfig()
        {
            // 1:1
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.BeamTextLayerName,
                Height = 2.5,
                WidthFactor = 0.7,
                TextStyleName = ThPrintStyleManager.THSTYLE3,
            };
        }

        public static PrintConfig GetBeamConfig(Dictionary<string, object> properties)
        {
            var config = ThBeamPrinter.GetBeamConfig();
            var lineType = properties.GetLineType();
            if (string.IsNullOrEmpty(lineType))
            {
                return config;
            }
            else
            {
                // 根据模板来设置
                if (lineType.ToUpper() == "CONTINUOUS")
                {
                    config.LineType = "ByBlock";
                }
                else
                {
                    config.LineType = "ByLayer";
                }
                return config;
            }
        }
    }
}
