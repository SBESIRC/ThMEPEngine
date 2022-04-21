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
using ThMEPStructure.StructPlane.Service;

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
        ///  读取SvgFile
        /// </summary>
        [CommandMethod("TIANHUACAD", "THReadSvg", CommandFlags.Modal)]
        public void THReadSvg()
        {
            var pofo = new PromptOpenFileOptions("\n选择要解析的Svg文件");
            pofo.Filter = "Svg files (*.svg)|*.svg";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 解析
                var svg = new ThMEPEngineCore.IO.SVG.ThSVGReader();
                var svgData = svg.ReadFromFile(pfnr.StringResult);

                //// 沿着X轴镜像
                //var mt = Matrix3d.Rotation(System.Math.PI, Vector3d.XAxis, Point3d.Origin);
                //geometries.ForEach(o => o.Boundary.TransformBy(mt));

                // Print                    
                var prinService = new ThSvgEntityPrintService(svgData.Item1, svgData.Item2);
                prinService.Print(Active.Database);
            }
        }
        [CommandMethod("TIANHUACAD", "THMUTSC", CommandFlags.Modal)]
        public void THMUTSC()
        {
            var pofo = new PromptOpenFileOptions("\n选择要成图的Ifc文件");
            pofo.Filter = "Ifc files (*.Ifc)|*.Ifc";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                var config = new ThStructurePlaneConfig()
                {
                    IfcFilePath = pfnr.StringResult,
                    SavePath = "",
                };
                var generator = new ThStructurePlaneGenerator(config);
                generator.Generate();
            }
        }
    }
}
