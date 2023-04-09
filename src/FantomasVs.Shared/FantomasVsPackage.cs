using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace FantomasVs
{

    // DO NOT REMOVE THIS MAGICAL INCANTATION NO MATTER HOW MUCH VS WARNS YOU OF DEPRECATION    
    // --------------------------------------------------------------------------------------
    [InstalledProductRegistration("F# Formatting", "F# source code formatting using Fantomas.", "1.0", IconResourceID = 400)]
    // --------------------------------------------------------------------------------------

    // Package registration attributes
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(FantomasVsPackage.PackageGuidString)]

    // Options page
    [ProvideProfile(typeof(OptionsProvider.FormattingOptions), "F# Tools", "Formatting", 0, 0, true)]
    [ProvideOptionPage(typeof(OptionsProvider.FormattingOptions), "F# Tools", "Formatting", 0, 0, supportsAutomation: true)]

    public sealed partial class FantomasVsPackage : AsyncPackage
    {
        /// <summary>
        /// FantomasVsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "74927147-72e8-4b47-a80d-5568807d6878";
    }
}
