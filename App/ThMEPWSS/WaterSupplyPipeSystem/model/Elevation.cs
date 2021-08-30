using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class Elevation
    {
        public static void Add(int i, AcadDatabase acadDatabase, List<ThWSSDBranchPipe> BranchPipe, ref bool elevateFlag)
        {
            if (i + 1 >= 6)//第六层放置水管引出标高
            {
                if (!BranchPipe[i].GetCheckValveSite().IsNull() && elevateFlag)
                {
                    elevateFlag = false;
                    for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
                    {
                        var ptLs = new Point3d[4];
                        ptLs[0] = BranchPipe[i].GetWaterPipeInterrupted()[j];

                        if (j == 0)
                        {
                            ptLs[1] = ptLs[0].OffsetX(500 + j * 300);
                            ptLs[2] = ptLs[1].OffsetY(350);
                            ptLs[3] = ptLs[2].OffsetX(500);
                        }
                        else
                        {
                            ptLs[1] = ptLs[0].OffsetX(500 + j * 300);
                            ptLs[2] = ptLs[1].OffsetX(350 * j);
                            ptLs[3] = ptLs[2].OffsetX(500);
                        }

                        var lineNote = new Polyline3d(0, new Point3dCollection(ptLs), false)
                        {
                            LayerId = DbHelper.GetLayerId("W-WSUP-NOTE")
                        };
                        acadDatabase.CurrentSpace.Add(lineNote);

                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                        ptLs[2], new Scale3d(0.8, 0.8, 0.8), 0, new Dictionary<string, string> { { "标高", "X.XX" } });
                    }
                }
            }

        }
    }
}
