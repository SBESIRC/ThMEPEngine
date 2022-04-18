using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class PRValveDetail
    {
        public static void Add(int i, AcadDatabase acadDatabase, List<ThWSSDBranchPipe> BranchPipe)
        {
            if (i+1==5)
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", "减压阀详图-AI-2",
                            BranchPipe[i].GetPRValveDetailSite(), new Scale3d(1, 1, 1), 0);
                var ptls = new Point3d[3];
                ptls[0] = BranchPipe[i - 1].GetPressureReducingValveSite();
                ptls[1] = new Point3d(BranchPipe[i - 1].GetPressureReducingValveSite().X - 500, BranchPipe[i].GetPRValveDetailSite().Y, 0);
                ptls[2] = BranchPipe[i].GetPRValveDetailSite().OffsetX(1778);
                var polyline = new Polyline3d(0, new Point3dCollection(ptls), false);
                polyline.LayerId = DbHelper.GetLayerId("W-NOTE");
                polyline.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(polyline);
            }  
        }

        public static void Add(int i, AcadDatabase acadDatabase, List<HalfBranchPipe> BranchPipe)
        {
            if (i + 1 == 5)
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", "减压阀详图-AI-2",
                            BranchPipe[i].PRValveDetailSite, new Scale3d(1, 1, 1), 0);
                var ptls = new Point3d[3];
                ptls[0] = BranchPipe[i - 1].PressureReducingValveSite;
                ptls[1] = new Point3d(BranchPipe[i - 1].PressureReducingValveSite.X - 500, BranchPipe[i].PRValveDetailSite.Y, 0);
                ptls[2] = BranchPipe[i].PRValveDetailSite.OffsetX(1778);
                var polyline = new Polyline3d(0, new Point3dCollection(ptls), false);
                polyline.LayerId = DbHelper.GetLayerId("W-NOTE");
                polyline.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(polyline);
            }
        }
    }
}
