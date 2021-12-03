using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage
{
    public class ThSingleRowCenterDrawingCmd : ThMEPBaseCommand, IDisposable
    {
        public ThSingleRowCenterDrawingCmd()
        {
            CommandName = "THDDXC";
            ActionName = "单排线槽中心线";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                short colorIndex = 7;
                Polyline singleRowCenter = ThMEPPolylineEntityJig.PolylineJig(colorIndex, "\n请选择下一个点", false);
                if (singleRowCenter.NumberOfVertices < 2)
                {
                    return;
                }
                // 添加到图纸中
                acdb.ModelSpace.Add(singleRowCenter);
                // 设置到指定图层
                if(!acdb.Layers.Contains(ThGarageLightCommon.SingleRowCenterLineLayerName))
                {
                    acdb.Database.AddLayer(ThGarageLightCommon.SingleRowCenterLineLayerName);
                    acdb.Database.SetLayerColor(ThGarageLightCommon.SingleRowCenterLineLayerName, colorIndex);
                }
                singleRowCenter.Layer = ThGarageLightCommon.SingleRowCenterLineLayerName;
            }
        }
    }
}
