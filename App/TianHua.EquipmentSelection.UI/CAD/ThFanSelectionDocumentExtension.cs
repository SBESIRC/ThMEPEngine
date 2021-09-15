using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using TianHua.FanSelection.Messaging;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.FanSelection.UI.CAD
{
    public static class ThFanSelectionDocumentExtension
    {
        public static void ShowModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
            }
            if (form != null)
            {
                if (!form.Visible)
                {
                    AcadApp.ShowModelessDialog(form);
                }
            }
        }

        public static void CreateModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (!document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = new fmFanSelection();
            }
            if (form != null)
            {
                document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] = form;
                document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI_VISIBLE] = false;
            }
        }

        public static void DestroyModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
            }
            if (form != null)
            {
                document.UserData.Remove(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI);
                document.UserData.Remove(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI_VISIBLE);
            }
            if (form != null)
            {
                form.Close();
                form.Dispose();
            }
        }

        public static void HideModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
            }
            if (form != null)
            {
                if (form.Visible)
                {
                    form.Hide();
                    HidefmOverView(form);
                }
            }
        }

        public static void SubscribeModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
            }
            if (form != null)
            {
                SubscribeToMessages(form);
            }
        }

        public static void UnsubscribeModelSelectionDialog(this Document document)
        {
            Form form = null;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
            }
            if (form != null)
            {
                UnSubscribeToMessages(form);
            }
        }

        private static void SubscribeToMessages(Form form)
        {
            if (form is fmFanSelection fm)
            {
                ThModelUndoMessage.Register(fm, fm.OnModelUndoHandler);
                ThModelCopyMessage.Register(fm, fm.OnModelCopiedHandler);
                ThModelDeleteMessage.Register(fm, fm.OnModelDeletedHandler);
                //ThModelBeginSaveMessage.Register(fm, fm.OnModelBeginSaveHandler);
            }
        }

        private static void UnSubscribeToMessages(Form form)
        {
            if (form is fmFanSelection fm)
            {
                ThModelUndoMessage.Unregister(fm, fm.OnModelUndoHandler);
                ThModelCopyMessage.Unregister(fm, fm.OnModelCopiedHandler);
                ThModelDeleteMessage.Unregister(fm, fm.OnModelDeletedHandler);
                //ThModelBeginSaveMessage.Unregister(fm, fm.OnModelBeginSaveHandler);
            }
        }

        private static void HidefmOverView(Form _Form)
        {
            if (_Form.Text == "风机选型")
            {
                var _fmFanSelection = _Form as fmFanSelection;
                if (_fmFanSelection != null)
                {
                    _fmFanSelection.m_fmOverView.Hide();
                }
            }
        }

        public static void PushModelSelectionDialogVisible(this Document document)
        {
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                Form form = document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as Form;
                document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI_VISIBLE] = form.Visible;
            }
        }

        public static bool PopModelSelectionDialogVisible(this Document document)
        {
            bool visible = false;
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                visible = (bool)document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI_VISIBLE];
            }
            return visible;
        }

        public static fmFanSelection Form(this Document document)
        {
            if (document.UserData.ContainsKey(ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI))
            {
                return document.UserData[ThFanSelectionUICommon.DOCUMENT_USER_DATA_UI] as fmFanSelection;
            }
            else
            {
                return null;
            }
        }
    }
}
