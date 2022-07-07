using System;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.FanLayout.Model;
using ThMEPHVAC.FanLayout.Engine;
using ThMEPHVAC.FanLayout.Service;

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
            using (var acadDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
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
            using (var acadDb = AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("H-HOLE");
            }
        }
        public bool GetTuplePoints(out Tuple<Point3d, Point3d> pts, string tips1, string tips2)
        {
            var ppo = new PromptPointOptions(tips1);
            ppo.Keywords.Add("S", "S", "设置(S)");
            ppo.AppendKeywordsToMessage = true;

            var point1 = Active.Editor.GetPoint(ppo);
            if (point1.Status == PromptStatus.Keyword)
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
            if (point1.Status != PromptStatus.OK)
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

            pts = Tuple.Create(point1.Value, point2.Value);
            return true;
        }

        private bool GetDuct(Tuple<Point3d, Point3d> tuplePts, out ThDuctInfo info)
        {
            var line = new Line(tuplePts.Item1, tuplePts.Item2);
            var area = line.Buffer(10);
            area.TransformBy(Active.Editor.UCS2WCS());
            var engine = new ThFanDuctRecognitionEngine();
            return engine.GetDuctInfo(area.Vertices(), out info);
        }

        private double GetHoleAngle(Vector3d dir)
        {
            return Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis) + Math.PI / 2.0;
        }

        private void OverrideFontHeight(ThDuctInfo info)
        {
            info.fontHeight = ThFanLayoutDealService.GetFontHeight(0, StrMapScale);
        }

        public override void SubExecute()
        {
            using (var doclock = Active.Document.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                ImportBlockFile();
                Tuple<Point3d, Point3d> tuplePts1;
                if (!GetTuplePoints(out tuplePts1, "请选择洞口插入的基点位置\n", "请选择洞口插入的第二点（方向）\n"))
                {
                    return;
                }
                var dir = tuplePts1.Item2 - tuplePts1.Item1;
                if (dir.Length < 1.0)
                {
                    Active.Editor.WriteMessage("请选择风管中心线上位置不同的两个点\n");
                    return;
                }

                if (GetDuct(tuplePts1, out ThDuctInfo info))
                {
                    OverrideFontHeight(info);

                    // 插入洞口到指定位置（UCS)
                    var location = tuplePts1.Item1;
                    string strSize = ThFanLayoutDealService.GetFanHoleSize(info.width, info.height, 100);
                    string strMark = ThFanLayoutDealService.GetFanHoleMark(1, info.markHeight - 0.05);
                    var holeObjId = InsertFanHole(database, location, GetHoleAngle(dir), info.fontHeight, info.width + 100, strSize, strMark);

                    // 转换到WCS
                    var hole = database.ElementOrDefault<BlockReference>(holeObjId, true);
                    if (hole != null)
                    {
                        hole.TransformBy(Active.Editor.UCS2WCS());
                    }
                }
            }
        }

        private ObjectId InsertFanHole(AcadDatabase acadDatabase, Point3d pt, double angle, double fontHeight, double width, string strSize, string mark)
        {
            var fanHole = new ThFanHoleModel();
            fanHole.FanHolePosition = pt;
            fanHole.FontHeight = fontHeight;
            fanHole.FanHoleWidth = width;
            fanHole.FanHoleAngle = angle;
            fanHole.FanHoleSize = strSize;
            fanHole.FanHoleMark = mark;
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            return toDbServiece.InsertFanHole(acadDatabase, fanHole);
        }
    }
}
