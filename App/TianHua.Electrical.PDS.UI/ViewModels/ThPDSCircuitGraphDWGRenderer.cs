using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Services;
using static TianHua.Electrical.PDS.UI.Models.DrawUtils;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class DrawingData
    {
        public LeftItem LeftItem;
        public List<RightItem> RightItems;
    }
    public class LeftItem
    {
        //1路进线
        //2路进线ATSE
        //3路进线
        //集中电源
        public string Type;
    }
    public class RightItem
    {
        //常规
        //漏电
        //接触器控制
        //热继电器保护
        //配电计量（上海CT）
        //配电计量（上海直接表）
        //配电计量（CT表在前）
        //配电计量（直接表在前）
        //配电计量（CT表在后）
        //配电计量（直接表在后）
        //电动机（分立元件）
        //电动机（CPS）
        //电动机（分立元件星三角启动）
        //电动机（CPS星三角启动）
        //双速电动机（分立元件 Δ/YY）
        //双速电动机（分立元件 Y/Y）
        //双速电动机（CPS Δ/YY）
        //双速电动机（CPS Y/Y）
        //分支小母排
        //小母排分支
        //消防应急照明回路（WFEL）
        //控制（从属接触器）
        //控制（从属CPS）
        //SPD
        public string Type;
        public string CircuitNumber = "回路編號";
        public string Power = "功率(低)";
        public string PhaseSequence = "相序";
        public string LoadNumber = "負載編號";
        public string FunctionalUse = "功能用途";
        public List<RightItem> Items;
    }
    public class ThPDSCircuitGraphDWGRenderer : ThPDSCircuitGraphRenderer
    {
        public ThPDSCircuitGraphComponentGenerator ComponentGenerator { get; set; }
        public Database DWG { get; set; }
        public override void Render(
            BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, ThPDSCircuitGraphRenderContext context)
        {
            {
                var info = ComponentGenerator.IN(null);
            }
            var leftnames = new List<string> { "1路进线", "2路进线ATSE", "3路进线", "集中电源", "设备自带控制箱" };
            var rightnames = new List<string> { "常规", "漏电", "接触器控制", "热继电器保护", "配电计量（上海CT）", "配电计量（上海直接表）", "配电计量（CT表在前）", "配电计量（直接表在前）", "配电计量（CT表在后）", "配电计量（直接表在后）", "电动机（分立元件）", "电动机（CPS）", "电动机（分立元件星三角启动）", "电动机（CPS星三角启动）", "双速电动机（分立元件 D-YY）", "双速电动机（分立元件 Y-Y）", "双速电动机（CPS D-YY）", "双速电动机（CPS Y-Y）", "分支小母排", "小母排分支", "消防应急照明回路（WFEL）", "控制（从属接触器）", "控制（从属CPS）", "SPD附件" };
            //var leftname = "1路进线";
            var leftname = (node.Details?.CircuitFormType ?? default).ToString();
            var _rightnames = graph.Vertices.Select(x => (x.Details?.CircuitFormType ?? default).ToString()).ToList();
            using var lck = DocLock;
            using var adb = AcadDatabase.Active();
            using var tr = new _DrawingTransaction(adb);
            var ok = TrySelectPoint(out var basePoint);
            if (!ok) return;
            var data = new DrawingData
            {
                LeftItem = new LeftItem() { Type = leftname, },
            };
            var rits = data.RightItems = new List<RightItem>();
            foreach (var name in _rightnames)
            {
                rits.Add(new RightItem() { Type = name, });
            }
            Point3d basePt;
            Point3d firstBusPt = default;
            Point3d lastBusPt = default;
            {
                var info = ComponentGenerator.IN(data.LeftItem.Type);
                if (info is null) return;
                drawInfo(info, basePoint);
                basePt = basePoint.OffsetXY(info.Boundary.Width, 0);
                firstBusPt = basePt;
                lastBusPt = basePt;
            }
            {
                foreach (var name in rits.Select(x => x.Type))
                {
                    var info = ComponentGenerator.OUT(name);
                    if (info is null) return;
                    drawInfo(info, basePt);
                    basePt = basePt.OffsetXY(0, -info.Boundary.Height);
                    lastBusPt = basePt;
                }
            }
            {
                DrawLineSegmentLazy(new GLineSegment(firstBusPt.OffsetY(30), lastBusPt.OffsetY(-30)), 5);
            }
            void drawInfo(PDSBlockInfo info, Point3d basePt)
            {
                var v3 = basePt - info.BasePoint;
                var v2 = v3.ToVector2d();
                foreach (var seg in info.Lines)
                {
                    DrawLineSegmentLazy(seg.Offset(v2));
                }
                foreach (var c in info.Circles)
                {
                    DrawCircleLazy(c.Center, c.Radius);
                }
                foreach (var a in info.Arcs)
                {
                    var arc = GetArc(a);
                    DrawEntityLazy(arc);
                }
                foreach (var ct in info.CTexts)
                {
                    DrawTextLazy(ct.Text, 12, ct.Boundary.LeftButtom + v2);
                }
            }
            static bool TrySelectPoint(out Point3d basePt)
            {
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK)
                {
                    basePt = default;
                    return false;
                }
                basePt = rst.Value.TransformBy(Active.Editor.UCS2WCS());
                return true;
            }
            static Arc GetArc(GArc a)
            {
                return new Arc(a.Center.ToPoint3d(), a.IsClockWise ? new Vector3d(0, 0, -1) : new Vector3d(0, 0, 1), a.Radius, a.StartAngle, a.EndAngle);
            }
            FlushDQ(adb);
        }
    }
}
