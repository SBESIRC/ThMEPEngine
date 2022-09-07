using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using AcHelper.Commands;
using ThCADExtension;
using ThMEPEngineCore;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThPlatform3D.Command
{
    public class ThDropSlabDrawCmd : IAcadCommand, IDisposable
    {
        private const string DropSlabLayerName = "TH-降板";
        private const string DropSlabMarkLayerName = "TH-降板";
        private const string DropSlabMarkTextStyle = "TH-STYLE3";        
        private DBObjectCollection _collectObjs;

        public ThDropSlabDrawCmd()
        {
            _collectObjs = new DBObjectCollection(); 
        }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            ImportTemplate();

            OpenLayer();

            DrawSlab();
        }

        private void DrawSlab()
        {
            var oldLayer = GetCurrentLayer();
            SetCurrentLayer(DropSlabLayerName);
            Active.Database.ObjectAppended += Database_ObjectAppended;
            Active.Editor.Command("_.PLINE");
            Active.Database.ObjectAppended -= Database_ObjectAppended;

            if(_collectObjs.Count>0)
            {
                var pdo = new PromptDoubleOptions("\n请输入降板相对标高(或跳过)");
                pdo.AllowNone = true;
                pdo.AllowArbitraryInput = true;
                pdo.AllowZero = true;
                var pdr = Active.Editor.GetDouble(pdo);
                if(pdr.Status == PromptStatus.OK)
                {
                    var ppr = Active.Editor.GetPoint("\n请选择降板标注的基点");
                    if(ppr.Status == PromptStatus.OK)
                    {
                        using (var acadDb = AcadDatabase.Active())
                        {
                            var wcsPt = ppr.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                            var text = CreateMark(wcsPt, pdr.Value, DropSlabMarkLayerName, DropSlabMarkTextStyle);
                            var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                            text.Rotation = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                            acadDb.ModelSpace.Add(text);
                        }  
                    }
                }
            }
            SetCurrentLayer(oldLayer);
        }

        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if(e.DBObject is Curve)
            {
                _collectObjs.Add(e.DBObject);
            }
        }

        private DBText CreateMark(Point3d position, double elevation,string layer,
            string textStyle,double height = 300.0,double widthFactor=0.7)
        {
            var text = new DBText(); 
            text.Position = position;
            text.TextString = elevation.ToString("#0.0");
            text.Height = height;
            text.Layer = layer;
            text.WidthFactor = widthFactor;
            text.Linetype = "ByLayer";
            text.LineWeight = LineWeight.ByLayer;
            text.TextStyleId = DbHelper.GetTextStyleId(textStyle);
            return text;
        }

        private void ImportTemplate()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThBIMCommon.CadTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(DropSlabLayerName), true);
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(DropSlabMarkLayerName), true);

                // 导入文字样式
                acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(DropSlabMarkTextStyle), true);
            }
        }

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(DropSlabLayerName);
            }
        }

        private string GetCurrentLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Element<LayerTableRecord>(acdb.Database.Clayer).Name;
            }
        }

        private void SetCurrentLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.SetCurrentLayer(layerName);
            }
        }
    }
}
