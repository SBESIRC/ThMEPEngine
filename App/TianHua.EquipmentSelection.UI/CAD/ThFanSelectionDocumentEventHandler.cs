using System;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDocumentEventHandler : IDisposable
    {
        private Document Document { get; set; }

        private ThFanSelectionDbSaveHandler DbSaveHandler { get; set; }

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
            Document.CommandWillStart += CommandWillStartHandler;
        }

        public void Dispose()
        {
            Document.CommandEnded -= CommandEndedHandler;
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
            else if (e.GlobalCommandName == "QSAVE")
            {
                // QSAVE: 
                //  Saves drawing with current filename.
                // SAVEAS:               
                //  Saves drawing as new name, continues in new name.
                // SAVE:                    
                //  Saves drawing as new name, continues in old name. (Command line only)
                // 暂时只支持QSAVE
                DbSaveHandler = new ThFanSelectionDbSaveHandler(Database);
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
                DbUndoHandler.Dispose();
                DbEraseHandler.Dispose();
            }
            else if (e.GlobalCommandName == "QSAVE")
            {
                SendSaveMessage();
                DbSaveHandler.Dispose();
            }
            else if (e.GlobalCommandName == "ERASE")
            {
                SendEraseMessage();
                DbEraseHandler.Dispose();
            }
            else if (e.GlobalCommandName == "COPY")
            {
                SendCopyMessage();
                DbDeepCloneHandler.Dispose();
            }
            else if (e.GlobalCommandName == "CUTCLIP")
            {
                SendEraseMessage();
                DbEraseHandler.Dispose();
            }
            else if (e.GlobalCommandName == "PASTECLIP")
            {
                SendCopyMessage();
                DbDeepCloneHandler.Dispose();
            }
        }

        public void SendSaveMessage()
        {
            ThModelSaveMessage.SendWith(new ThModelSaveMessageArgs()
            {
                FileName = DbSaveHandler.FileName,
            });
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
            if (DbEraseHandler.Models.Count == 0)
            {
                return;
            }

            _ = new ThFanSelectionAppIdleHandler()
            {
                Message = new ThModelDeleteMessage(),
                MessageArgs = new ThModelDeleteMessageArgs()
                {
                    Models = DbEraseHandler.Models,
                }
            };
        }

        public void SendCopyMessage()
        {
            if (DbDeepCloneHandler.ModelMapping.Count == 0)
            {
                return;
            }

            _ = new ThFanSelectionAppIdleHandler()
            {
                Message = new ThModelCopyMessage(),
                MessageArgs = new ThModelCopyMessageArgs()
                {
                    ModelMapping = DbDeepCloneHandler.ModelMapping,
                }
            };
        }
    }
}
