using System;
using AcHelper;
using System.Linq;
using AcHelper.Commands;
using System.Collections.Generic;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHAVC.CAD;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelUiUpdateCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        private List<string> Models()
        {
            // 这里不能使用AcadDatabase，因为它会破坏Undo
            // 这里采用StartOpenCloseTransaction()避免REDO被破坏
            // https://adndevblog.typepad.com/autocad/2012/06/nothing-to-redo-message-appears-straight-after-undoing-in-autocad-for-net-and-objectarx.html
            using (var tx = Active.Database.TransactionManager.StartOpenCloseTransaction())
            {
                return Active.Database.ModelSpace(tx).GetEntities<BlockReference>(tx)
                    .Where(o => o.IsModel())
                    .Select(o => o.GetModelIdentifier()).ToList();
            }
        }

        public void Execute()
        {
            if (ThFanSelectionService.Instance.Message is ThModelUndoMessage message &&
                ThFanSelectionService.Instance.MessageArgs is ThModelUndoMessageArgs args)
            {
                var models = Models();
                args.UnappendedModels.RemoveAll(o => models.Contains(o));
                args.ReappendedModels.RemoveAll(o => !models.Contains(o));
                if (args.UnappendedModels.Count == 0 &&
                    args.ReappendedModels.Count == 0)
                {
                    return;
                }
                ThModelUndoMessage.SendWith(args);
            }
            else if (ThFanSelectionService.Instance.Message is ThModelDeleteMessage eraseMessage &&
                ThFanSelectionService.Instance.MessageArgs is ThModelDeleteMessageArgs eraseArgs)
            {
                var models = Models();
                eraseArgs.ErasedModels.RemoveAll(o => models.Contains(o));
                eraseArgs.UnerasedModels.RemoveAll(o => !models.Contains(o));
                if (eraseArgs.ErasedModels.Count == 0 &&
                    eraseArgs.UnerasedModels.Count == 0)
                {
                    return;
                }
                ThModelDeleteMessage.SendWith(eraseArgs);
            }
            else if (ThFanSelectionService.Instance.Message is ThModelCopyMessage copyMessage &&
                ThFanSelectionService.Instance.MessageArgs is ThModelCopyMessageArgs copyArgs)
            {
                ThModelCopyMessage.SendWith(copyArgs);
            }
        }
    }
}
