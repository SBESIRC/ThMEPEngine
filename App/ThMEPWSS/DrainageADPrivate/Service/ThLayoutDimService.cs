using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageSystemDiagram.Service;

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThLayoutDimService
    {
        public static List<ThDrainageBlkOutput> LayoutDim(List<ThDrainageTreeNode> rootList)
        {
            var allNode = rootList.SelectMany(x => x.GetDescendant()).ToList();
            var allLine = ThLayoutDimService.TurnNodeToTransLine(allNode);
            var allIsolateLine = new List<Line>();
            allIsolateLine.AddRange(allLine);
            //之后要加上阀门???

            var nodeDiaDimOutput = ThLayoutDimService.CalculatePositionDim(allNode, allIsolateLine);

            return nodeDiaDimOutput;

        }

        private static List<Line> TurnNodeToTransLine(List<ThDrainageTreeNode> allNode)
        {
            var allLine = new List<Line>();

            foreach (var node in allNode)
            {
                if (node.Parent != null)
                {
                    var line = new Line(node.Parent.TransPt, node.TransPt);
                    allLine.Add(line);
                }
            }
            return allLine;
        }

        private static List<ThDrainageBlkOutput> CalculatePositionDim(List<ThDrainageTreeNode> allNode, List<Line> allIsolateLine)
        {
            var sDN = ThDrainageADCommon.DiameterDN_visi_pre;
            var dimBlkX = ThDrainageADCommon.DiameterDim_blk_x;
            var dimBlkY = ThDrainageADCommon.DiameterDim_blk_y;
            var blk_name = ThDrainageADCommon.BlkName_Dim;
            var visiPropertyName = ThDrainageADCommon.VisiName_valve;

            var alreadyDimArea = new List<Polyline>();
            var output = new List<ThDrainageBlkOutput>();

            foreach (var node in allNode)
            {
                if (node.Parent != null)
                {
                    var s = node.TransPt;
                    var e = node.Parent.TransPt;
                    var length = (e - s).Length;

                    if (length < dimBlkX * 1.5)
                    {
                        //太短的线跳过。一定要比bimBlkX+moveX要长否则终点位会变起点位再往前,但要小于1000（立管是1000左右）
                        //DrawUtils.ShowGeometry(s, "l0tooShorLine", colorIndex: 2, lineWeightNum: 30, r: 50);
                        continue;
                    }

                    var dir = (e - s).GetNormal();
                    var angle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);
                    if (179 * Math.PI / 180 <= angle && angle <= 359 * Math.PI / 180)
                    {
                        e = node.TransPt;
                        s = node.Parent.TransPt;
                        dir = (e - s).GetNormal();
                    }

                    var dimPtsTemp = PossiblePosition(s, e);
                    var dimOutlineList = dimPtsTemp.Select(x => ThDrainageADDiameterDim.ToDimOutline(x, dir, dimBlkX, dimBlkY)).ToList();
                    var dimOutline = ThDrainageSDDimService.GetDimOptimalArea(dimOutlineList, allIsolateLine, alreadyDimArea);
                    alreadyDimArea.Add(dimOutline);
                    var dimPt = dimOutline.StartPoint;

                    var visiValue = sDN + node.Dim.ToString();
                    if (node.Dim == 0)
                    {
                        visiValue = sDN + "15";
                    }

                    var thModel = new ThDrainageBlkOutput(dimPt);
                    thModel.Name = blk_name;
                    thModel.Dir = dir;
                    thModel.Visibility.Add(visiPropertyName, visiValue);
                    thModel.Scale = ThDrainageADCommon.Blk_scale_end;
                    thModel.Layer = ThDrainageADCommon.Layer_DIMS;

                    output.Add(thModel);
                }
            }

            if (alreadyDimArea.Count > 0)
            {
                //add last one for end-dim 
                var alreadyDimAreaLine = ThDrainageSDCommonService.GetLines(alreadyDimArea.Last());
                allIsolateLine.AddRange(alreadyDimAreaLine);
            }

            return output;

        }

        private static List<Point3d> PossiblePosition(Point3d baseLineS, Point3d baseLineE)
        {
            var dimPts = new List<Point3d>();

            //左边中点位
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 0));
            //右边中点位
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 3));
            //左边起点位
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 1));
            //右边起点位
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 2));
            //左边终点
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 4));
            //右边终点
            dimPts.Add(CalculatePositionDia(baseLineS, baseLineE, 5));

            return dimPts;
        }
        /// <summary>
        ///根据给定线和位置返回直径标记块的插入点
        ///0:线左，中点
        ///1:线左，起点
        ///2:线右，中点
        ///3:线右，起点
        ///4:线左，终点
        ///5:线右，终点
        /// </summary>
        /// <param name="baseLineS"></param>
        /// <param name="baseLineE"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static Point3d CalculatePositionDia(Point3d baseLineS, Point3d baseLineE, int position)
        {
            var dir = (baseLineE - baseLineS).GetNormal();
            var pt = new Point3d();
            double allMoveX = 0;
            double allMoveY = 0;
            var moveX = ThDrainageADCommon.DiameterDim_move_x;
            var moveY = ThDrainageADCommon.DiameterDim_move_y;
            var dimBlkX = ThDrainageADCommon.DiameterDim_blk_x;
            var dimBlkY = ThDrainageADCommon.DiameterDim_blk_y;

            if (position == 0)
            {
                pt = new Point3d((baseLineS.X + baseLineE.X) / 2, (baseLineS.Y + baseLineE.Y) / 2, 0);
                allMoveX = -dimBlkX / 2;
                allMoveY = moveY;
            }
            if (position == 1)
            {
                pt = baseLineS;
                allMoveX = moveX;
                allMoveY = moveY;
            }
            if (position == 2)
            {
                pt = new Point3d((baseLineS.X + baseLineE.X) / 2, (baseLineS.Y + baseLineE.Y) / 2, 0);
                allMoveX = -dimBlkX / 2;
                allMoveY = -dimBlkY - moveY;
            }
            if (position == 3)
            {
                pt = baseLineS;
                allMoveX = moveX;
                allMoveY = -dimBlkY - moveY;
            }
            if (position == 4)
            {
                pt = baseLineS;
                allMoveX = (baseLineE - baseLineS).Length - moveX - dimBlkX;
                allMoveY = moveY;
            }
            if (position == 5)
            {
                pt = baseLineS;
                allMoveX = (baseLineE - baseLineS).Length - moveX - dimBlkX;
                allMoveY = -dimBlkY - moveY;
            }

            var dimPt = pt + dir * allMoveX + allMoveY * dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);
            return dimPt;
        }

    }
}
