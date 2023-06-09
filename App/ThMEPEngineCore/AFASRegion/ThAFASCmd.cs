﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.AFASRegion
{
    public class ThAFASCmd
    {
        [CommandMethod("TIANHUACAD", "THAFASP", CommandFlags.Modal)]
        public void THAFASP()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //选择区域
                Active.Editor.WriteLine("\n请选择楼层块");
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new ObjectIdCollection();
                objs = result.Value.GetObjectIds().ToObjectIdCollection();

                //楼层
                var StoreysRecognitionEngine = new ThEStoreysRecognitionEngine();
                StoreysRecognitionEngine.RecognizeMS(acadDatabase.Database, objs);
                if (StoreysRecognitionEngine.Elements.Count == 0)
                {
                    return;
                }

                foreach (var s in StoreysRecognitionEngine.Elements)
                {
                    if (s is ThEStoreys sobj)
                    {
                        var blk = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        Polyline pline = GetBlockOBB(acadDatabase.Database, blk, blk.BlockTransform);

                        var pts = pline.Vertices();
                        //提取房间框线
                        var roomBuidler = new ThRoomBuilderEngine();
                        var Rooms = roomBuidler.BuildFromMS(acadDatabase.Database, pts);
                        var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acadDatabase.Database, pts);

                        //建筑墙
                        var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                        archWallEngine.Recognize(acadDatabase.Database, pts);
                        var walls = allStructure.ShearWallEngine.Elements.Union(archWallEngine.Elements).ToList();

                        var cmd = new AFASRegion();
                        cmd.Rooms = Rooms;
                        cmd.Beams = allStructure.BeamEngine.Elements;
                        cmd.Columns = allStructure.ColumnEngine.Elements;
                        cmd.Walls = walls;
                        cmd.Holes = new List<Polyline>();
                        cmd.BufferDistance = 500;
                        cmd.ReferBeams = false;

                        //获取可布置区域
                        var Arrangeablespace = cmd.DivideRoomWithPlacementRegion(pline);
                        foreach (var polygon in Arrangeablespace)
                        {
                            polygon.ColorIndex = 2;
                            acadDatabase.ModelSpace.Add(polygon);
                        }
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THAFASD", CommandFlags.Modal)]
        public void THAFASD()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //选择区域
                Active.Editor.WriteLine("\n请选择楼层块");
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new ObjectIdCollection();
                objs = result.Value.GetObjectIds().ToObjectIdCollection();

                //楼层
                var StoreysRecognitionEngine = new ThEStoreysRecognitionEngine();
                StoreysRecognitionEngine.RecognizeMS(acadDatabase.Database, objs);
                if (StoreysRecognitionEngine.Elements.Count == 0)
                {
                    return;
                }

                foreach (var s in StoreysRecognitionEngine.Elements)
                {
                    if (s is ThEStoreys sobj)
                    {
                        var blk = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        Polyline pline = GetBlockOBB(acadDatabase.Database, blk, blk.BlockTransform);

                        var cmd = new AFASRegion();
                        AFASBeamContour.WallThickness = 100;
                        //获取探测范围
                        var Detectionspace = cmd.DivideRoomWithDetectionRegion(pline);
                        foreach (var polygon in Detectionspace)
                        {
                            polygon.ColorIndex = 3;
                            acadDatabase.ModelSpace.Add(polygon);
                        }
                    }
                }
            }
        }

        private Polyline GetBlockOBB(Database database, BlockReference blockObj, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var btr = acadDatabase.Blocks.Element(blockObj.BlockTableRecord);
                var polyline = btr.GeometricExtents().ToRectangle().GetTransformedCopy(matrix) as Polyline;
                return polyline;
            }
        }
    }
}
