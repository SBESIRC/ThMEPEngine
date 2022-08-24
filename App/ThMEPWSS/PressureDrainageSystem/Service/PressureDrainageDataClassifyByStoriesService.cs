using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.PressureDrainageSystem.Model;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;
namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class PressureDrainageDataClassifyByStoriesService
    {
        public PressureDrainageDataClassifyByStoriesService()
        {         
        }
        public PressureDrainageDataCollectionService CollectDataService { get; set; }
        public PressureDrainageSystemDiagramVieModel Viewmodel { get; set; }
        public PressureDrainageModelData Modeldatas { get; set; }
      
        /// <summary>
        /// 按楼层将数据分类
        /// </summary>
        public void ClassifyDataByStories()
        {
            AppendVerticalPipeDataToModeldatas();
            AppendOtherDataToModeldates();
            AppendDrainWellsToModeldates();
            AppendWallLinesToModeldates();
        }
        
        /// <summary>
        /// 将立管数据添加进modeldatas
        /// </summary>
        private void AppendVerticalPipeDataToModeldatas()
        {
            AppendVerticalPipeToFloorDict();
            var labelLines = CollectDataService.CollectedData.LabelLines.Distinct().ToList();//标注引线          
            var horizontalLabelLines = labelLines.Where(e => TestLineHorizontal(e, 10)).ToList();//水平标注引线
            var obliqueLabelLines = labelLines.Where(e => !TestLineHorizontal(e, 10)).ToList();
            List<HorizontalLabelClass> HorizontalLabels = new ();//构建水平标注引线类
            foreach (var line in horizontalLabelLines)
            {
                HorizontalLabelClass horizontalLabel = new ();
                horizontalLabel.HorizontalLabelLine = line;
                HorizontalLabels.Add(horizontalLabel);
            }
            AppendLabelInfoToHorizontalLabel(HorizontalLabels);
            AppendLabelInfoToVerticalPipe(HorizontalLabels, obliqueLabelLines);
        }
       
        /// <summary>
        /// 将排水横管、集水井、潜水泵、套管等添加进modeldatas
        /// </summary>
        private void AppendOtherDataToModeldates()
        {
            var extendList = GetBoundaryExtendList(Viewmodel);
            int floorNumber = extendList.Count;
            for (int i = 0; i < floorNumber; i++)
            {
                Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].HorizontalPipe = new ();
                Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].SubmergedPumps = new ();
                Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].Wrappipes = new();
            }
            foreach (var horLine in CollectDataService.CollectedData.HorizontalPipes)
            {
                for (int i = 0; i < floorNumber; i++)
                {
                    if (Algorithms.IsPointIn(extendList[i], horLine.Line.GetMidpoint()))
                    {
                        Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].HorizontalPipe.Add(horLine);
                        break;
                    }
                }
            }
            double cond_QuitCycle = 0;
            for (int i = 0; i < CollectDataService.CollectedData.SubmergedPumps.Count; i++)
            {
                var pump = CollectDataService.CollectedData.SubmergedPumps[i];

                for (int j = 0; j < floorNumber; j++)
                {
                    //var s = Utils.PressureDrainageUtils.AnalysisPointList(CollectDataService.CollectedData.SubmergedPumps.Select(e => ((Extents3d)(e.Extents.Bounds)).MaxPoint).ToList());
                    if (Algorithms.IsPointIn(extendList[j], pump.Extents.GetCenter()))
                    {
                        cond_QuitCycle += 1;
                        Modeldatas.FloorDict[Modeldatas.FloorListDatas[j]].SubmergedPumps.Add(pump);
                        break;
                    }
                }
            }
            foreach (var wrappipe in CollectDataService.CollectedData.WrappingPipes)
            {
                for (int i = 0; i < floorNumber; i++)
                {
                    if (Algorithms.IsPointIn(extendList[i], new Point3d(wrappipe.CenterPoint().X, wrappipe.CenterPoint().Y,0)))
                    {
                        Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].Wrappipes.Add(wrappipe.ToRectangle());
                        break;
                    }
                }
            }
        }
       
        /// <summary>
        /// 将排水井数据添加进modeldates 
        /// </summary>
        private void AppendDrainWellsToModeldates()
        {
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[0]].DrainWells = new();
            var extents = GetBoundaryExtendList(Viewmodel)[0];
            foreach (var well in CollectDataService.CollectedData.DrainWells)
            {
                if (extents.Contains(well.Extents.GetCenter()))
                {
                    Modeldatas.FloorDict[Modeldatas.FloorListDatas[0]].DrainWells.Add(well);
                }
            }
        }

        /// <summary>
        /// 将墙线添加进modeldates
        /// </summary>
        private void AppendWallLinesToModeldates()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var ext = GetBoundaryExtendList(Viewmodel)[0];
                Modeldatas.WallLines = new List<Polyline>();
                DBObjectCollection objWalls = new();
                DBObjectCollection objColumns = new();
                DBObjectCollection objConcaveHull = new();
                DBObjectCollection objTmpPlys = new();
                foreach (var plyWalls in CollectDataService.CollectedData.WallPolyLines)
                {
                    if (ext.IsPointIn(plyWalls.GetMidpoint()))
                    {
                        objWalls.Add(plyWalls);
                        objTmpPlys.Add(plyWalls);
                        Modeldatas.WallLines.Add(plyWalls);
                    }
                }
                foreach (var plyColumns in CollectDataService.CollectedData.ColumnsPolyLines)
                {
                    if (ext.IsPointIn(plyColumns.GetMidpoint()))
                    {
                        objColumns.Add(plyColumns);
                        objTmpPlys.Add(plyColumns);
                    }
                }
                var concaveBuilder = new ThMEPConcaveBuilder(objTmpPlys, 8000);
                objConcaveHull = concaveBuilder.Build();
                Modeldatas.Boundaries = new List<Polyline>();
                Modeldatas.Boundaries.AddRange(objConcaveHull.Cast<Polyline>().ToList());
                //删除在边界附近的可疑线段，不认为是内墙
                if (Modeldatas.Boundaries.Count > 0)
                {
                    var bound = Modeldatas.Boundaries.OrderByDescending(e => e.Area).First().BufferPL(5000).Cast<Polyline>().OrderBy(e => e.Area).First();
                    for (int i = 0; i < Modeldatas.WallLines.Count; i++)
                    {
                        var pl = Modeldatas.WallLines[i];
                        if (pl.Intersects(bound) || !bound.Contains(pl.GetMidpoint()))
                        {
                            Modeldatas.WallLines.RemoveAt(i);
                            i--;
                        }
                    }
                }
                //objConcaveHull.Cast<Entity>().ToList().CreateGroup(adb.Database, (int)ColorIndex.Cyan);
                //objWalls.Cast<Entity>().ToList().CreateGroup(adb.Database, (int)ColorIndex.Cyan);
                //objColumns.Cast<Entity>().ToList().CreateGroup(adb.Database, (int)ColorIndex.Cyan);
            }
        }

        /// <summary>
        /// 将立管添加进对应楼层的立管列表
        /// </summary>
        private void AppendVerticalPipeToFloorDict()
        {
            var extendList = GetBoundaryExtendList(Viewmodel);
            Modeldatas.FloorLocPoints = new List<Point3d>();
            for (int i = 0; i < extendList.Count; i++)
            {
                foreach (var pt in CollectDataService.CollectedData.StoryFrameBasePt)
                {
                    if (extendList[i].IsPointIn(pt))
                    {
                        Modeldatas.FloorLocPoints.Add(pt);
                    }
                }
            }
            int floorNumber = extendList.Count;
            for (int i = 0; i < floorNumber; i++)
            {
                Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].VerticalPipes = new List<VerticalPipeClass>();
            }
            foreach (var cicle in CollectDataService.CollectedData.VerticalPipes)
            {
                for (int i = 0; i < floorNumber; i++)
                {
                    if (Algorithms.IsPointIn(extendList[i], cicle.Center))
                    {
                        VerticalPipeClass verticalPipe = new VerticalPipeClass();
                        verticalPipe.Circle = cicle;
                        if (cicle.Layer.Equals("AdditonPipe"))
                        {
                            verticalPipe.IsGenerated = true;
                        }
                        Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].VerticalPipes.Add(verticalPipe);
                        break;
                    }
                }
            }
        }
       
        /// <summary>
        /// 为水平标注引线类添加相应的文字标注属性值
        /// </summary>
        private void AppendLabelInfoToHorizontalLabel(List<HorizontalLabelClass> HorizontalLabels)
        {
            double positionDisX = 1000;
            var dbObjs = new DBObjectCollection();
            foreach (var dbText in CollectDataService.CollectedData.Labels)
            {
                dbText.Position = new Point3d(dbText.Position.X - positionDisX, dbText.Position.Y, 0);
                dbObjs.Add(dbText);
            }
            ThCADCoreNTSSpatialIndex labelSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            double tol = 1000;
            for (int i = 0; i < HorizontalLabels.Count; i++)
            {
                var e = HorizontalLabels[i].HorizontalLabelLine;
                var pl = ThDrawTool.ToRectangle(e.StartPoint, e.EndPoint, tol);
                Extents3d extent = pl.GeometricExtents;
                HorizontalLabels[i].DBTexts = GetCrossingDbTextsByExtent(extent, labelSpatialIndex);
            }
        }
       
        /// <summary>
        /// 为立管添加相应的文字标注属性值
        /// 遍历找到立管指定范围内的斜标注线，遍历找到与该标注线相连的水平标注线
        /// </summary>
        private void AppendLabelInfoToVerticalPipe(List<HorizontalLabelClass> HorizontalLabels, List<Line> obliqueLabelLines)
        {
            double tolOriginOblique = 400;
            for (int i = 0; i < Modeldatas.FloorListDatas.Count; i++)
            {
                foreach (var pipe in Modeldatas.FloorDict[Modeldatas.FloorListDatas[i]].VerticalPipes)
                {
                    pipe.SameTypeIdentifiers = new ();
                    var extent = new Circle(pipe.Circle.Center, Vector3d.ZAxis, tolOriginOblique).Bounds.Value;
                    List<HorizontalLabelClass> selectedHorizontals = new ();
                    for (int k = 0; k < obliqueLabelLines.Count; k++)
                    {
                        bool isStartPtInExtent = Algorithms.IsPointIn(extent, obliqueLabelLines[k].StartPoint);
                        bool isEndPtInExtent = Algorithms.IsPointIn(extent, obliqueLabelLines[k].EndPoint);
                        if (isStartPtInExtent || isEndPtInExtent)
                        {
                            double tolObliqueHorizontal = 10;
                            int minIndex = -1;
                            var ptOri = pipe.Circle.Center;
                            var ptStart = obliqueLabelLines[k].StartPoint;
                            var ptEnd = obliqueLabelLines[k].EndPoint;
                            Point3d ptmp = ptOri.DistanceTo(ptStart) > ptOri.DistanceTo(ptEnd) ? ptStart : ptEnd;
                            for (int t = 0; t < HorizontalLabels.Count; t++)
                            {
                                double dis = HorizontalLabels[t].HorizontalLabelLine.GetOrthoProjectedCurve(new Plane(Point3d.Origin, Vector3d.ZAxis)).GetDistToPoint(ptmp);
                                if (dis < tolObliqueHorizontal)
                                {
                                    minIndex = t;
                                    break;
                                }
                            }
                            if (minIndex > -1)
                            {
                                selectedHorizontals.Add(HorizontalLabels[minIndex]);
                            }
                        }
                    }
                    if (selectedHorizontals.Count > 0)
                    {
                        foreach (var horLine in selectedHorizontals)
                        {
                            foreach (var bText in horLine.DBTexts)
                            {
                                if (TestContainsChineseCharacter(bText.TextString))
                                {
                                    pipe.Label += bText.TextString;
                                }
                                else
                                {
                                    if (bText.TextString.Contains("F") || bText.TextString.Contains("L"))
                                    {
                                        pipe.Identifier += bText.TextString;
                                        pipe.SameTypeIdentifiers.Add(pipe.Identifier);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// 从viewmodel中读取楼层边框Extents3d
        /// </summary>
        /// <param name="viewmodel"></param>
        /// <returns></returns>
        private List<Polyline> GetBoundaryExtendList(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            var extendList = new List<Polyline>();
            for (int j = 0; j < viewmodel.FloorAreaList.Count; j++)
            {
                var ptslist = new List<Point3d>();
                foreach (var ptcollection in viewmodel.FloorAreaList[viewmodel.FloorNumList[j][0] - 1])
                {
                    foreach (Point3d pt in ptcollection)
                    {
                        ptslist.Add(pt);
                    }
                }
                var ptsListBoundary = ThGeometryTool.CalBoundingBox(ptslist);
                var ext = new Extents3d(ptsListBoundary[0], ptsListBoundary[1]);
                //旋转WCSTOUCS
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                var p = new Point3d(ext.MinPoint.X, ext.MaxPoint.Y, 0);
                PressureDrainageModelData.UCSRotateLocPoint = p;
                var rec = ext.ToRectangle();
                rec.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, p));
                extendList.Add(rec);
            }
            return extendList;
        }
    }
}