using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThRoomFunctionExtractCmd : ThMEPBaseCommand, IDisposable
    {
        string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "LoadCalculationConfig");
        string defaultFile = "房间功能映射表.xlsx";
        public ThRoomFunctionExtractCmd()
        {
            this.CommandName = "THTQFJ";
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
                var frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                var pts = frame.Vertices();
                if (pts.Count < 3)
                {
                    return;
                }
                if (LoadCalculationParameterFromConfig.RoomFunctionConfigDic.IsNull())
                {
                    LoadCalculationParameterFromConfig.RoomFunctionConfigTable = LoadRoomFunctionConfig();
                    LoadCalculationParameterFromConfig.RoomFunctionConfigDic = ParseTable(LoadCalculationParameterFromConfig.RoomFunctionConfigTable);
                }
                //初始化图纸(导入图层/图块等)
                InsertBlockService.initialization();

                //var roomBuilder = new ThRoomBuilderEngine();
                //var rooms = roomBuilder.BuildFromMS(database.Database, pts);
                var roomEngine = new ThRoomOutlineRecognitionEngine();
                roomEngine.RecognizeMS(database.Database, pts);
                var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markEngine = new ThDB3RoomMarkRecognitionEngine();
                markEngine.Recognize(database.Database, pts);
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var roomBuilder = new ThRoomBuilderEngine();
                roomBuilder.Build(rooms, marks);

                foreach (var room in rooms)
                {
                    var roomfunctions = room.Tags.SelectMany(tag => LoadCalculationParameterFromConfig.RoomFunctionConfigDic.Where(o => CompareRoom(o.Key, tag)).Select(o => o.Value));
                    string roomfunction = roomfunctions.LastOrDefault();
                    if (!string.IsNullOrEmpty(roomfunction))
                    {
                        var roomBoundary = room.Boundary;
                        var center = Point3d.Origin;
                        if (roomBoundary is Polyline polyline)
                        {
                            center = polyline.GetMaximumInscribedCircleCenter();
                        }
                        if (roomBoundary is MPolygon Mpolygon)
                        {
                            center = Mpolygon.GetMaximumInscribedCircleCenter();
                        }
                        InsertBlockService.InsertSpecifyBlock(roomfunction, center);
                    }
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
                var roomfunctionName = row["房间功能标签"].ToString();
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
