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
     public static class ThFireBranchPts
        {
            public static Point3d AddPt2Pt3(ThHalfFireBranchPipe halfBranchPipe, Point3d pt1, double pt1pt2Dist = -1, double pt3dist = -50)
            {
                double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
                halfBranchPipe.TextSite = pt1.OffsetY(-1400);
                if (pt1pt2Dist < 0)
                {
                    pt1pt2Dist = halfBranchPipe.BranchPipeX;
                }
                var pt2 = pt1.OffsetX(pt1pt2Dist);
                var pt231 = pt2.OffsetY(-50);
                var pt232 = pt231.OffsetY(-210);
                var pt3 = pt232.OffsetY(pt3dist);
                halfBranchPipe.PressureReducingValveSite = pt231;
                halfBranchPipe.PRValveDetailSite = new Point3d(pt1.X - 5000, curFloorLowerHeight + 200, 0);
                halfBranchPipe.AutoExhaustValveSite = pt1;
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt2));
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt1.OffsetY(-500)));
                if (!halfBranchPipe.PRValveStyle)
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt2, pt231));
                    halfBranchPipe.BranchPipes.Add(new Line(pt232, pt3));
                }
                else
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt2, pt3));
                }
                return pt3;
            }
            public static Point3d Get(Point3d spt, ThHalfFireBranchPipe halfBranchPipe, double pt3pt31Dist = 400)
            {
                Point3d pt1, pt2, pt3, pt4, pt5, pt6;
                pt1 = spt.OffsetX(pt3pt31Dist);
                pt2 = pt1.OffsetX(210);
                halfBranchPipe.BranchPipes.Add(new Line(spt, pt1));
                if (halfBranchPipe.PRValveStyle)
                {
                    pt5 = pt2.OffsetX(75);
                    pt6 = pt5.OffsetX(210);
                    pt3 = pt6.OffsetX(75);
                    halfBranchPipe.BranchPipes.Add(new Line(pt2, pt5));
                    halfBranchPipe.BranchPipes.Add(new Line(pt6, pt3));
                }
                else
                {
                    pt5 = pt2.OffsetX(75);
                    pt6 = pt5.OffsetX(210);
                    pt3 = pt2.OffsetX(75);
                    halfBranchPipe.BranchPipes.Add(new Line(pt2, pt3));
                }
                pt4 = pt3.OffsetX(210);
                halfBranchPipe.CheckValveSite.Add(GetMiddlePt(pt1, pt2));
                halfBranchPipe.PRValveSite.Add(pt5);
                halfBranchPipe.WaterMeterSite.Add(GetMiddlePt(pt3, pt4));
                return pt4;
            }
            public static Point3d Get(Point3d spt, List<Line> BranchPipes, bool PRValveStyle, List<Point3d> CheckValveSite,
                List<Point3d> PRValveSite, List<Point3d> WaterMeterSite)
            {
                Point3d pt1, pt2, pt3, pt4, pt5, pt6;
                pt1 = spt.OffsetX(400);
                pt2 = pt1.OffsetX(210);
                BranchPipes.Add(new Line(spt, pt1));
                if (PRValveStyle)
                {
                    pt5 = pt2.OffsetX(75);
                    pt6 = pt5.OffsetX(210);
                    pt3 = pt6.OffsetX(75);
                    BranchPipes.Add(new Line(pt2, pt5));
                    BranchPipes.Add(new Line(pt6, pt3));
                }
                else
                {
                    pt5 = pt2.OffsetX(75);
                    pt6 = pt5.OffsetX(210);
                    pt3 = pt2.OffsetX(75);
                    BranchPipes.Add(new Line(pt2, pt3));
                }
                pt4 = pt3.OffsetX(210);
                CheckValveSite.Add(GetMiddlePt(pt1, pt2));
                PRValveSite.Add(pt5);
                WaterMeterSite.Add(GetMiddlePt(pt3, pt4));
                return pt4;
            }
            public static Point3d GetMiddlePt(Point3d pt1, Point3d pt2)
            {
                return new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            }
        }
}