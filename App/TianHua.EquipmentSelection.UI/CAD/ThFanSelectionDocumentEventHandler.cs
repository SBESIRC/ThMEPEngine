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
                DbUndoHandler = new ThFanSelectionDbUndoHandler(Database);
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
        }

        public void CommandEndedHandler(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName == "U" ||
                e.GlobalCommandName == "UNDO" ||
                e.GlobalCommandName == "REDO" ||
                e.GlobalCommandName == "MREDO")
            {
                SendUndoMessage();
                DbUndoHandler.Dispose();
            }
            else if (e.GlobalCommandName == "QSAVE")
            {
                SendSaveMessage();
                DbSaveHandler.Dispose();
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
    }
}
