﻿using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThPlatform3D.Common;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.StructPlane.Service;
using System.Linq;
using System;

namespace ThPlatform3D.StructPlane.Print
{
    internal abstract class ThStruDrawingPrinter
    {
        #region ---------- input -----------
        protected List<ThFloorInfo> _floorInfos { get; set; }
        protected List<ThComponentInfo> _componentInfos { get; set; }
        protected List<ThGeometry> _geos { get; set; }
        protected Dictionary<string, string> _docProperties { get; set; }
        #endregion
        #region ---------- output ---------        
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; protected set; }
        #endregion
        /// <summary>
        /// 楼层底部标高
        /// </summary>
        protected double _flrBottomEle { get; set; }
        /// <summary>
        /// 楼层高度
        /// </summary>
        protected double _flrHeight { get; set; }
        protected ThPlanePrintParameter _printParameter { get; set; }
        public ThStruDrawingPrinter(ThSvgParseInfo input, ThPlanePrintParameter printParameter)
        {
            _geos = input.Geos;
            _floorInfos = input.FloorInfos;
            _docProperties = input.DocProperties;
            _componentInfos = input.ComponentInfos;
            ObjIds = new ObjectIdCollection();
            _printParameter = printParameter;            
            _flrHeight = _docProperties.GetFloorHeight();
            if(_docProperties.ContainsKey(ThSvgPropertyNameManager.FloorBottomElevationPropertyName))
            {
                _flrBottomEle = _docProperties.GetFloorBottomElevation();
            }
            else
            {
                _flrBottomEle = _docProperties.GetFloorBottomElevation(_floorInfos);
            }            
        }
        public abstract void Print(Database database);
        public void ClearObjIds()
        {
            ObjIds = ObjIds.OfType<ObjectId>()
                .Where(o => !o.IsNull && o.IsValid && !o.IsErased)
                .ToObjectIdCollection();
        }
        protected ObjectIdCollection PrintUpperColumn(AcadDatabase acadDb, ThGeometry column)
        {
            var outlineConfig = ThColumnPrinter.GetUpperColumnConfig();
            var hatchConfig = ThColumnPrinter.GetUpperColumnHatchConfig();
            return ThColumnPrinter.Print(acadDb, column.Boundary as Polyline, outlineConfig, hatchConfig);
        }
        protected ObjectIdCollection PrintBelowColumn(AcadDatabase acadDb, ThGeometry column)
        {
            var outlineConfig = ThColumnPrinter.GetBelowColumnConfig();
            var hatchConfig = ThColumnPrinter.GetBelowColumnHatchConfig();
            return ThColumnPrinter.Print(acadDb, column.Boundary as Polyline, outlineConfig, hatchConfig);
        }
        protected ObjectIdCollection PrintUpperShearWall(AcadDatabase acadDb, ThGeometry shearwall)
        {
            var outlineConfig = ThShearwallPrinter.GetUpperShearWallConfig();
            var hatchConfig = ThShearwallPrinter.GetUpperShearWallHatchConfig();
            if (shearwall.Boundary is Polyline polyline)
            {
                return ThShearwallPrinter.Print(acadDb, polyline, outlineConfig, hatchConfig);
            }
            else if (shearwall.Boundary is MPolygon mPolygon)
            {
                return ThShearwallPrinter.Print(acadDb, mPolygon, outlineConfig, hatchConfig);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        protected ObjectIdCollection PrintBelowShearWall(AcadDatabase acadDb, ThGeometry shearwall)
        {
            var outlineConfig = ThShearwallPrinter.GetBelowShearWallConfig();
            var hatchConfig = ThShearwallPrinter.GetBelowShearWallHatchConfig();
            if (shearwall.Boundary is Polyline polyline)
            {
                return ThShearwallPrinter.Print(acadDb, polyline, outlineConfig, hatchConfig);
            }
            else if (shearwall.Boundary is MPolygon mPolygon)
            {
                return ThShearwallPrinter.Print(acadDb, mPolygon, outlineConfig, hatchConfig);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }

        protected ObjectIdCollection PrintHeadText(AcadDatabase acadDb,string flrRange,Tuple<string,string,string> stdFlrInfo)
        {
            //stdFlrInfo->Start楼层编号，End楼层编号,标准层
            var extents = GetPrintObjsExtents(acadDb);
            var textCenter = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                extents.MinPoint.Y - _printParameter.HeadTextDisToPaperBottom, 0.0); // 3500 是文字中心到图纸底部的高度
            var printService = new ThPrintDrawingHeadService()
            {
                Head = flrRange,
                DrawingSacle = _printParameter.DrawingScale,
                BasePt = textCenter,
                StdFlrInfo = stdFlrInfo
            };
            return printService.Print(acadDb);
        }
        /// <summary>
        /// 获取 ObjIds 集合里的范围
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>

        protected Extents2d GetPrintObjsExtents(AcadDatabase acadDb)
        {
            // ObjIds 是收集每层打印的物体
            return ObjIds.ToDBObjectCollection(acadDb).ToExtents2d();
        }
        
        protected void Append(ObjectIdCollection objIds)
        {
            // 把objIds添加到 ObjIds 中，用于返回
            foreach (ObjectId objId in objIds)
            {
                ObjIds.Add(objId);
            }
        }
        protected List<ElevationInfo> GetElevationInfos()
        {
            var results = new List<ElevationInfo>();
            _floorInfos.ForEach(o =>
            {
                double flrBottomElevation = 0.0;
                if (double.TryParse(o.Bottom_elevation, out flrBottomElevation))
                {
                    flrBottomElevation /= 1000.0;
                }
                double flrHeight = 0.0;
                if (double.TryParse(o.Height, out flrHeight))
                {
                    flrHeight /= 1000.0;
                }
                results.Add(new ElevationInfo()
                {
                    FloorNo = o.FloorNo,
                    BottomElevation = flrBottomElevation.ToString("0.000"),
                    FloorHeight = flrHeight.ToString("0.000"),
                    WallColumnGrade = "",
                    BeamBoardGrade = "",
                });
            });
            return results;
        }

        protected ObjectIdCollection Difference(ObjectIdCollection objIds)
        {
            var dict = new Dictionary<ObjectId, bool>();  
            foreach(ObjectId objId in objIds)
            {
                if(dict.ContainsKey(objId))
                {
                    continue;
                }
                else
                {
                    dict.Add(objId, true);
                }
            }
            return dict.Keys.ToObjectIdCollection();
        }
    }
}
