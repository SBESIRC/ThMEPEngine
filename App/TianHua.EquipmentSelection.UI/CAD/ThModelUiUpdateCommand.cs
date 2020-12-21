using System;
using AcHelper;
using System.Linq;
using AcHelper.Commands;
using System.Collections.Generic;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelUiUpdateCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        private List<int> ModelNumbers(string identifier)
        {
            // 这里不能使用AcadDatabase，因为它会破坏Undo
            // 这里采用StartOpenCloseTransaction()避免REDO被破坏
            // https://adndevblog.typepad.com/autocad/2012/06/nothing-to-redo-message-appears-straight-after-undoing-in-autocad-for-net-and-objectarx.html
            using (var tx = Active.Database.TransactionManager.StartOpenCloseTransaction())
            {
                return Active.Database.ModelSpace(tx).GetEntities<BlockReference>(tx)
                    .Where(o => o.GetModelIdentifier() == identifier)
                    .Select(o => o.GetModelNumber()).ToList();
            }
        }

        public void Execute()
        {
            if (ThFanSelectionService.Instance.Message is ThModelUndoMessage message &&
                ThFanSelectionService.Instance.MessageArgs is ThModelUndoMessageArgs args)
            {
                var filters = new List<string>();
                foreach (var item in args.UnappendedModels)
                {
                    // 确保图纸中已经没有已经删除的图块
                    if (item.Value.Intersect(ModelNumbers(item.Key)).Any())
                    {
                        filters.Add(item.Key);
                    }
                }
                filters.ForEach(o => args.UnappendedModels.Remove(o));

                filters.Clear();
                foreach (var item in args.ReappendedModels)
                {
                    // 确保图纸中包含所有未被删除的图块
                    if (item.Value.Except(ModelNumbers(item.Key)).Any())
                    {
                        filters.Add(item.Key);
                    }
                }
                filters.ForEach(o => args.ReappendedModels.Remove(o));

                // 广播消息
                if (args.UnappendedModels.Count != 0 || args.ReappendedModels.Count != 0)
                {
                    ThModelUndoMessage.SendWith(args);
                }
            }
            else if (ThFanSelectionService.Instance.Message is ThModelDeleteMessage eraseMessage &&
                ThFanSelectionService.Instance.MessageArgs is ThModelDeleteMessageArgs eraseArgs)
            {
                var filters = new List<string>();
                foreach(var item in eraseArgs.ErasedModels)
                {
                    // 确保图纸中已经没有已经删除的图块
                    if (item.Value.Intersect(ModelNumbers(item.Key)).Any())
                    {
                        filters.Add(item.Key);
                    }
                }
                filters.ForEach(o => eraseArgs.ErasedModels.Remove(o));

                filters.Clear();
                foreach(var item in eraseArgs.UnerasedModels)
                {
                    // 确保图纸中包含所有未被删除的图块
                    if (item.Value.Except(ModelNumbers(item.Key)).Any())
                    {
                        filters.Add(item.Key);
                    }
                }
                filters.ForEach(o => eraseArgs.UnerasedModels.Remove(o));

                // 广播消息
                if (eraseArgs.ErasedModels.Count != 0 || eraseArgs.UnerasedModels.Count != 0)
                {
                    ThModelDeleteMessage.SendWith(eraseArgs);
                }
            }
            else if (ThFanSelectionService.Instance.Message is ThModelCopyMessage copyMessage &&
                ThFanSelectionService.Instance.MessageArgs is ThModelCopyMessageArgs copyArgs)
            {
                ThModelCopyMessage.SendWith(copyArgs);
            }
        }
    }
}
