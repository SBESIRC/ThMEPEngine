using System;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Command;
using AcHelper;

namespace ThMEPLighting.Garage
{
    public class ThDXDrawingCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                short colorIndex = 2;
                Polyline dx = ThMEPPolylineEntityJig.PolylineJig(colorIndex, "\n请选择下一个点", false);
                if (dx.NumberOfVertices < 2)
                {
                    return;
                }
                // 添加到图纸中
                acdb.ModelSpace.Add(dx);
                // 设置到指定图层
                if(!acdb.Layers.Contains(ThGarageLightCommon.DxCenterLineLayerName))
                {
                    acdb.Database.AddLayer(ThGarageLightCommon.DxCenterLineLayerName);
                    acdb.Database.SetLayerColor(ThGarageLightCommon.DxCenterLineLayerName, colorIndex);
                }
                dx.Layer = ThGarageLightCommon.DxCenterLineLayerName;
            }
        }
    }
}
