using System;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDocumentEventHandler : IDisposable
    {
        private Document Document { get; set; }

        private ThFanSelectionDbUndoHandler DbUndoHandler { get; set; }

        private ThFanSelectionDbEraseHandler DbEraseHandler { get; set; }

        private ThFanSelectionDbDeepCloneHandler DbDeepCloneHandler { get; set; }

        private Database Database
        {
            get
            {
                return Document.Database;
            }
        }

        public ThFanSelectionDocumentEventHandler(Document document)
        {
            Document = document;
            Document.CommandEnded += CommandEndedHandler;
            Document.CommandFailed += CommandFailedHandler;
            Document.CommandCancelled += CommandCancelledHandler;
            Document.CommandWillStart += CommandWillStartHandler;
        }

        public void Dispose()
        {
            Document.CommandEnded -= CommandEndedHandler;
            Document.CommandFailed -= CommandFailedHandler;
            Document.CommandCancelled -= CommandCancelledHandler;
            Document.CommandWillStart -= CommandWillStartHandler;
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
                DbUndoHandler = new ThFanSelectionDbUndoHandler(Database);
                DbEraseHandler = new ThFanSelectionDbEraseHandler(Database);
            }
            else if (e.GlobalCommandName == "ERASE")
            {
                DbEraseHandler = new ThFanSelectionDbEraseHandler(Database);
            }
            else if (e.GlobalCommandName == "COPY")
            {
                DbDeepCloneHandler = new ThFanSelectionDbDeepCloneHandler(Database);
            }
            else if (e.GlobalCommandName == "CUTCLIP")
            {
                DbEraseHandler = new ThFanSelectionDbEraseHandler(Database);
            }
            else if (e.GlobalCommandName == "PASTECLIP")
            {
                DbDeepCloneHandler = new ThFanSelectionDbDeepCloneHandler(Database);
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
                SendEraseMessage();
            }
            else if (e.GlobalCommandName == "PASTECLIP")
            {
                SendCopyMessage();
            }
            ResetDbHandlers();
        }

        public void CommandFailedHandler(object sender, CommandEventArgs e)
        {
            ResetDbHandlers();
        }

        public void CommandCancelledHandler(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName == "COPY")
            {
                SendCopyMessage();
            }
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
            if (DbDeepCloneHandler != null)
            {
                DbDeepCloneHandler.Dispose();
                DbDeepCloneHandler = null;
            }
        }

        public void SendUndoMessage()
        {
            if (DbUndoHandler.UnappendedModels.Count == 0 &&
                DbUndoHandler.ReappendedModels.Count == 0)
            {
                return;
            }

            _ = new ThFanSelectionAppIdleHandler()
            {
                Message = new ThModelUndoMessage(),
                MessageArgs = new ThModelUndoMessageArgs()
                {
                    UnappendedModels = DbUndoHandler.UnappendedModels,
                    ReappendedModels = DbUndoHandler.ReappendedModels,
                }
            };
        }

        public void SendEraseMessage()
        {
            if (DbEraseHandler.ErasedModels.Count == 0 &&
                DbEraseHandler.UnerasedModels.Count == 0)
            {
                return;
            }

            _ = new ThFanSelectionAppIdleHandler()
            {
                Message = new ThModelDeleteMessage(),
                MessageArgs = new ThModelDeleteMessageArgs()
                {
                    ErasedModels = DbEraseHandler.ErasedModels,
                    UnerasedModels = DbEraseHandler.UnerasedModels,
                }
            };
        }

        public void SendCopyMessage()
        {
            if (DbDeepCloneHandler.ModelSystemMapping.Count == 0)
            {
                return;
            }

            // 暂时只支持复制整个系统
            _ = new ThFanSelectionAppIdleHandler()
            {
                Message = new ThModelCopyMessage(),
                MessageArgs = new ThModelCopyMessageArgs()
                {
                    ModelSystemMapping = DbDeepCloneHandler.ModelSystemMapping,
                }
            };
        }
    }
}
