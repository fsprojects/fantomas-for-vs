using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Fantomas;

namespace FantomasVs
{
    [Guid(GuidString)]
    public class FantomasOptionsPage : DialogPage
    {
        public const string GuidString = "74927147-72e8-4b47-a70d-5568807d6878";

        public static FormatConfig.FormatConfig Defaults => FormatConfig.FormatConfig.Default;
        
        #region Indent

        [Category("Indentation")]
        [DisplayName("Indent Try-With")]
        public bool IndentOnTryWith { get; set; } = Defaults.IndentOnTryWith;

        [Category("Indentation")]
        [DisplayName("Indent Number of Spaces")]
        [Description("This will normally be set by the editor, this is the value to fallback to.")]
        public int IndentSpaceNum { get; set; } = Defaults.IndentSpaceNum;

        #endregion
        
        #region Boundaries

        [Category("Boundaries")]
        [DisplayName("Page Width")]
        public int PageWidth { get; set; } = Defaults.PageWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum If-Then-Else Width")]
        public int MaxIfThenElseShortWidth { get; set; } = Defaults.MaxIfThenElseShortWidth;

        #endregion

        #region Convention

        [Category("Convention")]
        [DisplayName("Semicolon at  End of Line")]
        public bool SemicolonAtEndOfLine { get; set; } = Defaults.SemicolonAtEndOfLine;

        [Category("Convention")]
        [DisplayName("Strict Mode")]
        public bool StrictMode { get; set; } = Defaults.StrictMode;

        [Category("Convention")]
        [DisplayName("Keep Newline After")]
        public bool KeepNewlineAfter { get; set; } = Defaults.KeepNewlineAfter;
        #endregion

        #region Spacing

        [Category("Spacing")]
        [DisplayName("Space Before Argument")]
        public bool SpaceBeforeArgument { get; set; } = Defaults.SpaceBeforeArgument;

        [Category("Spacing")]
        [DisplayName("Space Before Colon")]
        public bool SpaceBeforeColon { get; set; } = Defaults.SpaceBeforeColon;

        [Category("Spacing")]
        [DisplayName("Space After Comma")]
        public bool SpaceAfterComma { get; set; } = Defaults.SpaceAfterComma;

        [Category("Spacing")]
        [DisplayName("Space After Semicolon")]
        public bool SpaceAfterSemicolon { get; set; } = Defaults.SpaceAfterSemicolon;


        [Category("Spacing")]
        [DisplayName("Space Before Semicolon")]
        public bool SpaceBeforeSemicolon { get; set; } = Defaults.SpaceBeforeSemicolon;

        [Category("Spacing")]
        [DisplayName("Space Around Delimiter")]
        public bool SpaceAroundDelimiter { get; set; } = Defaults.SpaceAroundDelimiter;

        #endregion

        #region Ordering

        [Category("Ordering")]
        [DisplayName("Reorder Open Declaration")]
        public bool ReorderOpenDeclaration { get; set; } = Defaults.ReorderOpenDeclaration;

        #endregion
        
        #region Performance

        [Category("Performance")]
        [DisplayName("Apply As Diff")]
        [Description("Applies the formatting as changes, which shows which lines were changed. Turn off if computing the diff is too slow. ")]
        public bool ApplyDiff { get; set; } = true;

        [Category("Performance")]
        [DisplayName("Warmup On Start")]
        [Description("Runs through formatting code on startup to warm up the Jit. Reduces delay when first using it.")]
        public bool WarmUpOnStartup { get; set; } = false;

        #endregion

        #region On Save

        [Category("On Save")]
        [DisplayName("Format On Save")]
        public bool FormatOnSave { get; set; } = false;

        #endregion
    }
}
