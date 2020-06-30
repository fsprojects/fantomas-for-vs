using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Fantomas;
using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Task = System.Threading.Tasks.Task;
using FantomasConfig = Fantomas.FormatConfig.FormatConfig;

namespace FantomasVs
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.FSharpContentType)]
    [Name(PredefinedCommandHandlerNames.FormatDocument)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    public partial class FantomasHandler : ICommandHandler<FormatDocumentCommandArgs>
    {
        public string DisplayName => "Automatic Formatting";


        private Lazy<FSharpChecker> _checker = new Lazy<FSharpChecker>(() => 
            FSharpChecker.Create(null, null, null, null, null, null, null, null)
        );
        protected FSharpChecker CheckerInstance => _checker.Value;

        protected FantomasConfig GetOptions(EditorCommandArgs args, FantomasOptionsPage fantopts)
        {
            var localOptions = args.TextView.Options;
            var indentSpaces = localOptions?.GetIndentSize();

            var config = new FantomasConfig(
                indentSpaceNum: indentSpaces ?? fantopts.IndentSpaceNum,
                indentOnTryWith: fantopts.IndentOnTryWith,

                pageWidth: fantopts.PageWidth,
                semicolonAtEndOfLine: fantopts.SemicolonAtEndOfLine,
                spaceBeforeArgument: fantopts.SpaceBeforeArgument,
                spaceBeforeColon: fantopts.SpaceBeforeColon,
                spaceAfterComma: fantopts.SpaceAfterComma,
                spaceAfterSemicolon: fantopts.SpaceAfterSemicolon,
                spaceBeforeSemicolon: fantopts.SpaceBeforeSemicolon,
                spaceAroundDelimiter: fantopts.SpaceAroundDelimiter,
                reorderOpenDeclaration: fantopts.ReorderOpenDeclaration,
                keepNewlineAfter: fantopts.KeepNewlineAfter,
                maxIfThenElseShortWidth: fantopts.MaxIfThenElseShortWidth,
                strictMode: fantopts.StrictMode
            );

            return config;
        }

        protected void FullReplace(ITextBuffer buffer, string oldText, string newText)
        {
            buffer.Replace(new Span(0, oldText.Length), newText);
        }
        protected void DiffPatch(ITextBuffer buffer, ITextSnapshot snap, string oldText, string newText)
        {
            using (var edit = buffer.CreateEdit())
            {

                var diffBuilder = new InlineDiffBuilder();
                var diff = diffBuilder.BuildDiffModel(oldText, newText, false);
                int inserts = 0, deletes = 0, nochanges = 0;

                foreach (var current in diff.Lines)
                {
                    var line = nochanges + deletes;

                    switch (current.Type)
                    {
                        case ChangeType.Deleted:
                            {
                                var ln = snap.GetLineFromLineNumber(line);
                                edit.Delete(ln.ExtentIncludingLineBreak);
                                deletes++;
                                break;
                            }

                        case ChangeType.Inserted:
                            {
                                var ln = snap.GetLineFromLineNumber(Math.Max(0, line - 1));
                                edit.Insert(ln.EndIncludingLineBreak.Position, current.Text + Environment.NewLine);
                                inserts++;
                                break;
                            }

                        default:
                            {
                                nochanges++;
                                break;
                            }
                    }
                }

                edit.Apply();
            }
        }


        public async Task FormatAsync(EditorCommandArgs args, CancellationToken token)
        {
            var buffer = args.TextView.TextBuffer;

            var fantopts = FantomasVsPackage.Instance?.Options ?? new FantomasOptionsPage();
            var defaults = FSharpParsingOptions.Default;
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            var path = document.FilePath;

            var snap = buffer.CurrentSnapshot;
            var oldText = snap.GetText();


            var opts = new FSharpParsingOptions(
                sourceFiles: new string[] { path },
                conditionalCompilationDefines: defaults.ConditionalCompilationDefines,
                errorSeverityOptions: defaults.ErrorSeverityOptions,
                isInteractive: defaults.IsInteractive,
                lightSyntax: defaults.LightSyntax,
                compilingFsLib: defaults.CompilingFsLib,
                isExe: oldText.Contains("[<EntryPoint>]")
            );

            var origin = SourceOrigin.SourceOrigin.NewSourceString(oldText);
            var checker = CheckerInstance;
            var config = GetOptions(args, fantopts);

            var fsasync = CodeFormatter.FormatDocumentAsync(path, origin, config, opts, checker);

            try
            {
                var newText = await FSharpAsync.StartAsTask(fsasync, null, token);

                if (fantopts.ApplyDiff)
                {
                    DiffPatch(buffer, snap, oldText, newText);
                }
                else
                {
                    FullReplace(buffer, oldText, newText);
                }
            }
            catch (Exception ex)
            {

                Trace.TraceError(ex.ToString());
            }

        }

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            LogTask(FormatAsync(args, executionContext.OperationContext.UserCancellationToken));
            return false;
        }

        protected void LogTask(Task task)
        {
            var _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Trace.TraceError(t.Exception.ToString());
            }, TaskScheduler.Default);
        }

        public CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return CommandState.Unspecified;
        }
    }

}
