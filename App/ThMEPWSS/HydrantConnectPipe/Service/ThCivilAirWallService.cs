using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThCivilAirWallService
    {
        public List<ThCivilAirWall> GetCivilAirWall(Point3dCollection selectArea)
        {
            List<ThCivilAirWall> civilAirWalls = new List<ThCivilAirWall>();
            civilAirWalls.AddRange(GetCivilAirWallFromLayer(selectArea));
            return civilAirWalls;
        }
        public List<ThCivilAirWall> GetCivilAirWallFromLayer(Point3dCollection selectArea)//从层里面提取人防墙
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var hydrantPipe = acadDb.ModelSpace.OfType<Curve>().Where(o => o.Layer == "W-人防墙-AI").ToList();
                var rst = new List<Line>();
                hydrantPipe.ForEach(o =>
                {
                    if (o is Polyline polyline)
                    {
                        rst.AddRange(polyline.ToLines());
                    }
                    else if (o is Line line)
                    {
                        if (line.Length > 0)
                        {
                            rst.Add(line);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                });

                var civilAirWalls = new List<ThCivilAirWall>();
                foreach(var line in rst)
                {
                    civilAirWalls.Add(ThCivilAirWall.Create(line));
                }
                return civilAirWalls;
            }
        }
    }
}
