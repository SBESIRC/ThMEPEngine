using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class Elevation
    {
        public static void Add(int i, AcadDatabase acadDatabase, List<ThWSSDBranchPipe> BranchPipe, ref bool elevateFlag,
            List<int> FlushFaucet, int layingMethod, double floorHeight, Point3d insertPoint)
        {
            if (i + 1 >= 6)//第六层放置水管引出标高
            {
                if (!BranchPipe[i].GetCheckValveSite().IsNull() && elevateFlag)
                {
                    elevateFlag = false;
                    var ptLs = new Point3d[3];
                    double scale = 1.0;
                    double eleX;
                    
                 
                    var insertPt = new Point3d();
                    var eleYs = (from e in BranchPipe[i].GetWaterMeterSite() select e.Y).ToList();
                    eleYs.Sort((x, y) => -x.CompareTo(y));
                    var flagDuobi = Math.Abs(eleYs[0] - floorHeight * (i + 1) - insertPoint.Y) < 710 * scale;
                    if (layingMethod == 0)
                    {
                        eleX = (from e in BranchPipe[i].GetWaterPipeInterrupted() select e.X).Max() + 500;
                    }
                    else
                    {
                        eleX = (from e in BranchPipe[i].GetWaterPipeInterrupted() select e.X).Max() + 500;
                        
                    }
                    if (FlushFaucet.Contains(i + 1))
                    {
                        eleX = BranchPipe[i].GetVacuumBreakerSite().X + 500;
                    }
                    for (int k = 0; k < eleYs.Count; k++)
                    {
                        if (k == 0)
                        {
                            if (flagDuobi)
                            {
                                ptLs[0] = new Point3d(eleX, eleYs[0], 0);
                                ptLs[1] = ptLs[0].OffsetX(300);
                                ptLs[2] = new Point3d(ptLs[1].X, floorHeight * (i + 1) + insertPoint.Y + 100, 0);
                                insertPt = ptLs[2].OffsetX(250 * scale);//标高插入点
                            }
                            else
                            {
                                insertPt = new Point3d(eleX + 250 * scale, eleYs[0], 0);
                                ptLs[0] = new Point3d(eleX + 250 * scale, eleYs[0], 0);
                                ptLs[1] = new Point3d(eleX + 250 * scale, eleYs[0], 0);
                                ptLs[2] = new Point3d(eleX + 250 * scale, eleYs[0], 0);
                            }
                        }
                        else
                        {
                            if(flagDuobi)
                            {
                                insertPt = new Point3d(insertPt.X + 700, eleYs[k], 0);
                                ptLs[0] = new Point3d(eleX, eleYs[k], 0);
                                ptLs[1] = insertPt.OffsetX(-250 * scale);
                                ptLs[2] = insertPt.OffsetX(-250 * scale);
                            }
                            else
                            {
                                insertPt = new Point3d(insertPt.X + 700, eleYs[k], 0);
                                ptLs[0] = new Point3d(eleX, eleYs[k], 0);
                                ptLs[1] = insertPt.OffsetX(-250 * scale);
                                ptLs[2] = insertPt.OffsetX(-250 * scale);
                            }
                            
                        }
                        if(!ptLs[0].Equals(new Point3d()) && !ptLs[1].Equals(new Point3d()))
                        {
                            var lineNote = new Polyline3d(0, new Point3dCollection(ptLs), false)
                            {
                                LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                                ColorIndex = (int)ColorIndex.BYLAYER
                            };
                            acadDatabase.CurrentSpace.Add(lineNote);
                        }

                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                        insertPt, new Scale3d(scale, scale, scale), 0, new Dictionary<string, string> { { "标高", "X.XX" } });
                    }
                    
                }
            }
        }

        public static void Add(int i, AcadDatabase acadDatabase, List<HalfBranchPipe> BranchPipe, ref bool elevateFlag,
             Point3d insertPoint)
        {
            if (i + 1 == 7)//第六层放置水管引出标高
            {
                if (!BranchPipe[i].CheckValveSite.IsNull() && elevateFlag)
                {
                    double scale = 1.0;
                    var eleYs = (from e in BranchPipe[i].WaterMeterSite select e.Y).ToList();
                    eleYs.Sort((x, y) => -x.CompareTo(y));
                    var eleX = insertPoint.X + 7000;
  
                    for (int k = 0; k < eleYs.Count; k++)
                    {
                        var insertPt = new Point3d(eleX,eleYs[k],0);
                        var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                        insertPt, new Scale3d(scale, scale, scale), 0, new Dictionary<string, string> { { "标高", "X.XX" } });
                        short value = 1;
                        objID.SetDynBlockValue("翻转状态2", value);
                        eleX += 1400;
                    }
                }
            }
        }
    }
}
