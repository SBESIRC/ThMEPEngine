using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class Draw
    {
        public static void MainLoop(AcadDatabase acadDatabase, List<List<Point3dEx>> mainPathList)
        {
            foreach(var path in mainPathList)
            {
                for (int i = 0; i < mainPathList[0].Count - 1; i++)
                {
                    var pt1 = path[i]._pt;
                    var pt2 = path[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId("0");
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
            
        }
        public static void SubLoop(AcadDatabase acadDatabase, SpraySystem spraySystem)
        {
            foreach (var loop in spraySystem.SubLoops)
            {
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    var pt1 = loop[i]._pt;
                    var pt2 = loop[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId("0");
                    line.ColorIndex = 255;
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }
        public static void BranchLoop(AcadDatabase acadDatabase, SpraySystem spraySystem)
        {
            foreach (var loop in spraySystem.BranchLoops)
            {
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    var pt1 = loop[i]._pt;
                    var pt2 = loop[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId("0");
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }
    }
}
