using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service;
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.SecurityPlaneSystem.Utils;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThCADExtension;
using DotNetARX;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPWSS.HydrantConnectPipe.Service;
using Catel.Collections;
using ThMEPElectrical.ConnectPipe.Service;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class SystemConnectPipeService
    {
        public Dictionary<Line, List<Polyline>> Conenct(Polyline polyline, List<Polyline> columns, List<BlockModel> allBlocks, List<Line> trunkings, List<Line> ExistingLines, List<BlockModel> models, List<Polyline> holes)
        {
            Dictionary<Line, List<Polyline>> resLines = new Dictionary<Line, List<Polyline>>();
            if (trunkings.Count <= 0)
            {
                return resLines;
            }
            List<Polyline> allHoles = new List<Polyline>(holes);
            holes.AddRange(columns);
            var blocksBoundary = allBlocks.Select(o => o.Boundary);
            var blocks = blocksBoundary.Select(o => o.BufferPL(30)[0] as Polyline);
            ThCADCoreNTSSpatialIndex columnSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            ThCADCoreNTSSpatialIndex blockSpatialIndex = new ThCADCoreNTSSpatialIndex(blocks.ToCollection());
            foreach (BlockModel model in models)
            {
                var closetLine = GetClosetLane(trunkings, model.position, polyline);
                Polyline path = AdjustStartRoute(polyline, columns, blocksBoundary.Where(o => o != model.Boundary).ToList(), model, closetLine, columnSpatialIndex, blockSpatialIndex, ExistingLines);
                path = SetType(model, path);
                if (path.NumberOfVertices < 2)
                {
                    //舍弃掉该路径 do not
                }
                else if (resLines.ContainsKey(closetLine))
                {
                    resLines[closetLine].Add(path);
                }
                else
                {
                    resLines.Add(closetLine, new List<Polyline>() { path });
                }
            }
            return resLines;
        }

        /// <summary>
        /// 重选线槽
        /// </summary>
        /// <param name="trunking"></param>
        /// <param name="resPolyDic"></param>
        /// <returns></returns>
        public Dictionary<Line, List<Polyline>> ChooseTrunking(List<Line> trunking, Dictionary<Line, List<Polyline>> resPolyDic)
        {
            Dictionary<Line, List<Polyline>> reDic = new Dictionary<Line, List<Polyline>>();
            ThCADCoreNTSSpatialIndex SpatialIndex = new ThCADCoreNTSSpatialIndex(trunking.ToCollection());
            foreach (var Paths in resPolyDic)
            {
                if (!reDic.ContainsKey(Paths.Key))
                {
                    reDic.Add(Paths.Key, new List<Polyline>());
                }
                foreach (var path in Paths.Value)
                {
                    var IntersectingPath = SpatialIndex.SelectFence(path.ExtendPolyline(-5));
                    IntersectingPath.Remove(Paths.Key);
                    if(IntersectingPath.Count>0)
                    {
                        Line IntersectingLine = IntersectingPath[0] as Line;
                        var pts = new Point3dCollection();
                        IntersectingLine.IntersectWith(path, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count > 0)
                        {
                            Polyline newpath = ResetEndPath(path, pts[0]);
                            if (reDic.ContainsKey(IntersectingLine))
                            {
                                reDic[IntersectingLine].Add(newpath);
                            }
                            else
                            {
                                reDic.Add(IntersectingLine, new List<Polyline>() { newpath });
                            }
                            continue;
                        }
                    }
                    reDic[Paths.Key].Add(path);
                }
            }
            return reDic;
        }

        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Line GetClosetLane(List<Line> lanes, Point3d startPt, Polyline polyline)
        {
            //var minDistance = lanes.Min(x => x.DistanceTo(startPt, false)) * 1.2;
            //var choiseLanes = lanes.Where(x => x.DistanceTo(startPt, false) < minDistance);
            var minDistance = lanes.Min(x => GetDistanceToLine(x, startPt));
            var choiseLanes = lanes.Where(x => GetDistanceToLine(x, startPt) < minDistance * 1.2);
            var inLines = choiseLanes.Where(x => ThGeometryTool.IsProjectionPtInLine(x.StartPoint, x.EndPoint, startPt));
            Line choiseLine = new Line();
            if (inLines.Count() > 0)
            {
                choiseLine = inLines.OrderBy(x => x.DistanceTo(startPt, false)).First();
            }
            else
            {
                var MaxLenLine = choiseLanes.Where(x =>Math.Abs( GetDistanceToLine(x, startPt) - minDistance)<5).OrderByDescending(x => x.Length).First();
                choiseLine = MaxLenLine;
            }
            Line checkLine = new Line(startPt, choiseLine.GetClosestPointTo(startPt, false));
            if (!Utils.CheckService.CheckIntersectWithFrame(checkLine, polyline))
            {
                return choiseLine;
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(400);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lanes, polyline);
            return closetLine;
        }

        private Polyline AdjustStartRoute(Polyline polyline, List<Polyline> columns, List<Polyline> blocks, BlockModel model, Line closetLine, ThCADCoreNTSSpatialIndex columnsIndex, ThCADCoreNTSSpatialIndex blockSpatialIndex, List<Line> ExistingLines)
        {
            Polyline path = new Polyline();//选中路径
            //起点用A*寻路
            Point3d connectPoint = model.position;
            PipePathService pipePathService = new PipePathService();
            Polyline snappath = pipePathService.CreatePipePath(polyline, closetLine, connectPoint, columns.Union(blocks).ToList());
            if (snappath == null)
            {
                snappath = pipePathService.CreatePipePath(polyline, closetLine, connectPoint, columns);
                if (snappath == null)
                {
                    return new Polyline();
                }
            }
            snappath = ResetStartingPath(model, snappath, closetLine);
            path = snappath;
            if (snappath == null || snappath.NumberOfVertices == 0)
            {
                return new Polyline();
            }
            int fencecolumnsNum = columnsIndex.SelectFence(snappath).Count;
            int fenceblocksNum = blockSpatialIndex.SelectFence(snappath).Count;
            //需要调整连线
            connectPoint = snappath.StartPoint;
            Line intersectLine = ExistingLines.Where(o => o.StartPoint.DistanceTo(connectPoint) < 5).OrderBy(o => o.Length).FirstOrDefault();
            if (intersectLine.IsNull())
            {
                return snappath;
            }
            var branchDir = intersectLine.StartPoint.GetVectorTo(intersectLine.EndPoint);
            if (!branchDir.IsParallelWithTolerance(snappath.StartPoint.GetVectorTo(snappath.GetPoint3dAt(1)), 1))
            {
                return snappath;
            }
            var Dir = connectPoint.GetVectorTo(snappath.GetPoint3dAt(1)).GetNormal();
            var internalDir = model.position.GetVectorTo(connectPoint);
            var vectors = GetDirections(Dir);
            vectors.RemoveAll(o => internalDir.GetAngleTo(o) > Math.PI / 2.0);
            if (vectors.Count != 2)
            {
                throw new NotImplementedException();
            }
            //起始路线贴块，该路线不能被选择
            //vectors.RemoveAll(o => Math.Abs(Dir.GetAngleTo(o) - Math.PI / 4 * 3) < 0.1); 调整后不会再出现这种case
            int deleteCount = vectors.RemoveAll(o => o.IsParallelWithTolerance(branchDir, 1));
            if (deleteCount == 0)
            {
                Line secondLine = ExistingLines.Where(o => o.StartPoint.DistanceTo(intersectLine.EndPoint) < 1).FirstOrDefault();
                if (!secondLine.IsNull())
                {
                    branchDir = secondLine.StartPoint.GetVectorTo(secondLine.EndPoint);
                    vectors.RemoveAll(o => o.GetAngleTo(branchDir) < Math.PI / 2);
                }
            }
            if (vectors.Count == 2)
            {
                //可布置两个方向，但是要设置优先级
                if (closetLine.DistanceTo(connectPoint + vectors[0] * 400, false) > closetLine.DistanceTo(connectPoint + vectors[1] * 400, false))
                {
                    vectors.Reverse();
                }
            }
            for (int i = 0; i < vectors.Count; i++)
            {
                Vector3d vector = vectors[i];
                int MinfenceNum = fenceblocksNum;
                for (int distance = 400; distance <= 1000; distance += 200)
                {
                    Point3d extendPt = connectPoint + vector * distance;
                    Polyline revisepath = CreatNewPath(extendPt, snappath, closetLine);
                    if (columnsIndex.SelectFence(revisepath).Count == 0)
                    {
                        fenceblocksNum = blockSpatialIndex.SelectFence(revisepath).Count;
                        if (fenceblocksNum < 2)
                        {
                            return revisepath;
                        }
                        else if (fenceblocksNum < MinfenceNum)
                        {
                            path = revisepath;
                            MinfenceNum = fenceblocksNum;
                        }
                    }
                }
            }
            return path;
        }

        /// <summary>
        /// 调整终点路线
        /// </summary>
        /// <param name="colums"></param>
        /// <param name="ExistingLines"></param>
        /// <param name="paths"></param>
        public List<Polyline> AdjustEndRoute(List<Polyline> colums, List<Polyline> blocks, Dictionary<Line, List<Polyline>> paths)
        {
            List<Polyline> Path = new List<Polyline>();
            foreach (var item in paths.Keys)
            {
                var trunking = item;
                var trunkingPaths = paths[trunking];
                Vector3d endLineDic = trunking.StartPoint.GetVectorTo(trunking.EndPoint).GetNormal();
                var NearStartPaths = trunkingPaths.Where(o => o.EndPoint.DistanceTo(trunking.StartPoint) < 150).OrderBy(o => o.EndPoint.DistanceTo(trunking.StartPoint)).ToList();
                var NearEndPaths = trunkingPaths.Where(o => o.EndPoint.DistanceTo(trunking.EndPoint) < 150).OrderBy(o => o.EndPoint.DistanceTo(trunking.StartPoint)).ToList();
                trunkingPaths = trunkingPaths.Except(NearStartPaths).Except(NearEndPaths).OrderBy(o => o.EndPoint.DistanceTo(trunking.StartPoint)).ToList();
                //处理起点
                {
                    var parallelDirPath = NearStartPaths.Where(o => endLineDic.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 1))) < 1).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();//平行线

                    var sameDirPath = NearStartPaths.Where(o => o.NumberOfVertices > 2 && endLineDic.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//同向线
                    var DifferentDirPath = NearStartPaths.Where(o => o.NumberOfVertices > 2 && endLineDic.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//逆向线
                    var beeLinePath = NearStartPaths.Except(parallelDirPath).Except(sameDirPath).Except(DifferentDirPath).ToList();//直线
                    sameDirPath = AdjustSpacing(sameDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                    DifferentDirPath = AdjustSpacing(DifferentDirPath).OrderByDescending(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();

                    if (NearStartPaths.Count > 0)
                    {
                        //处理起点的垂直线
                        {
                            Point3d CurrentPoint = NearStartPaths[0].EndPoint;
                            int OffsetIndex = sameDirPath.Count + DifferentDirPath.Count + beeLinePath.Count - 1;//偏移量
                            Point3d endPt;
                            for (int i = 0; i < sameDirPath.Count(); i++)
                            {
                                var path = sameDirPath[i];
                                endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                            for (int i = 0; i < beeLinePath.Count(); i++)
                            {
                                var path = beeLinePath[i];
                                endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                            for (int i = 0; i < DifferentDirPath.Count(); i++)
                            {
                                var path = DifferentDirPath[i];
                                endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                        }
                        //处理起点的平行线
                        {
                            if (parallelDirPath.Count > 1)
                            {
                                Vector3d vector = Vector3d.ZAxis.CrossProduct(endLineDic).GetNormal();
                                sameDirPath = parallelDirPath.Where(o => o.NumberOfVertices > 2 && vector.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//同向线
                                DifferentDirPath = parallelDirPath.Where(o => o.NumberOfVertices > 2 && vector.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//逆向线
                                beeLinePath = parallelDirPath.Except(sameDirPath).Except(DifferentDirPath).ToList();//直线
                                sameDirPath = AdjustSpacing(sameDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                                DifferentDirPath=AdjustSpacing(DifferentDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                                bool HasbeeLinePath = false;
                                bool HassameDirPath = false;
                                if (beeLinePath.Count > 0)
                                {
                                    beeLinePath.ForEach(o => Path.Add(o));
                                    HasbeeLinePath = true;
                                }
                                Point3d endPt;
                                int OffsetIndex = sameDirPath.Count - 1;//偏移量
                                for (int i = 0; i < sameDirPath.Count(); i++)
                                {
                                    var path = sameDirPath[i];
                                    if (OffsetIndex == 0 && !HasbeeLinePath)
                                    {
                                        Path.Add(path);
                                        HassameDirPath = true;
                                        break;
                                    }
                                    endPt = path.EndPoint - endLineDic * 150 + vector * 150 * OffsetIndex--;
                                    if (path.GetPoint3dAt(path.NumberOfVertices - 2).DistanceTo(path.GetPoint3dAt(path.NumberOfVertices - 1)) < 150)
                                    {
                                        Line line = new Line(path.EndPoint, endPt);
                                        Point3dCollection pts = new Point3dCollection();
                                        line.IntersectWith(path, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                        if (pts.Count > 0)
                                            endPt = pts[0];
                                    }
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    path.AddVertexAt(path.NumberOfVertices, trunking.StartPoint.ToPoint2D(), 0, 0, 0);
                                    Path.Add(path);
                                }

                                OffsetIndex = DifferentDirPath.Count - 1;//偏移量
                                for (int i = 0; i < DifferentDirPath.Count(); i++)
                                {
                                    var path = DifferentDirPath[i];
                                    if (OffsetIndex == 0 && !HasbeeLinePath && !HassameDirPath)
                                    {
                                        Path.Add(path);
                                        break;
                                    }
                                    endPt = path.EndPoint - endLineDic * 150 - vector * 150 * OffsetIndex--;
                                    if (path.GetPoint3dAt(path.NumberOfVertices - 2).DistanceTo(path.GetPoint3dAt(path.NumberOfVertices - 1)) < 150)
                                    {
                                        Line line = new Line(path.EndPoint, endPt);
                                        Point3dCollection pts = new Point3dCollection();
                                        line.IntersectWith(path, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                        if (pts.Count > 0)
                                            endPt = pts[0];
                                    }
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    path.AddVertexAt(path.NumberOfVertices, trunking.StartPoint.ToPoint2D(), 0, 0, 0);
                                    Path.Add(path);
                                }
                            }
                            else
                            {
                                parallelDirPath.ForEach(o => Path.Add(o));
                            }
                        }
                    }
                }
                //处理终点
                {
                    var parallelDirPath = NearEndPaths.Where(o => endLineDic.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 1))) < 1).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();//平行线
                    var sameDirPath = NearEndPaths.Where(o => o.NumberOfVertices > 2 && endLineDic.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//同向线
                    var DifferentDirPath = NearEndPaths.Where(o => o.NumberOfVertices > 2 && endLineDic.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//逆向线
                    var beeLinePath = NearEndPaths.Except(parallelDirPath).Except(sameDirPath).Except(DifferentDirPath).ToList();//直线
                    sameDirPath = AdjustSpacing(sameDirPath).OrderByDescending(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                    DifferentDirPath = AdjustSpacing(DifferentDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                    if (NearEndPaths.Count > 0)
                    {
                        //处理终点的垂直线
                        {
                            Point3d CurrentPoint = NearEndPaths.Last().EndPoint;
                            int OffsetIndex = sameDirPath.Count + DifferentDirPath.Count + beeLinePath.Count - 1;//偏移量
                            Point3d endPt;
                            for (int i = 0; i < DifferentDirPath.Count(); i++)
                            {
                                var path = DifferentDirPath[i];
                                endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                            for (int i = 0; i < beeLinePath.Count(); i++)
                            {
                                var path = beeLinePath[i];
                                endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                            for (int i = 0; i < sameDirPath.Count(); i++)
                            {
                                var path = sameDirPath[i];
                                endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                Path.Add(path);
                            }
                        }
                        //处理终点的平行线
                        {
                            if (parallelDirPath.Count > 1)
                            {
                                Vector3d vector = Vector3d.ZAxis.CrossProduct(endLineDic).GetNormal();
                                sameDirPath = parallelDirPath.Where(o => o.NumberOfVertices > 2  && vector.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//同向线
                                DifferentDirPath = parallelDirPath.Where(o => o.NumberOfVertices > 2 && vector.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//逆向线
                                beeLinePath = parallelDirPath.Except(sameDirPath).Except(DifferentDirPath).ToList();//直线
                                sameDirPath = AdjustSpacing(sameDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                                DifferentDirPath=AdjustSpacing(DifferentDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                                bool HasbeeLinePath = false;
                                bool HassameDirPath = false;
                                if (beeLinePath.Count > 0)
                                {
                                    beeLinePath.ForEach(o => Path.Add(o));
                                    HasbeeLinePath = true;
                                }
                                Point3d endPt;
                                int OffsetIndex = sameDirPath.Count - 1;//偏移量
                                for (int i = 0; i < sameDirPath.Count(); i++)
                                {
                                    var path = sameDirPath[i];
                                    if (OffsetIndex == 0 && !HasbeeLinePath)
                                    {
                                        Path.Add(path);
                                        HassameDirPath = true;
                                        break;
                                    }
                                    endPt = path.EndPoint + endLineDic * 150 + vector * 150 * OffsetIndex--;
                                    if (path.GetPoint3dAt(path.NumberOfVertices - 2).DistanceTo(path.GetPoint3dAt(path.NumberOfVertices - 1)) < 150)
                                    {
                                        Line line = new Line(path.EndPoint, endPt);
                                        Point3dCollection pts = new Point3dCollection();
                                        line.IntersectWith(path, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                        if (pts.Count > 0)
                                            endPt = pts[0];
                                    }
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    path.AddVertexAt(path.NumberOfVertices, trunking.EndPoint.ToPoint2D(), 0, 0, 0);
                                    Path.Add(path);
                                }

                                OffsetIndex = DifferentDirPath.Count - 1;//偏移量
                                for (int i = 0; i < DifferentDirPath.Count(); i++)
                                {
                                    var path = DifferentDirPath[i];
                                    if (OffsetIndex == 0 && !HasbeeLinePath && !HassameDirPath)
                                    {
                                        Path.Add(path);
                                        break;
                                    }
                                    endPt = path.EndPoint + endLineDic * 150 - vector * 150 * OffsetIndex--;
                                    if (path.GetPoint3dAt(path.NumberOfVertices - 2).DistanceTo(path.GetPoint3dAt(path.NumberOfVertices - 1)) < 150)
                                    {
                                        Line line = new Line(path.EndPoint, endPt);
                                        Point3dCollection pts = new Point3dCollection();
                                        line.IntersectWith(path, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                        if (pts.Count > 0)
                                            endPt = pts[0];
                                    }
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    path.AddVertexAt(path.NumberOfVertices, trunking.EndPoint.ToPoint2D(), 0, 0, 0);
                                    Path.Add(path);
                                }
                            }
                            else
                            {
                                parallelDirPath.ForEach(o => Path.Add(o));
                            }
                        }
                    }
                }
                //处理中间点
                int index = 0;//标记正在处理的Path
                while (index < trunkingPaths.Count)
                {
                    var NeighborPath = trunkingPaths.Skip(index).Where(o => o.EndPoint.DistanceTo(trunkingPaths[index].EndPoint) < 150);
                    var neighborNum = NeighborPath.Count();
                    if (neighborNum < 2)
                    {
                        index++;
                    }
                    else
                    {
                        Point3d CurrentPoint = trunkingPaths[index].EndPoint;
                        var sameDirPath = NeighborPath.Where(o => o.NumberOfVertices > 2 && endLineDic.GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//同向线
                        var DifferentDirPath = NeighborPath.Where(o => o.NumberOfVertices > 2 && endLineDic.Negate().GetAngleTo(o.GetPoint3dAt(o.NumberOfVertices - 2).GetVectorTo(o.GetPoint3dAt(o.NumberOfVertices - 3))) < 1).ToList();//逆向线
                        var beeLinePath = NeighborPath.Except(sameDirPath).Except(DifferentDirPath).ToList();//直线
                        sameDirPath = AdjustSpacing(sameDirPath).OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                        DifferentDirPath= AdjustSpacing(DifferentDirPath).OrderByDescending(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
                        if (beeLinePath.Count() == 0)
                        {
                            //距离足够，往后偏移
                            Point3d endPt = trunkingPaths.Count > index + neighborNum ? trunkingPaths[index + neighborNum].EndPoint : trunking.EndPoint;
                            Point3d startPt = index > 0 ? trunkingPaths[index - 1].EndPoint : trunking.StartPoint;
                            if (endPt.DistanceTo(trunkingPaths[index].EndPoint) > neighborNum * 150)
                            {
                                int OffsetIndex = neighborNum - 1;//偏移量
                                for (int i = 0; i < sameDirPath.Count(); i++)
                                {
                                    var path = sameDirPath[i];
                                    endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    trunkingPaths[indexof] = path;
                                }
                                for (int i = 0; i < DifferentDirPath.Count(); i++)
                                {
                                    var path = DifferentDirPath[i];
                                    endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    trunkingPaths[indexof] = path;
                                }
                            }
                            //距离不够，往前偏移
                            else if (trunkingPaths[index].EndPoint.DistanceTo(startPt) > neighborNum * 150)
                            {
                                int OffsetIndex = neighborNum - 1;//偏移量
                                sameDirPath.Reverse();
                                DifferentDirPath.Reverse();
                                for (int i = 0; i < DifferentDirPath.Count(); i++)
                                {
                                    var path = DifferentDirPath[i];
                                    endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    trunkingPaths[indexof] = path;
                                }
                                for (int i = 0; i < sameDirPath.Count(); i++)
                                {
                                    var path = sameDirPath[i];
                                    endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                    int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                    path = ResetEndPath(path, endPt);
                                    trunkingPaths[indexof] = path;
                                }
                            }
                        }
                        else
                        {
                            //同向的往后偏移
                            int OffsetIndex = sameDirPath.Count();//偏移量
                            for (int i = 0; i < sameDirPath.Count(); i++)
                            {
                                var path = sameDirPath[i];
                                Point3d endPt = CurrentPoint + endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                trunkingPaths[indexof] = path;
                            }
                            //逆向的往前偏移
                            OffsetIndex = DifferentDirPath.Count();//偏移量
                            DifferentDirPath.Reverse();
                            for (int i = 0; i < DifferentDirPath.Count(); i++)
                            {
                                var path = DifferentDirPath[i];
                                Point3d endPt = CurrentPoint - endLineDic * 150 * OffsetIndex--;
                                int indexof = trunkingPaths.FindIndex(o => o.StartPoint == path.StartPoint);
                                path = ResetEndPath(path, endPt);
                                trunkingPaths[indexof] = path;
                            }
                        }
                        index += neighborNum;
                    }
                }
                Path.AddRange(trunkingPaths);
            }
            return Path;
        }
        
        /// <summary>
        /// 断开路径
        /// </summary>
        /// <param name="resPolys"></param>
        /// <param name="modelBoundary"></param>
        /// <returns></returns>
        public List<Line> DisconnectRoute(List<Polyline> resPolys, List<Polyline> modelBoundary)
        {
            Vector3d vector = Vector3d.XAxis;
            List<Line> allLines = resPolys.SelectMany(o => { var lines = o.GetAllLinesInPolyline(false); lines.ForEach(x => { x.Layer = o.Layer; x.Linetype = o.Linetype; x.ColorIndex = o.ColorIndex; }); return lines; }).ToList();
            List<Line> reLines = allLines.Where(o => o.Length <= 400).ToList();
            List<Line> VerticalLines = allLines.Except(reLines).Where(o => Math.Abs(vector.GetAngleTo(o.LineDirection()) - Math.PI / 2) < 0.1).ToList();
            List<Line> untreatedLines = allLines.Except(reLines).Except(VerticalLines).ToList();
            List<Line> processedLines = new List<Line>();//处理过的线
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(untreatedLines.ToCollection());
            spatialIndex.Update(modelBoundary.ToCollection(), new DBObjectCollection());

            foreach (var line in VerticalLines)
            {
                var intersectCollection = spatialIndex.SelectFence(line.ExtendLine(-10));
                intersectCollection.Remove(line);
                var intersectEntitys = intersectCollection.Cast<Entity>().ToList();
                var intersectPts = new List<Point3dCollection>();
                intersectEntitys.ForEach(o =>
                {
                    if (!(o is Line oline) || !line.IsParallelToEx(oline))
                    {
                        Point3dCollection pts = new Point3dCollection();
                        line.IntersectWith(o, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count > 0)
                            intersectPts.Add(pts);
                    }
                });

                processedLines.Add(line);
                reLines.AddRange(SegmentationLine(line, intersectPts));
            }
            foreach (var line in untreatedLines)
            {
                var intersectCollection = spatialIndex.SelectFence(line.ExtendLine(-10));
                intersectCollection.Remove(line);
                var intersectEntitys = intersectCollection.Cast<Entity>().ToList();
                var intersectPts = new List<Point3dCollection>();
                intersectEntitys.Where(o => o is Polyline).ForEach(o =>
                {
                    Point3dCollection pts = new Point3dCollection();
                    line.IntersectWith(o, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                        intersectPts.Add(pts);
                });
                intersectEntitys.Where(o => o is Line && !processedLines.Contains(o)).Cast<Line>().ForEach(o =>
                {
                    if (!line.IsParallelToEx(o))
                    {
                        Point3dCollection pts = new Point3dCollection();
                        line.IntersectWith(o, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count > 0)
                            intersectPts.Add(pts);
                    }
                });
                processedLines.Add(line);
                reLines.AddRange(SegmentationLine(line, intersectPts));
            }
            return reLines;
        }

        /// <summary>
        /// 调整间距
        /// </summary>
        /// <param name="sameDirPath"></param>
        /// <returns></returns>
        private List<Polyline> AdjustSpacing(List<Polyline> paths)
        {
            List<Polyline> newPaths = paths.OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2))).ToList();
            int index = 0;
            while (index < newPaths.Count)
            {
                var path = newPaths[index];
                double length = path.GetPoint3dAt(path.NumberOfVertices - 1).DistanceTo(path.GetPoint3dAt(path.NumberOfVertices - 2));
                var neighborPaths = newPaths.Skip(index).Where(o => o.GetPoint3dAt(o.NumberOfVertices - 1).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 2)) < length + 150).ToList();
                if (neighborPaths.Count == 1)
                {
                    index++;
                }
                else
                {
                    double lastlength = index == 0 ? 0 : newPaths[index - 1].GetPoint3dAt(newPaths[index - 1].NumberOfVertices - 1).DistanceTo(newPaths[index - 1].GetPoint3dAt(newPaths[index - 1].NumberOfVertices - 2));
                    double nextlength = index + neighborPaths.Count == newPaths.Count ? double.NaN : newPaths[index + neighborPaths.Count].GetPoint3dAt(newPaths[index + neighborPaths.Count].NumberOfVertices - 1).DistanceTo(newPaths[index + neighborPaths.Count].GetPoint3dAt(newPaths[index + neighborPaths.Count].NumberOfVertices - 2));
                    var directPath = neighborPaths.Where(o => o.NumberOfVertices < 4).ToList();
                    if (directPath.Count == 0)
                    {
                        //向内缩
                        if (length - lastlength > 150 * neighborPaths.Count)
                        {
                            neighborPaths = neighborPaths.OrderByDescending(o => o.GetPoint3dAt(o.NumberOfVertices - 2).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 3))).ToList();
                            int count = neighborPaths.Count - 1;
                            for (int i = 0; i < neighborPaths.Count; i++)
                            {
                                Polyline polyline = neighborPaths[i];
                                int numberOfVertices = polyline.NumberOfVertices;
                                var lastLine = new Line(polyline.GetPoint3dAt(numberOfVertices - 2), polyline.GetPoint3dAt(numberOfVertices - 1));
                                var lastpoint = lastLine.StartPoint + lastLine.LineDirection() * 150 * count--;
                                var thirdLine = new Line(polyline.GetPoint3dAt(numberOfVertices - 4), polyline.GetPoint3dAt(numberOfVertices - 3));
                                var thirdpoint = thirdLine.GetClosestPointTo(lastpoint, true);
                                Polyline newpath = new Polyline() { Layer = polyline.Layer, Linetype = polyline.Linetype, ColorIndex = polyline.ColorIndex };
                                int j = 0;
                                for (; j < polyline.NumberOfVertices - 3; j++)
                                {
                                    newpath.AddVertexAt(j, polyline.GetPoint2dAt(j), 0, 0, 0);
                                }
                                newpath.AddVertexAt(j++, thirdpoint.ToPoint2D(), 0, 0, 0);
                                newpath.AddVertexAt(j++, lastpoint.ToPoint2D(), 0, 0, 0);
                                newpath.AddVertexAt(j++, polyline.EndPoint.ToPoint2D(), 0, 0, 0);
                                neighborPaths[i] = newpath;
                            }
                            neighborPaths.Reverse();
                            newPaths.RemoveRange(index, neighborPaths.Count);
                            newPaths.InsertRange(index, neighborPaths);
                        }
                        //向外扩
                        else if (nextlength - length > 150 * neighborPaths.Count)
                        {
                            neighborPaths = neighborPaths.OrderBy(o => o.GetPoint3dAt(o.NumberOfVertices - 2).DistanceTo(o.GetPoint3dAt(o.NumberOfVertices - 3))).ToList();
                            int count = neighborPaths.Count - 1;
                            for (int i = 0; i < neighborPaths.Count; i++)
                            {
                                Polyline polyline = neighborPaths[i];
                                int numberOfVertices = polyline.NumberOfVertices;
                                var lastLine = new Line(polyline.GetPoint3dAt(numberOfVertices - 2), polyline.GetPoint3dAt(numberOfVertices - 1));
                                var lastpoint = lastLine.StartPoint - lastLine.LineDirection() * 150 * count--;
                                var thirdLine = new Line(polyline.GetPoint3dAt(numberOfVertices - 4), polyline.GetPoint3dAt(numberOfVertices - 3));
                                var thirdpoint = thirdLine.GetClosestPointTo(lastpoint, true);
                                Polyline newpath = new Polyline() { Layer = polyline.Layer, Linetype = polyline.Linetype, ColorIndex = polyline.ColorIndex };
                                int j = 0;
                                for (; j < polyline.NumberOfVertices - 3; j++)
                                {
                                    newpath.AddVertexAt(j, polyline.GetPoint2dAt(j), 0, 0, 0);
                                }
                                newpath.AddVertexAt(j++, thirdpoint.ToPoint2D(), 0, 0, 0);
                                newpath.AddVertexAt(j++, lastpoint.ToPoint2D(), 0, 0, 0);
                                newpath.AddVertexAt(j++, polyline.EndPoint.ToPoint2D(), 0, 0, 0);
                            }
                            newPaths.RemoveRange(index, neighborPaths.Count);
                            newPaths.InsertRange(index, neighborPaths);
                        }
                    }
                    //neighborPaths = AdjustNerbor(neighborPaths);
                    index += neighborPaths.Count;
                }
            }
            return newPaths;
        }

        /// <summary>
        /// 获取连接点
        /// </summary>
        /// <param name="blockModel"></param>
        /// <returns></returns>
        private List<Point3d> GetConnectPoints(BlockModel blockModel)
        {
            var pts = new List<Point3d>();
            if (blockModel is VMModel vmmodel)
            {
                pts = vmmodel.ConnectPts;
            }
            else if (blockModel is IAModel iamodel)
            {
                pts = iamodel.ConnectPts;
            }
            else if (blockModel is ACModel acmodel)
            {
                pts = acmodel.ConnectPts;
            }
            if (pts.IsNull())
            {
                throw new NotSupportedException();
            }
            return pts;
        }

        private List<Vector3d> GetDirections(Vector3d vector)
        {
            List<Vector3d> vectors = new List<Vector3d>();
            Vector3d newVector = vector.RotateBy(Math.PI / 4.0, Vector3d.ZAxis).GetNormal();
            for (int i = 0; i < 4; i++)
            {
                vectors.Add(newVector);
                newVector = newVector.RotateBy(Math.PI / 2.0, Vector3d.ZAxis).GetNormal();
            }
            return vectors;
        }

        private Polyline SetType(BlockModel model, Polyline path)
        {
            if (model is VMModel)
            {
                path.Layer = ThMEPCommon.VM_PIPE_LAYER_NAME;
                path.Linetype = ThMEPCommon.VM_PIPE_LINETYPE;
                path.ColorIndex = 256;
            }
            else if (model is ACModel)
            {
                path.Layer = ThMEPCommon.AC_PIPE_LAYER_NAME;
                path.Linetype = ThMEPCommon.AC_PIPE_LINETYPE;
                path.ColorIndex = 256;
            }
            else if (model is IAModel)
            {
                path.Layer = ThMEPCommon.IA_PIPE_LAYER_NAME;
                path.Linetype = ThMEPCommon.IA_PIPE_LINETYPE;
                path.ColorIndex = 256;
            }
            return path;
        }

        private Polyline CreatNewPath(Point3d turning,Polyline oldPath,Line endLine)
        {
            Polyline newpolyline = new Polyline();
            newpolyline.AddVertexAt(0, oldPath.StartPoint.ToPoint2D(), 0, 0, 0);
            newpolyline.AddVertexAt(1, turning.ToPoint2D(), 0, 0, 0);

            int pathedgs = oldPath.Vertices().Count;
            if (pathedgs > 2)
            {
                Line line = new Line(oldPath.GetPoint3dAt(1), oldPath.GetPoint3dAt(2));
                Point3d point = line.GetClosestPointTo(turning, true);
                newpolyline.AddVertexAt(2, point.ToPoint2D(), 0, 0, 0);
                for (int i = 2; i < pathedgs; i++)
                {
                    newpolyline.AddVertexAt(i + 1, oldPath.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
                }
            }
            else
            {
                Line line = endLine;
                Point3d point = line.GetClosestPointTo(turning, true);
                newpolyline.AddVertexAt(2, point.ToPoint2D(), 0, 0, 0);
                if (!point.IsPointOnLine(line))
                {
                    newpolyline.AddVertexAt(3, oldPath.EndPoint.ToPoint2D(), 0, 0, 0);
                }
            }
            return newpolyline;
        }

        private Polyline ResetStartingPath(BlockModel model,Polyline oldPath,Line endLine)
        {
            var Intersectpts = new Point3dCollection();
            oldPath.IntersectWith(model.Boundary, Intersect.OnBothOperands, Intersectpts, (IntPtr)0, (IntPtr)0);
            if(Intersectpts.Count == 0)
            {
                return new Polyline();
            }
            Point3d intersectPt = Intersectpts.Cast<Point3d>().OrderByDescending(o => o.DistanceTo(oldPath.StartPoint)).First();
            var pts = GetConnectPoints(model);
            int pathedgs = oldPath.NumberOfVertices;
            Point3d StartPt = pts.OrderBy(o => o.DistanceTo(intersectPt)).First();
            
            int i = 1;
            Line line = new Line();
            while (i < pathedgs)
            {
                if (!model.Boundary.Contains(oldPath.GetPoint3dAt(i)))
                {
                    line = new Line(oldPath.GetPoint3dAt(i - 1), oldPath.GetPoint3dAt(i));
                    break;
                }
                i++;
            }
            Line nextline = endLine;
            if (pathedgs -i > 1)
            {
                nextline = new Line(oldPath.GetPoint3dAt(i), oldPath.GetPoint3dAt(i +1));
            }
            Point3d secondPt= nextline.GetClosestPointTo(StartPt, true);
            Polyline newpolyline = new Polyline();
            newpolyline.AddVertexAt(0, StartPt.ToPoint2D(), 0, 0, 0);
            newpolyline.AddVertexAt(1, secondPt.ToPoint2D(), 0, 0, 0);
            int Number = pathedgs - i + 1;
            for (int j = 2; j < Number; j++)
            {
                newpolyline.AddVertexAt(j, oldPath.GetPoint3dAt(++i).ToPoint2D(), 0, 0, 0);
            }
            if(!newpolyline.EndPoint.IsPointOnLine(endLine))
            {
                newpolyline.AddVertexAt(newpolyline.NumberOfVertices, oldPath.EndPoint.ToPoint2D(), 0, 0, 0);
            }
            return newpolyline;
        }

        private Polyline ResetEndPath(Polyline oldPath, Point3d newEndPoint)
        {
            if (newEndPoint.DistanceTo(oldPath.EndPoint) < 1)
                return oldPath;
            Polyline polyline = new Polyline() { Layer = oldPath.Layer, Linetype = oldPath.Linetype, ColorIndex = oldPath.ColorIndex };
            var verticesCount = oldPath.NumberOfVertices;
            if (verticesCount > 2)
            {
                Line secondLastLine = new Line(oldPath.GetPoint3dAt(verticesCount - 2), oldPath.GetPoint3dAt(verticesCount - 3));
                var point = secondLastLine.GetClosestPointTo(newEndPoint, true);
                for (int i = 0; i < verticesCount - 2; i++)
                {
                    polyline.AddVertexAt(i, oldPath.GetPoint2dAt(i), 0, 0, 0);
                }
                polyline.AddVertexAt(verticesCount - 2, point.ToPoint2D(), 0, 0, 0);
                polyline.AddVertexAt(verticesCount - 1, newEndPoint.ToPoint2D(), 0, 0, 0);
                return polyline;
            }
            else
            {
                var Dir = oldPath.StartPoint.GetVectorTo(oldPath.EndPoint).GetNormal();
                var HorizontalDir = Vector3d.ZAxis.CrossProduct(Dir).GetNormal();
                Line line = new Line(oldPath.StartPoint + Dir * 150 + HorizontalDir * 300, oldPath.StartPoint + Dir * 150 - HorizontalDir * 300);
                var point = line.GetClosestPointTo(newEndPoint, false);
                polyline.AddVertexAt(0, oldPath.StartPoint.ToPoint2D(), 0, 0, 0);
                polyline.AddVertexAt(1, point.ToPoint2D(), 0, 0, 0);
                polyline.AddVertexAt(2, newEndPoint.ToPoint2D(), 0, 0, 0);
                return polyline;
            }
        }

        private List<Line> SegmentationLine(Line line, List<Point3dCollection> ptsCollection)
        {
            if (ptsCollection.Count == 0)
            {
                return new List<Line>() { line };
            }
            var dir = line.LineDirection();
            var pts = ptsCollection.OrderBy(o => o[0].DistanceTo(line.StartPoint));
            List<Point3d> keyPoints = new List<Point3d>() { line.StartPoint };
            Point3d pointer = line.StartPoint;
            foreach (var item in pts)
            {
                List<Point3d> points = item.Cast<Point3d>().ToList();
                var Nearestpoint = points.OrderBy(o => o.DistanceTo(pointer)).First();
                var Furthespoint = points.OrderBy(o => o.DistanceTo(pointer)).Last();
                if (Nearestpoint.DistanceTo(pointer) > 150 && Furthespoint.DistanceTo(line.EndPoint) > 150)
                {
                    keyPoints.Add(Nearestpoint - dir * 150);
                    keyPoints.Add(Furthespoint + dir * 150);
                    pointer = Furthespoint + dir * 150;
                }
                else if(Furthespoint.DistanceTo(line.EndPoint) > 150)
                {
                    keyPoints[keyPoints.Count - 1] = Furthespoint + dir * 150;
                    pointer = Furthespoint + dir * 150;
                }
            }
            keyPoints.Add(line.EndPoint);
            List<Line> reLines = new List<Line>();
            for (int i = 0; i < keyPoints.Count; i+=2)
            {
                reLines.Add(new Line(keyPoints[i], keyPoints[i + 1]));
            }
            reLines.ForEach(o => { o.Layer = line.Layer; o.Linetype = line.Linetype; o.ColorIndex = line.ColorIndex; });
            return reLines;
        }

        private double GetDistanceToLine(Line line,Point3d point)
        {
            Point3d closedPt = line.GetClosestPointTo(point, true);
            return point.DistanceTo(closedPt) + line.DistanceTo(closedPt, false);
        }
    }
}
