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
        public int IndentSize { get; set; } = Defaults.IndentSize;

        #endregion
        
        #region Boundaries

        [Category("Boundaries")]
        [DisplayName("Maximum Array or List Width")]
        public int MaxArrayOrListWidth { get; set; } = Defaults.MaxArrayOrListWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum Width for Elmish Views")]
        public int MaxElmishWidth { get; set; } = Defaults.MaxElmishWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum Function Binding Width")]
        public int MaxFunctionBindingWidth { get; set; } = Defaults.MaxFunctionBindingWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum If-Then-Else Width")]
        public int MaxIfThenElseShortWidth { get; set; } = Defaults.MaxIfThenElseShortWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum Infix Operator Expression")]
        public int MaxInfixOperatorExpression { get; set; } = Defaults.MaxInfixOperatorExpression;

        [Category("Boundaries")]
        [DisplayName("Maximum Line Length")]
        public int MaxLineLength { get; set; } = Defaults.MaxLineLength;

        [Category("Boundaries")]
        [DisplayName("Maximum Record Width")]
        public int MaxRecordWidth { get; set; } = Defaults.MaxRecordWidth;

        [Category("Boundaries")]
        [DisplayName("Maximum Value Binding Width")]
        public int MaxValueBindingWidth { get; set; } = Defaults.MaxValueBindingWidth;

        [Category("Boundaries")]
        [DisplayName("Multiline Block Brackets On Same Column")]
        public bool MultilineBlockBracketsOnSameColumn { get; set; } = Defaults.MultilineBlockBracketsOnSameColumn;

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
        public bool KeepIfThenInSameLine { get; set; } = Defaults.KeepIfThenInSameLine;


        [Category("Convention")]
        [DisplayName("Newline Between Type Definition And Members")]
        public bool NewlineBetweenTypeDefinitionAndMembers { get; set; } = Defaults.NewlineBetweenTypeDefinitionAndMembers;


        [Category("Convention")]
        [DisplayName("Single-Argument Web Mode")]
        public bool SingleArgumentWebMode { get; set; } = Defaults.SingleArgumentWebMode;

        #endregion

        #region Spacing

        [Category("Spacing")]
        [DisplayName("Before Parameter")]
        public bool SpaceBeforeParameter { get; set; } = Defaults.SpaceBeforeParameter;

        [Category("Spacing")]
        [DisplayName("Before Colon")]
        public bool SpaceBeforeColon { get; set; } = Defaults.SpaceBeforeColon;

        [Category("Spacing")]
        [DisplayName("After Comma")]
        public bool SpaceAfterComma { get; set; } = Defaults.SpaceAfterComma;

        [Category("Spacing")]
        [DisplayName("After Semicolon")]
        public bool SpaceAfterSemicolon { get; set; } = Defaults.SpaceAfterSemicolon;

        [Category("Spacing")]
        [DisplayName("Before Semicolon")]
        public bool SpaceBeforeSemicolon { get; set; } = Defaults.SpaceBeforeSemicolon;

        [Category("Spacing")]
        [DisplayName("Around Delimiter")]
        public bool SpaceAroundDelimiter { get; set; } = Defaults.SpaceAroundDelimiter;


        [Category("Spacing")]
        [DisplayName("Before Class Constructor")]
        public bool SpaceBeforeClassConstructor { get; set; } = Defaults.SpaceBeforeClassConstructor;

        [Category("Spacing")]
        [DisplayName("Before Lowercase Invocation")]
        public bool SpaceBeforeLowercaseInvocation { get; set; } = Defaults.SpaceBeforeLowercaseInvocation;


        [Category("Spacing")]
        [DisplayName("Before Uppercase Invocation")]
        public bool SpaceBeforeUppercaseInvocation { get; set; } = Defaults.SpaceBeforeUppercaseInvocation;

        [Category("Spacing")]
        [DisplayName("Before Member")]
        public bool SpaceBeforeMember { get; set; } = Defaults.SpaceBeforeMember;

        #endregion

        #region Ordering


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
