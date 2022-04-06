using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanLayout.Engine;
using ThMEPHVAC.FanLayout.Model;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.FanLayout.Command
{
    class ThFanHoleExtractCmd : ThMEPBaseCommand, IDisposable
    {
        static public string StrMapScale = "1:100";

        public ThFanHoleExtractCmd()
        {
            CommandName = "THFGLD";
            ActionName = "风机留洞";
        }

        public void Dispose()
        {
        }
        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("AI-洞口"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-洞口"));
                }
                if (blockDb.Layers.Contains("H-HOLE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-HOLE"));
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("H-HOLE");
            }
        }
        public bool GetTuplePoints(out Tuple<Point3d, Point3d> pts, string tips1, string tips2)
        {
            var ppo = new PromptPointOptions(tips1);
            ppo.Keywords.Add("S","S", "设置(S)");
            ppo.AppendKeywordsToMessage = true;

            var point1 = Active.Editor.GetPoint(ppo);
            if(point1.Status == PromptStatus.Keyword)
            {
                if (point1.StringResult == "S")
                {
                    //输入出图比例
                    var options = new PromptKeywordOptions("\n选择处理方式");
                    options.Keywords.Add("1:50", "A", "1:50(A)");
                    options.Keywords.Add("1:100", "B", "1:100(B)");
                    options.Keywords.Add("1:150", "C", "1:150(C)");
                    options.Keywords.Add("1:200", "D", "1:200(D)");
                    options.Keywords.Default = StrMapScale;
                    var result = Active.Editor.GetKeywords(options);
                    if (result.Status == PromptStatus.OK)
                    {
                        StrMapScale = result.StringResult;
                    }
                }
                point1 = Active.Editor.GetPoint(tips1);

            }
            if(point1.Status!= PromptStatus.OK)
            {
                pts = Tuple.Create(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                return false;
            }

            var ppo1 = new PromptPointOptions(tips2);
            ppo1.UseBasePoint = true;
            ppo1.BasePoint = point1.Value;

            var point2 = Active.Editor.GetPoint(ppo1);
            if (point2.Status != PromptStatus.OK)
            {
                pts = Tuple.Create(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                return false;
            }

            pts = Tuple.Create(point1.Value.TransformBy(Active.Editor.UCS2WCS()), point2.Value.TransformBy(Active.Editor.UCS2WCS()));
            return true;
        }
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                ImportBlockFile();
                Tuple<Point3d, Point3d> tuplePts1;
                if (!GetTuplePoints(out tuplePts1, "\n请选择洞口插入的基点位置", "\n请选择洞口插入的第二点（方向）："))
                {
                    return;
                }

                double fontHeight = ThFanLayoutDealService.GetFontHeight(0, StrMapScale);
                var point1 = tuplePts1.Item1;
                var point2 = tuplePts1.Item2;

                Vector3d basVector = new Vector3d(1, 0, 0);
                Vector3d refVector = new Vector3d(0, 0, 1);
                Vector3d vector = point2.GetVectorTo(point1).GetNormal();
                double holeAngle = basVector.GetAngleTo(vector, refVector) - Math.PI / 2.0;
                //通过，point1和point2构造一条直线，然后进行buffer，得到一个很小的范围，再在这个范围没，提取风管
                var tmpLine = new Line(point1, point2);
                if(tmpLine.Length < 1.0)
                {
                    Active.Editor.WriteMessage("请选择风管中心线上位置不同的两个点\n");
                    return;
                }
                var tmpAre = tmpLine.Buffer(10);
                //提取到风管，然后进行数据提取
                ThDuctInfo info;
                var ductEngine = new ThFanDuctRecognitionEngine();
                if (ductEngine.GetDuctInfo(tmpAre.Vertices(),out info))
                {
                    info.fontHeight = fontHeight;
                    //插入风管
                    string strSize = ThFanLayoutDealService.GetFanHoleSize(info.width, info.height, 100);
                    string strMark = ThFanLayoutDealService.GetFanHoleMark(1, info.markHeight - 0.05);
                    InsertFanHole(database, point1, holeAngle, info.fontHeight, info.width + 100, strSize, strMark);
                }
            }
        }

        private void InsertFanHole(AcadDatabase acadDatabase, Point3d pt, double angle, double fontHeight, double width, string strSize, string mark)
        {
            var fanHole = new ThFanHoleModel();
            fanHole.FanHolePosition = pt;
            fanHole.FontHeight = fontHeight;
            fanHole.FanHoleWidth = width;
            fanHole.FanHoleAngle = angle;
            fanHole.FanHoleSize = strSize;
            fanHole.FanHoleMark = mark;
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertFanHole(acadDatabase, fanHole);
        }
    }
}
