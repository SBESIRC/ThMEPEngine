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
        public class ThFireFloorZone
        {
            private Point3d StartPt { get; set; }
            private Point3d EndPt { get; set; }
            private List<double> LineXList { get; set; }
            public ThFireFloorZone(Point3d startPt, Point3d endPt, List<double> lineXList)
            {
                StartPt = startPt;
                EndPt = endPt;
                LineXList = lineXList;
                LineXList.Sort();
            }
            public Point3d[] CreatePolyLine(double X1, double X2, double Y1, double Y2)
            {
                var ptls = new Point3d[5];
                ptls[0] = new Point3d(X1, Y1, 0);
                ptls[1] = new Point3d(X2, Y1, 0);
                ptls[2] = new Point3d(X2, Y2, 0);
                ptls[3] = new Point3d(X1, Y2, 0);
                ptls[4] = new Point3d(X1, Y1, 0);
                return ptls;
            }
            public List<Point3dCollection> CreateRectList()
            {
                var rectls = new List<Point3dCollection>();
                if (LineXList.Count == 0)
                {
                    Point3d[] rect;
                    rect = CreatePolyLine(StartPt.X, EndPt.X, StartPt.Y, EndPt.Y);
                    rectls.Add(new Point3dCollection(rect));
                    return rectls;
                }
                rectls.Add(new Point3dCollection(CreatePolyLine(StartPt.X, EndPt.X, StartPt.Y, EndPt.Y)));
                for (int i = 0; i < LineXList.Count + 1; i++)
                {
                    Point3d[] rect;
                    if (i == 0)
                    {
                        rect = CreatePolyLine(StartPt.X, LineXList[i], StartPt.Y, EndPt.Y);
                    }
                    else if (i == LineXList.Count)
                    {
                        rect = CreatePolyLine(LineXList[i - 1], EndPt.X, StartPt.Y, EndPt.Y);
                    }
                    else
                    {
                        rect = CreatePolyLine(LineXList[i - 1], LineXList[i], StartPt.Y, EndPt.Y);
                    }
                    rectls.Add(new Point3dCollection(rect));
                }
                return rectls;
            }
        }
}