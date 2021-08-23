using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.IO.IOService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class TxtStrucInfoCommand : IAcadCommand, IDisposable
    {
        static string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
        static string blockConfigUrl = "D:\\连线功能白名单.xlsx";
        List<RoomTableTree> roomTableConfig = null;
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ReadRoomConfigTable();
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { ThMEPCommon.FRAME_LAYER_NAME });
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as BlockReference);
                }

                List<Polyline> frames = new List<Polyline>();
                foreach (var frameBlock in frameLst)
                {
                    var frame = CommonService.GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                var pt = frames.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frames = frames.Select(x =>
                {
                    //originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x);
                }).ToList();
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                foreach (var outFrame in frames)
                {
                    //acadDatabase.ModelSpace.Add(outFrame);
                    var firFrames = getPrimitivesService.GetFireFrame(outFrame);
                    for (int i = 0; i < firFrames.Count; i++)
                    {
                        var fFrame = firFrames[i];
                        var infos = GetMPolygonInfo(fFrame);
                        var polyFrame = infos.Key;
                        var holes = infos.Value;
                        //获取构建信息
                        var rooms = new List<ThIfcRoom>();
                        using (var ov = new ThCADCoreNTSArcTessellationLength(3000))
                        {
                            rooms = getPrimitivesService.GetRoomInfo(polyFrame);
                        }
                        List<Polyline> roomPolys = new List<Polyline>();
                        foreach (var room in rooms)
                        {
                            if (room.Tags.Count > 0 && !RoomConfigTreeService.IsPublicRoom(roomTableConfig, room.Tags[0]))
                            {
                                var roomInfos = GetMPolygonInfo(room.Boundary);
                                roomPolys.Add(roomInfos.Key);
                                holes.AddRange(roomInfos.Value);
                            }
                        }
                        getPrimitivesService.GetStructureInfo(polyFrame, out List<Polyline> columns, out List<Polyline> walls);
                        holes.AddRange(columns);
                        holes.AddRange(walls);
                        //foreach (var item in columns)
                        //{
                        //    acadDatabase.ModelSpace.Add(item);
                        //}
                        //foreach (var item in walls)
                        //{
                        //    acadDatabase.ModelSpace.Add(item);
                        //}
                        List<List<Line>> otherLanes = new List<List<Line>>();
                        var lanes = getPrimitivesService.GetLanes(polyFrame, out otherLanes);
                        lanes.AddRange(otherLanes);
                        var blocks = GetBlocks(polyFrame);
                        if (blocks.Count > 0)
                        {
                            //acadDatabase.ModelSpace.Add(polyFrame);
                            OutputPts(blocks.Select(x => x.Position).ToList(), "点位" + i.ToString());
                            OutputPolylines(holes, "洞口" + i.ToString(), 100);
                            OutputPolylines(roomPolys, "房间框线" + i.ToString(), 10);
                            OutputPolylines(new List<Polyline>() { polyFrame }, "防火分区" + i.ToString(), 10);
                            OutputLines(lanes.SelectMany(x => x).ToList(), "中心线" + i.ToString());
                        }
                    }
                }
            }
        }

        private void OutputLines(List<Line> lines, string txtName)
        {
            var strs = lines.Select(x =>
            {
                string str = "(";
                str = str + "(" + x.StartPoint.X.ToString() + "," + x.StartPoint.Y.ToString() + "),";
                str = str + "(" + x.EndPoint.X.ToString() + "," + x.EndPoint.Y.ToString() + ")";
                str = str + ")";
                return str;
            }).ToList();
            IOOperateService.OutputTxt("C:\\Users\\tangyongjing\\Desktop\\test\\" + txtName, strs);
        }


        private void OutputPts(List<Point3d> pts, string txtName)
        {
            var strs = pts.Select(x => "(" + x.X.ToString() + "," + x.Y.ToString() + ")").ToList();
            IOOperateService.OutputTxt("C:\\Users\\tangyongjing\\Desktop\\test\\" + txtName, strs);
        }

        private void OutputPolylines(List<Polyline> polys, string txtName, double Weight)
        {
            var strs = polys.Select(x =>
            {
                string str = "((";
                for (int i = 0; i < x.NumberOfVertices; i++)
                {
                    if (i != 0)
                    {
                        str = str + ",";
                    }
                    str = str + "(" + x.GetPoint2dAt(i).X.ToString() + "," + x.GetPoint2dAt(i).Y.ToString() + ")";
                }
                str = str + ")," + Weight.ToString() + ")";
                return str;
            }).ToList();
            IOOperateService.OutputTxt("C:\\Users\\tangyongjing\\Desktop\\test\\" + txtName, strs);
        }

        /// <summary>
        /// 查找框线内所有布置图块
        /// </summary>
        /// <param name="polyline"></param>
        private List<BlockReference> GetBlocks(Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //获取喷淋
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.BlockName) == string.Join(",", ReadBlockConfig()) &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var blocks = new List<Entity>();
                var status = Active.Editor.SelectAll(filterlist);
                if (status.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in status.Value.GetObjectIds())
                    {
                        blocks.Add(acadDatabase.Element<Entity>(obj));
                    }
                }
                var resBlocks = new List<BlockReference>();
                blocks.Where(o =>
                {
                    var pts = o.GeometricExtents;
                    var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                    return polyline.Contains(position);
                })
                .Cast<BlockReference>()
                .ForEachDbObject(o => resBlocks.Add(o));

                return resBlocks;
            }
        }

        /// <summary>
        /// 读取块名配置表
        /// </summary>
        private List<string> ReadBlockConfig()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(blockConfigUrl, true);
            List<string> layerNames = new List<string>();
            foreach (System.Data.DataTable table in dataSet.Tables)
            {
                for (int i = 2; i < table.Rows.Count; i++)
                {
                    DataRow dataRow = table.Rows[i];
                    layerNames.Add(dataRow[0].ToString());
                }
            }
            return layerNames;
        }

        /// <summary>
        /// 读取房间配置表
        /// </summary>
        private void ReadRoomConfigTable()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var table = dataSet.Tables[ThElectricalUIService.Instance.Parameter.RoomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }
        }

        private KeyValuePair<Polyline, List<Polyline>> GetMPolygonInfo(Entity entity)
        {
            List<Polyline> resHoles = new List<Polyline>();
            Polyline polyFrame = null;
            if (entity is Polyline polyline)
            {
                polyFrame = polyline;
            }
            else if (entity is MPolygon mPolygon)
            {
                for (int i = 0; i < mPolygon.Loops().Count; i++)
                {
                    if (i == 0) polyFrame = mPolygon.Loops()[i] as Polyline;
                    else resHoles.Add(mPolygon.Loops()[i]);
                }
            }
            return new KeyValuePair<Polyline, List<Polyline>>(polyFrame, resHoles);
        }
    }
}
