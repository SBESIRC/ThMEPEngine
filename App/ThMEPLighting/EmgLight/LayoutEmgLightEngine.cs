using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Common;
using ThMEPLighting.EmgLight.Model;



namespace ThMEPLighting.EmgLight
{
    class LayoutEmgLightEngine
    {

        public Polyline frame;
        public List<List<Line>> lanes;
        public List<Polyline> columns;
        public List<Polyline> walls;
        public Dictionary<BlockReference, BlockReference> evacBlk;
        public int singleSide = 0;

        public LayoutEmgLightEngine()
        {
            frame = new Polyline();
            lanes = new List<List<Line>>();
            columns = new List<Polyline>();
            walls = new List<Polyline>();
            evacBlk = new Dictionary<BlockReference, BlockReference>();
        }

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="lanes"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<Polyline, (Point3d, Vector3d)> LayoutLight()
        {
            Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo = new Dictionary<Polyline, (Point3d, Vector3d)>();
            List<ThStruct> layoutList = new List<ThStruct>();
            List<ThLane> laneList = new List<ThLane>();

            //跳过完全没有可布点的图
            if (columns.Count == 0 && walls.Count == 0)
            {
                return layoutPtInfo;
            }

            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                //特别短的线跳过
                var laneLength = lane.Sum(x => x.Length);
                if (laneLength < EmgLightCommon.TolLightRangeMin)
                {
                    continue;
                }

                var thLane = new ThLane(lane);
                laneList.Add(thLane);
            }

            for (int i = 0; i < laneList.Count; i++)
            {
                var thLane = laneList[i];
                var StructFilterService = new StructFilterService(thLane, columns, walls);

                var layoutServer = StructFilterService.getStructSeg();

                StructFilterService.FilterStruct(layoutServer, frame);

                if (layoutServer.UsefulColumns[0].Count == 0 && layoutServer.UsefulColumns[1].Count == 0 &&
                    layoutServer.UsefulWalls[0].Count == 0 && layoutServer.UsefulWalls[1].Count == 0)
                {
                    continue;
                }

                ////找出平均的一边. -1:no side 0:left 1:right.
                int uniformSide = FindUniformSideService.IfHasUniformSide(layoutServer, out var columnDistList);
                FindUniformSideService.DetermineStartSide(layoutServer, out var uniformSideStructsList, ref uniformSide, ref columnDistList);

                if (singleSide == 1)
                {
                    //单边布置
                    //var tolMin = EmgLightCommon.TolLightRangeSingleSideMin;
                    //var tolMax = EmgLightCommon.TolLightRangeSingleSideMax;

                    var tolMin = 2000;
                    var tolMax = 4000;

                    int sideHasEvac = FindUniformSideService.sideHasEvac(layoutServer, evacBlk, out var evacInLane);
                    var layoutSingle = new LayoutSingleSideService(layoutList, layoutServer, evacBlk);
                    if (sideHasEvac == -1)
                    {
                        layoutSingle.LayoutSingleSide(uniformSide, laneList, tolMin, tolMax);
                    }
                    else
                    {
                        var tolLenInEvac = 6000;

                        layoutSingle.LayoutSingleSideByEvac(sideHasEvac, laneList, evacInLane, tolMin, tolMax, tolLenInEvac);
                    }
                }
                else
                {
                    var tolMin = EmgLightCommon.TolLightRangeMin;
                    var tolMax = EmgLightCommon.TolLightRangeMax;

                    var layoutUniform = new LayoutUniformSideService(layoutList, layoutServer, uniformSideStructsList, columnDistList);
                    layoutUniform.LayoutUniformSide(uniformSide, laneList, out var uniformSideLayout, tolMin, tolMax);

                    var layoutNonUniform = new LayoutNonUniformSideService(layoutList, layoutServer, uniformSideLayout);
                    layoutNonUniform.LayoutOppositeSide(uniformSide, laneList, tolMin, tolMax);

                }

                layoutServer.AddLayoutStructPt(layoutList, ref layoutPtInfo);
            }

            return layoutPtInfo;
        }

        public void ResetResult(ref Dictionary<Polyline, (Point3d, Vector3d)> layoutInfo, ThMEPOriginTransformer transformer)
        {

            Dictionary<Polyline, (Point3d, Vector3d)> resetResult = new Dictionary<Polyline, (Point3d, Vector3d)>();

            layoutInfo.ForEach(x =>
            {
                var pt = new Point3d(x.Value.Item1.X, x.Value.Item1.Y, x.Value.Item1.Z);
                transformer.Reset(ref pt);
                resetResult.Add(x.Key, (pt, x.Value.Item2));
            });

            layoutInfo = resetResult;
        }

        public void ResetResult(ref List<Polyline> plList, ThMEPOriginTransformer transformer)
        {

            plList.ForEach(x =>
            {
                transformer.Reset( x);
            });

        }


        public void moveEmg(ref Dictionary<Polyline, (Point3d, Vector3d)> layoutInfo)
        {
            var tol = EmgLightCommon.TolGroupEvcaEmg;
            tol = 1500;

            if (evacBlk.Count > 0)
            {
                var blkConnectDict = GetBlockService.blkConnectDict();
                var connectPt = blkConnectDict[ThMEPLightingCommon.EvacLRBlockName];

                foreach (var evac in evacBlk)
                {
                    var closeEmgList = layoutInfo.Where(x => x.Value.Item1.DistanceTo(evac.Value.Position) <= tol);
                    var bAdd = false;
                    Point3d addPt = new Point3d();
                    if (closeEmgList.Count() > 0)
                    {
                        var connPtTop = connectPt[3].TransformBy(evac.Value.BlockTransform);
                        var connPtBottom = connectPt[1].TransformBy(evac.Value.BlockTransform);

                        var evacDir = (connPtTop - connPtBottom).GetNormal();
                        var closeEmg = closeEmgList.OrderBy(x => x.Value.Item1.DistanceTo(evac.Value.Position)).First();
                        var emgDir = closeEmg.Value.Item2.GetNormal();

                        var CosAngle = evacDir.DotProduct(emgDir);

                        //角度在20 以内, 找evac 的连接点
                        if (Math.Abs(CosAngle) > Math.Cos(20 * Math.PI / 180))
                        {
                            if (CosAngle < 0)
                            {
                                addPt = connPtBottom;
                            }
                            else
                            {
                                addPt = connPtTop;
                            }
                        }
                        var moveDir = (addPt - closeEmg.Value.Item1).GetNormal();
                        var moveAngle = moveDir.GetAngleTo(closeEmg.Value.Item2, Vector3d.ZAxis);
                        if ((170 * Math.PI / 180) <= moveAngle && moveAngle <= (190 * Math.PI / 180))
                        {
                            bAdd = false;
                        }
                        else
                        {
                            bAdd = true;
                        }

                        if (bAdd == true && addPt!=Point3d.Origin )
                        {
                            layoutInfo[closeEmg.Key] = (addPt, closeEmg.Value.Item2);
                        }
                    }
                }
            }
        }

        public static double getScale(GetBlockService getBlockS)
        {
            double scale = -1;
            if (getBlockS.evacR.Count > 0)
            {
                scale = getBlockS.evacR.First().Key.ScaleFactors.X;
            }
            scale = scale == -1 ? EmgLightCommon.BlockScaleNum : scale;

            return scale;
        }
    }
}
