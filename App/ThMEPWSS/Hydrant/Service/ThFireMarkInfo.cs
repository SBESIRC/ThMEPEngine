using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Assistant;
using ThMEPWSS.ViewModel;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using static ThMEPWSS.Assistant.DrawUtils;
using ThMEPEngineCore.Model.Common;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Exception = System.Exception;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Model;
using static ThMEPWSS.Hydrant.Service.Common;
namespace ThMEPWSS.Hydrant.Service
{
     public class ThFireMarkInfo
        {
            public static DBText NoteText(Point3d position, string textString)
            {
                var text = new DBText
                {
                    TextString = textString,
                    Position = new Point3d(position.X, position.Y, 0),
                    LayerId = DbHelper.GetLayerId("W-NOTE"),
                    WidthFactor = 0.7,
                    Height = 350,
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                    ColorIndex = (int)ColorIndex.BYLAYER
                };
                return text;
            }
            public static DBText PipeText(Point3d position, string textString)
            {
                var text = new DBText
                {
                    TextString = textString,
                    Position = position,
                    LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                    WidthFactor = 0.7,
                    Height = 350,
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                    ColorIndex = (int)ColorIndex.BYLAYER
                };
                return text;
            }
        }
}