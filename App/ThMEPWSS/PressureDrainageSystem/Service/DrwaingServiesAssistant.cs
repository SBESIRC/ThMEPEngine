using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.PressureDrainageSystem.Model;
using static DotNetARX.UCSTools;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public partial class PressureDrainageSystemDiagramService
    {
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
                else if (pipe.IsInitialDrainWell)
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
        public void DrawDrainWell(PipeLineSystemUnitClass pipeLineSystemUnit, Point3d ptloc, int index, double heightDisTofloorLine, List<Line> floorLines)
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
                if (false)//简单穿顶板DrainageMode
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
                else if (drainageMode == 1 || drainageMode == 2)//穿顶板进水井
                {
                    double disFromLineToGround = 800;//固定值
                    double lineLength = 3000;//固定值
                    Point3d pt1 = new Point3d(ptloc.X, ptloc.Y + heightDisTofloorLine + disFromLineToGround, 0);
                    Point3d pt2 = new Point3d(pt1.X + lineLength, pt1.Y, 0);
                    Line line1 = new Line(ptloc, pt1);
                    Line line2 = new Line(pt1, pt2);
                    tmpEntitiesLayerRAINPIPE.Add(line1);
                    tmpEntitiesLayerRAINPIPE.Add(line2);
                    Point3d brlocPt1 = new Point3d(pt1.X + disRefElv, pt1.Y, 0);
                    Point3d brlocPt4 = new Point3d(brlocPt1.X, ptloc.Y + heightDisTofloorLine + bushHeigthHalf_a, 0);
                    var brId4 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", brlocPt4, new Scale3d(0), 0);
                    var br4 = adb.Element<BlockReference>(brId4);
                    br4.Rotation = Math.PI / 2 * 3;
                    tmpBlocksLayerBUSH.Add(br4);
                    brlocPt1 = brlocPt1 + Vector3d.XAxis * 600;
                    Dictionary<string, string> atts1 = new Dictionary<string, string>();
                    atts1.Add("标高", "室外覆土敷设");
                    var brId1 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "标高", brlocPt1, new Scale3d(1), 0, atts1);
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
                    ObjectId brId3;
                    var horpipe = pipeLineSystemUnit.PipeLineUnits[0].HorizontalPipes;
                    if (horpipe.Count > 0 && horpipe[0].Layer == "W-DRAI-DOME-PIPE")
                    {
                        brId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", "污水井编号", brlocPt3, new Scale3d(0), 0, atts);
                    }
                    else
                    {
                        brId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", pipeLineSystemUnit.DrainWell != null ? pipeLineSystemUnit.DrainWell.WellTypeName : "重力流雨水井编号", brlocPt3, new Scale3d(0), 0, atts);
                    }
                    var br3 = adb.Element<BlockReference>(brId3);
                    DefinePropertiesOfCADObjects(br3, "W-NOTE");
                    drainwellbr.Add(br3);
                }
                else if (drainageMode == 4)//穿侧墙排水方式
                {
                    double disFromptloc = 1100;//固定值
                    Point3d pt1 = new Point3d(ptloc.X, ptloc.Y + disFromptloc, 0);
                    Point3d pt2 = new Point3d(pt1.X + disFromptloc, pt1.Y, 0);
                    Line line1 = new Line(pt1, pt2);
                    Line line2 = new Line(ptloc, pt1);
                    tmpEntitiesLayerRAINPIPE.Add(line1);
                    tmpEntitiesLayerRAINPIPE.Add(line2);
                    Point3d ptlocOnfloor = floorLines[0].GetClosestPointTo(pt1, false);
                    Line line3 = new Line(ptlocOnfloor.TransformBy(Matrix3d.Displacement(new Vector3d(-600, 0, 0))), ptlocOnfloor.TransformBy(Matrix3d.Displacement(new Vector3d(-600, 1000, 0))));
                    Line line4 = new Line(ptlocOnfloor.TransformBy(Matrix3d.Displacement(new Vector3d(600, 0, 0))), ptlocOnfloor.TransformBy(Matrix3d.Displacement(new Vector3d(600, 1000, 0))));
                    Line line5 = new Line(line3.EndPoint, line4.EndPoint);
                    List<Line> lines = new List<Line>() { line3, line4, line5 };
                    lines.ForEach(o => DefinePropertiesOfCADObjects(o, "W-NOTE"));
                    lines.ForEach(o => entities.Add(o));
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
                    Point3d ptlocbr2 = new Point3d(pt2.X - bushHeigthHalf_b * 2, pt2.Y, 0);
                    var brId2 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", ptlocbr2, new Scale3d(0), 0);
                    tmpBlocksLayerBUSH.Add(adb.Element<BlockReference>(brId2));
                }
                else//穿外墙排水方式:3
                {
                    double disFromptloc = 3000;//固定值
                    Point3d pt1 = new Point3d(ptloc.X + disFromptloc, ptloc.Y, 0);
                    Line line = new Line(ptloc, pt1);
                    tmpEntitiesLayerRAINPIPE.Add(line);
                    Point3d ptlocbr1 = new Point3d(ptloc.X + disRefElv, ptloc.Y, 0);
                    Point3d ptlocbr4 = new Point3d(ptlocbr1.X - bushHeigthHalf_b, pt1.Y, 0);
                    var brId4 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-BUSH", "套管系统", ptlocbr4, new Scale3d(0), 0);
                    tmpBlocksLayerBUSH.Add(adb.Element<BlockReference>(brId4));
                    ptlocbr1 += Vector3d.XAxis * 600;
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
                    var brId3 = adb.CurrentSpace.ObjectId.InsertBlockReference("W-NOTE", pipeLineSystemUnit.DrainWell != null ? pipeLineSystemUnit.DrainWell.WellTypeName : "重力流雨水井编号", ptlocbr3, new Scale3d(0), 0, atts);
                    var br10 = adb.Element<BlockReference>(brId3);
                    DefinePropertiesOfCADObjects(br10, "W-NOTE");
                    drainwellbr.Add(br10);

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
        /// 通过比较两排水系统的可比较部分来处理相同的排水系统
        /// </summary>
        /// <param name="guidelines"></param>
        /// <param name="pt"></param>
        public void CompareEachSystemUnit(List<List<Line>> guidelines, Point3d pt)
        {
            for (int i = 1; i < comparedEntitys.Count; i++)
            {
                foreach (var br in drainwellbrs[i])
                {
                    br.Visible = false;
                }
                for (int j = 0; j < i; j++)
                {
                    bool same = IsSameSystemUnits(comparedEntitys[i], drainwellbrs[i], comparedEntitys[j], drainwellbrs[j]);
                    if (same)
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
        }

        /// <summary>
        /// 将相同排水系统中对应立管的编号汇总
        /// </summary>
        public void ProcessIdentifersInSameSystemUnits()
        {
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
                    List<DBText> texts = new List<DBText>();
                    for (int i = 0; i < curText.Count; i++)
                    {
                        string str = curText[i].TextString;
                        if (str.Contains('L') && str.Contains('-'))
                        {
                            string str01 = str.Split('L', '-')[1];
                            string str02 = str.Split('L', '-')[2];
                            double num01 = 0;
                            double num02 = 0;
                            bool cond = true;
                            try
                            {
                                num01 = double.Parse(str01);
                                num02 = double.Parse(str02);
                            }
                            catch
                            {
                                cond = false;
                            }
                            if (!cond)
                            {
                                texts.Add(curText[i]);
                                curText.RemoveAt(i);
                                i--;
                                continue;
                            }
                            else
                            {
                                int dd = 0;
                                if (texts.Count == 0)
                                {
                                    texts.Add(curText[i]);
                                    curText.RemoveAt(i);
                                    i--;
                                    continue;
                                }
                                else
                                {
                                    for (int t = 0; t < texts.Count; t++)
                                    {
                                        string strt = texts[t].TextString;
                                        if (strt.Contains('L') && str.Contains('-'))
                                        {
                                            string strt1 = strt.Split('L', '-')[1];
                                            string strt2 = strt.Split('L', '-')[2];
                                            double numt1 = 0;
                                            double numt2 = 0;
                                            bool condt = true;
                                            try
                                            {
                                                numt1 = double.Parse(strt1);
                                                numt2 = double.Parse(strt2);
                                            }
                                            catch
                                            {
                                                condt = false;
                                            }
                                            if (!condt && numt1 > 0)
                                            {
                                                try
                                                {
                                                    strt2 = strt.Split('L', '-', ',')[2];
                                                    numt2 = double.Parse(strt2);
                                                }
                                                catch { }
                                            }
                                            if (numt2 > 0)
                                            {
                                                condt = true;
                                            }
                                            if (!condt)
                                            {
                                                texts.Insert(t, curText[i]);
                                                curText.RemoveAt(i);
                                                i--;
                                                dd = 1;
                                                break;
                                            }
                                            else
                                            {
                                                if (num01 < numt1)
                                                {
                                                    texts.Insert(t, curText[i]);
                                                    curText.RemoveAt(i);
                                                    i--;
                                                    dd = 1;
                                                    break;
                                                }
                                                else if (t == texts.Count - 1 && num01 != numt1)
                                                {
                                                    texts.Add(curText[i]);
                                                    curText.RemoveAt(i);
                                                    i--;
                                                    dd = 1;
                                                    break;
                                                }
                                                else if (num01 == numt1)
                                                {

                                                    texts[t].TextString += ", " + num02.ToString();
                                                    curText.RemoveAt(i);
                                                    i--;
                                                    dd = 1;
                                                    break;
                                                }
                                            }
                                        }
                                        else if (t == texts.Count - 1)
                                        {
                                            texts.Add(curText[i]);
                                            curText.RemoveAt(i);
                                            i--;
                                            continue;
                                        }
                                    }
                                    if (dd == 1) continue;
                                }
                            }
                        }
                        else
                        {
                            texts.Add(curText[i]);
                            curText.RemoveAt(i);
                            i--;
                        }
                    }
                    curText.Clear();

                    foreach (var t in texts)
                    {
                        try
                        {
                            if (t.TextString.Contains('L') && t.TextString.Contains('-'))
                            {
                                string str1 = t.TextString.Split('L', '-')[0];
                                string str2 = t.TextString.Split('L', '-')[1];
                                string str3 = t.TextString.Split('L', '-')[2];
                                List<string> numstrs = new List<string>();
                                List<double> nums = new List<double>();
                                numstrs = str3.Split(',').ToList();
                                numstrs.ForEach(o => nums.Add(double.Parse(o)));
                                nums = SortDouble(nums);
                                str3 = "";
                                foreach (var s in nums)
                                {
                                    str3 += s + ",";
                                }
                                str3 = str3.Substring(0, str3.Length - 1);
                                t.TextString = str1 + "L" + str2 + "-" + str3;
                            }
                        }
                        catch { }
                    }
                    texts.ForEach(e => curText.Add(e));
                    double disXform = 0;
                    foreach (var text in curText)
                    {
                        text.TransformBy(Matrix3d.Displacement(new Vector3d(0, disXform, 0)));
                        disXform += textHeight + 50;
                    }
                }
            }
        }

        /// <summary>
        /// 判断两个排水系统图是否相同
        /// </summary>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        public bool IsSameSystemUnits(List<Entity> ent1, List<BlockReference> br1, List<Entity> ent2, List<BlockReference> br2)
        {
            if (Math.Abs(ent1.Count - ent2.Count) > 1)
            {
                return false;
            }
            if (br1[0].Name != br2[0].Name) return false;
            List<Line> lines1 = new(), lines2 = new();
            List<Polyline> plys1 = new(), plys2 = new();
            List<DBText> dbs1 = new(), dbs2 = new();
            List<BlockReference> brs1 = new(), brs2 = new();
            double maxY1 = 0;
            double maxY2 = 0;
            foreach (var j in ent1)
            {
                if (j is Line lin)
                {
                    lines1.Add(lin);
                    maxY1 = maxY1 > lin.StartPoint.Y ? maxY1 : lin.StartPoint.Y;
                    maxY1 = maxY1 > lin.EndPoint.Y ? maxY1 : lin.EndPoint.Y;
                }
                else if (j is Polyline)
                {
                    plys1.Add((Polyline)j);
                }
                else if (j is DBText b)
                {
                    if (!b.TextString.Contains("集水井尺寸"))
                        dbs1.Add(b);
                }
                else
                {
                    brs1.Add((BlockReference)j);
                }
            }
            foreach (var j in ent2)
            {
                if (j is Line lin)
                {
                    lines2.Add(lin);
                    maxY2 = maxY2 > lin.StartPoint.Y ? maxY2 : lin.StartPoint.Y;
                    maxY2 = maxY2 > lin.EndPoint.Y ? maxY2 : lin.EndPoint.Y;
                }
                else if (j is Polyline)
                {
                    plys2.Add((Polyline)j);
                }
                else if (j is DBText b)
                {
                    if (!b.TextString.Contains("集水井尺寸"))
                        dbs2.Add(b);
                }
                else
                {
                    brs2.Add((BlockReference)j);
                }
            }
            if (Math.Abs(lines1.Count - lines2.Count) > 1 || plys1.Count != plys2.Count || dbs1.Count != dbs2.Count || brs1.Count != brs2.Count)
            {
                return false;
            }
            if (!IsSameDBTextsList(dbs1, dbs2))
            {
                return false;
            }
            if (maxY1 != maxY2) return false;
            List<Point3d> midpts1 = new(), midpts2 = new();
            foreach (var e in lines1)
            {
                if (e.Length > 0 && (!TestLineHorizontal(e, 1))) midpts1.Add(e.GetMidpoint());
            }
            foreach (var e in lines2)
            {
                if (e.Length > 0 && (!TestLineHorizontal(e, 1))) midpts2.Add(e.GetMidpoint());
            }
            midpts1 = SortPointsBasedSpecailCoordinates(midpts1, 1);
            midpts2 = SortPointsBasedSpecailCoordinates(midpts2, 1);
            int num = Math.Min(midpts1.Count, midpts2.Count);
            for (int i = 0; i < num; i++)
            {
                if (midpts1[i].Y != midpts2[i].Y) return false;
            }
            brs1.Clear();
            brs2.Clear();
            if (Math.Abs(lines1.Count - lines2.Count) == 1)
            {
                foreach (var j in lines1)
                {
                    int dd = 0;
                    foreach (var k in lines2)
                    {
                        if (j.Length == k.Length)
                        {
                            dd = 1;
                            break;
                        }
                    }
                    if (dd == 0)
                    {
                        if (j.Length < 1000)
                        {
                            return false;
                        }
                    }
                }
                foreach (var j in lines2)
                {
                    int dd = 0;
                    foreach (var k in lines1)
                    {
                        if (j.Length == k.Length)
                        {
                            dd = 1;
                            break;
                        }
                    }
                    if (dd == 0)
                    {
                        if (j.Length < 1000)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 删除多余的线段
        /// </summary>
        /// <param name="ents"></param>
        public void RemoveUnnecessaryLines(List<Entity> ents)
        {
            List<Line> linesVertical = new();
            List<Line> linesHorizontal = new();
            List<Line> linesToRemove = new();
            List<Line> lines = new();
            for (int i = 0; i < ents.Count; i++)
            {
                if (ents[i] is Line)
                {
                    lines.Add((Line)ents[i]);
                    ents.RemoveAt(i);
                    i--;
                }
            }
            lines = SortLinesFromLeftToRight(lines);
            for (int i = 0; i < lines.Count; i++)
            {
                Point3d ps = lines[i].StartPoint.X <= lines[i].EndPoint.X ? lines[i].StartPoint : lines[i].EndPoint;
                Point3d pe = lines[i].StartPoint.X <= lines[i].EndPoint.X ? lines[i].EndPoint : lines[i].StartPoint;
                var lin = lines[i];
                lines[i] = new Line(ps, pe);
                lines[i].Layer = lin.Layer;
                lines[i].Linetype = "CONTINOUS";
                lines[i].ColorIndex = lin.ColorIndex;
            }
            for (int i = 0; i < lines.Count; i++)
            {
                if (TestLineHorizontal(lines[i], 1))
                {
                    Point3d pt = lines[i].StartPoint;
                    double mindis = double.PositiveInfinity;
                    for (int j = 0; j < lines.Count; j++)
                    {
                        double dis = lines[j].GetClosestPointTo(pt, false).DistanceTo(pt);
                        if (i != j && dis < mindis)
                        {
                            mindis = dis;
                        }
                        if (lines[i].Length == 800 || lines[i].Length == 4600)
                        {
                            mindis = 0;
                        }
                    }
                    if (mindis > 0)
                    {
                        lines.RemoveAt(i);
                        i--;
                    }
                }
            }
            lines.ForEach(o => ents.Add(o));
        }
        public static List<BlockReference> SimplifyUnitsByRemovingUnusedTexts(List<Entity> entities, List<BlockReference> blocks)
        {
            var dnblocks = blocks.Where(e => e.GetEffectiveName().Contains("排水管径") || e.Name.Contains("排水管径")).ToList();
            blocks = blocks.Except(dnblocks).ToList();
            var lines = entities.Where(e => e is Line).Select(e => (Line)e).ToList();
            //去空
            dnblocks.ForEach(e => e.Visible = false);
            dnblocks = dnblocks.Where(t => ClosestPointInCurves(t.Position, lines) < 1000)
                .OrderBy(t => t.Position.X)
                .ToList();
            //去重
            if (dnblocks.Count >= 2)
            {
                for (int i = 0; i < dnblocks.Count - 1; i++)
                {
                    for (int j = i + 1; j < dnblocks.Count; j++)
                    {
                        var cond_vert = Math.Abs(dnblocks[i].Position.Y - dnblocks[j].Position.Y) < 1;
                        var cond_hor = dnblocks[j].Position.X - dnblocks[i].Position.X < 1000;
                        var cond = dnblocks[i].Id.GetDynBlockValue("可见性").Equals(dnblocks[j].Id.GetDynBlockValue("可见性"));
                        if (cond_vert && cond_hor && cond)
                        {
                            dnblocks[i].Visible = false;
                            dnblocks.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
            blocks.AddRange(dnblocks);
            return blocks; 
        }
    }
}
