using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class Draw
    {
        public static void DrawSeg(Chromosome chromosome, string layer = "0")
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                for (int i = 0; i < chromosome.Genome.Count; i++)
                {

                    var gene = chromosome.Genome[i];
                    var line = new Line();
                    if (gene.Direction)
                    {
                        var spt = new Point3d(gene.Value, gene.StartValue, 0);
                        var ept = new Point3d(gene.Value, gene.EndValue, 0);
                        line = new Line(spt, ept);
                    }
                    else
                    {
                        var spt = new Point3d(gene.StartValue, gene.Value, 0);
                        var ept = new Point3d(gene.EndValue, gene.Value, 0);
                        line = new Line(spt, ept);
                    }
                    line.LayerId = DbHelper.GetLayerId(layer);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
                
        }
    }
}
