using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using OutputPane = Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane;
using System.Diagnostics;
using System.Threading;

namespace FantomasVs
{
    public class OuptutLogging
    {
        private OutputPane _instance;

        private async Task<OutputPane> CreatePaneAsync(Guid paneGuid, string title, bool visible, bool clearWithSolution)
        {
            var instance = await FantomasVsPackage.Instance;
            var output = instance.OutputPane;
            
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Create a new pane.
            output.CreatePane(
                ref paneGuid,
                title,
                Convert.ToInt32(visible),
                Convert.ToInt32(clearWithSolution));

            // Retrieve the new pane.
            if (output.GetPane(ref paneGuid, out var pane) != VSConstants.S_OK)
                throw new InvalidOperationException();

            return pane;
        }

        private Task<OutputPane> CreatePaneAsync()
        {
            return CreatePaneAsync(new Guid(FantomasVsPackage.PackageGuidString), "F# Formatting", true, false);
        }

        public async Task<OutputPane> GetPaneAsync(CancellationToken token)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            if (_instance is OutputPane)
                return _instance;

            _instance = await CreatePaneAsync();
            return _instance;
        }

        public async Task LogTextAsync(string text, CancellationToken token)
        {
            try
            {
                var pane = await GetPaneAsync(token);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                pane.OutputStringThreadSafe(text + Environment.NewLine);
            }
            catch (Exception)
            {
                Trace.WriteLine(text);
            }
        }

        public async Task BringToFrontAsync(CancellationToken token)
        {
            try
            {
                var pane = await GetPaneAsync(token);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                pane.Activate();
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
