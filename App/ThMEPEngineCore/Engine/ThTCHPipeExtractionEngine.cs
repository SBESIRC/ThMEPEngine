using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[] {
                    ThMEPTCHService.DXF_PIPE,
                };
                var psResult = Active.Editor.SelectAll(ThSelectionFilterTool.Build(dxfNames));
                if (psResult.Status == PromptStatus.OK)
                {
                    psResult.Value.GetObjectIds().ForEach(o =>
                    {
                        HandleTCHPipe(acadDatabase.Element<Entity>(o));
                    });
                }
            }
        }

        private void HandleTCHPipe(Entity pipe)
        {
            Results.Add(new ThRawIfcFlowSegmentData()
            {
                // 暂时用“外包框”来代表其几何信息
                Geometry = pipe.GeometricExtents.ToRectangle(),
            });
        }
    }
}
