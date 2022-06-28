﻿using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.Model.Printer;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThMEPStructure.Common;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal abstract class ThArchDrawingPrinter
    {
        #region ---------- input -----------
        protected List<ThFloorInfo> FloorInfos { get; set; }
        protected List<ThComponentInfo> ComponentInfos { get; set; }
        protected List<ThGeometry> Geos { get; set; } 
        protected Dictionary<string, string> DocProperties { get; set; }
        #endregion
        protected double FlrBottomEle { get; set; }
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; protected set; }
        protected ThPlanePrintParameter PrintParameter { get; set; }
        public ThArchDrawingPrinter(ThArchSvgInput input, ThPlanePrintParameter printParameter)
        {
            Geos = input.Geos;
            FloorInfos = input.FloorInfos;
            DocProperties = input.DocProperties;
            ComponentInfos = input.ComponentInfos;
            ObjIds = new ObjectIdCollection();
            PrintParameter = printParameter;
            FlrBottomEle = DocProperties.GetFloorBottomElevation();
        }
        public abstract void Print(Database database);
        protected ObjectIdCollection PrintCommon(Database database,Curve curve)
        {
            var config = ThCommonPrinter.GetCommonConfig();
            var printer = new ThCommonPrinter(config);
            return printer.Print(database, curve);
        }
        protected ObjectIdCollection PrintAEWall(Database database, Curve curve)
        {
            // 打印属于AE-wall的边线
            var config = ThAEwallPrinter.GetAEWallConfig();
            var printer = new ThAEwallPrinter(null, config);
            return printer.Print(database, curve);
        }
        protected void AppendToObjIds(ObjectIdCollection objIds)
        {
            foreach (ObjectId objId in objIds)
            {
                ObjIds.Add(objId);
            }
        }
        protected virtual ObjectIdCollection PrintKanXian(Database db, ThGeometry geo)
        {
            var config = ThKanXianPrinter.GetConfig();
            bool isHidden = geo.Properties.ContainsKey("stroke-dasharray");
            if (isHidden)
            {
                config.LineType = "Hidden";
                config.LineTypeScale = 10.0;
            }
            var printer = new ThKanXianPrinter(config);
            return printer.Print(db, geo.Boundary as Curve);
        }
        protected ObjectIdCollection PrintHatch(Database db, Entity entity, Tuple<HatchPrintConfig, PrintConfig> hatchConfig)
        {
            var printer = new ThHatchPrinter(hatchConfig.Item1, hatchConfig.Item2);
            return printer.Print(db, entity);
        }
        protected ObjectIdCollection PrintBeam(Database db, Curve curve)
        {
            var config = ThBeamPrinter.GetSectionConfig();
            var printer = new ThBeamPrinter(config);
            return printer.Print(db, curve);
        }
        // 设置系统变量
        protected void SetSysVariables()
        {
            acadApp.Application.SetSystemVariable("LTSCALE", PrintParameter.LtScale);
            acadApp.Application.SetSystemVariable("MEASUREMENT", PrintParameter.Measurement);
        }
        protected ObjectIdCollection PrintHeadText(Database database,string flrRange)
        {
            // 打印自然层标识, eg 一层~五层平面层          
            var extents = ObjIds.ToDBObjectCollection(database).ToExtents2d();
            var textCenter = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                extents.MinPoint.Y - PrintParameter.HeadTextDisToPaperBottom, 0.0); // 3500 是文字中心到图纸底部的高度
            var printService = new ThPrintDrawingHeadService()
            {
                Head = flrRange,
                DrawingSacle = PrintParameter.DrawingScale,
                BasePt = textCenter,
            };
            return printService.Print(database); // 把结果存到ObjIds中
        }
    }
}