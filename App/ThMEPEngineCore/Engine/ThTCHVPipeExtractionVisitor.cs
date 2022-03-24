﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHVPipeExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (CheckLayerValid(dbObj) && dbObj.IsTCHPipe() && IsVerticalPipe(dbObj))
            {
                var geom = HandleTCHVPipe(dbObj);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (CheckLayerValid(dbObj) && dbObj.IsTCHPipe() && IsVerticalPipe(dbObj))
            {
                var geom = HandleTCHVPipe(dbObj);
                geom.TransformBy(matrix);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }

        public override bool IsFlowSegment(Entity e)
        {
            return e.IsTCHPipe();
        }
        public override bool CheckLayerValid(Entity e)
        {
            var bReturn = false;
            if (LayerFilter.Count > 0)
            {
                bReturn = LayerFilter.Contains(e.Layer);
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }

        public override bool IsFlowSegmentBlock(BlockTableRecord blockTableRecord)
        {
            //忽略外参
            if (blockTableRecord.IsFromExternalReference)
            {
                return false;
            }

            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }


        private bool IsVerticalPipe(Entity dbObj)
        {
            var bIsVP = false;
            var pipeParameters = ThOPMTools.GetOPMProperties(dbObj.Id);

            if (pipeParameters.ContainsKey("起点标高") && pipeParameters.ContainsKey("终点标高") && pipeParameters.ContainsKey("管长"))
            {
                var start = Convert.ToDouble(pipeParameters["起点标高"]);
                var end = Convert.ToDouble(pipeParameters["终点标高"]);
                var length = Convert.ToDouble(pipeParameters["管长"]);

                //有坡度的管子
                var verticalDiff = Math.Abs(end - start);
                if (verticalDiff > 0)
                {
                    if (Math.Abs(length - verticalDiff) <= 0.1)
                    {
                        //长度等于（起点-终点） 不是坡管
                        bIsVP = true;
                    }
                }
            }


            return bIsVP;
        }

        /// <summary>
        /// 抽象立管到中心点
        /// </summary>
        /// <param name="pipe"></param>
        private DBPoint HandleTCHVPipe(Entity pipe)
        {
            var pt = pipe.GeometricExtents.ToRectangle().GetCenter();
            var ptObj = new DBPoint(pt);
            return ptObj;
        }
    }
}
