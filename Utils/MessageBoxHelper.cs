using System.Windows;
using TA_WPF.Views;

namespace TA_WPF.Utils
{
    public static class MessageBoxHelper
    {
        public static bool? ShowInformation(string message, string title = "提示", Window owner = null)
        {
            return MessageDialog.Show(message, title, MessageType.Information, MessageButtons.Ok, owner);
        }

        public static bool? ShowWarning(string message, string title = "警告", Window owner = null)
        {
            return MessageDialog.Show(message, title, MessageType.Warning, MessageButtons.Ok, owner);
        }

        public static bool? ShowError(string message, string title = "错误", Window owner = null)
        {
            return MessageDialog.Show(message, title, MessageType.Error, MessageButtons.Ok, owner);
        }

        public static bool? ShowQuestion(string message, string title = "询问", Window owner = null)
        {
            return MessageDialog.Show(message, title, MessageType.Question, MessageButtons.YesNo, owner);
        }

        public static bool? ShowQuestionWithCancel(string message, string title = "询问", Window owner = null)
        {
            return MessageDialog.Show(message, title, MessageType.Question, MessageButtons.YesNoCancel, owner);
        }

        public static bool? ShowInfo(string message, string title = "提示", Window owner = null)
        {
            return ShowInformation(message, title, owner);
        }
    }
} 