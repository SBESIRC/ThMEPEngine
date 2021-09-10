using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Engine;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThOtherPipeLineService
    {
        public List<Line> GetOtherPipeLineList(Point3dCollection selectArea)
        {

            using (var database = AcadDatabase.Active())
            {
                var resLine = new List<Line>();
                var otherLineEngine = new ThOtherLineRecognitionEngine();
                otherLineEngine.Extract(selectArea);
                var dbObjs = otherLineEngine.Dbjs;
                foreach (Entity dbj in dbObjs)
                {
                    if (IsTianZhengElement(dbj))
                    {
                        List<Point3d> pts = new List<Point3d>();
                        foreach (Entity l in dbj.ExplodeToDBObjectCollection())
                        {
                            if (l is Polyline)
                            {
                                resLine.AddRange((l as Polyline).ToLines());
                            }
                            else if (l is Line)
                            {
                                resLine.Add(l as Line);
                            }
                        }
                    }
                    else
                    {
                        if (dbj is Line)
                        {
                            resLine.Add(dbj as Line);
                        }
                        else if(dbj is Polyline)
                        {
                            resLine.AddRange((dbj as Polyline).ToLines());
                        }
                    }
                }
                return resLine;
            }

        }

        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }
    }
}
