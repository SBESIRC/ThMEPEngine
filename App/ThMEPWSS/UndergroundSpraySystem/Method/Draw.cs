using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class Draw
    {
        public static void Rect(Polyline rect, string layer)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn(layer);
                rect.LayerId = DbHelper.GetLayerId(layer);
                acadDatabase.CurrentSpace.Add(rect);
            }
#endif
        }
        public static void MainLoop(AcadDatabase acadDatabase, List<List<Point3dEx>> mainPathList)
        {
#if DEBUG
            var layerNames = "主环";
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            foreach (var path in mainPathList)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var pt1 = path[i]._pt;
                    var pt2 = path[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }
        public static void MainLoop(AcadDatabase acadDatabase, List<Point3dEx> path, string layerNames)
        {
#if DEBUG
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var pt1 = path[i]._pt;
                    var pt2 = path[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }
        public static void MainLoops(AcadDatabase acadDatabase, List<List<Point3dEx>> mainPathList)
        {
#if DEBUG
            var index = 0;
            foreach (var path in mainPathList)
            {
                index++;
                var layerNames = "主环" + Convert.ToString(index);
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var pt1 = path[i]._pt;
                    var pt2 = path[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }
        public static void MainLoopsInOtherFloor(AcadDatabase acadDatabase, List<List<Point3dEx>> mainPathList)
        {
#if DEBUG
            var index = 0;
            foreach (var path in mainPathList)
            {
                index++;
                var layerNames = "其它层主环" + Convert.ToString(index);
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var pt1 = path[i]._pt;
                    var pt2 = path[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }

        public static void MainLoopsInOtherFloor(List<Point3dEx> path)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var index = 0;
                {
                    var layerNames = "其它层主环" + Convert.ToString(index);
                    if (!acadDatabase.Layers.Contains(layerNames))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                    }
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var pt1 = path[i]._pt;
                        var pt2 = path[i + 1]._pt;
                        var line = new Line(pt1, pt2);
                        line.LayerId = DbHelper.GetLayerId(layerNames);
                        acadDatabase.CurrentSpace.Add(line);
                    }
                }
            }

#endif
        }


        public static void SubLoop(AcadDatabase acadDatabase, SpraySystem spraySystem)
        {
#if DEBUG
            var layerNames = "次环";
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            foreach (var loop in spraySystem.SubLoops)
            {
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    var pt1 = loop[i]._pt;
                    var pt2 = loop[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    line.ColorIndex = 255;
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }
        public static void BranchLoop(AcadDatabase acadDatabase, SpraySystem spraySystem)
        {
#if DEBUG
            var layerNames = "支环";
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            foreach (var loop in spraySystem.BranchLoops)
            {
                if (loop.Count < 40)
                {
                    continue;
                }
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    var pt1 = loop[i]._pt;
                    var pt2 = loop[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }

        public static void BranchLoop(AcadDatabase acadDatabase, SpraySystem spraySystem, string layerNames)
        {
#if DEBUG
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            foreach (var loop in spraySystem.BranchLoops)
            {
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    var pt1 = loop[i]._pt;
                    var pt2 = loop[i + 1]._pt;
                    var line = new Line(pt1, pt2);
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }

        public static void ThroughPt(AcadDatabase acadDatabase, Point3dEx pt)
        {
#if DEBUG
            var layerNames = "穿越点";
            if (!acadDatabase.Layers.Contains(layerNames))
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
            }
            var circle = new Circle(pt._pt, new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1), 500);
            circle.LayerId = DbHelper.GetLayerId(layerNames);
            acadDatabase.CurrentSpace.Add(circle);
#endif
        }

        public static void RemovedVerticalPt(Point3dEx pt)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerNames = "删除掉的立管";
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                var circle = new Circle(pt._pt, new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1), 500);
                circle.LayerId = DbHelper.GetLayerId(layerNames);
                acadDatabase.CurrentSpace.Add(circle);
            }
#endif
        }


        public static void Verticals(Dictionary<Point3dEx,double> pts)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerNames = "立管test";
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                foreach (var pair in pts)
                {
                    var circle = new Circle(pair.Key._pt, new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1), pair.Value);
                    circle.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(circle);
                }
            }
#endif
        }

        public static void LeadLines(List<Line> lines)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerNames = "标注引线";
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                foreach (var line in lines)
                {
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#endif
        }

        public static void throughPtsInOtherFloor(List<List<Point3dEx>> ptsls)
        {
#if DEBUG
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerNames = "其它层的立管";
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                foreach (var pts in ptsls)
                {
                    foreach (var pt in pts)
                    {
                        var circle = new Circle(pt._pt, new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1), 500);
                        circle.LayerId = DbHelper.GetLayerId(layerNames);
                        acadDatabase.CurrentSpace.Add(circle);
                    }
                }
            }
#endif
        }

    }
}
