using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Business.Procedure
{
    public class SplitWallWorker
    {
        // 墙数据
        private List<Curve> m_wallCurves = null;

        private List<Curve> m_unClosedCurves = new List<Curve>();// 非闭合数据

        private List<Polyline> m_polys = new List<Polyline>();

        public static List<Polyline> MakeSplitWallProfiles(List<Curve> srcCurves)
        {
            if (srcCurves == null)
                return new List<Polyline>();

            var wallDataPicker = new SplitWallWorker(srcCurves);
            wallDataPicker.Do();
            return wallDataPicker.m_polys;
        }

        public SplitWallWorker(List<Curve> wallCurves)
        {
            m_wallCurves = wallCurves;
        }

        private List<Polyline> CalculateClosedPolys()
        {
            var polys = new List<Polyline>();
            foreach (var curve in m_wallCurves)
            {
                if (curve is Polyline poly && poly.Closed)
                    polys.Add(poly);
                else
                    m_unClosedCurves.Add(curve);
            }

            return polys;
        }

        public void Do()
        {
            // 闭合数据
            var polys = CalculateClosedPolys();
            if (polys.Count == 0)
                return;

            // 清洗外框线（MakeValid)
            var frames = polys.Select(o => ThMEPFrameService.Normalize(o)).ToList();

            //计算洞口和外包框
            var holeInfos = CalHoles(frames);
            var holes = holeInfos.Values.SelectMany(x=>x).ToList();

            //分割外包框线(tyj)
            var cuvFrames = frames.Cast<Curve>().ToList();
            cuvFrames.AddRange(m_unClosedCurves);
            var objs = GeomUtils.Curves2DBCollection(cuvFrames);
            var obLst = objs.Polygons();

            List<Polyline> resHoles = new List<Polyline>();
            List<Polyline> resFrames = new List<Polyline>();
            foreach (var ob in obLst)
            {
                if (ob is Polyline resPoly)
                {
                    var bufferCollection = resPoly.Buffer(-10);
                    if (bufferCollection.Count > 0)
                    {
                        var bufferPoly = bufferCollection[0] as Polyline;
                        if (holes.Any(x => x.Intersects(bufferPoly)))
                        {
                            resHoles.Add(resPoly);
                        }
                        else
                        {
                            resFrames.Add(resPoly);
                        }
                    }
                }
            }
            resHoles = resHoles.Where(x => resFrames.Any(y => (y.Buffer(-10)[0] as Polyline).Contains(x))).ToList();  //有效洞口
            var resPolys = new List<Polyline>(resFrames);
            resPolys.AddRange(resHoles);
            var predicateCurves = GeomUtils.CalculateCanBufferPolys(resPolys, ThMEPCommon.WallProfileShrinkDistance);
            m_polys.AddRange(predicateCurves);

            //// 非闭合数据处理(原)
            //foreach (var poly in frames)
            //{
            //    var polygonCurves = new List<Curve>();
            //    polygonCurves.Add(poly);
            //    polygonCurves.AddRange(m_unClosedCurves);
            //    var objs = GeomUtils.Curves2DBCollection(polygonCurves);
            //    var obLst = objs.Polygons();
            //    var resPolys = new List<Polyline>();
            //    for (int i = 0; i < obLst.Count; i++)
            //    {
            //        if (obLst[i] is Polyline resPoly)
            //            resPolys.Add(resPoly);
            //    }

            //    var predicateCurves = GeomUtils.CalculateCanBufferPolys(resPolys, ThMEPCommon.WallProfileShrinkDistance);
            //    m_polys.AddRange(predicateCurves);
            //}
        }

        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        public Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }
    }
}
