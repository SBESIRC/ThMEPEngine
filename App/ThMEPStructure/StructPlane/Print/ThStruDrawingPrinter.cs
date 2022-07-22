using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPStructure.Common;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.StructPlane.Service;
using System;

namespace ThMEPStructure.StructPlane.Print
{
    internal abstract class ThStruDrawingPrinter
    {
        #region ---------- input -----------
        protected List<ThFloorInfo> FloorInfos { get; set; }
        protected List<ThComponentInfo> ComponentInfos { get; set; }
        protected List<ThGeometry> Geos { get; set; }
        protected Dictionary<string, string> DocProperties { get; set; }
        #endregion
        /// <summary>
        /// 收集所有当前图纸打印的物体
        /// </summary>
        public ObjectIdCollection ObjIds { get; protected set; }
        /// <summary>
        /// 楼层底部标高
        /// </summary>
        protected double FlrBottomEle { get; set; }
        /// <summary>
        /// 楼层高度
        /// </summary>
        protected double FlrHeight { get; set; }
        protected ThPlanePrintParameter PrintParameter { get; set; }
        public ThStruDrawingPrinter(ThSvgInput input, ThPlanePrintParameter printParameter)
        {
            Geos = input.Geos;
            FloorInfos = input.FloorInfos;
            DocProperties = input.DocProperties;
            ComponentInfos = input.ComponentInfos;
            ObjIds = new ObjectIdCollection();
            PrintParameter = printParameter;            
            FlrHeight = DocProperties.GetFloorHeight();
            FlrBottomEle = DocProperties.GetFloorBottomElevation();
        }
        public abstract void Print(Database database);
        protected ObjectIdCollection PrintUpperColumn(Database db, ThGeometry column)
        {
            var outlineConfig = ThColumnPrinter.GetUpperColumnConfig();
            var hatchConfig = ThColumnPrinter.GetUpperColumnHatchConfig();
            var printer = new ThColumnPrinter(hatchConfig, outlineConfig);
            return printer.Print(db, column.Boundary as Polyline);
        }
        protected ObjectIdCollection PrintBelowColumn(Database db, ThGeometry column)
        {
            var outlineConfig = ThColumnPrinter.GetBelowColumnConfig();
            var hatchConfig = ThColumnPrinter.GetBelowColumnHatchConfig();
            var printer = new ThColumnPrinter(hatchConfig, outlineConfig);
            return printer.Print(db, column.Boundary as Polyline);
        }
        protected ObjectIdCollection PrintUpperShearWall(Database db, ThGeometry shearwall)
        {
            var outlineConfig = ThShearwallPrinter.GetUpperShearWallConfig();
            var hatchConfig = ThShearwallPrinter.GetUpperShearWallHatchConfig();
            var printer = new ThShearwallPrinter(hatchConfig, outlineConfig);
            if (shearwall.Boundary is Polyline polyline)
            {
                return printer.Print(db, polyline);
            }
            else if (shearwall.Boundary is MPolygon mPolygon)
            {
                return printer.Print(db, mPolygon);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        protected ObjectIdCollection PrintBelowShearWall(Database db, ThGeometry shearwall)
        {
            var outlineConfig = ThShearwallPrinter.GetBelowShearWallConfig();
            var hatchConfig = ThShearwallPrinter.GetBelowShearWallHatchConfig();
            var printer = new ThShearwallPrinter(hatchConfig, outlineConfig);
            if (shearwall.Boundary is Polyline polyline)
            {
                return printer.Print(db, polyline);
            }
            else if (shearwall.Boundary is MPolygon mPolygon)
            {
                return printer.Print(db, mPolygon);
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        protected void PrintHeadText(Database database)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = FloorInfos.GetFloorRange(FlrBottomEle);
            if (string.IsNullOrEmpty(flrRange))
            {
                return;
            }
            var extents = ObjIds.ToDBObjectCollection(database).ToExtents2d();
            var textCenter = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                extents.MinPoint.Y - PrintParameter.HeadTextDisToPaperBottom, 0.0); // 3500 是文字中心到图纸底部的高度
            var printService = new ThPrintDrawingHeadService()
            {
                Head = flrRange,
                DrawingSacle = PrintParameter.DrawingScale,
                BasePt = textCenter,
            };
            Append(printService.Print(database)); // 把结果存到ObjIds中
        }

        protected void Append(ObjectIdCollection objIds)
        {
            foreach (ObjectId objId in objIds)
            {
                ObjIds.Add(objId);
            }
        }
        protected List<ElevationInfo> GetElevationInfos()
        {
            var results = new List<ElevationInfo>();
            FloorInfos.ForEach(o =>
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
    }
}
