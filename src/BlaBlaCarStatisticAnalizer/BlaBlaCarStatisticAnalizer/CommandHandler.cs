using System;
using System.Windows.Input;

namespace BlaBlaCarStatisticAnalizer
{
    public class CommandHandler : ICommand
    {
        /// <summary>
        /// Оболочка для выполнения команд, которые вешаються в кнопки
        /// </summary>
        private readonly Action _action;
        private readonly bool _canExecute;

        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
