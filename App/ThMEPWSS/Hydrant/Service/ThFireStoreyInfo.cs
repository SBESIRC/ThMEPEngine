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
      public class ThFireStoreyInfo
        {
            private int FloorNumber { get; set; }
            private double FloorHeight { get; set; }
            private bool HasFlushFaucet { get; set; }
            private bool NoPRValve { get; set; }
            private int[] Households { get; set; }
            public ThFireStoreyInfo(int floorNumber, double floorHeight, bool hasFlushFaucet, bool noPRValve, int[] households)
            {
                FloorNumber = floorNumber;
                FloorHeight = floorHeight;
                HasFlushFaucet = hasFlushFaucet;
                NoPRValve = noPRValve;
                Households = households;
            }
            public int GetFloorNumber()
            {
                return FloorNumber;
            }
            public double GetFloorHeight()
            {
                return FloorHeight;
            }
            public bool GetFlushFaucet()
            {
                return HasFlushFaucet;
            }
            public bool GetPRValve()
            {
                return NoPRValve;
            }
            public int[] GetHouseholds()
            {
                return Households;
            }
            public Line CreateLine(Point3d insertPt, double floorLength)
            {
                var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);
                var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);
                var line = new Line(pt1, pt2);
                line.LayerId = DbHelper.GetLayerId("W-NOTE");
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                return line;
            }
            public List<Line> CreateHalfFloorLine(int floorNums, Point3d insertPt, double floorLength, List<int> highestStorey, Double[] PipeOffsetX)
            {
                var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);
                var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);
                var pt15 = pt1.OffsetY(FloorHeight * 0.5);
                var pt25 = pt2.OffsetY(FloorHeight * 0.5);
                var ls = new List<Line>();
                ls.Add(new Line(pt1.OffsetX(15000), pt1.OffsetX(17400)));
                if (FloorNumber <= floorNums)
                {
                    ls.Add(new Line(pt15, pt25));
                }
                return ls;
            }
            public void DrawStorey(int i, ThFireSysInfo sysIn)
            {
                var insertPt = sysIn.InsertPt;
                var floorLength = sysIn.FloorLength;
                var floorNums = sysIn.FloorNumbers;
                var floorHeightDic = sysIn.FloorHeightDic;
                using AcadDatabase acadDatabase = AcadDatabase.Active();
                {
                    var line = CreateLine(insertPt, floorLength);
                    acadDatabase.CurrentSpace.Add(line);
                    var textFirst = new DBText();
                    if (i < floorNums)
                    {
                        textFirst = ThFireMarkInfo.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
                    }
                    else
                    {
                        textFirst = ThFireMarkInfo.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), "RF");
                    }
                    textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
                    acadDatabase.CurrentSpace.Add(textFirst);
                    string height = "X.XX";
                    if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
                    {
                        height = floorHeightDic[Convert.ToString(i + 1)];
                    }
                    var attNameValues = new Dictionary<string, string>() { { "标高", height } };
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", Elevation,
                        insertPt.OffsetY(i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
                }
            }
            public void DrawHalfFloorStorey(int i, ThFireSysInfo sysIn, ThFireSysProcess sysProcess)
            {
                var insertPt = sysIn.InsertPt;
                var floorLength = sysIn.FloorLength;
                var highestStorey = sysIn.HighestStorey;
                var PipeOffsetX = sysProcess.PipeOffsetX;
                var floorNums = sysIn.FloorNumbers;
                var floorHeightDic = sysIn.FloorHeightDic;
                using AcadDatabase acadDatabase = AcadDatabase.Active();
                var lines = CreateHalfFloorLine(floorNums, insertPt, floorLength, highestStorey, PipeOffsetX);
                foreach (var line1 in lines)
                {
                    line1.LayerId = DbHelper.GetLayerId("W-NOTE");
                    line1.ColorIndex = (int)ColorIndex.BYLAYER;
                    acadDatabase.CurrentSpace.Add(line1);
                }
                var textFirst = new DBText();
                if (i < floorNums)
                {
                    textFirst = ThFireMarkInfo.NoteText(insertPt.OffsetXY(16500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
                }
                else
                {
                    textFirst = ThFireMarkInfo.NoteText(insertPt.OffsetXY(16500, i * FloorHeight + 100), "RF");
                }
                textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(textFirst);
                string height = "X.XX";
                string height2 = "X.XX";
                double heightInt = 0.0;
                if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
                {
                    height = floorHeightDic[Convert.ToString(i + 1)];
                    if (i == 0)
                    {
                        heightInt = 0;
                    }
                    else
                    {
                        heightInt = Convert.ToDouble(height);
                    }
                }
                if (floorHeightDic.ContainsKey(Convert.ToString(i + 2)))
                {
                    height2 = floorHeightDic[Convert.ToString(i + 2)];
                    var height2Int = Convert.ToDouble(height2);
                    height2 = Convert.ToString((height2Int + heightInt) / 2.0);
                }
                var attNameValues = new Dictionary<string, string>() { { "标高", height } };
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", Elevation,
                    insertPt.OffsetXY(15000, i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
                if (i < floorNums)
                {
                    var attNameValues2 = new Dictionary<string, string>() { { "标高", height2 } };
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", Elevation,
                        insertPt.OffsetY((i + 0.5) * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues2);
                }
            }
        }
}