using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using ThMEPWSS.ViewModel;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.Sprinkler.Analysis;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;

namespace ThMEPWSS.Command
{
    public class ThSprinklerCheckCmd : IAcadCommand, IDisposable
    {
        public static ThSprinklerCheckerVM SprinklerCheckerVM { get; set; }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var polylines = ThSprinklerLayoutAreaUtils.GetFrames();
                if (polylines.Count <= 0)
                {
                    return;
                }

                var factory = new ThSprinklerDataSetFactory();
                var geometries = factory.Create(currentDb.Database, new Point3dCollection()).Container;

                //var engine = new ThTCHSprinklerRecognitionEngine();
                //engine.RecognizeMS(currentDb.Database, frame.Vertices());

                var recognizeAllEngine = new ThTCHSprinklerRecognitionEngine();
                recognizeAllEngine.RecognizeMS(currentDb.Database);

                var range = SprinklerRange();
                var category = SprinklerType();
                var data = DangerGradeDataManager.Query(SprinklerCheckerVM.Parameter.DangerGrade, range);
                var checkBoxs = new List<bool>
                {
                    SprinklerCheckerVM.Parameter.CheckItem1,
                    SprinklerCheckerVM.Parameter.CheckItem2,
                    SprinklerCheckerVM.Parameter.CheckItem3,
                    SprinklerCheckerVM.Parameter.CheckItem6,
                    SprinklerCheckerVM.Parameter.CheckItem7,
                    SprinklerCheckerVM.Parameter.CheckItem8,
                    SprinklerCheckerVM.Parameter.CheckItem9,
                };

                var checkers = new List<ThSprinklerChecker>
                    {
                        new ThSprinklerBlindZoneChecker(),
                        new ThSprinklerDistanceFromBoundarySoFarChecker(),
                        new ThSprinklerRoomChecker(),
                        new ThSprinklerDistanceBetweenSprinklerChecker(),
                        new ThSprinklerDistanceFromBoundarySoCloseChecker(),
                        new ThSprinklerDistanceFromBeamChecker{RoomFrames = polylines },
                        new ThSprinklerBeamChecker()
                    };
                checkers.ForEach(o =>
                {
                    o.Category = category;
                    o.BeamHeight = SprinklerCheckerVM.Parameter.AboveBeamHeight;
                    o.RadiusA = data.A;
                    o.RadiusB = data.B;
                });

                polylines.ForEach(p =>
                {
                    for (int i = 0; i < checkers.Count; i++)
                    {
                        if (checkBoxs[i])
                        {
                            checkers[i].Check(recognizeAllEngine.Elements, geometries, p);
                        }
                    }
                });
            }
        }

        private string SprinklerRange()
        {
            if (SprinklerCheckerVM.Parameter.SprinklerRange == Sprinkler.Model.SprinklerRange.Standard)
            {
                return "标准覆盖";
            }
            else
            {
                return "扩大覆盖";
            }
        }

        private string SprinklerType()
        {
            if (SprinklerCheckerVM.Parameter.CheckSprinklerType == Sprinkler.Model.SprinklerType.Up)
            {
                return "上喷";
            }
            else if (SprinklerCheckerVM.Parameter.CheckSprinklerType == Sprinkler.Model.SprinklerType.Down)
            {
                return "下喷";
            }
            else
            {
                return "侧喷";
            }
        }
    }
}
