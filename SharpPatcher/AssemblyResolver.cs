using System;
using System.IO;
using System.Reflection;
using System.Windows;
using static SharpPatcher.Consts;

namespace SharpPatcher
{
    public static class AssemblyResolver
    {
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "References", $"{assemblyName}.dll");
            return Assembly.LoadFile(dllPath);
        }

        public static void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowMsgError(e.Exception.ToString());
            Application.Current.Shutdown();
            e.Handled = true;
        }
    }
}