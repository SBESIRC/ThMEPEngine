using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Tools;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDrawDbSpaceNameService
    {
        public void Draw()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var points = SelectPoints();
                GetTags(points).ForEach(o=> adb.ModelSpace.Add(o));
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
                    PromptPointOptions pso = new PromptPointOptions("请您分别选择插入名称的位置\n");
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
        private static List<DBText> GetTags(Point3dCollection points)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var texts=new List<DBText>();
                var textStyleId = ThWPipeOutputFunction.GetStyleIds(db.Database, "TH-STYLE3");
                for (int i = 0; i < points.Count; i++)
                {
                    DBText text = new DBText()
                    {
                        TextString = "未命名",
                        LayerId = ThExtractDbSpaceService.CreateLayer("AI-空间名称", 130),
                        Position = points[i],
                        TextStyleId = textStyleId,
                        Height = 200,
                        WidthFactor = 0.7
                    };
                    texts.Add(text);
                }
                return texts;
            }
        }
    }
}
