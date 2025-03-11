using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace TA_WPF.Views
{
    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Question
    }

    public enum MessageButtons
    {
        Ok,
        YesNo,
        YesNoCancel
    }

    public partial class MessageDialog : Window, INotifyPropertyChanged
    {
        private string _message;
        private PackIconKind _iconKind;
        private Brush _iconBrush;
        private MessageButtons _buttons;

        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        public PackIconKind IconKind
        {
            get => _iconKind;
            set
            {
                if (_iconKind != value)
                {
                    _iconKind = value;
                    OnPropertyChanged(nameof(IconKind));
                }
            }
        }

        public Brush IconBrush
        {
            get => _iconBrush;
            set
            {
                if (_iconBrush != value)
                {
                    _iconBrush = value;
                    OnPropertyChanged(nameof(IconBrush));
                }
            }
        }

        public MessageButtons Buttons
        {
            get => _buttons;
            set
            {
                if (_buttons != value)
                {
                    _buttons = value;
                    OnPropertyChanged(nameof(Buttons));
                    OnPropertyChanged(nameof(IsOkButtonVisible));
                    OnPropertyChanged(nameof(IsYesNoButtonsVisible));
                    OnPropertyChanged(nameof(IsCancelButtonVisible));
                }
            }
        }

        public bool IsOkButtonVisible => Buttons == MessageButtons.Ok;
        public bool IsYesNoButtonsVisible => Buttons == MessageButtons.YesNo || Buttons == MessageButtons.YesNoCancel;
        public bool IsCancelButtonVisible => Buttons == MessageButtons.YesNoCancel;

        public MessageDialog(string message, string title, MessageType type, MessageButtons buttons)
        {
            InitializeComponent();
            DataContext = this;
            
            Message = message;
            Title = title;
            Buttons = buttons;
            
            // 设置图标和颜色
            switch (type)
            {
                case MessageType.Information:
                    IconKind = PackIconKind.Information;
                    IconBrush = new SolidColorBrush(Colors.DodgerBlue);
                    break;
                case MessageType.Warning:
                    IconKind = PackIconKind.Alert;
                    IconBrush = new SolidColorBrush(Colors.Orange);
                    break;
                case MessageType.Error:
                    IconKind = PackIconKind.Error;
                    IconBrush = new SolidColorBrush(Colors.Red);
                    break;
                case MessageType.Question:
                    IconKind = PackIconKind.QuestionMark;
                    IconBrush = new SolidColorBrush(Colors.DodgerBlue);
                    break;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 静态方法，用于显示消息对话框
        public static bool? Show(string message, string title = "提示", MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.Ok, Window owner = null)
        {
            var dialog = new MessageDialog(message, title, type, buttons);
            
            if (owner != null)
            {
                dialog.Owner = owner;
            }
            else if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            
            return dialog.ShowDialog();
        }
    }
} 