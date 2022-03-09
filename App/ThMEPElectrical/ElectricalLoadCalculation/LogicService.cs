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
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPElectrical.ElectricalLoadCalculation
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
            foreach (Entity roomBoundary in rooms)
            {
                var SelectWindowobjs = blkSpatialIndex.SelectCrossingPolygon(roomBoundary);
                if (SelectWindowobjs.Count == 1)
                {
                    DBPoint dBPoint = SelectWindowobjs[0] as DBPoint;
                    BlockReference roomFunction = BlocksDic[dBPoint];

                    var loadCalculationtableobjs = tableSpatialIndex.SelectCrossingPolygon(roomBoundary);
                    bool findtable = false;
                    if (loadCalculationtableobjs.Count >0)
                    {
                        foreach (Entity item in loadCalculationtableobjs)
                        {
                            //本身含有表格
                            Polyline tableBoundary = item as Polyline;
                            Table existedTable = tableDic[tableBoundary];
                            if (IsContains(roomBoundary, existedTable.Position))
                            {
                                //找到表格
                                var table = FillTableData(roomBoundary, roomFunction, existedTable.Position);
                                if (!table.IsNull())
                                {
                                    findtable = true;
                                    Deprecatedtables.Add(existedTable);
                                    result.Add(table);
                                }
                                break;
                            }
                        }
                    }
                    if (!findtable)
                    {
                        var curveobjs = curveSpatialIndex.SelectFence(roomBoundary);
                        if (curveobjs.Count > 0)
                        {
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
                                        var table = FillTableData(roomBoundary, roomFunction, existedTable.Position);
                                        if (!table.IsNull())
                                        {
                                            int addRowCount = table.Rows.Count - existedTable.Rows.Count;
                                            var connectpoint = tableBoundary.Contains(curve.EndPoint) ? curve.EndPoint : curve.StartPoint;
                                            if (addRowCount != 0 && Math.Abs(connectpoint.Y - existedTable.Position.Y) > existedTable.Height / 2)
                                            {
                                                table.Position = table.Position + new Vector3d(0, addRowCount * 440, 0);
                                            }
                                            var tableRec = table.GeometricExtents.ToRectangle();
                                            if (!tableRec.Contains(connectpoint) && tableRec.Intersect(curve, Intersect.OnBothOperands).Count < 1)
                                            {
                                                table.Position = table.Position + new Vector3d(existedTable.Width - table.Width, 0, 0);
                                            }
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
                                var table = FillTableData(roomBoundary, roomFunction, roomFunction.Position);
                                if (!table.IsNull())
                                {
                                    result.Add(table);
                                }
                            }
                        }
                        else
                        {
                            //找不到表格，新建表格
                            var table = FillTableData(roomBoundary, roomFunction, roomFunction.Position);
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

        private bool IsContains(Entity roomBoundary, Point3d position)
        {
            if (roomBoundary is Polyline polyline)
            {
                return polyline.Contains(position);
            }
            else if (roomBoundary is MPolygon mPolygon)
            {
                return mPolygon.Contains(position);
            }
            else
                return false;
        }

        /// <summary>
        /// 填充表格数据
        /// </summary>
        /// <param name="roomBoundary"></param>
        /// <param name="block"></param>
        /// <param name="summerVentilationT"></param>
        /// <param name="winterVentilationT"></param>
        /// <returns></returns>
        private Table FillTableData(Entity roomBoundary, BlockReference block, Point3d tablePoint)
        {
            Table table = new Table()
            {
                Position = tablePoint,
                TableStyle= Active.Database.Tablestyle,
            };
            var TableData = CalculateData(roomBoundary, block);
            var ShowData = TableData.Where(o => o.Item1).ToList();
            if (ShowData.Count < 2)
            {
                return null;
            }
            table.SetSize(ShowData.Count, 2);
            table.SetRowHeight(440);
            table.SetTextHeight(300);
            // 根据文字的字符数估算列的长度
            table.Columns[0].Width = 2400;
            table.Columns[1].Width = ShowData.Max(o => o.Item3.Length) * 180 + 200;
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
        private List<Tuple<bool, string, string>> CalculateData(Entity roomBoundary, BlockReference block)
        {
            List<Tuple<bool, string, string>> tuples = new List<Tuple<bool, string, string>>();
            var blockAttDic = block.Id.GetAttributesInBlockReference();
            string roomFunctionName = blockAttDic["房间功能"];
            double roomHeigth = double.Parse(blockAttDic["房间净高"]);
            var modeldata = ElectricalLoadCalculationConfig.ModelDataList.FirstOrDefault(o => o.RoomFunction == roomFunctionName);
            if (modeldata.IsNull())
            {
                return tuples;
            }
            int Area = 0;//面积
            string ElectricalIndicators ="-";//用电指标
            string ElectricalLoad = "-";//用电量
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
                tuples.Add(new Tuple<bool, string, string>(ElectricalLoadCalculationConfig.chk_Area, @"面积(m{\H0.7x;\S2^;})", Area.ToString()));
            }
            //Column 2
            {
                if (!modeldata.PowerNorm.ByNorm || (modeldata.PowerNorm.NormValue.HasValue && modeldata.PowerNorm.NormValue.Value > 0))
                {
                    ElectricalIndicators = modeldata.PowerNorm.ByNorm ? modeldata.PowerNorm.NormValue.Value.ToString() : "-";
                }
                if (!ElectricalLoadCalculationConfig.chk_ElectricalIndicators)
                {
                    tuples.Add(new Tuple<bool, string, string>(false, "用电指标(W/m²)", ElectricalIndicators));
                }
                else
                {
                    tuples.Add(new Tuple<bool, string, string>(true, "用电指标(W/m²)", ElectricalIndicators));
                }
            }
            //Column 3
            {
                if (!modeldata.PowerNorm.ByNorm ||  modeldata.PowerNorm.NormValue.Value > 0)
                {
                    if (modeldata.PowerNorm.ByNorm)
                    {
                        int value1 = 0, value2 = 0;
                        if (modeldata.PowerNorm.NormValue.HasValue)
                        {
                            value1 = (int)Math.Ceiling((double)(modeldata.PowerNorm.NormValue.Value * Area * 1.0 / 1000));
                        }
                        if(modeldata.PowerNorm.MinTotalValue.HasValue)
                        {
                            value2 = modeldata.PowerNorm.MinTotalValue.Value;
                        }
                        var value = Math.Max(value1, value2);
                        ElectricalLoad = value > 0 ? value.ToString() : "-";
                    }
                    else
                    {
                        ElectricalLoad = modeldata.PowerNorm.TotalValue.ToString();
                    }
                }
                if (!ElectricalLoadCalculationConfig.chk_ElectricalLoad)
                {
                    tuples.Add(new Tuple<bool, string, string>(false, "用电负荷(kW)", ElectricalLoad.ToString()));
                }
                else
                {
                    tuples.Add(new Tuple<bool, string, string>(true, "用电负荷(kW)", ElectricalLoad.ToString()));
                }
            }
            return tuples;
        }
    }
}
