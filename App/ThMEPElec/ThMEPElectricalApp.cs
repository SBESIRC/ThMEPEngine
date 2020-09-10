using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPElectrical.Core;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using NFox.Cad.Collections;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPElectrical.Model;
using ThMEPElectrical.Broadcast;

namespace ThMEPElectrical
{
    public class ThMEPElectricalApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }


        [CommandMethod("TIANHUACAD", "THMainBeamRegion", CommandFlags.Modal)]
        public void ThBeamRegion()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                var polys = packageManager.DoMainBeamProfiles();
                DrawUtils.DrawProfile(polys. Polylines2Curves(), "MainBeamProfiles");
            }
        }

        [CommandMethod("TIANHUACAD", "ThBraodcast", CommandFlags.Modal)]
        public void ThBraodcast()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                SingleOnly = true,
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var filterlist = OpFilter.Bulid(o =>
              o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Polyline)).DxfName);
            var result = Active.Editor.GetSelection(options, filterlist);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame).Clone() as Polyline;
                    Point3dCollection points = new Point3dCollection();
                    for (int i = 0; i < plBack.NumberOfVertices; i++)
                    {
                        points.Add(plBack.GetPoint3dAt(i));
                    }

                    SelectionFilter selectionFilter = new SelectionFilter(
                        new TypedValue[] {
                            new TypedValue((int)DxfCode.LayerName, "AD-SIGN"),
                            new TypedValue((int)DxfCode.Start, "ARC,LINE,Polyline,LWPOLYLINE"),
                        });
                    var parkingRes = Active.Editor.SelectCrossingPolygon(points, selectionFilter);
                    if (parkingRes.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    List<Line> parkingPoly = new List<Line>();
                    foreach (var colId in parkingRes.Value.GetObjectIds())
                    {
                        parkingPoly.Add(acdb.Element<Line>(colId).Clone() as Line);
                    }

                    ParkingLinesService parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateParkingLines(plBack, parkingPoly, out List<List<Line>> otherPLines);

                    SelectionFilter selectionColumnFilter = new SelectionFilter(
                       new TypedValue[] {
                            new TypedValue((int)DxfCode.LayerName, "S_COLU"),
                            new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Polyline)).DxfName),
                       });
                    var columRes = Active.Editor.SelectWindowPolygon(points, selectionColumnFilter);
                    if (columRes.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    List<Polyline> columPoly = new List<Polyline>();
                    foreach (var colId in columRes.Value.GetObjectIds())
                    {
                        columPoly.Add(acdb.Element<Polyline>(colId).Clone() as Polyline);
                    }

                    ColumnService columnService = new ColumnService();
                    columnService.HandleColumns(parkingLines, otherPLines, columPoly, 
                        out Dictionary<List<Line>, List<ColumnModel>> mainColumns, out Dictionary<List<Line>, List<ColumnModel>> otherColumns);

                    LayoutService layoutService = new LayoutService();
                    var layoutCols = layoutService.LayoutBraodcast(plBack, mainColumns, otherColumns);

                    InsertBroadcastService.InsertSprayBlock(layoutCols);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THABBPlace", CommandFlags.Modal)]
        public void ThProfilesPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBMultiPlace", CommandFlags.Modal)]
        public void ThMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMultiWallMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THOBBRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBRect", CommandFlags.Modal)]
        public void ThABBRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamABBRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THMSABBMultiPlace", CommandFlags.Modal)]
        public void THMSABBMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainSecondBeamPlacePoints();
            }
        }
    }
}
