using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;

namespace FantomasVs
{
    //[Guid(GuidString)]
    //[ComVisible(true)]
    internal partial class OptionsProvider
    {
        public const string GuidString = "74927147-72e8-4b47-a70d-5568807d6878";

        // Register the options with these attributes on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "MyExtension", "General", 0, 0, true)]
        // [ProvideProfile(typeof(OptionsProvider.GeneralOptions), "MyExtension", "General", 0, 0, true)]
        [ComVisible(true)]
        public class FormattingOptions : BaseOptionPage<Formatting> { }
    }

    public class Formatting : BaseOptionModel<Formatting>
    {

        #region Performance

        [Category("Performance")]
        [DisplayName("Apply As Diff")]
        [Description("Applies the formatting as changes, which shows which lines were changed. Turn off if computing the diff is too slow. ")]
        [DefaultValue(true)]
        public bool ApplyDiff { get; set; } = true;

        [Category("Performance")]
        [DisplayName("Enable SpaceBar Heating")]
        [Description("xkcd/1172")]
        [DefaultValue(false)]
        public bool EnableSpaceBarHeating { get; set; } = false;

        #endregion

        #region On Save

        [Category("On Save")]
        [DisplayName("Format On Save")]
        [Description("This triggers a formatting whenever you hit save")]
        [DefaultValue(false)]
        public bool FormatOnSave { get; set; } = false;

        [Category("On Save")]
        [DisplayName("Commit Changes")]
        [Description("Set this to false if you don't want to commit formatting changes to the file unless you hit save once again")]
        [DefaultValue(true)]
        public bool CommitChanges { get; set; } = true;

        #endregion
    }
}
