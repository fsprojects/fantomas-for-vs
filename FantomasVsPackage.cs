using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace FantomasVs
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// DO NOT REMOVE THIS MAGICAL INCANTATION NO MATTER HOW MUCH
    /// VS WARNS YOU OF DEPRECATION
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(FantomasVsPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("F# Formatting", "", "FantomasVs", IconResourceID = 400)]
    [ProvideOptionPage(typeof(FantomasOptionsPage), "F# Tools", "Formatting", 0, 0, true)]

    public sealed partial class FantomasVsPackage : AsyncPackage
    {
        /// <summary>
        /// FantomasVsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "74927147-72e8-4b47-a80d-5568807d6878";

        private static TaskCompletionSource<FantomasVsPackage> _instance = new TaskCompletionSource<FantomasVsPackage>();
        public static Task<FantomasVsPackage> Instance => _instance.Task;

        public FantomasOptionsPage Options => GetDialogPage(typeof(FantomasOptionsPage)) as FantomasOptionsPage;

        public IComponentModel MefHost { get; private set; }

        public IVsStatusbar Statusbar { get; private set; }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {            
            Trace.WriteLine("----------------------------------------------");
            Trace.WriteLine("Fantomas VS Loaded");
            Trace.WriteLine("----------------------------------------------");

            MefHost = await this.GetServiceAsync<SComponentModel, IComponentModel>();
            Statusbar = await this.GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            
            // signal that package is ready
            _instance.SetResult(this);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        }

        #endregion
    }
}
