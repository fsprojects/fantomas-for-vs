using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace FantomasVs
{
    [Guid(GuidString)]
    public class FantomasOptionsPage : DialogPage
    {
        public const string GuidString = "74927147-72e8-4b47-a70d-5568807d6878";

        #region Performance

        [Category("Performance")]
        [DisplayName("Apply As Diff")]
        [Description("Applies the formatting as changes, which shows which lines were changed. Turn off if computing the diff is too slow. ")]
        public bool ApplyDiff { get; set; } = true;

        [Category("Performance")]
        [DisplayName("Enable SpaceBar Heating")]
        [Description("xkcd/1172")]
        public bool EnableSpaceBarHeating { get; set; } = false;

        #endregion

        #region On Save

        [Category("On Save")]
        [DisplayName("Format On Save")]
        [Description("This triggers a formatting whenever you hit save")]
        public bool FormatOnSave { get; set; } = false;

        [Category("On Save")]
        [DisplayName("Commit Changes")]
        [Description("Set this to false if you don't want to commit formatting changes to the file unless you hit save once again")]
        public bool CommitChanges { get; set; } = true;

        #endregion
    }
}
