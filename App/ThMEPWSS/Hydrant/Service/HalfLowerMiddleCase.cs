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
     public static class HalfLowerMiddleCase
        {
            public static void Init(ThHalfFireBranchPipe halfBranchPipe)
            {
                var pt1Y = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight + 100;
                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt3 = ThFireBranchPts.AddPt2Pt3(halfBranchPipe, pt1);
                var pt374 = ThFireBranchPts.Get(pt3, halfBranchPipe);
                Point3d pt7;
                Point3d pt11;
                if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                {
                    pt7 = pt374.OffsetX(275);
                    pt11 = new Point3d(pt7.X, halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * (halfBranchPipe.FloorHeight - 1) + 790, 0);
                    var pt15 = pt11.OffsetX((halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1) * 200 + 300);
                    halfBranchPipe.FloorPts.Add(pt15.OffsetXY(444, -444));
                    var pt1501 = pt15.OffsetXY(853, -853);
                    var pt1502 = pt1501.OffsetX(275);
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt1502);
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));
                    for (int i = 1; i < halfBranchPipe.Households[halfBranchPipe.AreaIndex]; i++)
                    {
                        var pt4 = pt3.OffsetY(-250);
                        var pt484 = ThFireBranchPts.Get(pt4, halfBranchPipe);
                        var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                        var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);
                        halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, -444));
                        var pt1601 = pt1501.OffsetY(-250);
                        var pt1602 = pt1502.OffsetY(-250);
                        halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                        halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
                        halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                        halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                        halfBranchPipe.WaterPipeInterrupted.Add(pt1602);
                        if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
                        {
                            halfBranchPipe.BranchPipes.Add(new Line(pt3, pt4));
                        }
                    }
                }
                if (halfBranchPipe.HasFlushFaucet)
                {
                    double pt19Y = pt3.Y - 250 * halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                    var pt19 = new Point3d(pt3.X, pt19Y, 0);
                    var pt19204 = ThFireBranchPts.Get(pt19, halfBranchPipe);
                    var pt20 = new Point3d(pt374.X + 275 + halfBranchPipe.Households[halfBranchPipe.AreaIndex] * 200, pt19.Y, 0);
                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));
                    BaseFireCase.CreateFlushFaucet(pt20, halfBranchPipe);
                }
            }
            public static void InitUpFloor(ThHalfFireBranchPipe halfBranchPipe, bool upperFloor)
            {
                if (upperFloor)
                {
                    var pt1Y = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight + 510;
                    var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                    var pt3 = ThFireBranchPts.AddPt2Pt3(halfBranchPipe, pt1);
                    var pt374 = ThFireBranchPts.Get(pt3, halfBranchPipe);
                    Point3d pt7;
                    Point3d pt11;
                    if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                    {
                        pt7 = pt374.OffsetX(275);
                        pt11 = new Point3d(pt7.X, halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight + 790, 0);
                        var pt15 = pt11.OffsetX((halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1) * 200 + 300);
                        halfBranchPipe.FloorPts.Add(pt15.OffsetXY(444, 444));
                        var pt1501 = pt15.OffsetXY(945, 945);
                        var pt1502 = pt1501.OffsetX(275);
                        halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                        halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                        halfBranchPipe.WaterPipeInterrupted.Add(pt1502);
                        halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                        halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));
                        for (int i = 1; i < halfBranchPipe.Households[halfBranchPipe.AreaIndex]; i++)
                        {
                            var pt4 = pt3.OffsetY(-250);
                            var pt484 = ThFireBranchPts.Get(pt4, halfBranchPipe);
                            var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                            var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                            var pt16 = pt15.OffsetY(-250 * i);
                            halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, 444));
                            var pt1601 = pt1501.OffsetY(-250);
                            var pt1602 = pt1502.OffsetY(-250);
                            halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                            halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
                            halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                            halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                            halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                            halfBranchPipe.WaterPipeInterrupted.Add(pt1602);
                        }
                    }
                    if (halfBranchPipe.HasFlushFaucet)
                    {
                        double pt19Y = pt3.Y - 250 * halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                        var pt19 = new Point3d(pt3.X, pt19Y, 0);
                        var pt19204 = ThFireBranchPts.Get(pt19, halfBranchPipe);
                        var pt20 = new Point3d(pt374.X + 275 + halfBranchPipe.Households[halfBranchPipe.AreaIndex] * 200, pt19.Y, 0);
                        halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                        halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));
                        BaseFireCase.CreateFlushFaucet(pt20, halfBranchPipe);
                    }
                }
                else
                {
                    var pt1Y = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight + 510;
                    var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                    var pt3 = ThFireBranchPts.AddPt2Pt3(halfBranchPipe, pt1, -1, -520);
                    var pt374 = ThFireBranchPts.Get(pt3, halfBranchPipe);
                    Point3d pt7;
                    Point3d pt11;
                    if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                    {
                        pt7 = pt374.OffsetX(1225);
                        pt11 = new Point3d(pt7.X, halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * (halfBranchPipe.FloorHeight - 1) + 790, 0);
                        var pt15 = pt11.OffsetX((halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1) * 200 + 300);
                        halfBranchPipe.FloorPts.Add(pt15.OffsetXY(444, -444));
                        var pt1501 = pt15.OffsetXY(853, -853);
                        var pt1502 = pt1501.OffsetX(275);
                        halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                        halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                        halfBranchPipe.WaterPipeInterrupted.Add(pt1502);
                        halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                        halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));
                        for (int i = 1; i < halfBranchPipe.Households[halfBranchPipe.AreaIndex]; i++)
                        {
                            var pt4 = pt3.OffsetY(-250);
                            var pt484 = ThFireBranchPts.Get(pt4, halfBranchPipe);
                            var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                            var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                            var pt16 = pt15.OffsetY(-250 * i);
                            halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, -444));
                            var pt1601 = pt1501.OffsetY(-250);
                            var pt1602 = pt1502.OffsetY(-250);
                            halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                            halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
                            halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                            halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                            halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                            halfBranchPipe.WaterPipeInterrupted.Add(pt1602);
                            if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
                            {
                                halfBranchPipe.BranchPipes.Add(new Line(pt3, pt4));
                            }
                        }
                    }
                    if (halfBranchPipe.HasFlushFaucet)
                    {
                        double pt19Y = pt3.Y - 250 * halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                        var pt19 = new Point3d(pt3.X, pt19Y, 0);
                        var pt19204 = ThFireBranchPts.Get(pt19, halfBranchPipe);
                        var pt20 = new Point3d(pt374.X + 275 + halfBranchPipe.Households[halfBranchPipe.AreaIndex] * 200, pt19.Y, 0);
                        halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                        halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));
                        BaseFireCase.CreateFlushFaucet(pt20, halfBranchPipe);
                    }
                }
            }
            public static void Init1Floor(ThHalfFireBranchPipe halfBranchPipe, string firstFloorMeterLocation, bool firstFloor)
            {
                if (firstFloorMeterLocation.Equals("0"))
                {
                    Init(halfBranchPipe);
                }
                else
                {
                    if (firstFloor)
                    {
                        BaseFireCase.InitLobby(halfBranchPipe);
                    }
                    else
                    {
                        Init(halfBranchPipe);
                    }
                }
            }
        }
}