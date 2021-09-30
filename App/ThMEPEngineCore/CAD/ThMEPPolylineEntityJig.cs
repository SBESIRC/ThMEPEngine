using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public class ThMEPPolylineEntityJig : EntityJig
    {
        Polyline pline;
        Point3d dragPt;
        Plane plane;
        private string mTip = "\n选择下一个点";
        private short mColorIndex;
        public bool ClosedTipSwitch { get; set; }

        public ThMEPPolylineEntityJig(Polyline pline, string tip, short colorIndex) : base(pline)
        {
            mTip = tip;
            this.pline = pline;
            mColorIndex = colorIndex;
            ClosedTipSwitch = true;
            plane = new Plane(Point3d.Origin, pline.Normal);
        }
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions(mTip);
            options.BasePoint = pline.GetPoint3dAt(pline.NumberOfVertices - 2);
            options.UseBasePoint = true;
            if (3 < pline.NumberOfVertices && ClosedTipSwitch)
            {
                options.Keywords.Add("c");
                options.AppendKeywordsToMessage = true;
            }
            options.UserInputControls =
                UserInputControls.Accept3dCoordinates |
                UserInputControls.GovernedByOrthoMode |
                UserInputControls.GovernedByUCSDetect |
                UserInputControls.NullResponseAccepted;
            var result = prompts.AcquirePoint(options);
            if (result.Value.IsEqualTo(dragPt))
                return SamplerStatus.NoChange;
            dragPt = result.Value;
            return SamplerStatus.OK;
        }
        protected override bool Update()
        {
            pline.SetPointAt(pline.NumberOfVertices - 1, dragPt.Convert2d(plane));
            pline.ColorIndex = mColorIndex;
            return true;
        }

        /// <summary>
        /// Draw Polyline jigger
        /// </summary>
        /// <param name="colorIndex"></param>
        /// <param name="tip"></param>
        /// <param name="closedTipSwitch">是否显示闭合[c]关键字</param>
        /// <returns></returns>
        public static Polyline PolylineJig(short colorIndex, string tip, bool closedTipSwitch = true)
        {
            using (var acdb = AcadDatabase.Active())
            {
                var ed = Active.Editor;
                var ppr = Active.Editor.GetPoint("\n选择第一个点");
                var pline = new Polyline();

                pline.AddVertexAt(0, ppr.Value.Convert2d(new Plane()), 0.0, 0.0, 0.0);
                pline.AddVertexAt(1, Point2d.Origin, 0.0, 0.0, 0.0);
                pline.TransformBy(ed.CurrentUserCoordinateSystem);
                var jig = new ThMEPPolylineEntityJig(pline, tip, colorIndex)
                {
                    ClosedTipSwitch = closedTipSwitch,
                };

                while (true)
                {
                    var pr = ed.Drag(jig);
                    if (pr.Status == PromptStatus.Cancel)
                    {
                        pline.RemoveVertexAt(pline.NumberOfVertices - 1);
                        break;
                    }
                    else if (pr.Status == PromptStatus.OK)
                    {
                        pline.AddVertexAt(pline.NumberOfVertices, Point2d.Origin, 0.0, 0.0, 0.0);
                    }
                    else
                    {
                        pline.RemoveVertexAt(pline.NumberOfVertices - 1);
                        if (pr.Status == PromptStatus.Keyword)
                        {
                            pline.Closed = true;
                        }
                        else if (pline.NumberOfVertices < 2)
                        {
                            return new Polyline();
                        }
                        // Print
                        break;
                    }
                }
                return pline;
            }
        }
    }
}
