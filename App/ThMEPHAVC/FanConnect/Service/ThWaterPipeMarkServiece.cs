using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThWaterPipeMarkServiece
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        //冷热水管标记
        public void CoolHotMark(ThFanTreeNode<ThFanPointModel> node, string coolPipe,string hotPipe, string strMarkHeight)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            //标记冷热水管
            if (!node.Item.IsCoolHotMarked)
            {
                var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
                string blockName = "";
                List<string> property = new List<string>();
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    markPt = markPt + direct * 200 * 1;
                    blockName = "AI-水管多排标注(2排)";
                    string strchs = "CHS " + coolPipe + strMarkHeight;
                    string strchr = "CHR " + hotPipe + strMarkHeight;
                    property.Add(strchs);
                    property.Add(strchr);
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    markPt = markPt + direct * 200 * 2;
                    blockName = "AI-水管多排标注(4排)";
                    string strcs = "CS " + coolPipe + strMarkHeight;
                    string strcr = "CR " + coolPipe + strMarkHeight;
                    string strhs = "HS " + hotPipe + strMarkHeight;
                    string strhr = "HR " + hotPipe + strMarkHeight;
                    property.Add(strcs);
                    property.Add(strcr);
                    property.Add(strhs);
                    property.Add(strhr);
                }
                toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
                node.Item.IsCoolHotMarked = true;
            }
        }
        //冷凝水管标记
        public void CondMark(ThFanTreeNode<ThFanPointModel> node, string condPipe)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            if (node.Item.IsCondMarked != true)
            {
                var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                var direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                if(ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
                {
                    if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)//两管制
                    {
                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                        {
                            markPt = markPt + direct * (200 * 1 + 120);
                        }
                        else
                        {
                            markPt = markPt + direct * 120;
                        }
                    }
                    else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)//四管制
                    {
                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                        {
                            markPt = markPt + direct * (200 * 2 + 120);
                        }
                        else
                        {
                            markPt = markPt + direct * 120;
                        }
                    }
                }
                else if(ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt + direct * (200 * 1 + 120);
                    }
                    else
                    {
                        markPt = markPt + direct * 120;
                    }
                }
                var strText = "C " + condPipe;
                toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                node.Item.IsCondMarked = true;
            }
        }
        public void NodeMark(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMark(node, curCoolPipe, curHotPipe, strMarkHeight);
                //标记冷凝水管
                CondMark(node, curCondPipe);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                CondMark(node, curCondPipe);
            }

        }
        public void PipeMark(ThPointTreeModel tree)
        {
            if(tree.RootNode.Children.Count == 0)
            {
                return;
            }
            NodeMark(tree.RootNode.Children.First());
            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if (node.Parent == null)
            {
                return;
            }
            if(node.Children.Count <= 1)
            {
                return;
            }
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            //父结点冷水管管径
            var parentCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.CoolFlow);
            //父结点热水管管径
            var parentHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.HotFlow);
            //父结点冷凝水管管径
            var parentCondPipe = queryDNServiece.QueryCondPipeDN(node.Parent.Item.CoolCapa);
            //当前结点冷水管管径
            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        CoolHotMark(node.Parent, parentCoolPipe, parentHotPipe, strMarkHeight);
                    }
                    CoolHotMark(node, curCoolPipe, curHotPipe, strMarkHeight);
                }
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, parentCondPipe);
                    }
                    CondMark(node, curCondPipe);
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, parentCondPipe);
                    }
                    CondMark(node, curCondPipe);
                }
            }
        }
        public void UpdateMark(ThPointTreeModel tree,List<Entity> marks)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            NodeMark(tree.RootNode.Children.First(),ref marks);
            //遍历树
            BianLiTree(tree.RootNode,ref marks);
        }
        public void NodeMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
                //标记冷凝水管
                CondMark(node, ref marks, curCondPipe);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                CondMark(node, ref marks, curCondPipe);
            }
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child ,ref marks);
            }
            if (node.Parent == null)
            {
                return;
            }
            if (node.Children.Count <= 1)
            {
                return;
            }
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            //父结点冷水管管径
            var parentCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.CoolFlow);
            //父结点热水管管径
            var parentHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.HotFlow);
            //父结点冷凝水管管径
            var parentCondPipe = queryDNServiece.QueryCondPipeDN(node.Parent.Item.CoolCapa); 
            //当前结点冷水管管径
            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        CoolHotMark(node.Parent,ref marks, parentCoolPipe, parentHotPipe, strMarkHeight);
                    }
                    CoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
                }
                else
                {
                    if (node.Parent.Parent != null)
                    {
                        RemoveCoolHotMark(node.Parent, ref marks);
                    }
                    RemoveCoolHotMark(node, ref marks);
                }
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    CondMark(node, ref marks, curCondPipe);
                }
                else
                {
                    //判断是否有mark
                    //如果有，删除,否则不处理
                    if (node.Parent.Parent != null)
                    {
                        RemoveCondMark(node.Parent, ref marks);
                    }
                    RemoveCondMark(node, ref marks);
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    CondMark(node, ref marks, curCondPipe);
                }
                else
                {
                    //判断是否有mark
                    //如果有，删除,否则不处理
                    if (node.Parent.Parent != null)
                    {
                        RemoveCondMark(node.Parent, ref marks);
                    }
                    RemoveCondMark(node, ref marks);
                }
            }
        }
        public void CoolHotMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks, string coolPipe, string hotPipe, string strMarkHeight)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //判断该结点是否有mark
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var coolHotMarkes = FindMarkFromLine(line,ref marks);
            if (coolHotMarkes.Count > 0)//如果有，判断是否可以使用，如果不能使用删除，重新生成
            {
                var firstMark = coolHotMarkes.First();
                coolHotMarkes.Remove(firstMark);
                UpdateMark(node,firstMark as BlockReference, coolPipe, hotPipe, strMarkHeight);
                foreach (var mark in coolHotMarkes)
                {
                    mark.UpgradeOpen();
                    mark.Erase();
                    mark.DowngradeOpen();
                }
            }
            else//如果没有，新生成一个
            {
                CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
            }
            node.Item.IsCoolHotMarked = true;
        }
        public void CondMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks, string condPipe)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //判断该结点是否有text
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var condMarkes = FindTextFromLine(line,ref marks);
            if (condMarkes.Count > 0)//如果有，判断是否可以使用，如果不能使用删除，重新生成
            {
                var firstMark = condMarkes.First();
                condMarkes.Remove(firstMark);
                UpdateText(firstMark as DBText, condPipe);
                foreach (var mark in condMarkes)
                {
                    mark.UpgradeOpen();
                    mark.Erase();
                    mark.DowngradeOpen();
                }
            }
            else//如果没有，新生成一个
            {
                CondMark(node, condPipe);
            }
            node.Item.IsCondMarked = true;
        }
        public void RemoveCoolHotMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var coolHotMarkes = FindMarkFromLine(line,ref marks);
            foreach (var mark in coolHotMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }
        public void RemoveCondMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var condMarkes = FindTextFromLine(line,ref marks);
            foreach (var mark in condMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }
        public List<Entity> FindMarkFromLine(Line line ,ref List<Entity> marks)
        {
            var box = line.Buffer(450);
            var retMark = new List<Entity>();
            foreach(var mark in marks)
            {
                if(mark is BlockReference)
                {
                    var blk = mark as BlockReference;
                    if(box.Contains(blk.Position))
                    {
                        if (blk.GetEffectiveName().Contains("AI-水管多排标注"))
                        {
                            retMark.Add(mark);
                        }
                    }
                }
            }
            marks = marks.Except(retMark).ToList();
            return retMark;
        }
        public List<Entity> FindTextFromLine(Line line,ref List<Entity> marks)
        {
            var box = line.Buffer(600);
            var retText = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is DBText)
                {
                    var text = mark as DBText;
                    if (box.Contains(text.Position))
                    {
                        retText.Add(mark);
                    }
                }
            }
            marks = marks.Except(retText).ToList();
            return retText;
        }
        public void UpdateMark(ThFanTreeNode<ThFanPointModel> node, BlockReference blk, string coolPipe, string hotPipe, string strMarkHeight)
        {
            var effName = blk.GetEffectiveName();
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                if(effName == "AI-水管多排标注(2排)")
                {
                    string strchs = "CHS " + coolPipe + strMarkHeight;
                    string strchr = "CHR " + hotPipe + strMarkHeight;
                    Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                    attNameValues.Add("水管标注1", strchs);
                    attNameValues.Add("水管标注2", strchr);
                    blk.ObjectId.UpdateAttributesInBlock(attNameValues);
                }
                else
                {
                    blk.UpgradeOpen();
                    blk.Erase();
                    blk.DowngradeOpen();
                    CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                if (effName == "AI-水管多排标注(4排)")
                {
                    string strcs = "CS " + coolPipe + strMarkHeight;
                    string strcr = "CR " + coolPipe + strMarkHeight;
                    string strhs = "HS " + hotPipe + strMarkHeight;
                    string strhr = "HR " + hotPipe + strMarkHeight;
                    Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                    attNameValues.Add("水管标注1", strcs);
                    attNameValues.Add("水管标注2", strcr);
                    attNameValues.Add("水管标注3", strhs);
                    attNameValues.Add("水管标注4", strhr);
                    blk.ObjectId.UpdateAttributesInBlock(attNameValues);
                }
                else
                {
                    blk.UpgradeOpen();
                    blk.Erase();
                    blk.DowngradeOpen();
                    CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
                }
            }
        }
        public void UpdateText(DBText text, string condPipe)
        {
            var strText = "C " + condPipe;
            text.UpgradeOpen();
            text.TextString = strText;
            text.DowngradeOpen();
        }
    }
}
