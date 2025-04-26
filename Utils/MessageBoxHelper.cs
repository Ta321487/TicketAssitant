using System.Windows;
using TA_WPF.Views;

namespace TA_WPF.Utils
{
    public class InputDialogResult
    {
        public bool IsConfirmed { get; set; }
        public string InputText { get; set; }

        public InputDialogResult(bool isConfirmed, string inputText)
        {
            IsConfirmed = isConfirmed;
            InputText = inputText;
        }
    }

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

        public static InputDialogResult ShowInputDialog(string title, string prompt, string initialValue = "", Window owner = null)
        {
            var dialog = new InputDialog(prompt)
            {
                Title = title,
                ResponseText = initialValue,
                Owner = owner ?? Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool? result = dialog.ShowDialog();
            return new InputDialogResult(result == true, dialog.ResponseText);
        }

        public static MessageBoxResult ShowConfirmation(string message, string title = "确认", MessageBoxButton buttons = MessageBoxButton.YesNo)
        {
            bool? result = MessageDialog.Show(message, title, MessageType.Question, 
                buttons == MessageBoxButton.OK ? MessageButtons.Ok : 
                buttons == MessageBoxButton.YesNo ? MessageButtons.YesNo : 
                MessageButtons.YesNoCancel);
            
            if (result == true)
            {
                return buttons == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.Yes;
            }
            else if (result == false)
            {
                return MessageBoxResult.No;
            }
            else
            {
                return MessageBoxResult.Cancel;
            }
        }
    }
}