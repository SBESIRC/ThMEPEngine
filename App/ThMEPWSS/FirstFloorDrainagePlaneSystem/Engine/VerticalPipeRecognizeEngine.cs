using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Config;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Engine
{
    public class VerticalPipeRecognizeEngine
    {
        public double dis = 150;
        /// <summary>
        /// 识别规则
        /// </summary>
        Dictionary<List<string>, VerticalPipeType> recognizeRules = new Dictionary<List<string>, VerticalPipeType>()
        {
            { new List<string>(){ "*WLx-x" },  VerticalPipeType.SewagePipe},
            { new List<string>(){ "*FLx-x" },  VerticalPipeType.WasteWaterPipe},
            { new List<string>(){ "YxLx-x" },  VerticalPipeType.RainPipe},
            { new List<string>(){ "PLx-x" },  VerticalPipeType.ConfluencePipe},
            { new List<string>(){ "*NLx-x", "FLx-0" },  VerticalPipeType.CondensatePipe},
            { new List<string>(){ "TLx-x" },  VerticalPipeType.HolePipe},
        };

        /// <summary>
        /// 识别
        /// </summary>
        /// <param name="pipes"></param>
        /// <param name="marks"></param>
        public List<VerticalPipeModel> Recognize(List<Entity> pipes, List<Entity> marks)
        {
            var markEnts = EntityService.GetBasicEntity(marks);
            var markLines = new List<Line>();
            var markTxts = new List<DBText>();
            markEnts.ForEach(x =>
            {
                if (x is Line line)
                {
                    markLines.Add(line);
                }
                else if (x is Polyline poly)
                {
                    DBObjectCollection dBObject = new DBObjectCollection();
                    poly.Explode(dBObject);
                    markLines.AddRange(dBObject.OfType<Line>());
                }
                else if (x is DBText text)
                {
                    markTxts.Add(text);
                }
            });

            var pipeDic = EntityService.GetBasicEntityDic(pipes);
            var pipeModel = MatchingPipeToMark(pipeDic, markLines, markTxts);
            return pipeModel;
        }

        /// <summary>
        /// 标注和立管匹配
        /// </summary>
        /// <param name="pipeDic"></param>
        /// <param name="markLines"></param>
        /// <param name="markTxts"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> MatchingPipeToMark(Dictionary<Entity, List<Entity>> pipeDic, List<Line> markLines, List<DBText> markTxts)
        {
            var allPipeCircles = pipeDic.SelectMany(x => x.Value.ToList()).OfType<Circle>().ToList();
            var canUseLines = new List<Line>(markLines);
            List<VerticalPipeModel> resModel = new List<VerticalPipeModel>();
            foreach (var line in markLines)
            {
                var connectPipe = allPipeCircles.FirstOrDefault(x => line.StartPoint.DistanceTo(x.Center) < x.Radius - 5 || line.EndPoint.DistanceTo(x.Center) < x.Radius - 5);
                if (connectPipe != null)
                {
                    canUseLines.Remove(line);
                    var tempLines = new List<Line>(canUseLines);
                    var connectLines = GeometryUtils.GetConenctLine(ref tempLines, line, 0.001).Distinct().ToList();
                    canUseLines = canUseLines.Except(connectLines).ToList();
                    connectLines.Add(line);
                    var resText = GetMathcingMarkTextByLine(connectLines, markTxts);
                    if (resText.Count > 0)
                    {
                        var models = CreateVerticalPipeModel(resText, line, connectLines, allPipeCircles, connectPipe);
                        resModel.AddRange(models);
                    }
                }
            }
            return resModel;
        }

        /// <summary>
        /// 找到引线上的标注
        /// </summary>
        /// <param name="connectLines"></param>
        /// <param name="dBTexts"></param>
        /// <returns></returns>
        private List<DBText> GetMathcingMarkTextByLine(List<Line> connectLines, List<DBText> dBTexts)
        {
            var textDic = dBTexts.ToDictionary(x => x, y => Vector3d.XAxis.RotateBy(y.Rotation, Vector3d.ZAxis));
            var resText = new List<DBText>();
            foreach (var cLine in connectLines)
            {
                var cLineDir = (cLine.EndPoint - cLine.StartPoint).GetNormal();
                var matchingText = textDic.Where(x =>
                {
                    var closetPt = cLine.GetClosestPointTo(x.Key.Position, false);
                    return closetPt.DistanceTo(x.Key.Position) < dis;
                })
                .Where(x =>
                {
                    if (cLineDir.IsParallelTo(x.Value, new Tolerance(0.01, 0.01)))
                    {
                        return CheckMarkToLineDir(cLine, x.Key);
                    }
                    return false;
                })
                .OrderBy(x => cLine.GetClosestPointTo(x.Key.Position, false).DistanceTo(x.Key.Position))
                .ToDictionary(x => x.Key, y => y.Value);
                if (matchingText.Count > 0)
                {
                    resText.Add(matchingText.First().Key);
                }
            }
            return resText;
        }

        /// <summary>
        /// 筛选出引线匹配的标注
        /// </summary>
        /// <param name="line"></param>
        /// <param name="dBText"></param>
        /// <returns></returns>
        private bool CheckMarkToLineDir(Line line, DBText dBText)
        {
            var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
            if (lineDir.Y < 0) lineDir = -lineDir;
            var closetPt = line.GetClosestPointTo(dBText.Position, true);
            var dir = (dBText.Position - closetPt).GetNormal();
            var checkAngle = lineDir.GetAngleTo(Vector3d.XAxis);
            if (checkAngle > Math.PI / 4 * 3 || checkAngle < Math.PI / 2)
            {
                return dir.Y > 0;
            }
            else
            {
                return dir.X < 0;
            }
        }

        /// <summary>
        /// 创建立管对象
        /// </summary>
        /// <param name="bTexts"></param>
        /// <param name="markLine"></param>
        /// <param name="connectLines"></param>
        /// <param name="allPipes"></param>
        /// <param name="connectPipe"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> CreateVerticalPipeModel(List<DBText> bTexts, Line markLine, List<Line> connectLines, List<Circle> allPipes, Circle connectPipe)
        {
            allPipes.Remove(connectPipe);
            List<VerticalPipeModel> resModel = new List<VerticalPipeModel>();
            if (bTexts.Count == 1)
            {
                var isTure = RecognizePipeType(bTexts.First(), out VerticalPipeType pipeType);
                if (isTure)
                {
                    VerticalPipeModel verticalPipe = new VerticalPipeModel(connectPipe.Center, connectPipe, connectLines, bTexts.First(), pipeType);
                    resModel.Add(verticalPipe);
                }
            }
            else if (bTexts.Count > 0)
            {
                var pipeDic = DistributionCennetPipe(markLine, allPipes, bTexts, connectPipe);
                foreach (var pipeD in pipeDic)
                {
                    var isTure = RecognizePipeType(pipeD.Value, out VerticalPipeType pipeType);
                    if (isTure)
                    {
                        VerticalPipeModel verticalPipe = new VerticalPipeModel(pipeD.Key.Center, pipeD.Key, connectLines, pipeD.Value, pipeType);
                        resModel.Add(verticalPipe);
                        allPipes.Remove(pipeD.Key);
                    }
                }
            }
            return resModel;
        }

        /// <summary>
        /// 匹配立管和标注
        /// </summary>
        /// <param name="markLine"></param>
        /// <param name="pipes"></param>
        /// <param name="dBTexts"></param>
        /// <param name="connectPipe"></param>
        /// <returns></returns>
        private Dictionary<Circle, DBText> DistributionCennetPipe(Line markLine, List<Circle> pipes, List<DBText> dBTexts, Circle connectPipe)
        {
            Dictionary<Circle, DBText> pipeDic = new Dictionary<Circle, DBText>();
            var connectPipes = pipes.Where(x => markLine.GetClosestPointTo(x.Center, false).DistanceTo(x.Center) < x.Radius).ToList();
            if (connectPipes.Count > dBTexts.Count - 1)
            {
                connectPipes = connectPipes.OrderBy(x => x.Center.DistanceTo(connectPipe.Center)).ToList();
                connectPipes.RemoveRange(dBTexts.Count - 1, connectPipes.Count - 1);
            }
            connectPipes.Add(connectPipe);
            connectPipes.ToDictionary(x => x, y => dBTexts.OrderByDescending(z => z.Position.DistanceTo(y.Center)).First());
            while (connectPipes.Count > 0)
            {
                var firPipe = connectPipes.First();
                var text = dBTexts.OrderByDescending(x => x.Position.DistanceTo(firPipe.Center)).FirstOrDefault();
                connectPipes.Remove(firPipe);
                if (text != null)
                {
                    dBTexts.Remove(text);
                    pipeDic.Add(firPipe, text);
                }
            }

            return pipeDic;
        }

        /// <summary>
        /// 识别立管类型
        /// </summary>
        /// <param name="dBText"></param>
        /// <param name="pipeType"></param>
        /// <returns></returns>
        private bool RecognizePipeType(DBText dBText, out VerticalPipeType pipeType)
        {
            pipeType = VerticalPipeType.SewagePipe;
            var recognize = recognizeRules.Where(x => x.Key.Any(y => ThRegularMatchingService.Matching(y, dBText.TextString))).Select(x => x.Value).ToList();
            if (recognize.Count > 0)
            {
                pipeType = recognize.First();
                return true;
            }
            return false;
        }
    }
}
