using System;
using System.Collections.Generic;
using Linq2Acad;
using System.IO;
using System.Linq;
using System.Data;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using NFox.Cad;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThRoomFunctionExtractCmd : ThMEPBaseCommand, IDisposable
    {
        string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "LoadCalculationConfig");
        string defaultFile = "房间功能映射表.xlsx";
        public ThRoomFunctionExtractCmd()
        {
            this.CommandName = "THFJGNTQ";
            this.ActionName = "提取房间功能";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var database = AcadDatabase.Active())
            {
                //初始化
                int StartingNo;
                if (!int.TryParse(ThLoadCalculationUIService.Instance.Parameter.TQStartingNum, out StartingNo))
                {
                    return;
                }

                // 获取房间框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { LoadCalculationParameterFromConfig.Room_Layer_Name });
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                ObjectIdCollection dBObject = new ObjectIdCollection();
                var dbobjs = new DBObjectCollection();
                foreach (ObjectId objid in result.Value.GetObjectIds())
                {
                    dBObject.Add(objid);
                    dbobjs.Add(database.Element<Entity>(objid));
                }
                

                if (LoadCalculationParameterFromConfig.RoomFunctionConfigDic.IsNull())
                {
                    LoadCalculationParameterFromConfig.RoomFunctionConfigTable = LoadRoomFunctionConfig();
                    LoadCalculationParameterFromConfig.RoomFunctionConfigDic = ParseTable(LoadCalculationParameterFromConfig.RoomFunctionConfigTable);
                }
                //初始化图纸(导入图层/图块/图层三板斧等)
                InsertBlockService.initialization();

                var Rectangle = dbobjs.GeometricExtents().ToRectangle();
                var startPt = Rectangle.StartPoint;
                var originTransformer = new ThMEPOriginTransformer(startPt);
                var pts = originTransformer.Transform(Rectangle.Vertices());

                // 获取块里和本地的房间名称
                var roomMarkExtraction = new ThDB3RoomMarkExtractionEngine();
                roomMarkExtraction.Extract(database.Database);
                roomMarkExtraction.Results.ForEach(r=>originTransformer.Transform(r.Geometry));
                var markEngine = new ThDB3RoomMarkRecognitionEngine();
                markEngine.Recognize(roomMarkExtraction.Results, pts);
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();

                // 获取房间轮廓线
                var roomOutlineExtraction = new ThDB3RoomOutlineExtractionEngine();
                roomOutlineExtraction.ExtractFromMS(database.Database, dBObject);
                roomOutlineExtraction.Results.ForEach(r => originTransformer.Transform(r.Geometry));
                var roomEngine = new ThDB3RoomOutlineRecognitionEngine();
                roomEngine.Recognize(roomOutlineExtraction.Results, pts);
                var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();

                // 造房间
                var roomBuilder = new ThRoomBuilderEngine();
                roomBuilder.Build(rooms, marks);
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                var roomFunctionBlocks = getPrimitivesService.GetRoomFunctionBlocks();

                LogicService logicService = new LogicService();
                var roomfunctions = logicService.InsertRoomFunctionBlk(rooms, roomFunctionBlocks, ThLoadCalculationUIService.Instance.Parameter.TQHasPrefix, ThLoadCalculationUIService.Instance.Parameter.TQPerfixContent, StartingNo);
                //移回原点
                originTransformer.Reset(roomFunctionBlocks.ToCollection());
                foreach (var item in roomfunctions)
                {
                    InsertBlockService.InsertRoomFunctionBlock(item.Item3, item.Item2, originTransformer.Reset(item.Item1));
                }
            }
        }

        private System.Data.DataTable LoadRoomFunctionConfig()
        {
            string path= urlFolder + "\\" + defaultFile;
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataset=excelSrevice.ReadExcelToDataSet(path, true);
            return dataset.Tables["房间名称处理"];
        }

        private Dictionary<string, string> ParseTable(System.Data.DataTable roomFunctionConfigTable)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (DataRow row in roomFunctionConfigTable.Rows)
            {
                var roomfunctionName = row["暖通房间功能标签"].ToString();
                if (!string.IsNullOrWhiteSpace(roomfunctionName))
                {
                    for (int column = 0; column < 4; column++)
                    {
                        var columnValue = row[column].ToString();
                        if (!string.IsNullOrWhiteSpace(columnValue))
                        {
                            if (!result.ContainsKey(columnValue))
                            {
                                result.Add(columnValue, roomfunctionName);
                            }
                            else
                            {
                                result[columnValue] = roomfunctionName;
                            }
                        }
                    }

                    var synonymValue = row[4].ToString();
                    var synonymValueList = synonymValue.Replace('，', ',').Split(',');
                    foreach (var columnValue in synonymValueList)
                    {
                        if (!string.IsNullOrWhiteSpace(columnValue))
                        {
                            if (!result.ContainsKey(columnValue))
                            {
                                result.Add(columnValue, roomfunctionName);
                            }
                            else
                            {
                                result[columnValue] = roomfunctionName;
                            }
                        }
                    }
                }
            }
            return result;
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
