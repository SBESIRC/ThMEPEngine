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
      public class ThPipeComputeService
        {
            private double U0 { get; set; }
            private double Ng { get; set; }
            public ThPipeComputeService(double u0, double ng)
            {
                Ng = ng;
                U0 = u0;
            }
            public string PipeDiameterCompute()
            {
                Dictionary<double, double> U0ToAlphaC = new Dictionary<double, double>
            {{1.0, 0.00323 }, {1.5, 0.00697 }, {2.0, 0.01097 }, {2.5, 0.01512 }, {3.0, 0.01939 }, {3.5, 0.02374 },
            {4.0, 0.02816 }, {4.5, 0.03263 }, {5.0, 0.03715 }, {6.0, 0.04629 }, {7.0, 0.05555 }, {8.0, 0.06489 }};
                double key1 = 1.0;
                double alphaC = 0;
                foreach (double key in U0ToAlphaC.Keys)
                {
                    if (U0 >= key)
                    {
                        key1 = key;
                    }
                    if (U0 < key)
                    {
                        alphaC = (U0ToAlphaC[key] - U0ToAlphaC[key1]) * (U0 - key1) / (key - key1) + U0ToAlphaC[key1];
                    }
                }
                double U = (1 + alphaC * Math.Pow((Ng - 1), 0.49)) / (Math.Sqrt(Ng));
                double qg = 0.2 * U * Ng;
                Dictionary<string, double> pipeDList = new Dictionary<string, double>
            { {"DN20",0.0213 }, {"DN25",0.0273 },  {"DN32",0.0354 }, {"DN40",0.0413 },
              {"DN50",0.0527 }, {"DN65",0.0681 },  {"DN80",0.0809 }, {"DN100",0.1063 },
              {"DN125",0.131 }, {"DN150",0.1593 }, {"DN200",0.2071 } };
                foreach (string key in pipeDList.Keys)
                {
                    double d = pipeDList[key];
                    double FlowRate = qg * 4 / (Math.PI * Math.Pow(d, 2) * 1000);
                    switch (key)
                    {
                        case "DN20":
                            if (FlowRate <= 0.8)
                            {
                                return key;
                            }
                            break;
                        case "DN25":
                        case "DN32":
                        case "DN40":
                            if (FlowRate <= 1)
                            {
                                return key;
                            }
                            break;
                        case "DN50":
                        case "DN65":
                            if (FlowRate <= 1.2)
                            {
                                return key;
                            }
                            break;
                        default:
                            if (FlowRate <= 1.5)
                            {
                                return key;
                            }
                            break;
                    }
                }
                return "DN15";
            }
        }
}