using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPTCH.Model;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Service;
using ThMEPTCH.TCHDrawServices;

namespace ThMEPWSS
{
    public class ThPipeCmds
    {
        /// <summary>
        /// 立管标注
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLGBZ", CommandFlags.Modal)]
        public void THLGBZ()
        {
            using (var cmd = new ThPipeCreateCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 楼层框线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLCKX", CommandFlags.Modal)]
        public void THLGLC()
        {
            using (var cmd = new ThPipeInsertFloorFrameCmd())
            {
                cmd.Execute();
            }
        }


        [CommandMethod("TIANHUACAD", "THLGYY", CommandFlags.Modal)]
        public static void THLGYY()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThApplyPipesEngine.Apply(ThTagParametersService.sourceFloor, ThTagParametersService.targetFloors);
            }
        }

        [CommandMethod("TIANHUACAD", "THTCHPIPE", CommandFlags.Modal)]
        public static void THTCHPIPE()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var service = new TCHDrawTwtPipeService();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    if (acadDatabase.Element<Line>(obj) is Line line)
                    {
                        var tchPipe = new ThTCHTwtPipe();
                        tchPipe.StartPtID = new ThTCHTwtPoint
                        {
                            Point = line.StartPoint,
                        };
                        tchPipe.EndPtID = new ThTCHTwtPoint
                        {
                            Point = line.EndPoint,
                        };
                        tchPipe.System = "消防";
                        tchPipe.Material = "镀锌钢管";
                        tchPipe.DnType = "DN";
                        tchPipe.Dn = 65.0;
                        tchPipe.Gradient = 0.0;
                        tchPipe.Weight = 3.5;
                        tchPipe.HideLevel = 0;
                        tchPipe.DocScale = 100.0;
                        tchPipe.DimID = new ThTCHTwtPipeDimStyle
                        {
                            ShowDim = true,
                            DnStyle = DnStyle.Type1,
                            GradientStyle = GradientStyle.NoDimension,
                            LengthStyle = LengthStyle.NoDimension,
                            ArrangeStyle = false,
                            DelimiterStyle = DelimiterStyle.Blank,
                            SortStyle = SortStyle.Type0,
                        };

                        var valve = new ThTCHTwtPipeValve();
                        var center = line.GetCenter();
                        valve.LocationID = new ThTCHTwtPoint
                        {
                            Point = center,
                        };
                        valve.DirectionID = new ThTCHTwtVector
                        {
                            Vector = new Vector3d(1, 0, 0),
                        };
                        valve.BlockID = new ThTCHTwtBlock
                        {
                            Type = "VALVE",
                            Number = "00000856",
                        };
                        //valve.PipeID = tchPipe;
                        valve.System = "消防";
                        valve.Length = 240.0;
                        valve.Width = 180.0;
                        valve.InterruptWidth = 200.0;
                        valve.DocScale = 100.0;

                        service.Valves.Add(valve);
                        service.Pipes.Add(tchPipe);
                    }
                }
                service.DrawExecute(true, false);
            }
        }
    }
}
