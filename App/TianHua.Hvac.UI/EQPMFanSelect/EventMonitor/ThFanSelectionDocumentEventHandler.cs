using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.Hvac.UI.EQPMFanSelect.EventMonitor
{
    class ThFanSelectionDocumentEventHandler : IDisposable
    {
        private Document Document;
        private ThFanSelectionDbUndoHandler DbUndoHandler;

        private ThFanSelectionDbEraseHandler DbEraseHandler;
        public ThFanSelectionDocumentEventHandler(Document document)
        {
            Document = document;
            Document.CommandWillStart += CommandWillStartHandler;
            Document.CommandEnded += CommandEndedHandler;
            Document.CommandCancelled += CommandCancelledHandler;
            Document.CommandFailed += CommandFailedHandler;
        }

        public void Dispose()
        {
            Document.CommandWillStart -= CommandWillStartHandler;
            Document.CommandEnded -= CommandEndedHandler;
            Document.CommandCancelled -= CommandCancelledHandler;
            Document.CommandFailed -= CommandFailedHandler;
        }
        public void CommandWillStartHandler(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName == "U" ||
                e.GlobalCommandName == "UNDO" ||
                e.GlobalCommandName == "REDO" ||
                e.GlobalCommandName == "MREDO")
            {
                // UNDO/REDO ERASE命令会触发Database.ObjectErased事件
                // 所以这里同时挂接UNDO/REDO + ERASE事件
                DbUndoHandler = new ThFanSelectionDbUndoHandler(Document.Database);
                DbEraseHandler = new ThFanSelectionDbEraseHandler(Document.Database);
            }
            else if (e.GlobalCommandName == "ERASE")
            {
                DbEraseHandler = new ThFanSelectionDbEraseHandler(Document.Database);
            }
            else if (e.GlobalCommandName == "COPY")
            {
            }
            else if (e.GlobalCommandName == "CUTCLIP")
            {
            }
            else if (e.GlobalCommandName == "PASTECLIP")
            {
            }
        }
        public void CommandEndedHandler(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName == "U" ||
                e.GlobalCommandName == "UNDO" ||
                e.GlobalCommandName == "REDO" ||
                e.GlobalCommandName == "MREDO")
            {
                // UNDO/REDO ERASE命令会触发Database.ObjectErased事件
                // 所以这里同时触发UNDO/REDO + ERASE事件
                SendUndoMessage();
                SendEraseMessage();
            }
            else if (e.GlobalCommandName == "ERASE")
            {
                SendEraseMessage();
            }
            else if (e.GlobalCommandName == "COPY")
            {
                SendCopyMessage();
            }
            else if (e.GlobalCommandName == "CUTCLIP")
            {
            }
            else if (e.GlobalCommandName == "PASTECLIP")
            {
                SendCopyMessage();
            }
            ResetDbHandlers();
        }
        public void CommandCancelledHandler(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName == "COPY")
            {
                SendCopyMessage();
            }
        }
        public void CommandFailedHandler(object sender, CommandEventArgs e)
        {
            ResetDbHandlers();
        }
        private void ResetDbHandlers()
        {
            if (DbUndoHandler != null)
            {
                DbUndoHandler.Dispose();
                DbUndoHandler = null;
            }
            if (DbEraseHandler != null)
            {
                DbEraseHandler.Dispose();
                DbEraseHandler = null;
            }
        }
        public void SendUndoMessage()
        {
            if (DbUndoHandler.UnappendedModels.Count == 0 &&
                DbUndoHandler.ReappendedModels.Count == 0)
            {
                return;
            }
            var ids = new List<string>();
            foreach (var item in DbUndoHandler.UnappendedModels)
            {
                if (ids.Any(c => c == item))
                    continue;
                ids.Add(item);
            }
            foreach (var item in DbUndoHandler.ReappendedModels)
            {
                if (ids.Any(c => c == item))
                    continue;
                ids.Add(item);
            }
            EQPMUIServices.Instance.RefreshDeleteData(ids);
        }

        public void SendEraseMessage()
        {
            if (DbEraseHandler.ErasedModels.Count == 0 &&
                DbEraseHandler.UnerasedModels.Count == 0)
            {
                return;
            }
            var ids = new List<string>();
            foreach (var item in DbEraseHandler.ErasedModels)
            {
                if (ids.Any(c => c == item))
                    continue;
                ids.Add(item);
            }
            foreach (var item in DbEraseHandler.UnerasedModels)
            {
                if (ids.Any(c => c == item))
                    continue;
                ids.Add(item);
            }
            EQPMUIServices.Instance.RefreshDeleteData(ids);
        }
        public void SendCopyMessage()
        {
            EQPMUIServices.Instance.RefreshCopyData();
        }
    }
}
