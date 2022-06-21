using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchitecturePlane.Service;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    /// <summary>
    /// 根据墙的厚度把窗户图块Y比例
    /// </summary>
    internal class ThAdjustWindowBlkYScaleService
    {
        public ThAdjustWindowBlkYScaleService()
        {
        }
        public void Adjust(List<ThComponentInfo> windows)
        {
            windows.ForEach(w =>
            {
                Adjust(w);
            });
        }

        private void Adjust(ThComponentInfo window)
        {
            var sp = window.Start.ToPoint3d();
            var ep = window.End.ToPoint3d();
            if (sp.HasValue && ep.HasValue)
            {
                if (sp.Value.DistanceTo(ep.Value) <= 10.0)
                {
                    return;
                }
                var dir = sp.Value.GetVectorTo(ep.Value);
                if (ThGeometryTool.IsParallelToEx(dir, Vector3d.ZAxis))
                {
                    return; //和Z轴平行
                }
                var wallThick = window.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
                if (wallThick <= 10.0)
                {
                    return;
                }
                if(window.Element is BlockReference br)
                {
                    var blkName = br.GetEffectiveName();
                    var data = GetBlkInitData(blkName);
                    //var thick = GetWindowThick(data.Item1, data.Item2, wallThick);
                    //获取方向窗户的厚度(For A-Win-1)
                    var ownerthick = GetWindowThick(window.Element as BlockReference);
                    var newThick = GetWindowThick(data.Item1, data.Item2, wallThick);
                    if (newThick > 0.0 && ownerthick > 0.0)
                    {
                        AdjustWindowBlkThick(br, ownerthick, newThick);
                    }
                }
            }
        }

        private void AdjustWindowBlkThick(BlockReference br, double ownerThick, double newThick)
        {
            var scale = newThick / ownerThick;
            if (scale > 0.0 && Math.Abs(scale -1.0)>1e-6)
            {
                var oldScale = br.ScaleFactors;
                br.ScaleFactors = new Scale3d(oldScale.X, scale, oldScale.Z);
                var dir = Vector3d.XAxis.RotateBy(br.Rotation + Math.PI / 2.0, br.Normal);
                var mt = Matrix3d.Displacement(dir.Negate().MultiplyBy((newThick - ownerThick) / 2.0));
                br.TransformBy(mt);
            }
        }

        private double GetWindowThick(double initWallThick, double initWindowThick, double wallThick)
        {
            return initWallThick > 0.0 ? wallThick * initWindowThick / initWallThick : 0.0;
        }

        private Tuple<double, double> GetBlkInitData(string blkName)
        {
            if (ThArchPrintBlockManager.Instance.WindowThickReferenceTbl.ContainsKey(blkName))
            {
                //<100,30> 表示100的墙厚对应30的厚度
                return ThArchPrintBlockManager.Instance.WindowThickReferenceTbl[blkName];
            }
            else
            {
                return Tuple.Create(0.0, 0.0);
            }
        }

        private double GetWindowThick(BlockReference windowBlk)
        {
            // 适用于长方形的窗户，
            var rectangle = windowBlk.GetBlkCurveMinimumRectangle();
            var disList = new List<double>();
            for(int i =0;i< rectangle.NumberOfVertices;i++)
            {
                if(rectangle.GetSegmentType(i) == SegmentType.Line)
                {
                    disList.Add(rectangle.GetLineSegmentAt(i).Length);
                }
            }
            disList = disList.OrderBy(x => x).ToList();
            for(int i=0;i< disList.Count-1;i++)
            {
                if(disList[i]<=1.0)
                {
                    continue;
                }    
                for (int j = i+1; j < disList.Count; j++)
                {
                    if(Math.Abs(disList[j] - disList[i])<=1.0)
                    {
                        return disList[i];
                    }
                }
            }
            return 0.0;
        }
    }
}
