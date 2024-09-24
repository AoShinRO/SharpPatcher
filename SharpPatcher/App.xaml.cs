using SharpCompress.Common;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using static SharpPatcher.Consts;

namespace SharpPatcher
{
    /// <summary>
    /// Interação lógica para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // My code goes here, but nothing ever happens.
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "References", $"{assemblyName}.dll");
            if (File.Exists(dllPath))
            {
                return Assembly.LoadFile(dllPath);
            }
            else
            {
                throw new FileNotFoundException($"Could not resolve assembly: {assemblyName}");
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ShowMsgError(e.Exception.ToString());
            Current.Shutdown();
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // we cannot handle this, but not to worry, I have not encountered this exception yet.
            // However, you can show/log the exception message and show a message that if the application is terminating or not.
            var isTerminating = e.IsTerminating;
        }
    }
}