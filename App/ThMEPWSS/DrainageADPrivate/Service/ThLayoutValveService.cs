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

using ThMEPWSS.DrainageSystemDiagram.Model;
using ThMEPWSS.DrainageSystemDiagram.Service;

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThLayoutValveService
    {
        public static List<ThDrainageSDADBlkOutput> LayoutValve(ThDrainageADPDataPass dataPass, List<ThDrainageTreeNode> rootList)
        {
            var valveList = new List<ThValve>();
            var valves = new List<ThValve>();
            var openingSign = new List<ThValve>();
            var casing = new List<ThValve>();

            valves.AddRange(dataPass.Valve);
            openingSign.AddRange(dataPass.OpeningSign);
            casing.AddRange(dataPass.Casing);

            valveList.AddRange(valves);
            valveList.AddRange(openingSign);
            valveList.AddRange(casing);

            CalculateValveTransPt(ref valveList, rootList);
            var valveOuput = new List<ThDrainageSDADBlkOutput>();
            valveOuput.AddRange(CreateValveOutputModel(valves));
            valveOuput.AddRange(CreateValveOutputModel(openingSign));
            valveOuput.AddRange(CreateCasingOutputModel(casing));

            return valveOuput;
        }


        private static void CalculateValveTransPt(ref List<ThValve> valveList, List<ThDrainageTreeNode> rootList)
        {
            var allNode = rootList.SelectMany(x => x.GetDescendant()).ToList();
            allNode.AddRange(rootList);

            var allLineDict = TurnNodeToZ0LineDict(allNode);

            for (int i = 0; i < valveList.Count(); i++)
            {
                var valve = valveList[i];
                FindCloseLine(valve, allLineDict, out var connectLineDict);
                var multiple = GetLineMultiple(valve, connectLineDict.Value);
                SetTransInsert(ref valve, connectLineDict, multiple);
            }
        }

        private static void FindCloseLine(ThValve valve, Dictionary<ThDrainageTreeNode, Line> allLineDict, out KeyValuePair<ThDrainageTreeNode, Line> connectLineDict)
        {
            var tol = new Tolerance(10, 10);
            var projValveInsertPt = new Point3d(valve.InsertPt.X, valve.InsertPt.Y, 0);
            var orderLines = allLineDict.OrderBy(x => x.Value.GetDistToPoint(projValveInsertPt, false)).ToList();
            connectLineDict = new KeyValuePair<ThDrainageTreeNode, Line>();
            
            foreach (var lineDict in orderLines)
            {
                if (lineDict.Value.StartPoint.IsEqualTo(lineDict.Value.EndPoint, tol))
                {
                    //忽略立管
                    continue;
                }
                var line = lineDict.Value;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var diamDir = valve.Dir.GetNormal();

                if (valve.Name == ThDrainageADCommon.BlkName_ShutoffValve ||
                    valve.Name == ThDrainageADCommon.BlkName_GateValve ||
                    valve.Name == ThDrainageADCommon.BlkName_CheckValve ||
                    valve.Name == ThDrainageADCommon.BlkName_AntifoulingCutoffValve 
                    )
                {
                    //本身和线路有90度角
                    diamDir = valve.Dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);
                }

                var angle = diamDir.GetAngleTo(lineDir, Vector3d.ZAxis);

                if (Math.Abs(Math.Cos(angle)) >= Math.Cos(1 * Math.PI / 180))
                {
                    connectLineDict = lineDict;
                    break;
                }
            }
        }

        private static double GetLineMultiple(ThValve valve, Line connectLine)
        {
            double multiple = 0.0;
            if (connectLine != null)
            {
                var sameDir = 1;
                
                var projValveInsertPt = new Point3d(valve.InsertPt.X, valve.InsertPt.Y, 0);
                var vs = projValveInsertPt - connectLine.StartPoint;
                var es = connectLine.EndPoint - connectLine.StartPoint;

                var angle = vs.GetAngleTo(es, Vector3d.ZAxis);
                if (Math.Cos(angle) < 0)
                {
                    sameDir = -1;
                }

                multiple = (vs.Length / es.Length) * sameDir;
            }

            return multiple;
        }

        private static void SetTransInsert(ref ThValve valve, KeyValuePair<ThDrainageTreeNode, Line> connectLineDict, double multiple)
        {
            var esTrans = connectLineDict.Key.TransPt - connectLineDict.Key.Parent.TransPt;
            var transInsertVect = esTrans * multiple;
            var transPt = connectLineDict.Key.Parent.TransPt + transInsertVect;
            valve.TransInsertPt = transPt;
            valve.ConnectNode = connectLineDict.Key;
        }

        private static Dictionary<ThDrainageTreeNode, Line> TurnNodeToZ0LineDict(List<ThDrainageTreeNode> allNode)
        {
            var allLineDict = new Dictionary<ThDrainageTreeNode, Line>();

            foreach (var node in allNode)
            {
                if (node.Parent != null)
                {
                    var line = new Line(new Point3d(node.Parent.Pt.X, node.Parent.Pt.Y, 0), new Point3d(node.Pt.X, node.Pt.Y, 0));
                    allLineDict.Add(node, line);
                }
            }
            return allLineDict;
        }

        private static List<ThDrainageSDADBlkOutput> CreateValveOutputModel(List<ThValve> valveList)
        {
            var output = new List<ThDrainageSDADBlkOutput>();
            foreach (var valve in valveList)
            {
                var oriNodeDir = (valve.ConnectNode.Pt - valve.ConnectNode.Parent.Pt).GetNormal();
                var transNodeDir = (valve.ConnectNode.TransPt - valve.ConnectNode.Parent.TransPt).GetNormal();
                var rotateAngle = oriNodeDir.GetAngleTo(transNodeDir, Vector3d.ZAxis);
                var printDir = valve.Dir.RotateBy(rotateAngle, Vector3d.ZAxis);

                var thModel = new ThDrainageSDADBlkOutput(valve.TransInsertPt);
                thModel.Name = valve.Name;
                thModel.Dir = printDir;
                thModel.Scale = ThDrainageADCommon.Blk_scale_end;

                output.Add(thModel);
            }
            return output;
        }

        //public static void CreateOpeningOutputModel(List<ThValve> valveList)
        //{
        //    var output = new List<ThDrainageSDADBlkOutput>();
        //    foreach (var valve in valveList)
        //    {
        //        var oriNodeDir = (valve.ConnectNode.Pt - valve.ConnectNode.Parent.Pt).GetNormal();
        //        var transNodeDir = (valve.ConnectNode.TransPt - valve.ConnectNode.Parent.TransPt).GetNormal();
        //        var rotateAngle = oriNodeDir.GetAngleTo(transNodeDir, Vector3d.ZAxis);
        //        var printDir = valve.Dir.RotateBy(rotateAngle, Vector3d.ZAxis);

        //        var thModel = new ThDrainageSDADBlkOutput(valve.TransInsertPt);
        //        thModel.Name = valve.Name;
        //        thModel.Dir = printDir;
        //        thModel.Scale = ThDrainageADCommon.Blk_scale_end;

        //        output.Add(thModel);
        //    }
        //    return output;
        //}

        private static List<ThDrainageSDADBlkOutput> CreateCasingOutputModel(List<ThValve> valveList)
        {
            var output = new List<ThDrainageSDADBlkOutput>();
            foreach (var valve in valveList)
            {

                var transNodeDir = (valve.ConnectNode.TransPt - valve.ConnectNode.Parent.TransPt).GetNormal();
                var visiDir = ThLayoutAngleValveService.CalculateVisibilityDir(transNodeDir, ThDrainageADCommon.BlkName_Casing_AD);
                
                var oriNodeDir = (valve.ConnectNode.Pt - valve.ConnectNode.Parent.Pt).GetNormal();
                var rotateAngle = oriNodeDir.GetAngleTo(transNodeDir, Vector3d.ZAxis);
                var printDir = valve.Dir.RotateBy(rotateAngle, Vector3d.ZAxis);

                var thModel = new ThDrainageSDADBlkOutput(valve.TransInsertPt);
                thModel.Name = ThDrainageADCommon.BlkName_Casing_AD;
                thModel.Dir = printDir;
                thModel.Scale = ThDrainageADCommon.Blk_scale_end;
                thModel.Visibility.Add(ThDrainageADCommon.VisiName_valve, visiDir);

                output.Add(thModel);
            }
            return output;
        }



    }
}
