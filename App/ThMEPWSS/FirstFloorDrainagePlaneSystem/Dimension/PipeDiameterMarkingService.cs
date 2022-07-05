﻿using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Print;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Dimension
{
    public class PipeDiameterMarkingService
    {
        public double scale = 1;
        double startDis = 1100;
        double moveDis = 200;
        List<RouteModel> routes;
        ThMEPOriginTransformer originTransformer;
        public PipeDiameterMarkingService(List<RouteModel> _routes, ThMEPOriginTransformer _originTransformer)
        {
            routes = _routes.Where(x => !x.IsBranchPipe).ToList();
            originTransformer = _originTransformer;
        }

        public void CreateDim()
        {
            var classifyRoutes = routes.GroupBy(x => x.verticalPipeType).ToList();
            foreach (var cRoutes in classifyRoutes)
            {
                var layoutInfo = CalLayoutInfo(cRoutes.ToList());
                Print(cRoutes.Key, layoutInfo);
            }
        }

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="cRoutes"></param>
        /// <returns></returns>
        private List<KeyValuePair<Point3d, Vector3d>> CalLayoutInfo(List<RouteModel> cRoutes)
        {
            var layoutInfo = new List<KeyValuePair<Point3d, Vector3d>>();
            foreach (var route in cRoutes)
            {
                var routeLine = route.route;
                var lastPt = routeLine.GetPoint3dAt(routeLine.NumberOfVertices - 1);
                var secPt = routeLine.GetPoint3dAt(routeLine.NumberOfVertices - 2);
                var dir = (secPt - lastPt).GetNormal();
                var moveDir = Vector3d.ZAxis.CrossProduct(dir);
                var startDisTemp = startDis;
                var angle = dir.GetAngleTo(Vector3d.YAxis);             //和y轴10°以内认为是竖直线，竖直线标注在左侧，横线标注在上侧
                if (angle > Math.PI)
                {
                    angle = angle - Math.PI;
                }
                if (angle > Math.PI / 2)
                {
                    angle = Math.PI - angle;
                }
                if (angle < 10.0 / 180.0 * Math.PI)
                {
                    if (dir.Y > 0)
                    {
                        moveDir = -moveDir;
                        startDisTemp = 0;
                    }
                }
                else
                {
                    if (dir.X > 0)
                    {
                        moveDir = -moveDir;
                        startDisTemp = 0;
                    }
                }
                var sPt = lastPt + dir * startDisTemp;// + moveDir * moveDis;
                if (route.verticalPipeType != VerticalPipeType.CondensatePipe)
                {
                    sPt = originTransformer.Reset(sPt);
                }
                layoutInfo.Add(new KeyValuePair<Point3d, Vector3d>(sPt, -moveDir));
            }

            return layoutInfo;
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="type"></param>
        /// <param name="layoutInfos"></param>
        private void Print(VerticalPipeType type, List<KeyValuePair<Point3d, Vector3d>> layoutInfos)
        {
            string layerName = "";
            string attriName = "";
            switch (type)
            {
                case VerticalPipeType.ConfluencePipe:
                case VerticalPipeType.SewagePipe:
                    attriName = "De110";
                    layerName = ThWSSCommon.DraiDimsLayerName;
                    break;
                case VerticalPipeType.WasteWaterPipe:
                    attriName = "De110";
                    layerName = ThWSSCommon.DraiDimsLayerName;
                    break;
                case VerticalPipeType.CondensatePipe:
                case VerticalPipeType.rainPipe:
                    attriName = "De110";
                    layerName = ThWSSCommon.RainDimsLayerName;
                    break;
                default:
                    break;
            }
            var attri = new Dictionary<string, object>() { { "可见性", attriName } };
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertBlock(layoutInfos, layerName, ThWSSCommon.DimsBlockName, attri, true);
        }
    }
}
