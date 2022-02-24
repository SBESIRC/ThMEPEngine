using System;
using System.Collections.Generic;
using System.Linq;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Engine;
using ThMEPWSS.Sprinkler.Analysis;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.Command
{
    public class ThSprinklerCheckCmd : ThMEPBaseCommand, IDisposable
    {
        public static ThSprinklerCheckerVM SprinklerCheckerVM { get; set; }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var range = SprinklerRange();
                var category = SprinklerType();
                var data = DangerGradeDataManager.Query(SprinklerCheckerVM.Parameter.DangerGrade, range);
                var checkBoxs = new List<bool>
                {
                    SprinklerCheckerVM.Parameter.CheckItem1,
                    SprinklerCheckerVM.Parameter.CheckItem2,
                    SprinklerCheckerVM.Parameter.CheckItem3,
                    SprinklerCheckerVM.Parameter.CheckItem4,
                    SprinklerCheckerVM.Parameter.CheckItem5,
                    SprinklerCheckerVM.Parameter.CheckItem6,
                    SprinklerCheckerVM.Parameter.CheckItem7,
                    SprinklerCheckerVM.Parameter.CheckItem8,
                    SprinklerCheckerVM.Parameter.CheckItem9,
                    SprinklerCheckerVM.Parameter.CheckItem10,
                    SprinklerCheckerVM.Parameter.CheckItem11,
                    SprinklerCheckerVM.Parameter.CheckItem12,
                };

                var remainder = new DBObjectCollection();
                var polylines = ThSprinklerLayoutAreaUtils.GetFrames(out remainder).Where(p => p.Area > 0.8e6).ToList();
                if (polylines.Count == 0 && remainder.Count == 0)
                {
                    return;
                }

                var checkers = new List<ThSprinklerChecker>
                    {
                        new ThSprinklerBlindZoneChecker(),
                        new ThSprinklerDistanceFromBoundarySoFarChecker(),
                        new ThSprinklerRoomChecker(),
                        new ThSprinklerParkingStallChecker{ BlockNameDict = SprinklerCheckerVM.Parameter.BlockNameDict},
                        new ThSprinklerMechanicalParkingStallChecker{ BlockNameDict = SprinklerCheckerVM.Parameter.BlockNameDict},
                        new ThSprinklerDistanceBetweenSprinklerChecker(),
                        new ThSprinklerDistanceFromBoundarySoCloseChecker(),
                        new ThSprinklerDistanceFromBeamChecker{RoomFrames = polylines },
                        new ThSprinklerBeamChecker(),
                        new ThSprinklerPipeChecker(),
                        new ThSprinklerDuctChecker(),
                        new ThSprinklerDensityChecker{AreaDensity = SprinklerCheckerVM.Parameter.AreaDensity },
                    };
                checkers.ForEach(o =>
                {
                    o.Category = category;
                    o.BeamHeight = SprinklerCheckerVM.Parameter.AboveBeamHeight;
                    o.RadiusA = data.A;
                    o.RadiusB = data.B;
                });

                //提取数据
                var factory = new ThSprinklerDataSetFactory();
                var geometries = factory.Create(currentDb.Database, new Point3dCollection()).Container;
                var recognizeAllEngine = new ThTCHSprinklerRecognitionEngine();
                recognizeAllEngine.RecognizeMS(currentDb.Database, new Point3dCollection());

                var index = new List<int> { 3, 4, 5, 8, 9, 10, 11 };
                var frame = new Extents3d();
                polylines.ForEach(p => frame.AddExtents(p.GeometricExtents));
                frame.AddExtents(remainder.GeometricExtents());
                for (int i = 0; i < checkers.Count; i++)
                {
                    if (checkBoxs[i])
                    {
                        checkers[i].Extract(currentDb.Database, frame.ToRectangle());
                        checkers[i].Clean(frame.ToRectangle());
                        polylines.ForEach(p =>
                        {
                            checkers[i].Check(recognizeAllEngine.Elements, geometries, p);
                        });

                        if (index.Contains(i))
                        {
                            remainder.OfType<Entity>().ToList().ForEach(p =>
                            {
                                checkers[i].Check(recognizeAllEngine.Elements, geometries, p);
                            });
                        }
                    }
                }
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
