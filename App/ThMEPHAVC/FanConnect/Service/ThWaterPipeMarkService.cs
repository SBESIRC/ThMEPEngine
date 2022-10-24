using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.Service;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;
using ThMEPHVAC.FanLayout.Service;


namespace ThMEPHVAC.FanConnect.Service
{
    public class ThWaterPipeMarkService
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        
        /// <summary>
        /// 创建标记
        /// </summary>
        /// <param name="root"></param>
        /// <param name="pipeTreeNodes"></param>
        public void CreateMark(ThFanTreeNode<ThFanPointModelNew> root, List<ThFanTreeNode<ThFanPipeModel>> pipeTreeNodes)
        {
            GetMarkPosition(root, out var coolHotNodes, out var condNodes, out var acNodes);
            //string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";

            var node2 = pipeTreeNodes.Where(x => x.Item.IsFlag == true).ToList();

            foreach (var node in coolHotNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }
                if (node.Item.IsFlag == -1)
                {
                    node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                }
                CreateCoolHotMark(node);

            }

            foreach (var node in condNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }
                if (node.Item.IsFlag == -1)
                {
                    var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                    node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                }
                CreateCondMark(node);
            }

            foreach (var node in acNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }
                if (node.Item.IsFlag == -1)
                {
                    var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                    node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                }
                CreateACMark(node);
            }
        }

        /// <summary>
        /// 根据ui需求找变径的node点
        /// </summary>
        /// <param name="root"></param>
        /// <param name="coolHotNodes"></param>
        /// <param name="condNodes"></param>
        private void GetMarkPosition(ThFanTreeNode<ThFanPointModelNew> root, out List<ThFanTreeNode<ThFanPointModelNew>> coolHotNodes, out List<ThFanTreeNode<ThFanPointModelNew>> condNodes, out List<ThFanTreeNode<ThFanPointModelNew>> acNodes)
        {
            var allNodes = root.GetDecendent();
            coolHotNodes = new List<ThFanTreeNode<ThFanPointModelNew>>();
            condNodes = new List<ThFanTreeNode<ThFanPointModelNew>>();
            acNodes = new List<ThFanTreeNode<ThFanPointModelNew>>();

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    //两管
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        coolHotNodes.AddRange(allNodes.Where(x => x.Item.IsCoolHotMaxChangeMark || x.Item.IsLevelChangeMark));
                    }

                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    //四管
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        coolHotNodes.AddRange(allNodes.Where(x => x.Item.IsCoolHotChangeMark || x.Item.IsLevelChangeMark));
                    }
                }

            }

            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                condNodes.AddRange(allNodes.Where(x => x.Item.IsCapaChangeMarked || x.Item.IsLevelChangeMark));
            }

            //冷媒管
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)
            {
                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe &&
                    ConfigInfo.WaterSystemConfigInfo.IsACPipeDim)
                {
                    acNodes.AddRange(allNodes.Where(x => x.Item.IsACChangeMarked || x.Item.IsLevelChangeMark));
                }
            }

        }

        /// <summary>
        /// 更新标记
        /// </summary>
        /// <param name="root"></param>
        /// <param name="pipeTreeNodes"></param>
        /// <param name="mark"></param>
        public void UpdateMark(ThFanTreeNode<ThFanPointModelNew> root, List<ThFanTreeNode<ThFanPipeModel>> pipeTreeNodes, List<Entity> mark)
        {
            // string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";
            var updateMark = new List<Entity>();

            BuildMarkIndex(mark, out var markIndex, out var markBoundaryDict);
            var nodeMarkDict = GetNearTreeMarks(root, markIndex, markBoundaryDict);
            GetMarkPosition(root, out var coolHotNodes, out var condNodes, out var acNodes);

            foreach (var node in coolHotNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }

                var isNearMark = CheckNearMark(node, nodeMarkDict, 0, out var nearMark);
                if (isNearMark)
                {
                    updateMark.Add(nearMark);
                    UpdateCoolHotMark(node, nearMark as BlockReference);
                }
                else
                {
                    if (node.Item.IsFlag == -1)
                    {
                        var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                        node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                    }
                    CreateCoolHotMark(node);
                }
            }

            foreach (var node in condNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }

                var isNearMark = CheckNearMark(node, nodeMarkDict, 1, out var nearMark);
                if (isNearMark)
                {
                    updateMark.Add(nearMark);
                    UpdateCondMark(node, nearMark as DBText);
                }
                else
                {
                    if (node.Item.IsFlag == -1)
                    {
                        var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                        node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                    }
                    CreateCondMark(node);

                }
            }


            foreach (var node in acNodes)
            {
                if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    continue;
                }
                if (node.Parent == null)
                {
                    continue;
                }

                var isNearMark = CheckNearMark(node, nodeMarkDict, 2, out var nearMark);
                if (isNearMark)
                {
                    updateMark.Add(nearMark);
                    UpdateACMark(node, nearMark as DBText);
                }
                else
                {
                    if (node.Item.IsFlag == -1)
                    {
                        var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                        node.Item.IsFlag = CheckNodeFlag(node, pipeTreeNodes);
                    }
                    CreateACMark(node);
                }
            }


            var nearAllNodeMark = nodeMarkDict.SelectMany(x => x.Value);
            var removeMark = nearAllNodeMark.Except(updateMark).ToList();

            RemoveMark(removeMark);

        }

        private static void RemoveMark(List<Entity> marks)
        {
            foreach (var mark in marks)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }

        /// <summary>
        /// 找垂直在线附近的块标注
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodeMarkDict"></param>
        /// <param name="pipeType">0:垂直 blockReference 冷热水 1:平行 DBText 冷凝 2:平行 DBText 冷媒</param>
        /// <param name="nearMark"></param>
        /// <returns></returns>
        private bool CheckNearMark(ThFanTreeNode<ThFanPointModelNew> node, Dictionary<ThFanTreeNode<ThFanPointModelNew>, List<Entity>> nodeMarkDict, int pipeType, out Entity nearMark)
        {
            var breturn = false;
            nearMark = null;

            if (node.Parent != null && nodeMarkDict.TryGetValue(node, out var nearMarkList))
            {
                var pipeL = new Line(node.Parent.Item.BasePt, node.Item.BasePt);
                var minDist = double.MaxValue;

                foreach (var mark in nearMarkList)
                {
                    //冷热水 找出平行的mark
                    if (pipeType == 0 && mark is BlockReference br)
                    {
                        var angleD = br.Rotation - pipeL.Angle;

                        if (Math.Abs(Math.Cos(angleD)) >= Math.Cos(1 * Math.PI / 180))
                        {
                            var isSameBlock = IsNearMarkSameBlk(br);
                            if (isSameBlock)
                            {
                                //找出最近的
                                var midPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                                var dist = br.Position.DistanceTo(midPt);
                                if (dist <= minDist)
                                {
                                    nearMark = br;
                                    minDist = dist;
                                    breturn = true;
                                }
                            }
                        }
                    }
                    if ((pipeType == 1 || pipeType == 2) && mark is DBText dt)
                    {
                        var dtIsCondMark = ThEquipElementExtractService.IsCondMark(dt.TextString); //冷凝
                        var dtIsACMark = ThEquipElementExtractService.IsACMark(dt.TextString); //冷媒

                        if (pipeType == 1 && dtIsCondMark == false)
                        {
                            //匹配冷凝
                            continue;
                        }
                        if (pipeType == 2 && dtIsACMark == false)
                        {
                            //冷媒找到冷凝跳出
                            continue;
                        }

                        var angleD = dt.Rotation - pipeL.Angle;
                        if (Math.Abs(Math.Cos(angleD)) >= Math.Cos(1 * Math.PI / 180))
                        {
                            //找出最近的
                            var midPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
                            var dist = dt.Position.DistanceTo(midPt);
                            if (dist <= minDist)
                            {
                                nearMark = dt;
                                minDist = dist;
                                breturn = true;
                            }
                        }
                    }
                }
            }
            return breturn;

        }

        private static void BuildMarkIndex(List<Entity> mark, out ThCADCoreNTSSpatialIndex markIndex, out Dictionary<Polyline, Entity> markBoundaryDict)
        {
            markBoundaryDict = new Dictionary<Polyline, Entity>();
            var markObj = new DBObjectCollection();
            foreach (var m in mark)
            {
                Polyline pl = null;
                if (m is BlockReference br)
                {
                    pl = ThGeomUtil.GetVisibleOBB(br);
                }
                if (m is DBText dt)
                {
                    pl = dt.GeometricExtents.ToRectangle();
                }
                ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(pl, "l0mark", colorIndex: 4);
                markBoundaryDict.Add(pl, m);
                markObj.Add(pl);
            }
            markIndex = new ThCADCoreNTSSpatialIndex(markObj);

        }

        /// <summary>
        /// 找树附近的标注位
        /// </summary>
        /// <param name="root"></param>
        /// <param name="markIndex"></param>
        /// <param name="markBoundaryDict"></param>
        /// <returns></returns>
        private static Dictionary<ThFanTreeNode<ThFanPointModelNew>, List<Entity>> GetNearTreeMarks(ThFanTreeNode<ThFanPointModelNew> root, ThCADCoreNTSSpatialIndex markIndex, Dictionary<Polyline, Entity> markBoundaryDict)
        {
            var nodeMarkDict = new Dictionary<ThFanTreeNode<ThFanPointModelNew>, List<Entity>>();
            var allNodes = root.GetDecendent();

            foreach (var node in allNodes)
            {
                if (node.Parent != null)
                {
                    nodeMarkDict.Add(node, new List<Entity>());
                    var pipeL = new Line(node.Parent.Item.BasePt, node.Item.BasePt);
                    var pipeLBuffer = pipeL.Buffer(ThFanConnectCommon.MoveLength * 2);
                    var inBufferM = markIndex.SelectCrossingPolygon(pipeLBuffer).OfType<Polyline>();
                    ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(pipeLBuffer, "l0linebuffer");

                    foreach (var mark in inBufferM)
                    {
                        if (markBoundaryDict.TryGetValue(mark, out var m))
                        {
                            nodeMarkDict[node].Add(m);
                        }
                    }
                }
            }

            return nodeMarkDict;
        }

        private bool IsNearMarkSameBlk(BlockReference nearMark)
        {
            var isSame = false;
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0 && nearMark.GetEffectiveName().Contains(ThFanConnectCommon.BlkName_PipeDim2))
            {
                isSame = true;
            }
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1 && nearMark.GetEffectiveName().Contains(ThFanConnectCommon.BlkName_PipeDim4))
            {
                isSame = true;
            }

            return isSame;
        }

        /// <summary>
        /// 根据管线树的翻转匹配标注树的翻转
        /// 翻转决定管线在中心路由左还是右也决定了标注的方向
        /// </summary>
        /// <param name="pointTreeNode"></param>
        /// <param name="pipeTreeNodes"></param>
        /// <returns></returns>
        private static int CheckNodeFlag(ThFanTreeNode<ThFanPointModelNew> pointTreeNode, List<ThFanTreeNode<ThFanPipeModel>> pipeTreeNodes)
        {
            var flag = 0;

            var markPt = pointTreeNode.Item.BasePt.GetMidPt(pointTreeNode.Parent.Item.BasePt);
            var markPtDir = (pointTreeNode.Parent.Item.BasePt - pointTreeNode.Item.BasePt).GetNormal();

            var selectpipeNode = pipeTreeNodes.Where(x => x.Item.PLine.GetClosestPointTo(markPt, false).DistanceTo(markPt) < ThFanConnectCommon.Tol_SamePoint);
            if (selectpipeNode.Count() > 0)
            {
                var pipeNode = selectpipeNode.First().Item;
                var pipeNodeDir = (pipeNode.PLine.EndPoint - pipeNode.PLine.StartPoint).GetNormal();

                var angle = markPtDir.GetAngleTo(pipeNodeDir);//找当前树和管线树夹角=>确定当前树供水管在左边（0）还是右边（1）
                if (Math.Cos(angle) > 0)
                {
                    //0 度附近
                    flag = selectpipeNode.First().Item.IsFlag == true ? 1 : 0;
                }
                else
                {
                    //180度附近
                    flag = selectpipeNode.First().Item.IsFlag == true ? 0 : 1;
                }
            }

            return flag;
        }

        private void CreateCoolHotMark(ThFanTreeNode<ThFanPointModelNew> node)//冷热水管标记
        {
            //标记冷热水管
            var isMoveAndNotReverse = true;//0(左）move 1(右） not move， 需要+180度时：0（左）not move 1（右）move
            var dir = (node.Parent.Item.BasePt - node.Item.BasePt).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(dir);
            var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);

            if (node.Item.IsFlag == 1)
            {
                isMoveAndNotReverse = false;
            }

            //根据角度调整标签角度
            var tmpPt1 = node.Parent.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = (tmpPt1 - tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);

            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                //90~270度之间
                markAg = markAg + Math.PI;
                isMoveAndNotReverse = isMoveAndNotReverse == true ? false : true; //需要 + 180度时：0（左）not move 1（右）move
            }

            //标注基点平移方向
            var moveDir = dir.RotateBy(Math.PI / 2, Vector3d.ZAxis); //默认左边
            if (node.Item.IsFlag == 1)
            {
                moveDir = -moveDir;
            }

            string blockName = "";
            string strMarkHeight = "";
            List<string> property = new List<string>();
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                if (node.Item.IsFirstPart)
                {
                    blockName = ThFanConnectCommon.BlkName_PipeDim2;
                    strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";
                }
                else
                {
                    blockName = ThFanConnectCommon.BlkName_PipeDim2_NoH;
                }

                var dim = node.Item.CoolDim >= node.Item.HotDim ? node.Item.CoolDim : node.Item.HotDim;
                string strchs = "CHS " + "DN" + dim + strMarkHeight;
                string strchr = "CHR " + "DN" + dim + strMarkHeight;
                property.Add(strchs);
                property.Add(strchr);

                if (isMoveAndNotReverse)
                {
                    markPt = markPt + moveDir * ThFanConnectCommon.MoveLength * 1;
                }
                else
                {
                    property.Reverse();
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                if (node.Item.IsFirstPart)
                {
                    blockName = ThFanConnectCommon.BlkName_PipeDim4;
                    strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";
                }
                else
                {
                    blockName = ThFanConnectCommon.BlkName_PipeDim4_NoH;
                }

                string strcs = "CS " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strcr = "CR " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strhs = "HS " + "DN" + node.Item.HotDim + strMarkHeight;
                string strhr = "HR " + "DN" + node.Item.HotDim + strMarkHeight;
                property.Add(strcs);
                property.Add(strcr);
                property.Add(strhs);
                property.Add(strhr);

                if (isMoveAndNotReverse)
                {
                    markPt = markPt + moveDir * ThFanConnectCommon.MoveLength * 2;
                }
                else
                {
                    markPt = markPt - moveDir * ThFanConnectCommon.MoveLength * 1;
                    property.Reverse();
                }
            }

            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property, ThFanConnectCommon.MoveLength, (!isMoveAndNotReverse));
        }

        private void CreateCondMark(ThFanTreeNode<ThFanPointModelNew> node)//冷凝水管标记
        {
            var dir = (node.Parent.Item.BasePt - node.Item.BasePt).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(dir);
            var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);

            var moveDir = dir.RotateBy(Math.PI / 2, Vector3d.ZAxis); //默认左边
            if (node.Item.IsFlag == 0) //冷凝往右
            {
                moveDir = -moveDir;
            }

            //根据角度调整标签角度
            var tmpPt1 = node.Parent.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = (tmpPt1 - tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg + Math.PI;
            }

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)//两管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt + moveDir * (ThFanConnectCommon.MoveLength * 1 + 140);
                    }
                    else
                    {
                        markPt = markPt + moveDir * 140;
                    }
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)//四管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt + moveDir * (ThFanConnectCommon.MoveLength * 2 + 140);
                    }
                    else
                    {
                        markPt = markPt + moveDir * 140;
                    }
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    markPt = markPt + moveDir * (ThFanConnectCommon.MoveLength * 1 + 140);
                }
                else
                {
                    markPt = markPt + moveDir * 140;
                }
            }

            var strText = "C " + "DN" + node.Item.CoolCapaDim;
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
        }

        private void CreateACMark(ThFanTreeNode<ThFanPointModelNew> node)//冷媒水管标记
        {
            var dir = (node.Parent.Item.BasePt - node.Item.BasePt).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(dir);
            var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);

            var moveDir = dir.RotateBy(Math.PI / 2, Vector3d.ZAxis); //默认左边
            if (node.Item.IsFlag == 1) //冷媒往左
            {
                moveDir = -moveDir;
            }

            //根据角度调整标签角度
            var tmpPt1 = node.Parent.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = (tmpPt1 - tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg + Math.PI;
            }

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    markPt = markPt + moveDir * (ThFanConnectCommon.MoveLength * 1 + 140);
                }
            }

            var strText = string.Format("%%C{0}/%%C{1}", node.Item.ACPipeDim.Item1, node.Item.ACPipeDim.Item2);
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);

        }

        private void UpdateCoolHotMark(ThFanTreeNode<ThFanPointModelNew> node, BlockReference mark)
        {
            //方向问题
            List<string> property = new List<string>();
            string strMarkHeight = "";

            if (node.Item.IsFirstPart)
            {
                strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";
            }

            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                var dim = node.Item.CoolDim >= node.Item.HotDim ? node.Item.CoolDim : node.Item.HotDim;

                string strchs = "CHS " + "DN" + dim + strMarkHeight;
                string strchr = "CHR " + "DN" + dim + strMarkHeight;
                property.Add(strchs);
                property.Add(strchr);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                string strcs = "CS " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strcr = "CR " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strhs = "HS " + "DN" + node.Item.HotDim + strMarkHeight;
                string strhr = "HR " + "DN" + node.Item.HotDim + strMarkHeight;
                property.Add(strcs);
                property.Add(strcr);
                property.Add(strhs);
                property.Add(strhr);
            }

            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            for (int i = 0; i < property.Count; i++)
            {
                string strKey = "水管标注" + (i + 1).ToString();
                attNameValues.Add(strKey, property[i]);
            }

            mark.ObjectId.UpdateAttributesInBlock(attNameValues);

        }

        private static void UpdateCondMark(ThFanTreeNode<ThFanPointModelNew> node, DBText mark)
        {
            var strText = "C " + "DN" + node.Item.CoolCapaDim;
            mark.UpgradeOpen();
            mark.TextString = strText;
            mark.DowngradeOpen();
        }

        private static void UpdateACMark(ThFanTreeNode<ThFanPointModelNew> node, DBText mark)
        {
            var strText = string.Format("%%C{0}/%%C{1}", node.Item.ACPipeDim.Item1, node.Item.ACPipeDim.Item2);
            mark.UpgradeOpen();
            mark.TextString = strText;
            mark.DowngradeOpen();
        }
    }
}
