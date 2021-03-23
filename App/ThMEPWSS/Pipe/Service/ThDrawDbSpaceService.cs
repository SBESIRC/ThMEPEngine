using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPWSS.Pipe.Service
{
    public  class ThDrawDbSpaceService
    {
        public void Draw()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var points = SelectPoints();
                adb.ModelSpace.Add(GetDrawing(points));
            }
        }
        private static Point3dCollection SelectPoints()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var points = new Point3dCollection();
                PromptPointResult psr;
                do
                {
                    PromptPointOptions pso = new PromptPointOptions("请您依次选择顶点\n");
                    psr = Active.Editor.GetPoint(pso);
                    if (psr.Status == PromptStatus.OK)
                    {
                        points.Add(psr.Value);
                    }
                    else
                    {
                        break;
                    }
                } while (psr.Status == PromptStatus.OK);
                return points;
            }
        }
        private static Polyline GetDrawing(Point3dCollection points)
        {
            Polyline ent_line1 = new Polyline();
            for(int i=0;i< points.Count;i++)
            {
                ent_line1.AddVertexAt(i, points[i].ToPoint2d(), 0, 20, 20);
            }      
            ent_line1.LayerId = ThExtractDbSpaceService.CreateLayer("AI-空间框线", 30, LineWeight.LineWeight020);
            ent_line1.Closed = true;
            return ent_line1;
        }
    }
}
