using System;
using System.Windows;
using JetBrains.Annotations;

namespace DistSimServerWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        protected override void OnStartup([NotNull] StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException([NotNull] object sender, [NotNull] UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show(e.ExceptionObject.ToString());
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
        }
    }
}
