using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThOptimizeGroupService
    {
        /// <summary>
        /// 断环算法的优化，断开连接较少的两根线之间的所有连线，形成多个图
        /// 1、转换到正交坐标系
        /// 2、断开容差45mm以上的共线的线
        /// 3、生成共线的组
        /// 4、断开连接较少的线
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="printTag"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> GetSprinklerPtOptimizedNet(List<ThSprinklerNetGroup> netList, string printTag)
        {
            List<ThSprinklerNetGroup> transNetList = ThSprinklerNetGroupListService.ChangeToOrthogonalCoordinates(netList);
            ThSprinklerNetGroupListService.CorrectGraphConnection(ref transNetList, 45.0);

            // test
            //for (int i = 0; i < transNetList.Count; i++)
            //{
            //    var net = transNetList[i];
            //    List<Point3d> pts = ThCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-245mm-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}


            ThSprinklerNetGroupListService.GenerateCollineationGroup(ref transNetList);
            List<ThSprinklerNetGroup> opNetList = ThSprinklerNetGroupListService.CutOffLinesWithFewerConnections(transNetList);

            // test
            //for (int i = 0; i < opNetList.Count; i++)
            //{
            //    var net = opNetList[i];
            //    List<Point3d> pts = ThCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-3OpNet-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}

            return opNetList;
        }
    }
}
