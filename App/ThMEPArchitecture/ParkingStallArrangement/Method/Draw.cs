using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPEngineCore;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class Draw
    {
        public static void DrawSeg(Chromosome chromosome, string layerNames = "最终分割线")
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                for (int i = 0; i < chromosome.Genome.Count; i++)
                {

                    var gene = chromosome.Genome[i];
                    var line = new Line();
                    if (gene.VerticalDirection)
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
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }

        public static void DrawSeg(List<Line> lines, int index)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var layerNames = "t" + Convert.ToString(index);
                    if (!acadDatabase.Layers.Contains(layerNames))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 0);
                    }
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }


        public static void DrawSeg(List<Line> lines, string layerNames )
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (!acadDatabase.Layers.Contains(layerNames))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 0);
                    }
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }
    }
}
