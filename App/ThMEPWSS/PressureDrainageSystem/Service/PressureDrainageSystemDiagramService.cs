using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPWSS.PressureDrainageSystem.Model;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;
namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class PressureDrainageSystemDiagramService
    {
        public PressureDrainageSystemDiagramService(List<PipeLineSystemUnitClass> pipeLineSystemUnits, PressureDrainageModelData modeldatas, Point3d insertPt)
        {
            PipeLineSystemUnits = pipeLineSystemUnits;
            InsertPt = insertPt;
            Modeldatas = modeldatas;
        }
        public List<PipeLineSystemUnitClass> PipeLineSystemUnits { get; set; }
        public Point3d InsertPt { get; set; }
        public PressureDrainageModelData Modeldatas { get; set; }

        public List<Entity> entities = new ();//用全局变量来承接每个排水系统中的元素
        public List<List<Entity>> allEntities = new ();//用全局变量来承接所有Entity元素
        public List<BlockReference> blocks = new ();//用全局变量来承接每个排水系统中的块参照
        public List<List<BlockReference>> allBlocks = new ();//用全局变量来承接所有块参照
        public double leftCoord = double.PositiveInfinity;//用全局变量来记录单元系统图当前最左边的X坐标
        public Line crossLayerGuideLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));//用全局变量来记录单元系统图中楼层切换时的横管线
        public List<List<Line>> testLines = new ();//用全局变量承接数据用于绘制系统图与原始图纸对应连线便于测试
        public Dictionary<int, List<int>> someLayerPipeIndexes = new ();//用全局变量记录相同排水系统单元对应立管的索引值
        public Dictionary<string, string> iDict = new();//用全局变量记录相同排水系统单元对应立管的编号字典
        public List<Dictionary<string, string>> identiferDict = new();//用全局变量记录相同排水系统单元对应立管的编号字典
        public List<List<Entity>> comparedEntitys = new ();//用全局变量承接用于比较是否为相同系统图的元素
        public List<Entity> comparedEntity = new ();//用全局变量承接用于比较是否为相同系统图的元素
        public List<Entity> uniqueEntitys = new ();//用全局变量承接用于承接相同排水系统图中独立的元素
        public List<List<List<DBText>>> identifiers = new();//用全局变量记录相同排水系统单元对应立管的编号
        public List<List<Point3d>> ptlocidentifers = new ();//用全局变量记录相同排水系统单元对应立管的编号的定位点
        public List<List<DBText>> identifer = new();//用全局变量记录相同排水系统单元对应立管的编号
        public List<Point3d> ptlocidentifer = new ();//用全局变量记录相同排水系统单元对应立管的编号的定位点
        public List<List<BlockReference>> drainwellbrs = new ();//用全局变量记录相同排水系统单元的排水井
        public List<BlockReference> drainwellbr = new ();//用全局变量记录相同排水系统单元的排水井
        public List<List<Line>> lineIdspump = new ();//用全局变量记录相同排水系统单元的潜水泵编号下划线
        public List<Line> lineIdpump = new ();//用全局变量记录相同排水系统单元的潜水泵编号下划线

        const double spacing = 50000;//排水系统间距&参考值
        const double textHeight = 350;//文字高度
        public int TmpParLayer = -1;//系统图绘制递归中上一立管的层数
        public int TmpParIndex = -1;//系统图绘制递归中上一立管的索引值
        public bool InCycle = false;//系统图绘制递归中上一立管的索引值
        public Point3d ptloctotalQ;//系统图中标注总流量的定位点
        public double totalQ = 0;//当前排水系统图的总流量
        const double pumpUnitSpacing = 9000;//排水系统图中并列潜水泵间距
      
        /// <summary>
        /// 绘制系统图主函数
        /// </summary>
        public void Draw()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                //画楼板线
                var floorLines = DrawFloorLine();

                //给排水单元内的每一根立管赋予标识Id，便于在递归中检测立管是否进入递归
                AttachIDToVerticalPipeForEachSystemUnit();

                //处理图形数据并开始递归
                ProcessDataAndStartRecursion(floorLines);
                MergeSameSystemUnits(floorLines);

                //排水系统图排版
                LayoutForSystemDiagram(floorLines);
            }
            return;
        }

        /// <summary>
        /// 绘制楼板线
        /// </summary>
        public List<Line> DrawFloorLine()
        {
            List<Line> floorLines = new ();
            double FloorLineLength = spacing * PipeLineSystemUnits.Count;//楼板线长度&参考值
            floorLines.Add(new Line(InsertPt, new Point3d(InsertPt.X + FloorLineLength, InsertPt.Y, 0)));
            int floorNumber = Modeldatas.FloorListDatas.Count;
            for (int i = 0; i < floorNumber + 1; i++)
            {
                if (i > 0)
                {
                    double floorLineSpace = Modeldatas.FloorLineSpace;
                    Line floorLine = new Line(floorLines[i - 1].StartPoint, floorLines[i - 1].EndPoint);
                    floorLine.TransformBy(Matrix3d.Displacement(new Vector3d(0, -floorLineSpace, 0)));
                    floorLines.Add(floorLine);
                }
            }
            return floorLines;
        }
       
        /// <summary>
        /// 给排水单元内的每一根立管赋予标识Id，便于在递归中检测立管是否进入递归
        /// </summary>
        public void AttachIDToVerticalPipeForEachSystemUnit()
        {
            foreach (var systemUnit in PipeLineSystemUnits)
            {
                systemUnit.verticalPipeId = new List<int>();
                int idNumber = -1;
                foreach (var unit in systemUnit.PipeLineUnits)
                {
                    foreach (var pipe in unit.VerticalPipes)
                    {
                        idNumber += 1;
                        pipe.Id = idNumber;
                        systemUnit.verticalPipeId.Add(idNumber);
                    }
                }
            }
            return;
        }
      
        /// <summary>
        /// 处理图形数据并开始递归
        /// </summary>
        /// <param name="floorLines"></param>
        public void ProcessDataAndStartRecursion(List<Line> floorLines)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                Point3d ptloc = floorLines[0].StartPoint;//每一个排水单元绘制的起点
                double heightDisTofloorLine = 590;//固定值
                double widthDisTofloorLineStartPt = 50000;//参考值
                ptloc = ptloc.TransformBy(Matrix3d.Displacement(new Vector3d(widthDisTofloorLineStartPt, -heightDisTofloorLine, 0)));
                int effectiveUnitsCount = 0;//有效的排水系统单元数
                for (int i = 0; i < PipeLineSystemUnits.Count; i++)
                {
                    int indexCurPoint = FindIndexStartVerticalPipe(PipeLineSystemUnits[i]);//排水口立管在排水单元中的索引值
                    int unitLayerCount = PipeLineSystemUnits[i].CrossLayerConnectedArrs.Count;
                    indexCurPoint = indexCurPoint > -1 ? indexCurPoint : 0;
                    someLayerPipeIndexes = new Dictionary<int, List<int>>();
                    effectiveUnitsCount += 1;
                    entities = new();
                    lineIdpump = new();
                    drainwellbr = new();
                    ptlocidentifer = new();
                    identifer = new();
                    iDict = new();
                    uniqueEntitys = new();
                    comparedEntity = new();
                    blocks = new();
                    List<int> ids = new();
                    List<Point3d> parPoints = new();
                    List<int> parLayers = new();
                    List<int> parIndexes = new();
                    for (int j = 0; j < PipeLineSystemUnits[i].PipeLineUnits.Count; j++)
                    {
                        someLayerPipeIndexes.Add(j, new List<int>());
                    }
                    PipeLineSystemUnits[i].verticalPipeId.ForEach(o => ids.Add(o));
                    ids.Remove(PipeLineSystemUnits[i].PipeLineUnits[0].VerticalPipes[indexCurPoint].Id);
                    parLayers.Add(-1);
                    parIndexes.Add(-1);
                    leftCoord = ptloc.X;
                    DrawDrainWell(PipeLineSystemUnits[i], ptloc, indexCurPoint, heightDisTofloorLine);
                    drainwellbrs.Add(new List<BlockReference>());
                    drainwellbr.ForEach(o => drainwellbrs[drainwellbrs.Count - 1].Add(o));
                    drainwellbr.ForEach(o => o.Visible = false); ;
                    drainwellbr.Clear();
                    int curLayer = 0;
                    int curIndex = indexCurPoint;
                    totalQ = 0;
                    RecursivelyPlot(PipeLineSystemUnits[i], floorLines, ids, ref curLayer, curIndex, ptloc, parLayers, parIndexes, parPoints);
                    lineIdspump.Add(lineIdpump);
                    ptlocidentifers.Add(ptlocidentifer);
                    identifiers.Add(identifer);
                    identiferDict.Add(iDict);
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "排水管径100", ptloctotalQ, new Scale3d(1), 0);
                    brId1.SetDynBlockValue("可见性", "DN" + CalculateMergePipeDiameter(totalQ).ToString());
                    var br1 = adb.Element<BlockReference>(brId1);
                    DefinePropertiesOfCADObjects(br1, "W-NOTE");
                    blocks.Add(br1);
                    entities.ForEach(o => comparedEntity.Add(o));
                    blocks.ForEach(o => comparedEntity.Add(o));
                    comparedEntitys.Add(comparedEntity);
                    allEntities.Add(entities);
                    allBlocks.Add(new List<BlockReference>());
                    blocks.ForEach(o => allBlocks[allBlocks.Count - 1].Add(o));
                    blocks.Clear();
                }
                return;
            }
        }
       
        /// <summary>
        /// 合并相同的排水系统单元图
        /// </summary>
        /// <param name="floorLines"></param>
        public void MergeSameSystemUnits(List<Line> floorLines)
        {
            double heightDisTofloorLine = 590;//固定值
            double widthDisTofloorLineStartPt = 50000;//参考值
            Point3d pt = floorLines[0].StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(widthDisTofloorLineStartPt, -heightDisTofloorLine, 0)));
            List<List<Line>> guidelines = new ();
            for (int i = 0; i < PipeLineSystemUnits.Count; i++)
            {
                List<Line> lines = new List<Line>();
                lines.Add(new Line(PipeLineSystemUnits[i].SameUnitsStartPt[0], pt));
                guidelines.Add(lines);
            }
            for (int i = 1; i < comparedEntitys.Count; i++)
            {
                foreach (var br in drainwellbrs[i])
                {
                    br.Visible = false;
                }
                for (int j = 0; j < i; j++)
                {
                    if (IsSameSystemUnits(comparedEntitys[i], comparedEntitys[j]))
                    {
                        guidelines[j].Add(new Line(PipeLineSystemUnits[i].SameUnitsStartPt[0], pt));
                        guidelines.RemoveAt(i);
                        lineIdspump.RemoveAt(i);
                        if (drainwellbrs[i].Count > 0)
                        {
                            int cond_QuitCycle = 0;
                            foreach (var br in drainwellbrs[j])
                            {
                                if (br.Id.GetAttributeInBlockReference("-") == drainwellbrs[i][0].Id.GetAttributeInBlockReference("-"))
                                {
                                    cond_QuitCycle = 1;
                                    break;
                                }
                            }
                            if (cond_QuitCycle == 0)
                            {
                                drainwellbrs[j].Add(drainwellbrs[i][0]);
                            }
                        }
                        drainwellbrs.RemoveAt(i);
                        foreach (var curId in identifiers[i])
                        {
                            int cond_QuitCycle = 0;
                            foreach (var curText in curId)
                            {
                                foreach (var testId in identifiers[j])
                                {
                                    foreach (var testText in testId)
                                    {
                                        if (identiferDict[i].ContainsKey(curText.TextString) && identiferDict[j].ContainsKey(testText.TextString))
                                        {
                                            if (identiferDict[i][curText.TextString] == identiferDict[j][testText.TextString] && curText.HorizontalMode == testText.HorizontalMode)
                                            {
                                                curText.Position = testText.Position;
                                                curText.AlignmentPoint = testText.AlignmentPoint;
                                                testId.Add(curText);
                                                cond_QuitCycle = 1;
                                                break;
                                            }
                                        }
                                    }
                                    if (cond_QuitCycle == 1)
                                    {
                                        break;
                                    }
                                }
                                if (cond_QuitCycle == 1)
                                {
                                    break;
                                }
                            }
                        }
                        identifiers.RemoveAt(i);
                        identiferDict.RemoveAt(i);
                        allEntities.RemoveAt(i);
                        allBlocks[i].ForEach(o => o.Visible = false);
                        allBlocks.RemoveAt(i);
                        PipeLineSystemUnits.RemoveAt(i);
                        comparedEntitys.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            comparedEntitys.Clear();
            foreach (var curId in identifiers)
            {
                foreach (var curText in curId)
                {
                    List<DBText> dBTexts = new();
                    if (curText.Count > 0)
                    {
                        dBTexts.Add(curText[0]);
                        if (curText.Count > 1)
                        {
                            for (int p = 1; p < curText.Count; p++)
                            {
                                int cond_QuitCycle = 0;
                                foreach (var testText in dBTexts)
                                {
                                    if (curText[p].TextString == testText.TextString)
                                    {
                                        cond_QuitCycle = 1;
                                        break;
                                    }
                                }
                                if (cond_QuitCycle == 0)
                                {
                                    dBTexts.Add(curText[p]);
                                }
                            }
                        }
                    }
                    curText.Clear();
                    dBTexts.ForEach(o => curText.Add(o));
                    double disXform = 0;
                    foreach (var text in curText)
                    {
                        text.TransformBy(Matrix3d.Displacement(new Vector3d(0, disXform, 0)));
                        disXform += textHeight + 50;
                    }
                }
            }
            for (int i = 0; i < identifiers.Count; i++)
            {
                foreach (var text in identifiers[i])
                {
                    text.ForEach(o => allEntities[i].Add(o));
                }
            }
            identifiers.Clear();
            testLines.Clear();
            for (int i = 0; i < guidelines.Count; i++)
            {
                testLines.Add(new List<Line>());
                foreach (var line in guidelines[i])
                {
                    testLines[i].Add(line);
                }
            }
            foreach (var well in drainwellbrs)
            {
                double disXform = 0;
                foreach (var br in well)
                {
                    br.TransformBy(Matrix3d.Displacement(new Vector3d(disXform, 0, 0)));
                    disXform += 800;
                }
            }
            for (int i = 0; i < drainwellbrs.Count; i++)
            {
                foreach (var br in drainwellbrs[i])
                {
                    allBlocks[i].Add(br);
                }
            }
            drainwellbrs.Clear();
            for (int i = 0; i < lineIdspump.Count; i++)
            {
                foreach (var pump in lineIdspump[i])
                {
                    allEntities[i].Add(pump);
                }
            }
        }
       
        /// <summary>
        /// 排水系统图排版
        /// </summary>
        /// <param name="allEntities"></param>
        public void LayoutForSystemDiagram(List<Line> floorLines)
        {
            double totalspacine = 0;
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                for (int i = 0; i < allEntities.Count; i++)
                {
                    Matrix3d mat = Matrix3d.Displacement(new Vector3d(totalspacine, 0, 0));
                    foreach (var ent in allEntities[i])
                    {
                        ent.TransformBy(mat);
                        ent.AddToCurrentSpace();
                    }
                    foreach (var br in allBlocks[i])
                    {
                        br.Visible = true;
                        br.TransformBy(mat);
                    }
                    foreach (var line in testLines[i])
                    {
                        Point3d pt = line.EndPoint.TransformBy(mat);
                        var k = new Line(line.StartPoint, pt);
                        k.AddToCurrentSpace();
                    }
                    totalspacine += spacing;
                }
                foreach (var floorLine in floorLines)
                {
                    floorLine.TransformBy(Matrix3d.Scaling((totalspacine + spacing) / floorLine.Length, floorLine.StartPoint));
                    floorLine.AddToCurrentSpace();
                    DefinePropertiesOfCADObjects(floorLine, "W-NOTE");
                }
                MessageBox.Show("共有排水单元" + allEntities.Count.ToString() + "组");
            }
        }
        
        //子函数
        /// <summary>
        /// 寻找排水口立管在排水系统中的索引值
        /// </summary>
        /// <param name="pipeLineSystemUnit"></param>
        /// <returns></returns>
        public static int FindIndexStartVerticalPipe(PipeLineSystemUnitClass pipeLineSystemUnit)
        {
            int indexCurPoint = -1;
            for (int j = 0; j < pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes.Count; j++)
            {
                var pipe = pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[j];
                if (pipe.isUnitStart)
                {
                    indexCurPoint = j;
                }
                else if (pipe.AppendedDrainWell != null)
                {
                    indexCurPoint = j;
                }
            }
            return indexCurPoint;
        }
       
        /// <summary>
        /// 绘制排水井
        /// </summary>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="ptloc"></param>
        /// <param name="index"></param>
        public void DrawDrainWell(PipeLineSystemUnitClass pipeLineSystemUnit, Point3d ptloc, int index, double heightDisTofloorLine)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                double disRefDrainWell = 400;//定义集水井相对位置&参考值
                double bushHeigthHalf_a = 100;//套管高度&参考值
                double bushHeigthHalf_b = 300;//套管高度&参考值
                double disRefElv = 450;//定义标高相对位置&参考值
                double disRefPipeDiameter = 450;//定义排水管径相对位置&参考值
                List<BlockReference> tmpBlocksLayerBUSH = new List<BlockReference>();
                List<BlockReference> tmpBlocksLayerNOTE = new List<BlockReference>();
                List<Entity> tmpEntitiesLayerRAINPIPE = new List<Entity>();
                int drainageMode = pipeLineSystemUnit.DrainageMode;
                if (drainageMode == 1 || drainageMode == 2)//简单穿顶板DrainageMode
                {
                    double disFromLineToGround = 800;//固定值
                    Point3d pt1 = new Point3d(ptloc.X, ptloc.Y + heightDisTofloorLine + disFromLineToGround, 0);
                    Line line1 = new Line(ptloc, pt1);
                    tmpEntitiesLayerRAINPIPE.Add(line1);
                    Point3d brlocPt = new Point3d(ptloc.X, ptloc.Y + heightDisTofloorLine - bushHeigthHalf_a, 0);
                    var brId = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", brlocPt, new Scale3d(0), 0);
                    var br = adb.Element<BlockReference>(brId);
                    br.Rotation = Math.PI / 2;
                    tmpBlocksLayerBUSH.Add(br);
                }
                else if (false)//穿顶板进水井
                {
                    double disFromLineToGround = 800;//固定值
                    double lineLength = 3000;//固定值
                    Point3d pt1 = new Point3d(ptloc.X, ptloc.Y + heightDisTofloorLine + disFromLineToGround, 0);
                    Point3d pt2 = new Point3d(pt1.X + lineLength, pt1.Y, 0);
                    Line line1 = new Line(ptloc, pt1);
                    Line line2 = new Line(pt1, pt2);
                    tmpEntitiesLayerRAINPIPE.Add(line1);
                    tmpEntitiesLayerRAINPIPE.Add(line2);
                    Point3d brlocPt1 = new Point3d(pt1.X + disRefElv, 0, 0);
                    Dictionary<string, string> atts1 = new Dictionary<string, string>();
                    atts1.Add("标高", "室外覆土敷设");
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", brlocPt1, new Scale3d(0), 0, atts1);
                    brId1.SetDynBlockValue("翻转状态1", (short)1);
                    brId1.SetDynBlockValue("翻转状态2", (short)0);
                    tmpBlocksLayerNOTE.Add(adb.Element<BlockReference>(brId1));
                    Point3d brlocPt2 = new Point3d(pt1.X + disRefPipeDiameter, pt1.Y, 0);
                    ptloctotalQ = brlocPt2;
                    Point3d brlocPt3 = new Point3d(pt2.X + disRefDrainWell, pt2.Y - disRefDrainWell, 0);
                    Dictionary<string, string> atts = new Dictionary<string, string>();
                    string str = "-";
                    if (pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell != null)
                    {
                        str = pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell.Label;
                    }
                    atts.Add("-", str);
                    var brId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "重力流雨水井编号", brlocPt3, new Scale3d(0), 0, atts);
                    var br3 = adb.Element<BlockReference>(brId3);
                    DefinePropertiesOfCADObjects(br3, "W-NOTE");
                    drainwellbr.Add(br3);
                    Point3d brlocPt4 = new Point3d(ptloc.X, ptloc.Y + heightDisTofloorLine + bushHeigthHalf_a, 0);
                    var brId4 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", brlocPt4, new Scale3d(0), 0);
                    var br4 = adb.Element<BlockReference>(brId4);
                    br4.Rotation = Math.PI / 2 * 3;
                    tmpBlocksLayerBUSH.Add(br4);
                }
                else if (drainageMode == 3)//穿外墙排水方式
                {
                    double disFromptloc = 1100;//固定值
                    Point3d pt1 = new Point3d(ptloc.X, ptloc.Y + disFromptloc, 0);
                    Point3d pt2 = new Point3d(pt1.X + disFromptloc, pt1.Y, 0);
                    Line line1 = new Line(pt1, pt2);
                    Line line2 = new Line(ptloc, pt1);
                    tmpEntitiesLayerRAINPIPE.Add(line1);
                    tmpEntitiesLayerRAINPIPE.Add(line2);
                    Point3d ptlocbr1 = new Point3d(pt2.X + disRefDrainWell, pt2.Y - disRefDrainWell, 0);
                    Dictionary<string, string> atts = new Dictionary<string, string>();
                    string str = "-";
                    if (pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell != null)
                    {
                        str = pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell.Label;
                    }
                    atts.Add("-", str);
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", pipeLineSystemUnit.DrainWell != null ? pipeLineSystemUnit.DrainWell.WellTypeName : "重力流雨水井编号", ptlocbr1, new Scale3d(0), 0, atts);
                    var br9 = adb.Element<BlockReference>(brId1);
                    DefinePropertiesOfCADObjects(br9, "W-NOTE");
                    drainwellbr.Add(br9);
                    Point3d ptlocbr2 = new Point3d(pt2.X - bushHeigthHalf_b, pt2.Y, 0);
                    var brId2 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", ptlocbr2, new Scale3d(0), 0);
                    tmpBlocksLayerBUSH.Add(adb.Element<BlockReference>(brId2));
                }
                else//穿侧墙排水方式
                {
                    double disFromptloc = 3000;//固定值
                    Point3d pt1 = new Point3d(ptloc.X + disFromptloc, ptloc.Y, 0);
                    Line line = new Line(ptloc, pt1);
                    tmpEntitiesLayerRAINPIPE.Add(line);
                    Point3d ptlocbr1 = new Point3d(ptloc.X + disRefElv, ptloc.Y, 0);
                    Dictionary<string, string> atts1 = new Dictionary<string, string>();
                    atts1.Add("标高", "室外覆土敷设");
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", ptlocbr1, new Scale3d(0), 0, atts1);
                    brId1.SetDynBlockValue("翻转状态1", (short)1);
                    brId1.SetDynBlockValue("翻转状态2", (short)0);
                    tmpBlocksLayerNOTE.Add(adb.Element<BlockReference>(brId1));
                    Point3d ptlocbr2 = new Point3d(ptloc.X + disRefPipeDiameter, ptloc.Y, 0);
                    ptloctotalQ = ptlocbr2;
                    Point3d ptlocbr3 = new Point3d(pt1.X + disRefDrainWell, pt1.Y - disRefDrainWell, 0);
                    Dictionary<string, string> atts = new Dictionary<string, string>();
                    string str = "-";
                    if (pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell != null)
                    {
                        str = pipeLineSystemUnit.PipeLineUnits[0].VerticalPipes[index].AppendedDrainWell.Label;
                    }
                    atts.Add("-", str);
                    var brId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "重力流雨水井编号", ptlocbr3, new Scale3d(0), 0, atts);
                    var br10 = adb.Element<BlockReference>(brId3);
                    DefinePropertiesOfCADObjects(br10, "W-NOTE");
                    drainwellbr.Add(br10);
                    Point3d ptlocbr4 = new Point3d(pt1.X - bushHeigthHalf_b, pt1.Y, 0);
                    var brId4 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", ptlocbr4, new Scale3d(0), 0);
                    tmpBlocksLayerBUSH.Add(adb.Element<BlockReference>(brId4));
                }
                tmpEntitiesLayerRAINPIPE.ForEach(o => DefinePropertiesOfCADObjects(o, "W-RAIN-PIPE", "CONTINOUS"));
                tmpEntitiesLayerRAINPIPE.ForEach(o => entities.Add(o));
                tmpBlocksLayerNOTE.ForEach(o => DefinePropertiesOfCADObjects(o, "W-NOTE"));
                tmpBlocksLayerNOTE.ForEach(o => blocks.Add(o));
                tmpBlocksLayerBUSH.ForEach(o => DefinePropertiesOfCADObjects(o, "W-BUSH"));
                tmpBlocksLayerBUSH.ForEach(o => blocks.Add(o));
            }
        }
       
        /// <summary>
        /// 判断两个排水系统图是否相同
        /// </summary>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        public bool IsSameSystemUnits(List<Entity> ent1, List<Entity> ent2)
        {
            if (ent1.Count != ent2.Count)
            {
                return false;
            }
            List<Line> lines1 = new(), lines2 = new();
            List<Polyline> plys1 = new(), plys2 = new();
            List<DBText> dbs1 = new(), dbs2 = new();
            List<BlockReference> brs1 = new(), brs2 = new();
            foreach (var j in ent1)
            {
                if (j is Line)
                {
                    lines1.Add((Line)j);
                }
                else if (j is Polyline)
                {
                    plys1.Add((Polyline)j);
                }
                else if (j is DBText)
                {
                    dbs1.Add((DBText)j);
                }
                else
                {
                    brs1.Add((BlockReference)j);
                }
            }
            foreach (var j in ent2)
            {
                if (j is Line)
                {
                    lines2.Add((Line)j);
                }
                else if (j is Polyline)
                {
                    plys2.Add((Polyline)j);
                }
                else if (j is DBText)
                {
                    dbs2.Add((DBText)j);
                }
                else
                {
                    brs2.Add((BlockReference)j);
                }
            }
            if (lines1.Count != lines2.Count || plys1.Count != plys2.Count || dbs1.Count != dbs2.Count || brs1.Count != brs2.Count)
            {
                return false;
            }
            double length1 = 0;
            double length2 = 0;
            foreach (var j in lines1)
            {
                if (j.Layer == "W-RAIN-PIPE")
                {
                    length1 += j.Length;
                }
            }
            foreach (var j in lines2)
            {
                if (j.Layer == "W-RAIN-PIPE")
                {
                    length2 += j.Length;
                }
            }
            if (length1 != length2)
            {
                return false;
            }
            brs1.Clear();
            brs2.Clear();
            return true;
        }

        //递归绘制系统图函数及其子函数
        /// <summary>
        /// 递归绘制排水系统单元
        /// </summary>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="floorLines"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <param name="curPoint"></param>
        /// <param name="parLayers"></param>
        /// <param name="parIndexes"></param>
        /// <param name="parPoints"></param>
        public void RecursivelyPlot(PipeLineSystemUnitClass pipeLineSystemUnit, List<Line> floorLines, List<int> ids, ref int curLayer, int curIndex, Point3d curPoint, List<int> parLayers, List<int> parIndexes, List<Point3d> parPoints)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                //判断该立管下一层是否有对应立管并处理
                bool cond_HasSonPipe = PlotForHasingSonVerticalPipeInLowerLayer(pipeLineSystemUnit, ref ids, ref curLayer, ref curIndex, ref curPoint, ref parLayers, ref parIndexes, ref parPoints);
                if (cond_HasSonPipe)
                {
                    //从该立管开始递归
                    int layer = parLayers[parLayers.Count - 1];
                    int ind = parIndexes[parIndexes.Count - 1];
                    Point3d point = parPoints[parPoints.Count - 1];
                    RecursivelyPlot(pipeLineSystemUnit, floorLines, ids, ref curLayer, curIndex, curPoint, parLayers, parIndexes, parPoints);
                    var pipe = pipeLineSystemUnit.PipeLineUnits[layer].VerticalPipes[ind];
                    if (pipe.AppendedSubmergedPump != null)
                    {
                        pipe.totalQ += CalculateUsedPump(pipe.AppendedSubmergedPump.Allocation) * CalculatePipeDiameter(pipe.AppendedSubmergedPump.paraQ);
                    }
                    int diameter = CalculateMergePipeDiameter(pipe.totalQ);
                    diameter = diameter > 50 ? diameter : 50;
                    Point3d ptlocelv = new Line(point, curPoint).GetMidpoint();
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "排水管径100", ptlocelv, new Scale3d(1), Math.PI / 2);
                    brId1.SetDynBlockValue("可见性", "DN" + diameter.ToString());
                    var br1 = adb.Element<BlockReference>(brId1);
                    DefinePropertiesOfCADObjects(br1, "W-NOTE");
                    blocks.Add(br1);
                }
                else
                {
                    //如果是水泵立管
                    bool cond_IsPumpPipe = pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].AppendedSubmergedPump != null ? true : false;
                    if (cond_IsPumpPipe)
                    {
                        var pump = pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].AppendedSubmergedPump;
                        Point3d ptLocPumpRec = floorLines[curLayer + 1].GetClosestPointTo(curPoint, false);
                        double frameHeigth = pump.PumpCount >= 3 ? 2150 : 1650;
                        double frameWidth = 1500 + Math.Max(0, pump.PumpCount - 2) * 800;//水泵框宽度
                        ptLocPumpRec = ptLocPumpRec.TransformBy(Matrix3d.Displacement(new Vector3d(0, -frameHeigth / 2, 0)));
                        Polyline frameRec = ptLocPumpRec.CreateRectangle(frameWidth, frameHeigth);
                        //初步绘制潜水泵立管
                        InitiallyPlotPumpVerticalPipe(pipeLineSystemUnit, pump, ptLocPumpRec, frameHeigth, frameWidth, frameRec, curPoint, parLayers, parIndexes);
                        //绘制潜水泵立管细节
                        PlotPumpVerticalPipe(ref pipeLineSystemUnit, pump, floorLines, ptLocPumpRec, frameHeigth, frameWidth, frameRec, curLayer, curIndex, curPoint, parLayers, parIndexes);
                        //完善具有特殊用途的潜水泵立管
                        if (pump.Location == "消防电梯" || pump.Location == "电缆沟")
                        {
                            CompletePumpVerticalPipeForSpecialUse(pump, ptLocPumpRec, frameRec, frameHeigth, frameWidth);
                        }
                    }
                    //如果不是潜水泵立管
                    {
                        List<int> indexes = new List<int>();
                        List<Point3d> points = new List<Point3d>();
                        List<int> layers = new List<int>();
                        //对于同层还有其它立管情况下的递归前处理
                        PreProcessForSameLayerPipeCondition(indexes, points, layers, pipeLineSystemUnit, ids, curLayer, curIndex, curPoint);
                        if (layers.Count > 0)
                        {
                            //继续绘图并递归到下一个立管
                            ProcessForSameLayerAndRecursionToNextPipe(indexes, points, layers, pipeLineSystemUnit, floorLines, ids, curLayer, curIndex, curPoint, parLayers, parIndexes, parPoints);
                        }
                        else
                        {
                            //如果当前层不是B1层，跳回上一层循环
                            if (InCycle)
                            {
                                crossLayerGuideLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
                                return;
                            }
                            else if (parLayers.Count != 1)
                            {
                                curLayer = parLayers[parLayers.Count - 1];
                                curIndex = parIndexes[parIndexes.Count - 1];
                                double coordX = parPoints[parPoints.Count - 1].X;
                                crossLayerGuideLine = new Line(parPoints[parPoints.Count - 1], new Point3d(curPoint.X, parPoints[parPoints.Count - 1].Y, 0));
                                if (parPoints[parPoints.Count - 1].X - curPoint.X > 2000)
                                {
                                    coordX = curPoint.X;
                                    DefinePropertiesOfCADObjects(crossLayerGuideLine, "W-RAIN-PIPE", "CONTINOUS");
                                }
                                leftCoord = curPoint.X < leftCoord ? curPoint.X : leftCoord;
                                Point3d nextpt = new Point3d(coordX, parPoints[parPoints.Count - 1].Y, 0);
                                curPoint = nextpt;
                                leftCoord = curPoint.X < leftCoord ? curPoint.X : leftCoord;
                                TmpParLayer = parLayers[parLayers.Count - 1];
                                parLayers.RemoveAt(parLayers.Count - 1);
                                TmpParIndex = parIndexes[parIndexes.Count - 1];
                                parIndexes.RemoveAt(parIndexes.Count - 1);
                                parPoints.RemoveAt(parPoints.Count - 1);
                                RecursivelyPlot(pipeLineSystemUnit, floorLines, ids, ref curLayer, curIndex, curPoint, parLayers, parIndexes, parPoints);
                            }
                            else
                            {
                                crossLayerGuideLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
                                return;
                            }
                        }
                    }
                }
            }
        }
      
        /// <summary>
        /// 判断该立管下一层是否有对应立管
        /// </summary>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <returns></returns>
        public bool HasSonVerticalPipe(PipeLineSystemUnitClass pipeLineSystemUnit, List<int> ids, int curLayer, int curIndex)
        {
            bool cond_HasSonPipe = false;
            if (curLayer != pipeLineSystemUnit.CrossLayerConnectedArrs.Count - 1)
            {
                for (int i = 0; i < pipeLineSystemUnit.PipeLineUnits[curLayer + 1].VerticalPipes.Count; i++)
                {
                    bool cond_a = pipeLineSystemUnit.CrossLayerConnectedArrs[curLayer + 1][curIndex, i] == 1;
                    bool cond_b = ids.Contains(pipeLineSystemUnit.PipeLineUnits[curLayer + 1].VerticalPipes[i].Id);
                    if (cond_a && cond_b)
                    {
                        cond_HasSonPipe = true;
                    }
                    break;
                }
            }
            return cond_HasSonPipe;
        }
       
        /// <summary>
        /// 判断该立管下一层是否有对应立管并处理
        /// </summary>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="floorLines"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <param name="curPoint"></param>
        /// <param name="parLayers"></param>
        /// <param name="parIndexes"></param>
        /// <param name="parPoints"></param>
        /// <returns></returns>
        public bool PlotForHasingSonVerticalPipeInLowerLayer(PipeLineSystemUnitClass pipeLineSystemUnit, ref List<int> ids, ref int curLayer, ref int curIndex, ref Point3d curPoint, ref List<int> parLayers, ref List<int> parIndexes, ref List<Point3d> parPoints)
        {
            bool cond_HasSonPipe = false;
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                if (curLayer != pipeLineSystemUnit.CrossLayerConnectedArrs.Count - 1)
                {
                    for (int i = 0; i < pipeLineSystemUnit.PipeLineUnits[curLayer + 1].VerticalPipes.Count; i++)
                    {
                        bool cond_IsSameCrossed = pipeLineSystemUnit.CrossLayerConnectedArrs[curLayer + 1][curIndex, i] == 1;
                        bool cond_IdsContains = ids.Contains(pipeLineSystemUnit.PipeLineUnits[curLayer + 1].VerticalPipes[i].Id);
                        if (cond_IsSameCrossed && cond_IdsContains)
                        {
                            pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].HasChildPipe = true;
                            parLayers.Add(curLayer);
                            parIndexes.Add(curIndex);
                            parPoints.Add(curPoint);
                            cond_HasSonPipe = true;
                            curLayer += 1;
                            curIndex = i;
                            //Draw
                            double disFromParPtToParFloor = curLayer > 1 ? 900 : 590;//上层立管距上层楼板线距离
                            double disBetweenParSonVertical = Modeldatas.FloorLineSpace + 900 - disFromParPtToParFloor;//两层立管之间的距离
                            Point3d nextPt = new Point3d(curPoint.X, curPoint.Y - disBetweenParSonVertical, 0);
                            Line line = new Line(curPoint, nextPt);
                            DefinePropertiesOfCADObjects(line, "W-RAIN-PIPE", "CONTINOUS");
                            entities.Add(line);
                            curPoint = nextPt;
                            leftCoord = curPoint.X < leftCoord ? curPoint.X : leftCoord;
                            ids.Remove(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].Id);
                            Point3d ptlocId = line.GetMidpoint().TransformBy(Matrix3d.Displacement(new Vector3d(100, 0, 0)));
                            double textLength = 0;
                            foreach (var identifier in pipeLineSystemUnit.PipeLineUnits[parLayers[parLayers.Count - 1]].VerticalPipes[parIndexes[parIndexes.Count - 1]].SameTypeIdentifiers)
                            {
                                DBText dBText = new DBText();
                                DefinePropertiesOfCADDBTexts(dBText, "W-NOTE", identifier, ptlocId, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"));
                                Extents3d ext = (Extents3d)dBText.Bounds;
                                textLength = ext.ToRectangle().Length / 2 > textLength ? ext.ToRectangle().Length / 2 : textLength;
                                identifer.Add(new List<DBText>());
                                identifer[identifer.Count - 1].Add(dBText);
                                ptlocidentifer.Add(ptlocId);
                                if (!iDict.ContainsKey(identifier))
                                {
                                    iDict.Add(identifier, "");
                                }
                            }
                            Point3d ptlocLine = line.GetMidpoint().TransformBy(Matrix3d.Displacement(new Vector3d(0, -textHeight * 0.8, 0)));
                            Line lineIdentifier = new Line(ptlocLine, new Point3d(ptlocLine.X + textLength, ptlocLine.Y, 0));
                            DefinePropertiesOfCADObjects(lineIdentifier, "W-NOTE");
                            entities.Add(lineIdentifier);
                            break;
                        }
                    }
                }
            }
            return cond_HasSonPipe;
        }
       
        /// <summary>
        /// 初步绘制潜水泵立管
        /// </summary>
        /// <param name="pump"></param>
        /// <param name="ptLocPumpRec"></param>
        /// <param name="frameHeigth"></param>
        /// <param name="frameWidth"></param>
        /// <param name="frameRec"></param>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="floorLines"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <param name="curPoint"></param>
        /// <param name="parLayers"></param>
        /// <param name="parIndexes"></param>
        /// <param name="parPoints"></param>
        /// <returns></returns>
        public SubmergedPumpClass InitiallyPlotPumpVerticalPipe(PipeLineSystemUnitClass pipeLineSystemUnit, SubmergedPumpClass pump, Point3d ptLocPumpRec, double frameHeigth, double frameWidth, Polyline frameRec, Point3d curPoint, List<int> parLayers, List<int> parIndexes)
        {
            if (crossLayerGuideLine.Length != 1)
            {
                crossLayerGuideLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
                curPoint = new Point3d(curPoint.X - pumpUnitSpacing, curPoint.Y, 0);
                leftCoord = curPoint.X < leftCoord ? curPoint.X : leftCoord;
            }
            DefinePropertiesOfCADObjects(frameRec, "W-NOTE");
            if (pump.PumpCount == 1)
            {
                frameRec.TransformBy(Matrix3d.Displacement(new Vector3d(-250, 0, 0)));
                ptLocPumpRec = ptLocPumpRec.TransformBy(Matrix3d.Displacement(new Vector3d(-250, 0, 0)));
            }
            else if (pump.PumpCount == 2)
            {
                frameRec.TransformBy(Matrix3d.Displacement(new Vector3d(-500, 0, 0)));
                ptLocPumpRec = ptLocPumpRec.TransformBy(Matrix3d.Displacement(new Vector3d(-500, 0, 0)));
            }
            else if (pump.PumpCount > 2)
            {
                frameRec.TransformBy(Matrix3d.Displacement(new Vector3d(-800, 0, 0)));
                ptLocPumpRec = ptLocPumpRec.TransformBy(Matrix3d.Displacement(new Vector3d(-800, 0, 0)));
            }
            entities.Add(frameRec);
            string allocation = pump.Allocation;
            string para1 = "待填入", para2 = "待填入", para3 = "待填入", para4 = "待填入", para5 = "待填入", para6 = "待填入";
            double paraQ_real = -1;
            if (pump.paraH > 0)
            {
                if (pump.Allocation == "")
                {
                }
                else if (pump.Allocation == "一用" || pump.Allocation == "一用一备")
                {
                    para1 = Math.Round(pump.paraH - 0.10, 2).ToString("0.00") + "m";
                    para2 = Math.Round(pump.paraH - 0.20, 2).ToString("0.00") + "m";
                    paraQ_real = pump.paraQ;
                }
                else if (pump.Allocation == "两用" || pump.Allocation == "两用一备")
                {
                    para1 = Math.Round(pump.paraH - 0.10, 2).ToString("0.00") + "m";
                    para4 = Math.Round(pump.paraH - 0.20, 2).ToString("0.00") + "m";
                    para5 = Math.Round(pump.paraH - 0.30, 2).ToString("0.00") + "m";
                    paraQ_real = pump.paraQ * 2;
                }
                else
                {
                    para1 = Math.Round(pump.paraH - 0.10, 2).ToString("0.00") + "m";
                    para3 = Math.Round(pump.paraH - 0.20, 2).ToString("0.00") + "m";
                    para4 = Math.Round(pump.paraH - 0.30, 2).ToString("0.00") + "m";
                    para5 = Math.Round(pump.paraH - 0.40, 2).ToString("0.00") + "m";
                    paraQ_real = pump.paraQ * 3;
                }
            }
            if (pump.paraQ > 0 && pump.Depth > 0)
            {
                if (paraQ_real <= 20)
                {
                    para6 = Math.Round(pump.paraH - pump.Depth + 0.30, 2).ToString("0.00") + "m";
                }
                else if (paraQ_real <= 65)
                {
                    para6 = Math.Round(pump.paraH - pump.Depth + 0.35, 2).ToString("0.00") + "m";
                }
                else
                {
                    para6 = Math.Round(pump.paraH - pump.Depth + 0.45, 2).ToString("0.00") + "m";
                }
            }
            Point3d ptloc_br_tmp_01;
            if (pump.PumpCount >= 3)
            {
                ptloc_br_tmp_01 = new Point3d(ptLocPumpRec.X + frameWidth / 2 + 300, ptLocPumpRec.Y - frameHeigth / 2 + 200, 0);
            }
            else
            {
                ptloc_br_tmp_01 = new Point3d(ptLocPumpRec.X + frameWidth / 2 + 300, ptLocPumpRec.Y - frameHeigth / 2 + 200, 0);
            }
            List<DBText> bTexts = new List<DBText>();
            DBText dB1 = new(), dB2 = new(), dB3 = new(), dB4 = new(), dB5 = new(), dB6 = new(), dB7 = new(), dB8 = new();
            dB1.TextString = "报警液位:     " + para1;
            dB8.TextString = "启泵液位:     " + para2;
            dB7.TextString = "三泵启泵液位: " + para3;
            dB2.TextString = "二泵启泵液位: " + para4;
            dB3.TextString = "一泵启泵液位: " + para5;
            dB4.TextString = "停泵液位:     " + para6;
            dB1.Position = dB2.Position = dB3.Position = dB4.Position = dB7.Position = dB8.Position = ptloc_br_tmp_01;
            if (pump.PumpCount == 1 || pump.Allocation == "一用一备")
            {
                bTexts.Add(dB4);
                bTexts.Add(dB8);
                bTexts.Add(dB1);
            }
            else if (pump.PumpCount == 2 || pump.Allocation == "两用一备")
            {
                bTexts.Add(dB4);
                bTexts.Add(dB3);
                bTexts.Add(dB2);
                bTexts.Add(dB1);
            }
            else
            {
                bTexts.Add(dB4);
                bTexts.Add(dB3);
                bTexts.Add(dB2);
                bTexts.Add(dB7);
                bTexts.Add(dB1);
            }
            double textSpacing = 400;
            for (int i = 0; i < bTexts.Count; i++)
            {
                DefinePropertiesOfCADDBTexts(bTexts[i], "W-NOTE", bTexts[i].TextString, new Point3d(ptloc_br_tmp_01.X, ptloc_br_tmp_01.Y + textSpacing * i, 0), textHeight, DbHelper.GetTextStyleId("TH-STYLE3"));
            }
            double paraQ = pump.paraQ;
            double paraH = pump.paraH;
            double paraN = pump.paraN;
            Point3d ptloc_br_tmp_06 = new Point3d(ptLocPumpRec.X + 300, ptLocPumpRec.Y - frameHeigth / 2 - 1100, 0);
            dB6.TextString = "Q=" + paraQ.ToString() + "m3/h,H=" + paraH.ToString() + "m,N=" + paraN.ToString() + "kW";
            DefinePropertiesOfCADDBTexts(dB6, "W-NOTE", dB6.TextString, ptloc_br_tmp_06, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextLeft, TextVerticalMode.TextVerticalMid, (int)ColorIndex.White);
            bTexts.Add(dB6);
            Point3d pointtmp01 = new(ptLocPumpRec.X, ptLocPumpRec.Y - frameHeigth / 2 - 800, 0);
            Extents3d ext = (Extents3d)dB6.Bounds;
            double line2Length = ext.ToRectangle().Length / 2;
            Point3d pointtmp02 = new(ptLocPumpRec.X + line2Length, ptLocPumpRec.Y - frameHeigth / 2 - 800, 0);
            Point3d pointtmp03 = new(ptLocPumpRec.X, ptLocPumpRec.Y - frameHeigth / 2, 0);
            Line line01 = new Line(pointtmp01, pointtmp02);
            Line line02 = new Line(pointtmp03, pointtmp01);
            DefinePropertiesOfCADObjects(line01, "W-NOTE");
            DefinePropertiesOfCADObjects(line02, "W-NOTE");
            entities.Add(line01);
            entities.Add(line02);
            Point3d ptloc_br_tmp_05 = new Point3d(line01.GetMidpoint().X, line01.GetMidpoint().Y + textHeight, 0);
            dB5.TextString = pump.Location + " " + pump.Allocation;
            DefinePropertiesOfCADDBTexts(dB5, "W-NOTE", dB5.TextString, ptloc_br_tmp_05, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextMid, TextVerticalMode.TextVerticalMid, (int)ColorIndex.White);
            bTexts.Add(dB5);
            bTexts.ForEach(o => entities.Add(o));
            Point3d ptloc_ply_tmp = new Point3d(ptLocPumpRec.X - frameWidth / 2 - 650, ptLocPumpRec.Y - frameHeigth / 2 - 800, 0);
            Point3d ptloc_ply_tmp01 = ptloc_ply_tmp.TransformBy(Matrix3d.Displacement(new Vector3d(-900, 0, 0)));
            Point3d ptloc_ply_tmp02 = ptloc_ply_tmp.TransformBy(Matrix3d.Displacement(new Vector3d(900, 0, 0)));
            Line line09 = new Line(ptloc_ply_tmp01, ptloc_ply_tmp02);
            Polyline ply = new Polyline();
            ply.AddVertexAt(0, ptloc_ply_tmp01.ToPoint2D(), 0, 70, 70);
            ply.AddVertexAt(1, ptloc_ply_tmp02.ToPoint2D(), 0, 70, 70);
            DefinePropertiesOfCADObjects(ply, "W-NOTE");
            entities.Add(ply);
            DBText db07 = new();
            Point3d ptloc_ply_tmp03 = ply.GetMidpoint().TransformBy(Matrix3d.Displacement(new Vector3d(0, textHeight, 0)));
            DefinePropertiesOfCADDBTexts(db07, "W-NOTE", pump.Serial + "#集水井", ptloc_ply_tmp03, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextMid, TextVerticalMode.TextVerticalMid);
            entities.Add(db07);
            if (parLayers[parLayers.Count - 1] == 0)
            {
                var j = pipeLineSystemUnit.PipeLineUnits[parLayers[parLayers.Count - 1]].VerticalPipes[parIndexes[parIndexes.Count - 1]].SameTypeIdentifiers;
                string str = j.Count > 0 ? j[0] : "";
                string s = pump.Allocation + pump.PumpCount.ToString() + pump.Serial + pump.paraH.ToString() + pump.paraN.ToString() + pump.paraQ.ToString();
                if (j.Count > 0)
                {
                    iDict[str] += s;
                }
            }
            else if (parLayers[parLayers.Count - 1] == 1)
            {
                var j = pipeLineSystemUnit.PipeLineUnits[parLayers[parLayers.Count - 1]].VerticalPipes[parIndexes[parIndexes.Count - 1]].SameTypeIdentifiers;
                string str = j.Count > 0 ? j[0] : "";
                string s = pump.Allocation + pump.PumpCount.ToString() + pump.Serial + pump.paraH.ToString() + pump.paraN.ToString() + pump.paraQ.ToString();
                if (j.Count > 0)
                {
                    iDict[str] += s;
                }
                j = pipeLineSystemUnit.PipeLineUnits[parLayers[parLayers.Count - 2]].VerticalPipes[parIndexes[parIndexes.Count - 2]].SameTypeIdentifiers;
                str = j.Count > 0 ? j[0] : "";
                if (j.Count > 0)
                {
                    iDict[str] += s;
                }
            }
            return pump;
        }
       
        /// <summary>
        /// 绘制潜水泵立管细节
        /// </summary>
        /// <param name="pump"></param>
        /// <param name="floorLines"></param>
        /// <param name="ptLocPumpRec"></param>
        /// <param name="frameHeigth"></param>
        /// <param name="frameWidth"></param>
        /// <param name="frameRec"></param>
        /// <param name="curLayer"></param>
        /// <param name="curPoint"></param>
        public void PlotPumpVerticalPipe(ref PipeLineSystemUnitClass pipeLineSystemUnit, SubmergedPumpClass pump, List<Line> floorLines, Point3d ptLocPumpRec, double frameHeigth, double frameWidth, Polyline frameRec, int curLayer, int curIndex, Point3d curPoint, List<int> parLayers, List<int> parIndexes)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                double dim1 = 2100;//固定值
                double dim3 = 540;//固定值
                double dim4 = 300;//固定值
                double dim5 = 1115;//参考值
                double line2Length = 445;//参考值
                double dimelv150_a = 225;//参考值
                double dimelv150_b = 640;//参考值
                double dimelv200 = 50;//参考值
                double dimelv160 = 150;//参考值
                double disfromPumpCenterToButtom = 112;//参考值
                List<Entity> tmpEntitiesUnique = new();
                List<Entity> tmpEntitiesCopied = new();
                List<Entity> tmpEntitiesUniqueNOTE = new();
                List<BlockReference> tmpBlocksUnique = new();
                List<BlockReference> tmpBlocksCopied = new();
                double line1Length = floorLines[curLayer + 1].GetClosestPointTo(curPoint, false).DistanceTo(curPoint) - dim1;
                Line line1 = new Line(curPoint, new Point3d(curPoint.X, curPoint.Y - line1Length, 0));
                tmpEntitiesUnique.Add(line1);
                Line line2 = new Line(line1.EndPoint, new Point3d(line1.EndPoint.X, line1.EndPoint.Y - line2Length, 0));
                tmpEntitiesCopied.Add(line2);
                Line line3 = new Line(new Point3d(curPoint.X, floorLines[curLayer + 1].GetClosestPointTo(curPoint, false).Y + dim3, 0), new Point3d(curPoint.X, floorLines[curLayer + 1].GetClosestPointTo(curPoint, false).Y - frameHeigth + disfromPumpCenterToButtom + dim4, 0));
                tmpEntitiesCopied.Add(line3);
                Line line4 = new Line(line3.EndPoint, new Point3d(line3.EndPoint.X - dim4, line3.EndPoint.Y - dim4, 0));
                tmpEntitiesCopied.Add(line4);
                Point3d ptlocid01 = line3.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(0, -300, 0)));
                Point3d ptlocid02 = ptlocid01.TransformBy(Matrix3d.Displacement(new Vector3d(3600, 0, 0)));
                Line lineId = new Line(ptlocid01, ptlocid02);
                DefinePropertiesOfCADObjects(lineId, "W-NOTE");
                if (pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].SameTypeIdentifiers.Count > 0)
                {
                    var p = pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].SameTypeIdentifiers[0];
                    if (!iDict.ContainsKey(p))
                    {
                        lineIdpump.Add(lineId);
                        DBText dBText = new DBText();
                        DefinePropertiesOfCADDBTexts(dBText, "W-NOTE", p, new Point3d(ptlocid02.X, ptlocid02.Y + textHeight, 0), textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextRight);
                        identifer.Add(new List<DBText>());
                        identifer[identifer.Count - 1].Add(dBText);
                        string str = pump.Allocation + pump.PumpCount.ToString() + pump.Serial + pump.paraH.ToString() + pump.paraN.ToString() + pump.paraQ.ToString();
                        iDict.Add(p, str);
                    }
                }
                Point3d ptlocpump = new Point3d(curPoint.X - dim4, line2.StartPoint.Y - dim1 - frameHeigth, 0);
                var blkId_pump = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "潜水泵系统", ptlocpump, new Scale3d(1), 0);
                tmpBlocksCopied.Add(adb.Element<BlockReference>(blkId_pump));
                adb.Element<BlockReference>(blkId_pump).Visible = false;
                Point3d ptlocbr1 = line3.GetMidpoint().TransformBy(Matrix3d.Displacement(new Vector3d(0, -450, 0)));
                var blkId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "排水管径100", ptlocbr1, new Scale3d(1), Math.PI / 2);
                int diameter = CalculatePipeDiameter(pump.paraQ);
                string allo = "DN" + diameter.ToString();
                blkId1.SetDynBlockValue("可见性", allo);
                tmpBlocksCopied.Add(adb.Element<BlockReference>(blkId1));
                adb.Element<BlockReference>(blkId1).Visible = false;
                totalQ += CalculateUsedPump(pump.Allocation) * pump.paraQ;
                if (curLayer > 0)
                {
                    pipeLineSystemUnit.PipeLineUnits[parLayers[parLayers.Count - 1]].VerticalPipes[parIndexes[parIndexes.Count - 1]].totalQ += pump.paraQ * CalculateUsedPump(pump.Allocation);
                }
                Point3d ptlocbr2 = new Point3d(line2.EndPoint.X, line2.EndPoint.Y - dim5, 0);
                var blkId2 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "潜水泵出水管阀组", ptlocbr2, new Scale3d(1), 0);
                blkId2.SetDynBlockValue("可见性1", "闸阀");
                tmpBlocksCopied.Add(adb.Element<BlockReference>(blkId2));
                adb.Element<BlockReference>(blkId2).Visible = false;
                Point3d ptlocelv150 = line2.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(dimelv150_b, -dimelv150_a, 0)));
                Point3d ptlocelv200 = line1.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-dimelv200, 0, 0)));
                Point3d ptlocelv160 = frameRec.GetCenter().TransformBy(Matrix3d.Displacement(new Vector3d(-frameWidth / 2 - dimelv160, -frameHeigth / 2, 0)));
                double disBetweenPumps = 600;//两台潜水泵间距
                double disFromInitialPump = 0;//两台潜水泵间距
                for (int i = 0; i < pump.PumpCount; i++)
                {
                    Vector3d vec = i == 0 ? new Vector3d(0, 0, 0) : new Vector3d(-disBetweenPumps, 0, 0);
                    Matrix3d mat = Matrix3d.Displacement(vec);
                    ptlocelv200 = ptlocelv200.TransformBy(mat);
                    if (i >= 1)
                    {
                        Point3d ptConnected = line1.EndPoint;
                        Line lineConnected = new Line(new Point3d(ptConnected.X - disBetweenPumps * i, ptConnected.Y, 0), new Point3d(ptConnected.X - disBetweenPumps * (i - 1), ptConnected.Y, 0));
                        tmpEntitiesUnique.Add(lineConnected);
                    }
                    foreach (var ent in tmpEntitiesCopied)
                    {
                        ent.TransformBy(mat);
                        var line = (Line)ent;
                        tmpEntitiesUnique.Add(new Line(line.StartPoint, line.EndPoint));
                    }
                    foreach (var br in tmpBlocksCopied)
                    {
                        br.Position = br.Position.TransformBy(mat);
                        tmpBlocksUnique.Add(adb.Element<BlockReference>(adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", br.Name, br.Position, new Scale3d(1), br.Rotation)));
                    }
                    disFromInitialPump += disBetweenPumps;
                }
                tmpBlocksCopied.Clear();
                tmpEntitiesCopied.Clear();
                Dictionary<string, string> atts01 = new();
                atts01.Add("标高", "h+1.50");
                var blkId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", ptlocelv150, new Scale3d(0), 0, atts01);
                blkId3.SetDynBlockValue("翻转状态1", (short)0);
                blkId3.SetDynBlockValue("翻转状态2", (short)0);
                tmpBlocksUnique.Add(adb.Element<BlockReference>(blkId3));
                Line lineelv3 = new Line(new Point3d(ptlocelv150.X - 300 - 250, ptlocelv150.Y, 0), new Point3d(ptlocelv150.X - 250, ptlocelv150.Y, 0));
                tmpEntitiesUniqueNOTE.Add(lineelv3);
                Dictionary<string, string> atts02 = new Dictionary<string, string>();
                atts02.Add("标高", "h+2.00");
                var blkId4 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", ptlocelv200, new Scale3d(0), 0, atts02);
                blkId4.SetDynBlockValue("翻转状态1", (short)0);
                blkId4.SetDynBlockValue("翻转状态2", (short)1);
                tmpBlocksUnique.Add(adb.Element<BlockReference>(blkId4));
                Line lineelv4 = new Line(new Point3d(ptlocelv200.X - 250, ptlocelv200.Y, 0), new Point3d(ptlocelv200.X + 50, ptlocelv200.Y, 0));
                tmpEntitiesUniqueNOTE.Add(lineelv4);
                Dictionary<string, string> atts03 = new Dictionary<string, string>();
                atts03.Add("标高", "h-1.60");
                var blkId5 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", ptlocelv160, new Scale3d(0), 0, atts03);
                blkId5.SetDynBlockValue("翻转状态1", (short)0);
                blkId5.SetDynBlockValue("翻转状态2", (short)1);
                tmpBlocksUnique.Add(adb.Element<BlockReference>(blkId5));
                Line lineelv5 = new Line(new Point3d(ptlocelv160.X - 250, ptlocelv160.Y, 0), new Point3d(ptlocelv160.X + 150, ptlocelv160.Y, 0));
                tmpEntitiesUniqueNOTE.Add(lineelv5);
                foreach (var ent in tmpEntitiesUnique)
                {
                    DefinePropertiesOfCADObjects(ent, "W-RAIN-PIPE", "CONTINOUS");
                    entities.Add(ent);
                }
                foreach (var ent in tmpEntitiesUniqueNOTE)
                {
                    DefinePropertiesOfCADObjects(ent, "W-NOTE");
                    entities.Add(ent);
                }
                foreach (var br in tmpBlocksUnique)
                {
                    DefinePropertiesOfCADObjects(br, "W-NOTE");
                    blocks.Add(br);
                }
                tmpBlocksUnique.Clear();
                tmpEntitiesUnique.Clear();
                tmpEntitiesUniqueNOTE.Clear();
            }
        }
       
        /// <summary>
        /// 完善具有特殊用途的潜水泵立管
        /// </summary>
        /// <param name="pump"></param>
        /// <param name="ptLocPumpRec"></param>
        /// <param name="frameHeigth"></param>
        /// <param name="frameWidth"></param>
        /// <param name="frameRec"></param>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="floorLines"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <param name="curPoint"></param>
        /// <param name="parLayers"></param>
        /// <param name="parIndexes"></param>
        /// <param name="parPoints"></param>
        public void CompletePumpVerticalPipeForSpecialUse(SubmergedPumpClass pump, Point3d ptLocPumpRec, Polyline frameRec, double frameHeigth, double frameWidth)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                double dis1 = 800;//固定值
                double dis2 = 500;//固定值
                double textHeight = 350;//固定值
                double guideLineHeight = 2100;//固定值
                double guideLineWidth = 4600;//固定值
                double dislocText2 = 300;//参考值
                double dislocText3 = 250;//参考值
                double diselv = 250;//参考值
                List<Entity> tmpEntities = new();
                List<DBText> tmpBTexts = new();
                Point3d ptmp2 = frameRec.GetCenter().TransformBy(Matrix3d.Displacement(new Vector3d(-frameWidth / 2, 0, 0)));
                Point3d ptmp3 = ptmp2.TransformBy(Matrix3d.Displacement(new Vector3d(-dis1, 0, 0)));
                Line line1 = new Line(ptmp2, ptmp3);
                short colorIndex = 210;
                try
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, "W-RAIN-Y-PIPE", colorIndex);
                }
                catch { }
                DefinePropertiesOfCADObjects(line1, "W-RAIN-Y-PIPE");
                entities.Add(line1);
                Point3d ptlocRec_tmp = ptmp3.TransformBy(Matrix3d.Displacement(new Vector3d(-dis2, 0, 0)));
                Point3d ptlocRec = ptlocRec_tmp.TransformBy(Matrix3d.Displacement(new Vector3d(0, frameHeigth / 2 / 2, 0)));
                Polyline rec = ptlocRec.CreateRectangle(2 * dis2, frameHeigth / 2);
                tmpEntities.Add(rec);
                DBText dBText1 = new DBText();
                DefinePropertiesOfCADDBTexts(dBText1, "W-NOTE", pump.Location, ptlocRec, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextCenter);
                tmpBTexts.Add(dBText1);
                Point3d ptmp4 = line1.GetMidpoint();
                Point3d ptmp5 = ptmp4.TransformBy(Matrix3d.Displacement(new Vector3d(0, guideLineHeight, 0)));
                Point3d ptmp6 = ptmp5.TransformBy(Matrix3d.Displacement(new Vector3d(-guideLineWidth, 0, 0)));
                tmpEntities.Add(new Line(ptmp4, ptmp5));
                Line line2 = new Line(ptmp5, ptmp6);
                tmpEntities.Add(line2);
                Point3d ptlocText2 = line2.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(0, dislocText2, 0)));
                Point3d ptlocText3 = line2.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(0, -dislocText3, 0)));
                DBText dBText2 = new DBText();
                string str_tmp1 = pump.Location == "消防电梯" ? "电梯基坑" : "电缆沟";
                string str1 = str_tmp1 + "预埋镀锌钢管，管内底平基坑底";
                DefinePropertiesOfCADDBTexts(dBText2, "W-NOTE", str1, ptlocText2, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextLeft, TextVerticalMode.TextVerticalMid, (int)ColorIndex.White);
                tmpBTexts.Add(dBText2);
                DBText dBText3 = new DBText();
                dBText3.TextString = "2xDNXXX,i=0.01";
                DefinePropertiesOfCADDBTexts(dBText3, "W-NOTE", dBText3.TextString, ptlocText3, textHeight, DbHelper.GetTextStyleId("TH-STYLE3"), TextHorizontalMode.TextLeft, TextVerticalMode.TextVerticalMid, (int)ColorIndex.White);
                tmpBTexts.Add(dBText3);
                Point3d ptmp7 = ptlocRec_tmp.TransformBy(Matrix3d.Displacement(new Vector3d(-diselv, 0, 0)));
                Dictionary<string, string> atts = new Dictionary<string, string>();
                atts.Add("标高", "h1");
                var blkId = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", ptmp7, new Scale3d(0), 0, atts);
                blkId.SetDynBlockValue("翻转状态1", (short)0);
                blkId.SetDynBlockValue("翻转状态2", (short)1);
                var br = adb.Element<BlockReference>(blkId);
                DefinePropertiesOfCADObjects(br, "W-NOTE");
                blocks.Add(br);
                tmpBTexts.ForEach(o => entities.Add(o));
                tmpEntities.ForEach(o => DefinePropertiesOfCADObjects(o, "W-NOTE"));
                tmpEntities.ForEach(o => entities.Add(o));
            }
        }
       
        /// <summary>
        /// 对于同层还有其它立管情况下的递归前处理
        /// </summary>
        public void PreProcessForSameLayerPipeCondition(List<int> indexes, List<Point3d> points, List<int> layers, PipeLineSystemUnitClass pipeLineSystemUnit, List<int> ids, int curLayer, int curIndex, Point3d curPoint)
        {
            for (int i = 0; i < pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes.Count; i++)
            {
                bool cond_a = pipeLineSystemUnit.PipeLineUnits[curLayer].VertPipeConnectedArr[curIndex, i] == 1 && curIndex != i;
                bool cond_b = ids.Contains(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[i].Id);
                bool cond_c = !someLayerPipeIndexes[curLayer].Contains(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[i].Id);
                if (cond_a && cond_b && cond_c)
                {
                    layers.Add(curLayer);
                    indexes.Add(i);
                    points.Add(curPoint);
                    someLayerPipeIndexes[curLayer].Add(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[i].Id);
                }
            }
        }
       
        /// <summary>
        /// 继续绘图并递归到下一个立管
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="points"></param>
        /// <param name="layers"></param>
        /// <param name="pipeLineSystemUnit"></param>
        /// <param name="floorLines"></param>
        /// <param name="ids"></param>
        /// <param name="curLayer"></param>
        /// <param name="curIndex"></param>
        /// <param name="curPoint"></param>
        /// <param name="parLayers"></param>
        /// <param name="parIndexes"></param>
        /// <param name="parPoints"></param>
        public void ProcessForSameLayerAndRecursionToNextPipe(List<int> indexes, List<Point3d> points, List<int> layers, PipeLineSystemUnitClass pipeLineSystemUnit, List<Line> floorLines, List<int> ids, int curLayer, int curIndex, Point3d curPoint, List<int> parLayers, List<int> parIndexes, List<Point3d> parPoints)
        {
            double dis1 = 2200;//参考值
            double dis2 = 200;//参考值
            double dis4 = 600;//参考值           
            double dis5 = pumpUnitSpacing;//参考值
            double dis6 = 2000;//参考值
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                ids.Remove(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].Id);
                int num = layers.Count;
                for (int k = 0; k < num; k++)
                {
                    if (k != num - 1)
                    {
                        InCycle = true;
                    }
                    int tempparIndex = curIndex;
                    curLayer = layers[layers.Count - 1];
                    curIndex = indexes[indexes.Count - 1];
                    var pipe = pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[tempparIndex];
                    if (crossLayerGuideLine.Length != 1)
                    {
                        crossLayerGuideLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
                    }
                    if (curPoint.X - leftCoord > dis6)
                    {
                        Point3d pt = new Point3d(leftCoord, curPoint.Y, 0);
                        Line lin = new Line(curPoint, pt);
                        DefinePropertiesOfCADObjects(lin, "W-RAIN-PIPE", "CONTINOUS");
                        curPoint = pt.TransformBy(Matrix3d.Displacement(new Vector3d(-pumpUnitSpacing, 0, 0)));
                        Line lin2 = new Line(pt, curPoint);
                        DefinePropertiesOfCADObjects(lin2, "W-RAIN-PIPE", "CONTINOUS");
                        entities.Add(lin);
                        entities.Add(lin2);

                    }
                    else
                    {
                        double dis;
                        if (pipe.AppendedSubmergedPump == null && pipe.HasChildPipe == false && curLayer == 0)
                        {
                            dis = dis2;
                        }
                        else if (pipe.AppendedSubmergedPump == null && pipe.HasChildPipe == false && curLayer > 0)
                        {
                            dis = dis4;
                        }
                        else
                        {
                            dis = dis5;
                        }
                        Point3d pt = new Point3d(leftCoord, curPoint.Y, 0);
                        pt = curPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-dis, 0, 0)));
                        Line lin = new Line(curPoint, pt);
                        DefinePropertiesOfCADObjects(lin, "W-RAIN-PIPE", "CONTINOUS");
                        curPoint = pt;
                        if (true)
                        {
                            entities.Add(lin);
                        }
                    }
                    Point3d nextpt = new Point3d(curPoint.X - pumpUnitSpacing, curPoint.Y, 0);
                    Line line = new Line(curPoint, nextpt);
                    if (k == 0 && pipe.AppendedSubmergedPump == null && pipe.HasChildPipe == false && curLayer == 0)
                    {
                        nextpt = new Point3d(curPoint.X - dis1, curPoint.Y, 0);
                        line = new Line(curPoint, nextpt);
                    }
                    else if (k == 0 && pipe.AppendedSubmergedPump == null && pipe.HasChildPipe == false && curLayer > 0)
                    {
                        nextpt = new Point3d(curPoint.X - dis4, curPoint.Y, 0);
                        line = new Line(curPoint, nextpt);
                    }
                    DefinePropertiesOfCADObjects(line, "W-RAIN-PIPE", "CONTINOUS");
                    leftCoord = curPoint.X < leftCoord ? curPoint.X : leftCoord;
                    layers.RemoveAt(layers.Count - 1);
                    indexes.RemoveAt(indexes.Count - 1);
                    points.RemoveAt(points.Count - 1);
                    ids.Remove(pipeLineSystemUnit.PipeLineUnits[curLayer].VerticalPipes[curIndex].Id);
                    RecursivelyPlot(pipeLineSystemUnit, floorLines, ids, ref curLayer, curIndex, curPoint, parLayers, parIndexes, parPoints);
                }
                InCycle = false;
            }
        }
    }
}