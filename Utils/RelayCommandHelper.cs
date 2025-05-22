using System;
using System.Windows.Input;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 提供命令实现的通用工具类
    /// </summary>
    public static class RelayCommandHelper
    {
        // 辅助方法可以在这里添加
    }

    /// <summary>
    /// 实现ICommand接口的命令类，用于绑定UI元素的命令
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// 初始化RelayCommand的新实例
        /// </summary>
        /// <param name="execute">命令执行的动作</param>
        /// <param name="canExecute">确定命令是否可以执行的函数</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 当命令可执行状态发生变化时触发的事件
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 确定此命令是否可以在其当前状态下执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// 执行命令的方法
        /// </summary>
        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 实现ICommand接口的泛型命令类，用于绑定UI元素的命令
    /// </summary>
    /// <typeparam name="T">命令参数类型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// 初始化RelayCommand的新实例
        /// </summary>
        /// <param name="execute">命令执行的动作</param>
        /// <param name="canExecute">确定命令是否可以执行的函数</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 当命令可执行状态发生变化时触发的事件
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 确定此命令是否可以在其当前状态下执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// 执行命令的方法
        /// </summary>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
} 