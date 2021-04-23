using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.PrintEntity
{
    public class CalEvacuationPathTypeService
    {
        double tol = 2;
        public List<EvacuationPathModel> CalPathType(List<ExtendLineModel> extendLines, List<Line> lanes)
        {
            List<Line> allLines = new List<Line>(lanes);
            List<Line> allExtendLines = new List<Line>();
            var startExtendModel = extendLines.Where(x => x.priority == Priority.MergeStartLine || x.priority == Priority.startExtendLine).ToList();
            var startExtendLines = startExtendModel
                .SelectMany(x =>
                {
                    DBObjectCollection objs = new DBObjectCollection();
                    x.line.Explode(objs);
                    return objs.Cast<Line>();
                }).ToList();
            allExtendLines.AddRange(startExtendLines);
            var otherExtendLines = extendLines.Except(startExtendModel)
                .SelectMany(x =>
                {
                    DBObjectCollection objs = new DBObjectCollection();
                    x.line.Explode(objs);
                    return objs.Cast<Line>();
                }).ToList();
            allExtendLines.AddRange(otherExtendLines);
            allLines.AddRange(allExtendLines);

            var nodedLines = GeUtils.GetNodedLines(allLines);
            List<EvacuationPathModel> pathModels = nodedLines.Select(x =>
            {
                EvacuationPathModel evacuationPath = new EvacuationPathModel();
                evacuationPath.line = x;
                return evacuationPath;
            }).ToList();

            ClassifyType(pathModels, startExtendLines, allExtendLines);
            return pathModels;
        }

        /// <summary>
        /// 判断装配类型
        /// </summary>
        /// <param name="extendLines"></param>
        /// <param name="startExtendLines"></param>
        /// <param name="allExtendLines"></param>
        private void ClassifyType(List<EvacuationPathModel> extendLines, List<Line> startExtendLines, List<Line> allExtendLines)
        {
            List<Line> allLines = extendLines.Select(x => x.line).ToList();
            List<Line> resLines = new List<Line>();
            while (allLines.Count > 0)
            {
                bool needCheck = false;
                List<Line> checkLines = new List<Line>(allLines);
                foreach (var line in checkLines)
                {
                    if(GetPathType(line, checkLines, startExtendLines) == PathType.AuxiliaryPath)
                    {
                        resLines.Add(line);
                        allLines.Remove(line);
                        needCheck = true;
                    }
                }

                if (!needCheck)
                {
                    break;
                }
            }

            foreach (var lineModel in extendLines)
            {
                if (resLines.Contains(lineModel.line))
                {
                    lineModel.evaPathType = PathType.AuxiliaryPath;
                }
                else
                {
                    lineModel.evaPathType = PathType.MainPath;
                }

                lineModel.setUpType = GetSetUpType(lineModel.line, allExtendLines);
            }
        }

        /// <summary>
        /// 判断疏散路径类型（主疏散路径和辅助疏散路径）
        /// </summary>
        /// <param name="line"></param>
        /// <param name="allLines"></param>
        /// <param name="startExtendLines"></param>
        /// <returns></returns>
        private PathType GetPathType(Line line, List<Line> allLines, List<Line> startExtendLines)
        {
            var checkLines = new List<Line>(allLines);
            checkLines.Remove(line);
            var isSIntersect = SelectService.SelelctCrossing(checkLines, CreateEndEntity(line.StartPoint)).Count() > 0;
            var isEIntersect = SelectService.SelelctCrossing(checkLines, CreateEndEntity(line.EndPoint)).Count() > 0;

            if (!isSIntersect || !isEIntersect)
            {
                if (GeUtils.IsHasOverlapInList(line, startExtendLines, tol))
                {
                    return PathType.MainPath;
                }
                return PathType.AuxiliaryPath;
            }

            return PathType.MainPath;
        }

        /// <summary>
        /// 判断安装类型（吊装和壁装）
        /// </summary>
        /// <param name="line"></param>
        /// <param name="extendLines"></param>
        /// <returns></returns>
        private SetUpType GetSetUpType(Line line, List<Line> extendLines)
        {
            if (GeUtils.IsHasOverlapInList(line, extendLines, tol))
            {
                return SetUpType.ByHoisting;
            }

            return SetUpType.ByWall;
        }

        /// <summary>
        /// 创建端头实体（用于判断是否搭接）
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Polyline CreateEndEntity(Point3d pt)
        {
            double tolValue = 2;
            Point3d p1 = pt - Vector3d.XAxis * tolValue + Vector3d.YAxis * tolValue;
            Point3d p2 = pt - Vector3d.XAxis * tolValue - Vector3d.YAxis * tolValue;
            Point3d p3 = pt + Vector3d.XAxis * tolValue - Vector3d.YAxis * tolValue;
            Point3d p4 = pt + Vector3d.XAxis * tolValue + Vector3d.YAxis * tolValue;
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(2, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(3, p4.ToPoint2D(), 0, 0, 0);

            return polyline;
        }
    }
}

