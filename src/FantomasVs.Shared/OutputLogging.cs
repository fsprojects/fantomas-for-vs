using System;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using Community.VisualStudio.Toolkit;

namespace FantomasVs
{
    public static class OutputLogging
    {
        static OutputWindowPane pane;

        static OutputLogging()
        {
            pane = ThreadHelper.JoinableTaskFactory.Run(
                async () => await VS.Windows.CreateOutputWindowPaneAsync("F# Formatting", lazyCreate: true));
        }

        public static async Task LogTextAsync(string text)
        {
            try
            {
                await pane.WriteLineAsync(text);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(text);
                Trace.WriteLine("Logging error:");
                Trace.WriteLine(ex);
            }
        }

        public static async Task BringToFrontAsync()
        {
            try
            {
                await pane.ActivateAsync();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
