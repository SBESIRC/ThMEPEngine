using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWPipeRun : IThWDraw
    {
        virtual public void Draw(Point3d basePt, Matrix3d mat)
        {
            throw new NotImplementedException();
        }

        virtual public void Draw(Point3d basePt)
        {
            throw new NotImplementedException();
        }
    }

    public class ThWRainPipeRun : ThWPipeRun, IEquatable<ThWRainPipeRun>
    {
        /// <summary>
        /// 楼层
        /// </summary>
        public ThWSDStorey Storey { get; set; } = new ThWSDStorey();

        /// <summary>
        /// 主雨水管
        /// </summary>
        public ThWSDPipe MainRainPipe { get; set; } = new ThWSDPipe();

        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThWSDDrain> FloorDrains { get; set; } = new List<ThWSDDrain>();

        /// <summary>
        /// 冷凝管
        /// </summary>
        public List<ThWSDCondensePipe> CondensePipes { get; set; } = new List<ThWSDCondensePipe>();

        /// <summary>
        /// 转管
        /// </summary>
        public ThWSDTranslatorPipe TranslatorPipe { get; set; } = new ThWSDTranslatorPipe();

        /// <summary>
        /// 检查口
        /// </summary>
        public ThWSDCheckPoint CheckPoint { get; set; } = new ThWSDCheckPoint();

        public ThWRainPipeRun()
        {
            //FloorDrains = new List<ThWFloorDrain>();
            //CondensePipes = new List<ThWCondensePipe>();
        }

        override public void Draw(Point3d _basePt)
        {

            if (Storey != null)
            {
                var basePt = _basePt.ReplaceY(Storey.StoreyBasePoint.Y);
                //DrawUtils.DrawTextLazy($"雨水立管，Label:{TranslatorPipe.Label} Storey.Label:{Storey.Label}", 100,bsPt);
                DrawUtils.DrawLineLazy(basePt, basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                var r=DrawUtils.DrawRectLazyFromLeftTop(basePt.OffsetXY(100, -100), 5000, 1500);
                r.ColorIndex = 4;
                int i = 2, j = 2;
                int delta = 200;
                FloorDrains.ForEach(o => o.Draw(basePt.OffsetXY(i++ * delta, -j * delta)));
                j++;
                CondensePipes.ForEach(o => o.Draw(basePt.OffsetXY(i++ * delta, -j * delta)));
                j++;
                TranslatorPipe.Draw(basePt.OffsetXY(i++ * delta, -j * delta));
                j++;
                CheckPoint.Draw(basePt.OffsetXY(i++ * delta, -j * delta));
            }
            else
            {
                DrawUtils.DrawTextLazy($"雨水立管，TranslatorPipe.Label:{TranslatorPipe.Label} Storey is null ...", 100, _basePt);
            }

            //NoDraw.Text("ThWRainPipeRun " + TranslatorPipe.Label, 100, basePt).AddToCurrentSpace();
            //return;
            //MainRainPipe.Draw(basePt);
            //todo

        }

        public override int GetHashCode()
        {
            return this.Storey.GetHashCode();
        }

        public bool Equals(ThWRainPipeRun other)
        {
            return this.Storey.Equals(other.Storey)
                && this.MainRainPipe.Equals(other.MainRainPipe)
                && FloorDrains.Count.Equals(other.FloorDrains.Count)
                && CondensePipes.Count.Equals(other.CondensePipes.Count)
                && TranslatorPipe.Equals(other.TranslatorPipe)
                && CheckPoint.Equals(other.CheckPoint);
        }
    }
}
