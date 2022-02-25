using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.DrainageSystemDiagram;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerPiping.Engine
{
    class SprinklerTreeEngine
    {
        SprinklerPipingParameter parameter;
        double bestReward = -1;
        List<SprinklerTreeNode> bestNodes = new List<SprinklerTreeNode>();
        SprinklerTreeNode curNode;
        int cnt = 0;
        int iterCnt = 0;
        double lastExpPara = 250;

        public List<Line> SprinklerTreeSearch(SprinklerPipingParameter parameter)
        {
            this.parameter = parameter;
            List<Line> pipes = new List<Line>();
            SprinklerTreeState initState = new SprinklerTreeState(parameter.sprinklerPoints);
            SprinklerTreeNode root = new SprinklerTreeNode(initState, null, SprinklerTreeNode.nodeType.root);

            //车位
            SprinklerPipingEngine.ParkingPiping(parameter, root);
            //foreach(var par in parameter.sprinklerParkingRows)
            //{
            //    if (par.choices.Count != 0)
            //    {
            //        pipes.Add(new Line(par.ptColumn[0].pos, par.ptColumn[par.ptColumn.Count - 1].pos));
            //        foreach (var choice in par.choices)
            //        {
            //            pipes.Add(choice);
            //        }
            //    }
            //    //for (int i = 0; i < 4; i++)
            //    //{
            //    //    int nexti = i < 3 ? i + 1 : 0;
            //    //    pipes.Add(new Line(par.inParkingRow.GetPoint3dAt(i), par.inParkingRow.GetPoint3dAt(nexti)));
            //    //}
            //}
            //return pipes;

            //Random rand = new Random();
            //var choiceidx = rand.Next(root.state.choices.Count);
            //foreach (var pipe in root.state.choices[choiceidx].state.pipes)
            //{
            //    pipes.Add(pipe.pipe);
            //}
            //return pipes;
            //pipes = root.state.choices[choiceidx].state.pipes;

            //TODO:坡道塔楼小房间 以及其他细节
            //方向——长度
            //time or search limit
            //ExecuteRound
            //get best child
            //现有管道做垂直，根据垂足位置判断起点分叉数量（从起点引线数量）
            DateTime startTime = DateTime.Now;
            if (parameter.timeLimit != -1)
            {
                
                while((DateTime.Now - startTime).TotalSeconds < parameter.timeLimit)
                {
                    ExecuteRound(root);
                }
            }
            else if(parameter.iterLimit != -1)
            {
                for(int i=0; i<parameter.iterLimit || lastExpPara > 20; i++)
                {
                    iterCnt = i;
                    cnt = 0;
                    ExecuteRound(root);
                    //foreach (var pipe in curNode.state.pipes)
                    //{
                    //    pipes.Add(pipe.pipe);
                    //}
                    //DrawUtils.ShowGeometry(pipes, string.Format("l00new-pipes-{0}-{1}", i, bestReward), lineWeightNum: 50);
                    //pipes.Clear();
                    //break;
                    if ((DateTime.Now - startTime).TotalSeconds >= 1800)
                    {
                        break;
                    }

                    //if(cnt == 200)
                    //{
                    //    return pipes;
                    //}
                }
            }

            if (bestNodes.Count != 0 && bestNodes[bestNodes.Count - 1].state.pipes.Count != 0)
            {
                foreach (var pipe in bestNodes[bestNodes.Count - 1].state.pipes)
                {
                    pipes.Add(pipe.pipe);
                }
            }
            return pipes;
        }

        public void ExecuteRound(SprinklerTreeNode node)
        {
            SprinklerTreeNode newNode = SelectNode(node);
            double reward = -1;
            bool limitFlag = false;
            if(!newNode.isTerminal && newNode.type != SprinklerTreeNode.nodeType.connecting)
            {
                //SprinklerPipingEngine.GetConnectingChoices(parameter, newNode, false);
                SprinklerPipingEngine.GetStartPointChoices(parameter, newNode);
            }
            //TODO: 走不通的要剪枝 -> 每个都存下来以防下一次访问  -> 代价太大没有必要  -> 记忆化优化（存高频节点）
            //TODO: 或者直接快速走子 不存
            while (!newNode.state.isTerminal())
            {
                if (!newNode.isFullyExpanded)
                {
                    SprinklerPipingEngine.GetConnectingChoices(parameter, newNode);
                }

                if(newNode.state.choices.Count == 0)
                {
                    //剪枝
                    while (newNode.state.choices.Count == 0)
                    {
                        newNode.parent.state.choices.Remove(newNode);
                        newNode.parent.children.Remove(newNode);
                        newNode = newNode.parent;
                    }
                    //reward = 0;
                    //break;
                }
                else
                {
                    //快速走子
                    Random rand = new Random();
                    newNode = newNode.state.choices[rand.Next(newNode.state.choices.Count)];

                    //List<Line> pipes = new List<Line>();
                    //foreach(var pipe in newNode.state.pipes)
                    //{
                    //    pipes.Add(pipe.pipe);
                    //}
                    //cnt++;
                    //DrawUtils.ShowGeometry(pipes, string.Format("l00new-pipes-{0}-{1}", cnt++, newNode.state.pipes.Count));
                    //if (cnt == 200)
                    //    break;

                    //SprinklerTreeNode child = newNode.state.choices[newNode.children.Count];
                    //newNode.children.Add(child);
                    //newNode = child;

                    //assigned
                }
                cnt++;
                if(cnt == parameter.searchLimit)
                {
                    limitFlag = true;
                    break;
                }
                //break;
            }
            //TODO: rollout, reward
            //if(reward == -1)
            if (limitFlag)
            {
                reward = -1;
            }
            else
            {
                reward = 200 - newNode.len - newNode.turnCnt - newNode.wallCnt;
            }
            if(reward > bestReward)
            {
                bestNodes.Clear();
                bestNodes.Add(newNode);
                bestReward = reward;
            }
            else if(reward == bestReward && reward != -1)
            {
                bestNodes.Add(newNode);
            }
            curNode = newNode;
            BackPropagate(newNode, reward);
        }

        //public double Rollout(SprinklerTreeState state)
        //{

        //}

        public void BackPropagate(SprinklerTreeNode node, double reward)
        {
            while(node != null)
            {
                node.numVisits++;
                node.totalReward += reward;
                node.bestReward = reward > node.bestReward ? reward : node.bestReward;
                node = node.parent;
            }
        }

        public SprinklerTreeNode SelectNode(SprinklerTreeNode node)
        {
            while (!node.isTerminal)
            {
                if (node.children.Count != 0 && node.children.Count == node.state.choices.Count)
                {
                    //get best
                    node = GetBestChild(node);
                }
                else
                {
                    //expand
                    return Expand(node);
                }
            }
            return node;
        }

        public SprinklerTreeNode GetBestChild(SprinklerTreeNode node)
        {
            List<SprinklerTreeNode> bestChildren = new List<SprinklerTreeNode>();
            double bestVal = double.NegativeInfinity;
            double childRMin = node.children[0].bestReward + node.children[0].initWeight;
            double childRMax = node.children[0].bestReward + node.children[0].initWeight;
            Random rand = new Random();
            foreach (var child in node.children.Skip(1))
            {
                childRMax = child.bestReward + child.initWeight > childRMax ? child.bestReward + child.initWeight : childRMax;
                childRMin = child.bestReward + child.initWeight < childRMin ? child.bestReward + child.initWeight : childRMin;
            }
            if(childRMax == childRMin)
            {
                return node.children[rand.Next(node.children.Count)];
            }
            foreach (var child in node.children)
            {
                //TODO:也许不用UCB
                //double curVal = child.initWeight + child.totalReward / child.numVisits
                //    + parameter.explorationConstant * Math.Sqrt(2 * Math.Log(node.numVisits) / child.numVisits);
                double curReward = (child.bestReward + child.initWeight - childRMin) / (childRMax - childRMin);
                //double expPara = iterCnt < 200 ? 220 - iterCnt : 20;
                //double expPara = 200;
                //double expPara = parameter.iterLimit - iterCnt;
                //double expPara = 100;
                //if(iterCnt > 0)
                //{
                //    int iii = 0;
                //}
                double expPara;
                if(bestReward < 10)
                {
                    expPara = lastExpPara;
                }
                else
                {
                    expPara = lastExpPara > 20 ? lastExpPara - 1 : 20;
                    lastExpPara = expPara;
                }
                if(rand.Next(100) < 1)
                {
                    expPara = 250;
                }
                double curVal = child.initWeight + child.bestReward + parameter.explorationConstant * expPara * Math.Sqrt(2 * Math.Log(node.numVisits) / child.numVisits); 
                if (curVal > bestVal)
                {
                    bestChildren.Clear();
                    bestChildren.Add(child);
                    bestVal = curVal;
                } else if (curVal == bestVal)
                {
                    bestChildren.Add(child);
                }
            }
            return bestChildren[rand.Next(bestChildren.Count)];
        }

        public SprinklerTreeNode Expand(SprinklerTreeNode node)
        {
            if (!node.isFullyExpanded)
            {
                SprinklerPipingEngine.GetConnectingChoices(parameter, node);

            }
            if (node.isTerminal)
            {
                return node;
            }
            SprinklerTreeNode newNode = node.state.choices[node.children.Count];
            node.children.Add(newNode);
            return newNode;
        }


    }
}
