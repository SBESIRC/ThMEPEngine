using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThCADExtension;
using ThMEPElectrical.AFASRegion.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.AFASRegion
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

                        var cmd = new AFASRegion();
                        cmd.BufferDistance = 500;
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
                        var Detectionspace = cmd.DivideRoomWithDetectionRegion(pline, AFASDetector.SmokeDetectorLow);
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
