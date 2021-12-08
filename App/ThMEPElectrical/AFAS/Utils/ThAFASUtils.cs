using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmFixLayout.Data;

namespace ThMEPElectrical.AFAS.Utils
{
    public static class ThAFASUtils
    {
        public static void MoveToOrigin(this ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3RailingVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        public static Point3dCollection GetFrame()
        {
            Point3dCollection pts = new Point3dCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });

                if (frame.Area > 1e-4)
                {
                    pts = frame.Vertices();
                }

                return pts;
            }
        }

        public static Point3dCollection GetFrameBlk()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                Point3dCollection pts = new Point3dCollection();

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return pts;
                }


                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as BlockReference);
                }

                List<Polyline> frames = new List<Polyline>();
                foreach (var frameBlock in frameLst)
                {
                    var frame = GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                var frameL = frames.OrderByDescending(x => x.Area).FirstOrDefault();

                if (frameL != null && frameL.Area > 10)
                {
                    frameL = ProcessFrame(frameL);
                }
                if (frameL != null && frameL.Area > 10)
                {
                    pts = frameL.Vertices();
                }

                return pts;
            }
        }

        private static List<Entity> GetBlockInfo(BlockReference blockReference)
        {
            var matrix = blockReference.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var results = new List<Entity>();
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId).Clone() as Entity;
                    dbObj.TransformBy(matrix);
                    results.Add(dbObj);
                }
                return results;
            }
        }

        public static List<ThGeometry> GetSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, double wallThick, bool needDetective)
        {

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var datasetFactory = new ThAFASAreaDataSetFactory()
                {
                    ReferBeam = referBeam,
                    WallThick = wallThick,
                    NeedDetective = needDetective,
                    BlkNameList = extractBlkList,
                };
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(dataset.Container);

            }

            return geos;
        }

        public static List<ThGeometry> GetFixLayoutData(Point3dCollection pts, List<string> extractBlkList)
        {
            var geos = new List<ThGeometry>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var datasetFactory = new ThAFASFixLayoutDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                };
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(dataset.Container);

                return geos;
            }
        }

        public static List<ThGeometry> GetDistLayoutData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, bool needConverage)
        {
            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var datasetFactory = new ThAFASDistanceDataSetFactory()
                {
                    ReferBeam = referBeam,
                    NeedConverage = needConverage,
                    BlkNameList = extractBlkList,
                };

                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(dataset.Container);

                return geos;
            }
        }
        /// <summary>
        /// 将数据转回原点。同时返回transformer
        /// </summary>
        /// <param name="geos"></param>
        /// <returns></returns>
        public static ThMEPOriginTransformer TransformToOrig(Point3dCollection pts, List<ThGeometry> geos)
        {
            ThMEPOriginTransformer transformer = null;

            if (pts.Count > 0)
            {
                var center = pts.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }

            foreach (var o in geos)
            {
                if (o.Boundary != null)
                {
                    transformer.Transform(o.Boundary);
                }
            }

            geos.ProjectOntoXYPlane();

            return transformer;
        }


        /// <summary>
        /// 计算blk外扩距离
        /// </summary>
        /// <param name="blkNameList"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static double GetPriorityExtendValue(List<string> blkNameList, double scale)
        {
            double extend = -1;
            var size = new List<double>();
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item1));
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item2));

            extend = size.OrderByDescending(x => x).First();
            extend = extend * scale / 2;
            return extend;
        }


        #region DebugFunction
        ///// <summary>
        ///// for debug
        ///// </summary>
        ///// <param name="pts"></param>
        ///// <param name="extractBlkList"></param>
        ///// <returns></returns>
        //public static List<ThGeometry> WriteSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, double wallThick,bool needDetective)
        //{
        //    var fileInfo = new FileInfo(Active.Document.Name);
        //    var path = fileInfo.Directory.FullName;

        //    var geos = new List<ThGeometry>();
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {

        //        var datasetFactory = new ThFaAreaLayoutDataSetFactory()
        //        {
        //            ReferBeam = referBeam,
        //            WallThick = wallThick,
        //            NeedDetective = needDetective,
        //        }; 
        //        var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(dataset.Container);

        //        ThGeoOutput.Output(geos, path, fileInfo.Name);

        //    }

        //    return geos;
        //}

        /// <summary>
        /// for debug
        /// </summary>
        public static Polyline SelectFrame()
        {
            var frame = new Polyline();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return frame;
                }

                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = ProcessFrame(frameTemp);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }
                frame = frameList.OrderByDescending(x => x.Area).First();

                return frame;
            }
        }

        private static Polyline ProcessFrame(Polyline frame)
        {
            Polyline nFrame = null;
            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }
            return nFrame;
        }

        #endregion
    }
}
