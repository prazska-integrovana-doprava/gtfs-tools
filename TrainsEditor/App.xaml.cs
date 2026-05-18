using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText(
                "ui-crash.log",
                e.Exception.ToString());

            MessageBox.Show(e.Exception.ToString());

            e.Handled = true;
        }

        private void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            File.WriteAllText(
                "fatal.log",
                e.ExceptionObject.ToString());
        }

        private void OnUnobservedTaskException(
            object sender,
            UnobservedTaskExceptionEventArgs e)
        {
            File.WriteAllText(
                "task.log",
                e.Exception.ToString());
        }
    }
}
