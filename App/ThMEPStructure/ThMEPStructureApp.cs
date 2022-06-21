using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.GirderConnect.Command;
using ThMEPStructure.Reinforcement.Command;
using ThMEPStructure.ArchiecturePlane;
using ThMEPStructure.StructPlane.Service;
using ThMEPStructure.ArchiecturePlane.Print;
using ThMEPStructure.ArchiecturePlane.Service;
using ThMEPStructure.Common;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure
{
    public class ThMEPStructureApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        /// <summary>
        /// 生成主梁
        /// </summary>
        [CommandMethod("TIANHUACAD", "THZLSC", CommandFlags.Modal)]
        public void THZLSC()
        {
            using (var cmd = new ThBeamConnectorCommand())
            {
                cmd.SubExecute();
            }
        }

        /// <summary>
        /// 生成次梁
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCLSC", CommandFlags.Modal)]
        public void THCLSC()
        {
            using (var cmd = new SecondaryBeamConnectCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 生成双线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSXSC", CommandFlags.Modal)]
        public void THSXSC()
        {
            using (var cmd = new ThDoubleBeamLineCommand())
            {
                cmd.Execute();
            }
        }

        #region ---------- 纵筋测试命令 --------
        /// <summary>
        /// 绘制标准一字型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawRectTypeReinforce", CommandFlags.Modal)]
        public void TestDrawRectTypeReinforce()
        {
            using (var cmd = new ThRectTypeReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 绘制标准计算型一字型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawRectTypeCalReinforce", CommandFlags.Modal)]
        public void ThRectTypeCalReinforceDrawCmd()
        {
            using (var cmd = new ThRectTypeCalReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 绘制标准L型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawLTypeReinforce", CommandFlags.Modal)]
        public void TestDrawLTypeReinforce()
        {
            using (var cmd = new ThLTypeReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 绘制标准计算型L型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawLTypeCalReinforce", CommandFlags.Modal)]
        public void TestDrawLTypeCalReinforce()
        {
            using (var cmd = new ThLTypeCalReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 绘制标准T型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawTTypeReinforce", CommandFlags.Modal)]
        public void TestDrawTTypeReinforce()
        {
            using (var cmd = new ThTTypeReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 绘制标准计算型T型纵筋
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawTTypeCalReinforce", CommandFlags.Modal)]
        public void TestDrawTTypeCalReinforce()
        {
            using (var cmd = new ThTTypeCalReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 生成柱表
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestDrawReinforceTable", CommandFlags.Modal)]
        public void TestDrawReinforceTable()
        {
            using (var cmd = new ThReinforceTableDrawCmd())
            {
                cmd.Execute();
            }
        }

        #endregion

        /// <summary>
        ///  读取结构SvgFile
        /// </summary>
        [CommandMethod("TIANHUACAD", "THReadStruSvg", CommandFlags.Modal)]
        public void THReadStruSvg()
        {
            var pofo = new PromptOpenFileOptions("\n选择要解析的Svg文件");
            pofo.Filter = "Svg files (*.svg)|*.svg";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 解析
                var svg = new ThMEPEngineCore.IO.SVG.ThStructureSVGReader();
                svg.ReadFromFile(pfnr.StringResult);

                //// 沿着X轴镜像
                //var mt = Matrix3d.Rotation(System.Math.PI, Vector3d.XAxis, Point3d.Origin);
                //geometries.ForEach(o => o.Boundary.TransformBy(mt));

                // Print                    
                var prinService = new ThSvgEntityPrintService(svg.Geos,
                    svg.FloorInfos,svg.DocProperties);
                prinService.Print(Active.Database);
            }
        }
        /// <summary>
        ///  读取SvgFile
        /// </summary>
        [CommandMethod("TIANHUACAD", "THReadArchSvg", CommandFlags.Modal)]
        public void THReadArchSvg()
        {
            var pofo = new PromptOpenFileOptions("\n选择要解析的Svg文件");
            pofo.Filter = "Svg files (*.svg)|*.svg";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 解析
                var svg = new ThMEPEngineCore.IO.SVG.ThArchitectureSVGReader();
                svg.ReadFromFile(pfnr.StringResult);

                // Print
                var svgInput = new ThArchSvgInput()
                {
                    Geos = svg.Geos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                    ComponentInfos = svg.ComponentInfos,
                };
                var printParameter = new ThPlanePrintParameter()
                {
                    DrawingScale = "1:100"
                };
                var drawingType =  svg.DocProperties.GetDrawingType();
                ThArchDrawingPrinter printer = null;
                switch (drawingType)
                {
                    case DrawingType.Plan:
                        printer= new ThArchPlanDrawingPrinter(svgInput, printParameter);
                        break;
                    case DrawingType.Elevation:
                        printer = new ThArchElevationDrawingPrinter(svgInput, printParameter);
                        break;
                    case DrawingType.Section:
                        printer = new ThArchSectionDrawingPrinter(svgInput, printParameter);    
                        break;
                }
                if(printer!=null)
                {
                    printer.Print(Active.Database);
                } 
            }
        }
        [CommandMethod("TIANHUACAD", "THMUTSC", CommandFlags.Modal)]
        public void THMUTSC()
        {
            var pofo = new PromptOpenFileOptions("\n选择要成图的Ydb文件");
            pofo.Filter = "Ydb files (*.ydb)|*.ydb|Ifc files (*.ifc)|*.ifc";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                string ifcFilePath = "";
                if(System.IO.Path.GetExtension(pfnr.StringResult).ToUpper()==".YDB")
                {
                    var ydbToIfcService = new ThYdbToIfcConvertService();
                    ifcFilePath = ydbToIfcService.Convert(pfnr.StringResult);
                }
                else
                {
                    ifcFilePath = pfnr.StringResult;
                }
                
                if(!string.IsNullOrEmpty(ifcFilePath))
                {
                    var config = new ThPlaneConfig()
                    {
                        IfcFilePath = ifcFilePath,
                        SvgSavePath = "",
                        DrawingType = DrawingType.Structure,
                    };
                    var generator = new ThStructurePlaneGenerator(config);
                    generator.Generate();
                }               
            }
        }

        [CommandMethod("TIANHUACAD", "THAUTSC", CommandFlags.Modal)]
        public void THAUTSC()
        {
            var pofo = new PromptOpenFileOptions("\n选择要成图的Ifc文件");
            pofo.Filter = "Ifc files (*.ifc)|*.ifc";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                var config = new ThPlaneConfig()
                {
                    IfcFilePath = pfnr.StringResult,
                    SvgSavePath = "",
                    DrawingScale ="1:100",
                    DrawingType = DrawingType.Plan,
                };
                var generator = new ThArchitectureGenerator(config);
                generator.Generate();
            }
        }
    }
}
