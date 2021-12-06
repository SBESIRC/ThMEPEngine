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
    public class ThFDXDrawingCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                short colorIndex = 1;
                Polyline fdx = ThMEPPolylineEntityJig.PolylineJig(colorIndex, "\n请选择下一个点", false);
                if (fdx.NumberOfVertices < 2)
                {
                    return;
                }
                // 添加到图纸中
                acdb.ModelSpace.Add(fdx);
                // 设置到指定图层
                if(!acdb.Layers.Contains(ThGarageLightCommon.FdxCenterLineLayerName))
                {
                    acdb.Database.AddLayer(ThGarageLightCommon.FdxCenterLineLayerName);
                    acdb.Database.SetLayerColor(ThGarageLightCommon.FdxCenterLineLayerName, colorIndex);
                }
                fdx.Layer = ThGarageLightCommon.FdxCenterLineLayerName;
            }
        }
    }
}
