using System;
using System.Linq;
using NFox.Cad;
using AcHelper;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Command
{
    public class ThPickRoomCmd : ThMEPBaseCommand, IDisposable
    {
        public DBObjectCollection RoomDatas { get; set; }
        public ThPickRoomCmd(DBObjectCollection roomDatas)
        {
            RoomDatas = Clone(roomDatas);
        }

        private DBObjectCollection Clone(DBObjectCollection objs)
        {
            return objs
                .OfType<Entity>()
                .Select(e => e.Clone() as Entity)
                .ToCollection();
        }

        public void Dispose()
        {
            //
            RoomDatas.OfType<Entity>().ForEach(e => e.Dispose());
        }

        public override void SubExecute()
        {
            // 原始数据处理
            Active.Editor.WriteLine("\n数据分析中......");
            var roomPickUpService = new ThKJSQInteractionService();
            roomPickUpService.Process(RoomDatas);

            // 拾取空间
            roomPickUpService.Run();

            // 输出结果到图纸
            if (roomPickUpService.Status == PickUpStatus.OK)
            {
                roomPickUpService.PrintRooms();
            }
        }
    }
}
