using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace BlaBlaCarStatisticAnalizer
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var path = Directory.GetCurrentDirectory();
            using (var sr = File.CreateText(path+@"\FatalError.txt"))
            {
                sr.Write($"Sender: {sender}\nError: {e.Exception.Message}");
            }

            e.Handled = true;
        }
    }
}
