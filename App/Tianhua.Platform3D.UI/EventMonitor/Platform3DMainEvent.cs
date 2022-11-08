﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.EventMonitor
{
    class Platform3DMainEvent
    {
        public static void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            Platform3DMainService.Instace.DocumentActivated(e);
        }
        public static void DocumentManager_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            Platform3DMainService.Instace.DocumentToBeDestroyed(e);
        }
        public static void DocumentManager_DocumentToBeActivated(object sender, DocumentCollectionEventArgs e)
        {
            Platform3DMainService.Instace.DocumentToBeActivated(e);
        }

        public static void DocumentManager_DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            Platform3DMainService.Instace.DocumentDestroyed(e);
        }
    }
}