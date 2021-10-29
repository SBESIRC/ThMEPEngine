using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.LoadCalculation.Extension;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.LoadCalculation.Service
{
    public class LogicService
    {
        public List<Table> InsertLoadCalculationTable(Database database, List<Entity> rooms, List<BlockReference> roomFunctionBlocks, List<Table> loadCalculationtables, List<Curve> curves, out List<Table> Deprecatedtables)
        {
            var result = new List<Table>();
            Deprecatedtables = new List<Table>();
            Dictionary<DBPoint, BlockReference> BlocksDic = roomFunctionBlocks.ToDictionary(key => new DBPoint(key.Position), value => value);
            Dictionary<Polyline, Table> tableDic = loadCalculationtables.ToDictionary(key => key.GeometricExtents.ToRectangle(), value => value);
            ThCADCoreNTSSpatialIndex blkSpatialIndex = new ThCADCoreNTSSpatialIndex(BlocksDic.Keys.ToCollection());
            ThCADCoreNTSSpatialIndex tableSpatialIndex = new ThCADCoreNTSSpatialIndex(tableDic.Keys.ToCollection());
            ThCADCoreNTSSpatialIndex curveSpatialIndex = new ThCADCoreNTSSpatialIndex(curves.ToCollection());
            var dbSourceService = new ModelDataDbSourceService();
            double summerVentilationT;
            double winterVentilationT;
            dbSourceService.Load(database);
            if (!double.TryParse(dbSourceService.dataModel.SummerTemperature.Replace("°C", ""), out summerVentilationT)
                || !double.TryParse(dbSourceService.dataModel.WinterTemperature.Replace("°C", ""), out winterVentilationT))
            {
                throw new Exception("获取室外通风温度异常！");
            }
            foreach (Entity roomBoundary in rooms)
            {
                var SelectWindowobjs = blkSpatialIndex.SelectCrossingPolygon(roomBoundary);
                if (SelectWindowobjs.Count == 1)
                {
                    DBPoint dBPoint = SelectWindowobjs[0] as DBPoint;
                    BlockReference roomFunction = BlocksDic[dBPoint];

                    var loadCalculationtableobjs = tableSpatialIndex.SelectCrossingPolygon(roomBoundary);
                    if (loadCalculationtableobjs.Count == 1)
                    {
                        //本身含有表格
                        Polyline tableBoundary = loadCalculationtableobjs[0] as Polyline;
                        Table existedTable = tableDic[tableBoundary];
                        //找到表格
                        var table = FillTableData(roomBoundary, roomFunction, summerVentilationT, winterVentilationT, existedTable.Position);
                        if (!table.IsNull())
                        {
                            Deprecatedtables.Add(existedTable);
                            result.Add(table);
                        }
                    }
                    else
                    {
                        var curveobjs = curveSpatialIndex.SelectFence(roomBoundary);
                        if (curveobjs.Count > 0)
                        {
                            bool findtable = false;
                            foreach (Curve curve in curveobjs)
                            {
                                var pts = new Point3dCollection();
                                roomBoundary.IntersectWith(curve, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                if (pts.Count == 1)
                                {
                                    var tableobjs = tableSpatialIndex.SelectFence(curve);
                                    if (tableobjs.Count == 1)
                                    {
                                        //本身含有表格
                                        Polyline tableBoundary = tableobjs[0] as Polyline;
                                        Table existedTable = tableDic[tableBoundary];
                                        var table = FillTableData(roomBoundary, roomFunction, summerVentilationT, winterVentilationT, existedTable.Position);
                                        int addRowCount = table.Rows.Count - existedTable.Rows.Count;
                                        if (addRowCount != 0)
                                        {
                                            var connectpoint = tableBoundary.Contains(curve.EndPoint) ? curve.EndPoint : curve.StartPoint;
                                            if (Math.Abs(connectpoint.Y - existedTable.Position.Y) > existedTable.Height / 2)
                                            {
                                                table.Position = table.Position + new Vector3d(0, addRowCount * 440, 0);
                                            }
                                        }
                                        if (!table.IsNull())
                                        {
                                            Deprecatedtables.Add(existedTable);
                                            result.Add(table);
                                        }
                                        findtable = true;
                                        break;
                                    }
                                }
                            }
                            if (!findtable)
                            {
                                //找不到表格，新建表格
                                var table = FillTableData(roomBoundary, roomFunction, summerVentilationT, winterVentilationT, roomFunction.Position);
                                if (!table.IsNull())
                                {
                                    result.Add(table);
                                }
                            }
                        }
                        else
                        {
                            //找不到表格，新建表格
                            var table = FillTableData(roomBoundary, roomFunction, summerVentilationT, winterVentilationT, roomFunction.Position);
                            if (!table.IsNull())
                            {
                                result.Add(table);
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 填充表格数据
        /// </summary>
        /// <param name="roomBoundary"></param>
        /// <param name="block"></param>
        /// <param name="summerVentilationT"></param>
        /// <param name="winterVentilationT"></param>
        /// <returns></returns>
        private Table FillTableData(Entity roomBoundary, BlockReference block, double summerVentilationT, double winterVentilationT, Point3d tablePoint)
        {
            Table table = new Table()
            {
                Position = tablePoint,
            };

            var TableData = CalculateData(roomBoundary, block, summerVentilationT, winterVentilationT);
            var ShowData = TableData.Where(o => o.Item1).ToList();
            if (ShowData.Count < 1)
            {
                return null;
            }
            table.SetSize(ShowData.Count, 2);
            table.SetRowHeight(440);
            table.SetTextHeight(300);
            table.SetColumnWidth(2400);
            table.GenerateLayout();
            table.Rows[0].Style = "数据";//将第一行样式由表头->数据
            table.Rows[1].Style = "数据";//将第二行样式由标题->数据

            //第一行：标题
            for (int i = 0; i < ShowData.Count; i++)
            {
                table.Cells[i, 0].TextString = ShowData[i].Item2;
                table.Cells[i, 1].TextString = ShowData[i].Item3;
            }

            table.SetAlignment(CellAlignment.MiddleCenter);
            return table;
        }

        /// <summary>
        /// 负荷计算
        /// </summary>
        /// <param name="roomBoundary"></param>
        /// <param name="block"></param>
        /// <param name="summerVentilationT"></param>
        /// <param name="winterVentilationT"></param>
        /// <returns></returns>
        private List<Tuple<bool, string, string>> CalculateData(Entity roomBoundary, BlockReference block, double summerVentilationT, double winterVentilationT)
        {
            List<Tuple<bool, string, string>> tuples = new List<Tuple<bool, string, string>>();
            var blockAttDic = block.Id.GetAttributesInBlockReference();
            string roomFunctionName = blockAttDic["房间功能"];
            double roomHeigth = double.Parse(blockAttDic["房间净高"]);
            var modeldata = ThLoadCalculationUIService.Instance.Parameter.ModelDataList.FirstOrDefault(o => o.RoomFunction == roomFunctionName);
            if (modeldata.IsNull())
            {
                return tuples;
            }
            int Area = 0;//面积
            double coldL = 0;//冷负荷
            double hotL = 0;//热负荷
            double coldW = 0;//冷水温差
            double hotW = 0;//热水温差
            int coldWP = 0;//冷水管径
            int hotWP = 0;//冷水管径
            int CondensateWP = 0;//冷凝水管径
            int ReshAir = 0;//新风量
            int Lampblack = 0;//排油烟
            int FumeSupplementary = 0;//油烟补风
            int AccidentExhaust = 0;//事故排风
            int NormalAirVolume = 0;//平时排风
            int NormalFumeSupplementary = 0;//平时补风
            //Column 1
            {
                if (roomBoundary is Polyline polyline)
                {
                    Area = (int)Math.Ceiling(polyline.Area * 1E-6);
                }
                else if (roomBoundary is MPolygon mPolygon)
                {
                    Area = (int)Math.Ceiling(mPolygon.Area * 1E-6);
                }
                else
                {
                    return tuples;
                }
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_Area, "面积(m²)", Area.ToString()));
            }
            //Column 2
            {
                coldL = modeldata.ColdNorm.ByNorm ? (modeldata.ColdNorm.NormValue * Area / 1000).Ceiling(1) : modeldata.ColdNorm.TotalValue;
                hotL = modeldata.HotNorm.ByNorm ? (modeldata.HotNorm.NormValue * Area / 1000).Ceiling(1) : modeldata.HotNorm.TotalValue;
                if (!ThLoadCalculationUIService.Instance.Parameter.chk_ColdL && !ThLoadCalculationUIService.Instance.Parameter.chk_HotL)
                {
                    tuples.Add(new Tuple<bool, string, string>(false, "冷/热负荷(kW)", string.Format("{0}/{1}", coldL, hotL)));
                }
                else
                {
                    tuples.Add(new Tuple<bool, string, string>(true, "冷/热负荷(kW)", string.Format("{0}/{1}", ThLoadCalculationUIService.Instance.Parameter.chk_ColdL ? coldL.ToString() : "-",
                      ThLoadCalculationUIService.Instance.Parameter.chk_HotL ? hotL.ToString() : "-")));
                }
            }
            //Column 3
            {
                coldW = (coldL / 1.163 / modeldata.CWaterTemperature).Ceiling(1);
                hotW = (hotL / 1.163 / modeldata.HWaterTemperature).Ceiling(1);
                if (!ThLoadCalculationUIService.Instance.Parameter.chk_ColdW && !ThLoadCalculationUIService.Instance.Parameter.chk_HotW)
                {
                    tuples.Add(new Tuple<bool, string, string>(false, "冷/热水量(m3/h)", string.Format("{0}/{1}", coldW, hotW)));
                }
                else
                {
                    tuples.Add(new Tuple<bool, string, string>(true, "冷/热水量(m3/h)", string.Format("{0}/{1}", ThLoadCalculationUIService.Instance.Parameter.chk_ColdW ? coldW.ToString() : "-",
                      ThLoadCalculationUIService.Instance.Parameter.chk_HotW ? hotW.ToString() : "-")));
                }
            }
            //Column 4
            {
                foreach (var item in LoadCalculationParameterFromConfig.WPipeDiameterConfig[ThLoadCalculationUIService.Instance.Parameter.chk_ColdWP_Index])
                {
                    if (coldW > item.Key)
                    {
                        coldWP = item.Value;
                        break;
                    }
                }
                foreach (var item in LoadCalculationParameterFromConfig.WPipeDiameterConfig[ThLoadCalculationUIService.Instance.Parameter.chk_HotWP_Index])
                {
                    if (hotW > item.Key)
                    {
                        hotWP = item.Value;
                        break;
                    }
                }
                if (!ThLoadCalculationUIService.Instance.Parameter.chk_ColdWP && !ThLoadCalculationUIService.Instance.Parameter.chk_HotWP)
                {
                    tuples.Add(new Tuple<bool, string, string>(false, "空调冷/热水管径", string.Format("{0}/{1}", coldWP, hotWP)));
                }
                else
                {
                    tuples.Add(new Tuple<bool, string, string>(true, "空调冷/热水管径", string.Format("{0}/{1}", ThLoadCalculationUIService.Instance.Parameter.chk_ColdWP ? "DN" + coldWP.ToString() : "-",
                      ThLoadCalculationUIService.Instance.Parameter.chk_HotWP ? "DN" + hotWP.ToString() : "-")));
                }
            }
            //Column 5
            {
                if (coldWP < 25)
                {
                    CondensateWP = 25;
                }
                else if (coldWP < 100)
                {
                    CondensateWP = 32;
                }
                else if (coldWP < 300)
                {
                    CondensateWP = 40;
                }
                else if (coldWP < 800)
                {
                    CondensateWP = 50;
                }
                else if (coldWP < 1600)
                {
                    CondensateWP = 80;
                }
                else if (coldWP < 3000)
                {
                    CondensateWP = 100;
                }
                else if (coldWP < 12000)
                {
                    CondensateWP = 125;
                }
                else
                {
                    CondensateWP = 150;
                }
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_CondensateWP, "冷凝水管径", "De" + CondensateWP));
            }
            //Column 6
            {
                ReshAir = modeldata.ReshAir.ByNorm ? ((int)Math.Ceiling(Area * modeldata.ReshAir.PersonnelDensity * modeldata.ReshAir.ReshAirNormValue)).CeilingInteger(50) : (int)modeldata.ReshAir.TotalValue;
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_AirVolume, "新风量(m3/h)", ReshAir.ToString()));
            }
            //Column 7
            {
                Lampblack = modeldata.Lampblack.ByNorm ? ((int)Math.Ceiling(Area * modeldata.Lampblack.Proportion * roomHeigth * modeldata.Lampblack.AirNum)).CeilingInteger(50) : (int)modeldata.Lampblack.TotalValue;
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_FumeExhaust, "排油烟(m3/h)", Lampblack.ToString()));
            }
            //Column 8
            {
                FumeSupplementary = modeldata.LampblackAir.ByNorm ? ((int)Math.Ceiling(Lampblack * modeldata.LampblackAir.NormValue)).CeilingInteger(50) : modeldata.LampblackAir.TotalValue;
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_FumeSupplementary, "油烟补风(m3/h)", FumeSupplementary.ToString()));
            }
            //Column 9
            {
                AccidentExhaust = modeldata.AccidentAir.ByNorm ? ((int)Math.Ceiling(Area * modeldata.AccidentAir.Proportion * roomHeigth * modeldata.AccidentAir.AirNum)).CeilingInteger(50) : (int)modeldata.AccidentAir.TotalValue;
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_AccidentExhaust, "事故排风(m3/h)", AccidentExhaust.ToString()));
            }
            //Column 10
            {
                if (modeldata.Exhaust.ByNorm == 1)
                {
                    NormalAirVolume = ((int)Math.Ceiling(Area * roomHeigth * modeldata.Exhaust.NormValue)).CeilingInteger(50);
                }
                else if (modeldata.Exhaust.ByNorm == 2)
                {
                    NormalAirVolume = modeldata.Exhaust.TotalValue;
                }
                else
                {
                    int NormalAirNewVolume = (int)Math.Ceiling(Area * roomHeigth * modeldata.Exhaust.BreatheNum);
                    int HeatBalanceValue = 0;
                    if (modeldata.Exhaust.CapacityType == 1)
                    {
                        HeatBalanceValue = (int)Math.Ceiling(3600 * modeldata.Exhaust.TransformerCapacity * modeldata.Exhaust.HeatDissipation * 0.01 / 1.2 / (summerVentilationT - modeldata.Exhaust.RoomTemperature));
                    }
                    else if (modeldata.Exhaust.CapacityType == 2)
                    {
                        HeatBalanceValue = (int)Math.Ceiling(3600 * modeldata.Exhaust.BoilerCapacity * modeldata.Exhaust.HeatDissipation * 0.01 / 1.2 / (summerVentilationT - modeldata.Exhaust.RoomTemperature));
                    }
                    else
                    {
                        HeatBalanceValue = (int)Math.Ceiling(3600 * modeldata.Exhaust.FirewoodCapacity * modeldata.Exhaust.HeatDissipation * 0.01 / 1.2 / (summerVentilationT - modeldata.Exhaust.RoomTemperature));
                    }
                    NormalAirVolume = Math.Max(NormalAirNewVolume, HeatBalanceValue).CeilingInteger(50);
                }
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_NormalAirVolume, "平时排风(m3//h)", NormalAirVolume.ToString()));
            }
            //Column 11
            {
                if (modeldata.Exhaust.ByNorm == 1)
                {
                    NormalFumeSupplementary = ((int)Math.Ceiling(NormalAirVolume * modeldata.AirCompensation.NormValue)).CeilingInteger(50);
                }
                else if (modeldata.Exhaust.ByNorm == 2)
                {
                    NormalFumeSupplementary = modeldata.AirCompensation.TotalValue;
                }
                else
                {
                    if (modeldata.AirCompensation.CapacityType == 1)
                    {
                        NormalFumeSupplementary = NormalAirVolume + ((int)Math.Ceiling(modeldata.AirCompensation.BoilerCapacity * modeldata.Exhaust.HeatDissipation * 0.01 / 8000 / 4.18 * 3600 / 0.9 * 9.6 * 1.2)).CeilingInteger(50);
                    }
                    else
                    {
                        NormalFumeSupplementary = (NormalAirVolume + modeldata.AirCompensation.FirewoodCapacity * modeldata.AirCompensation.CombustionAirVolume).CeilingInteger(50);
                    }
                }
                tuples.Add(new Tuple<bool, string, string>(ThLoadCalculationUIService.Instance.Parameter.chk_NormalFumeSupplementary, "平时补风量(m3//h)", NormalFumeSupplementary.ToString()));
            }
            return tuples;
        }

        public List<Tuple<Point3d, string,string>> InsertRoomFunctionBlk(List<ThMEPEngineCore.Model.ThIfcRoom> rooms, List<BlockReference> roomFunctionBlocks, bool hasPrefix, string perfixContent, int startingNo)
        {
            List<Tuple<Point3d, string, string>> tuples = new List<Tuple<Point3d, string, string>>();
            var roomdic = rooms.ToDictionary(key =>
            {
                var roomBoundary = key.Boundary;
                var center = Point3d.Origin;
                if (roomBoundary is Polyline polyline)
                {
                    center = polyline.GetMaximumInscribedCircleCenter();
                }
                if (roomBoundary is MPolygon Mpolygon)
                {
                    center = Mpolygon.GetMaximumInscribedCircleCenter();
                }
                return center;
            }, value => value);
            var BlocksDic = roomFunctionBlocks.ToDictionary(key => new DBPoint(key.Position), value => value);
            ThCADCoreNTSSpatialIndex blkSpatialIndex = new ThCADCoreNTSSpatialIndex(BlocksDic.Keys.ToCollection());

            var sortList = new List<Point3d>();
            var roomCenters = roomdic.Keys.ToList();
            while (sortList.Count != roomCenters.Count)
            {
                var points = roomCenters.Except(sortList).ToList();
                var ConvexHullpoints = Algorithms.GetConvexHull(points);
                var exceptPoints = points.Except(ConvexHullpoints).ToList();
                if(exceptPoints.Count>0)
                {
                    int index = 0;
                    while (ConvexHullpoints.Count != index)
                    {
                        var pt1 = ConvexHullpoints[index];
                        index++;
                        var pt2 = ConvexHullpoints[index == ConvexHullpoints.Count ? 0 : index];
                        Line line = new Line(pt1, pt2);
                        var onlinepts= exceptPoints.Where(o => line.IsOnLine(o));
                        ConvexHullpoints.InsertRange(index, onlinepts);
                        index += onlinepts.Count();
                        exceptPoints=exceptPoints.Except(onlinepts).ToList();
                    }
                }

                sortList.AddRange(ConvexHullpoints);

            }
            foreach (Point3d centerPt in sortList)
            {
                var room = roomdic[centerPt];
                if (!room.IsNull())
                {
                    var SelectWindowobjs = blkSpatialIndex.SelectCrossingPolygon(room.Boundary);
                    if (SelectWindowobjs.Count == 0)
                    {
                        var roomfunctions = room.Tags.SelectMany(tag => LoadCalculationParameterFromConfig.RoomFunctionConfigDic.Where(o => CompareRoom(o.Key, tag)).Select(o => o.Value));
                        string roomfunction = roomfunctions.LastOrDefault();
                        if (!string.IsNullOrEmpty(roomfunction))
                        {
                            tuples.Add(new Tuple<Point3d, string, string>(centerPt, roomfunction, string.Format("{0}{1}", hasPrefix ? perfixContent : "", startingNo++.ToString("00"))));
                        }
                    }
                    else if (SelectWindowobjs.Count == 1)
                    {
                        DBPoint dBPoint = SelectWindowobjs[0] as DBPoint;
                        BlockReference roomFunction = BlocksDic[dBPoint];
                        roomFunction.Id.UpdateAttributesInBlock(new Dictionary<string, string>() { { "房间编号", string.Format("{0}{1}", hasPrefix ? perfixContent : "", startingNo++.ToString("00")) } });
                    }
                }
            }
            return tuples;
        }

        public int ChangeRoonFunctionBlk(List<BlockReference> roomFunctionBlocks, bool hasPrefix, string perfixContent, int startingNo)
        {
            var BlocksDic = roomFunctionBlocks.ToDictionary(key => key.Position, value => value);
            var sortList = new List<Point3d>();
            var blockcenters = BlocksDic.Keys.ToList();
            while (sortList.Count != blockcenters.Count)
            {
                var points = blockcenters.Except(sortList).ToList();
                var ConvexHullpoints = Algorithms.GetConvexHull(points);
                var exceptPoints = points.Except(ConvexHullpoints).ToList();
                if (exceptPoints.Count > 0)
                {
                    int index = 0;
                    while (ConvexHullpoints.Count != index)
                    {
                        var pt1 = ConvexHullpoints[index];
                        index++;
                        var pt2 = ConvexHullpoints[index == ConvexHullpoints.Count ? 0 : index];
                        Line line = new Line(pt1, pt2);
                        var onlinepts = exceptPoints.Where(o => line.IsOnLine(o));
                        ConvexHullpoints.InsertRange(index, onlinepts);
                        index += onlinepts.Count();
                        exceptPoints = exceptPoints.Except(onlinepts).ToList();
                    }
                }
                sortList.AddRange(ConvexHullpoints);
            }
            foreach (Point3d centerPt in sortList)
            {
                var roomFunction = BlocksDic[centerPt];
                if (!roomFunction.IsNull())
                {
                    roomFunction.Id.UpdateAttributesInBlock(new Dictionary<string, string>() { { "房间编号", string.Format("{0}{1}", hasPrefix ? perfixContent : "", startingNo++.ToString("00")) } });
                }
            }
            return startingNo;
        }

        /// <summary>
        /// 判断一个字符串中是否包括指定房间名
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public static bool CompareRoom(string roomName, string roomTag)
        {
            if (roomName == roomTag)
            {
                return true;
            }

            if (roomName.Contains("*"))
            {
                string str = roomName;
                if (roomName[0] != '*')
                {
                    str = '^' + str;
                }
                if (roomName[roomName.Length - 1] != '*')
                {
                    str = str + '$';
                }
                str = str.Replace("*", ".*");
                if (System.Text.RegularExpressions.Regex.IsMatch(roomTag, str))
                {
                    return true;
                }
            }
            return false;
        }
    }
}