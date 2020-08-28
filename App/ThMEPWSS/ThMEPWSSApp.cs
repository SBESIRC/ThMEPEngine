using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad.Collections;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThWSS.Bussiness;

namespace ThMEPWSS
{
    public class ThMEPWSSApp : IExtensionApplication
    {
        public void Initialize()
        {
            //throw new System.NotImplementedException();
        }

        public void Terminate()
        {
            //throw new System.NotImplementedException();
        }

        [CommandMethod("TIANHUACAD", "THGETGRID", CommandFlags.Modal)]
        public void ThGetGridModel()
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
                            new TypedValue((int)DxfCode.LayerName, "S_COLU"),
                            new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Polyline)).DxfName),
                        });
                    var columRes = Active.Editor.SelectCrossingPolygon(points, selectionFilter);
                    if (columRes.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    List<Polyline> columPoly = new List<Polyline>();
                    foreach (var colId in columRes.Value.GetObjectIds())
                    {
                        columPoly.Add(acdb.Element<Polyline>(colId).Clone() as Polyline);
                    }

                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plBack, columPoly);

                    //List<SprayLayoutData> roomSprays = new List<SprayLayoutData>();
                    //foreach (var lpts in sprayPts)
                    //{
                    //    roomSprays.AddRange(GeoUtils.CalRoomSpray(plBack, lpts, out List<SprayLayoutData> outsideSpary));
                    //}

                    //放置喷头
                    //InsertSprayService.InsertSprayBlock(roomSprays.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);
                }
            }
        }
    }
}
