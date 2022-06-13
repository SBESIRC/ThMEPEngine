using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    public class ThUcsAreaExtractor
    {
        public List<FrameModel> GetPickData()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frames = GetFrame(acadDatabase, out List<Curve> cutLines);                      //外包框
                var frameInfos = CalHoles(frames);                                                  //计算框线信息
                var blocks = GetPowerBlock(acadDatabase);                                           //电源
                var frameModels = SplitUcsCurve(frameInfos, cutLines);                              //分割ucs区域
                var ucsInfo = GetUcsBlock(acadDatabase);                                            //ucs块信息
                return MatchFrameAndPower(frameModels, blocks, ucsInfo);
            }
        }

        /// <summary>
        /// 匹配选中的电源和框线
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="frameInfo"></param>
        /// <returns></returns>
        private List<FrameModel> MatchFrameAndPower(List<FrameModel> frameModels, List<BlockReference> blocks, List<UcsInfo> ucsInfos) 
        {
            foreach (var model in frameModels)
            {
                var framePower = blocks.Where(x => model.OriginFrame.Contains(x.Position)).FirstOrDefault();
                if (framePower != null)
                {
                    model.Power = framePower;
                }
                if (model.UcsPolys.Count > 0)
                {
                    foreach (var ucsPoly in model.UcsPolys)
                    {
                        var frameUcs = ucsInfos.Where(x => ucsPoly.Frame.Contains(x.ucsInsertPoint)).FirstOrDefault();
                        if (frameUcs != null)
                        {
                            ucsPoly.dir = frameUcs.ucsMatrix.CoordinateSystem3d.Xaxis;
                        }
                    }
                }
                else
                {
                    var frameUcs = ucsInfos.Where(x => model.OriginFrame.Contains(x.ucsInsertPoint)).FirstOrDefault();
                    if (frameUcs != null)
                    {
                        model.dir = frameUcs.ucsMatrix.CoordinateSystem3d.Xaxis;
                    }
                }
            }
            return frameModels;
        }

        /// <summary>
        /// 获取电源
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        private List<BlockReference> GetPowerBlock(AcadDatabase acadDatabase)
        {
            //获取电源箱
            PromptSelectionOptions blockOptions = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择所有区域内的电源箱",
                RejectObjectsOnLockedLayers = true,
            };
            var blockDxfNames = new string[]
            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var blockFilter = ThSelectionFilterTool.Build(blockDxfNames);
            var blockResult = Active.Editor.GetSelection(blockOptions, blockFilter);

            var resBlocks = new List<BlockReference>();
            if (blockResult.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in blockResult.Value.GetObjectIds())
                {
                    resBlocks.Add(acadDatabase.Element<BlockReference>(obj));
                }
            }
            return resBlocks;
        }

        /// <summary>
        /// 计算ucs框线
        /// </summary>
        /// <param name="frameLst"></param>
        private List<FrameModel> SplitUcsCurve(Dictionary<Polyline, List<Polyline>> frameInfo, List<Curve> cutLines)
        {
            List<FrameModel> frameModels = new List<FrameModel>();
            foreach (var frameDic in frameInfo)
            {
                // 清洗外框线（MakeValid)
                var frame = ThMEPFrameService.Normalize(frameDic.Key);

                FrameModel frameModel = new FrameModel();
                frameModel.OriginFrame = frame;
                frameModel.Holes = frameDic.Value;
                //分割外包框线
                var needCutLines = cutLines.Where(x => x.IsIntersects(frame)).ToList();
                if (needCutLines.Count > 0)
                {
                    needCutLines.Add(frame);
                    var objs = needCutLines.ToCollection();
                    var obLst = objs.PolygonsEx();

                    foreach (var ob in obLst)
                    {
                        if (ob is Polyline resPoly)
                        {
                            var bufferCollection = resPoly.Buffer(-10);
                            if (bufferCollection.Count > 0)
                            {
                                var bufferPoly = bufferCollection[0] as Polyline;
                                UcsFrameModel ucsFrame = new UcsFrameModel() { Frame = resPoly };
                                frameModel.UcsPolys.Add(ucsFrame);
                            }
                        }
                    }
                }
                frameModels.Add(frameModel);
            }

            return frameModels;
        }

        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 延长线
        /// </summary>
        /// <param name="srcCurve"></param>
        /// <param name="entityExtendDis"></param>
        /// <returns></returns>
        private Curve ExtendCurve(Curve srcCurve, double entityExtendDis)
        {
            if (srcCurve is Polyline poly)
            {
                var ptS = poly.StartPoint;
                var ptE = poly.EndPoint;
                if (ptS.DistanceTo(ptE) < 1000)
                {
                    var clonePoly = poly.Clone() as Polyline;
                    clonePoly.Closed = true;
                    return clonePoly;
                }
                else
                {
                    var resPolyline = OptimizePolyline(poly);
                    var pts = resPolyline.Vertices();
                    var resPts = new Point3dCollection();
                    var vecFir = resPolyline.GetFirstDerivative(ptS).GetNormal();
                    var extendPtS = ptS - vecFir * entityExtendDis;

                    var vecEnd = resPolyline.GetFirstDerivative(ptE).GetNormal();
                    var extendPtE = ptE + vecEnd * entityExtendDis;
                    resPts.Add(extendPtS);
                    foreach (Point3d srcPt in pts)
                        resPts.Add(srcPt);
                    resPts.Add(extendPtE);
                    var extendPoly = new Polyline();
                    extendPoly.CreatePolyline(resPts);
                    return extendPoly;
                }
            }
            else
            {
                // 直线
                var line = srcCurve as Line;
                var ptS = line.StartPoint;
                var ptE = line.EndPoint;
                var vec = (ptE - ptS).GetNormal();
                return new Line(ptS - vec * entityExtendDis, ptE + vec * entityExtendDis);
            }
        }

        /// <summary>
        /// 优化框线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Polyline OptimizePolyline(Polyline polyline)
        {
            var pts = polyline.Vertices();
            var resPts = new Point3dCollection();
            for (int i = 0; i < pts.Count; i++)
            {
                resPts.Add(new Point3d(pts[i].X, pts[i].Y, 0));
            }

            var resPoly = new Polyline();
            resPoly.CreatePolyline(resPts);
            return resPoly;
        }

        /// <summary>
        /// 提取ucs块信息
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        private List<UcsInfo> GetUcsBlock(AcadDatabase acadDatabase)
        {
            var blockRefs = acadDatabase.ModelSpace.OfType<BlockReference>()
                    .Where(p => p.Layer.ToUpper().Contains(ThMEPEngineCoreCommon.UCS_COMPASS_LAYER_NAME))
                    .ToList();

            List<UcsInfo> UcsInfos = new List<UcsInfo>();
            foreach (var block in blockRefs)
            {
                var copyBlock = (BlockReference)block.Clone();
                var blockSystem = copyBlock.BlockTransform.CoordinateSystem3d;
                var transBlock = Matrix3d.AlignCoordinateSystem(Point3d.Origin, blockSystem.Xaxis, blockSystem.Yaxis,
                    blockSystem.Zaxis, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
                UcsInfos.Add(new UcsInfo(copyBlock.Position, transBlock, copyBlock.Rotation, blockSystem.Xaxis, copyBlock.BlockTransform));
            }

            return UcsInfos;
        }

        /// <summary>
        /// 获取外包框
        /// </summary>
        /// <param name="acadDatabase"></param>
        private List<Polyline> GetFrame(AcadDatabase acadDatabase, out List<Curve> cutLines)
        {
            var frameLst = new List<Polyline>();
            cutLines = new List<Curve>();
            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                 RXClass.GetClass(typeof(Polyline)).DxfName,
                 RXClass.GetClass(typeof(Line)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Curve>(obj);
                    if (frame is Polyline)
                    {
                        var extendCurve = ExtendCurve(frame, 1000) as Polyline;
                        if (frame.Closed && frame.EndPoint.DistanceTo(frame.StartPoint) < 1)
                        {
                            frameLst.Add(extendCurve);
                        }
                        else
                        {
                            cutLines.Add(extendCurve);
                        }
                    }
                    else
                    {
                        cutLines.Add(ExtendCurve(frame, 1000));
                    }
                    var objs = frameLst.ToCollection();
                    frameLst = ThCADCoreNTSGeometryFilter.GeometryEquality(objs).OfType<Polyline>().ToList();
                }
            }

            return frameLst;
        }
    }
}
