using System;
using DiffPlex;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Threading;
using Fantomas;
using Task = System.Threading.Tasks.Task;
using FantomasConfig = Fantomas.FormatConfig.FormatConfig;

namespace FantomasVs
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.FSharpContentType)]
    [Name(PredefinedCommandHandlerNames.FormatDocument)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    public partial class FantomasHandler :
        ICommandHandler<FormatDocumentCommandArgs>,
        ICommandHandler<FormatSelectionCommandArgs>,
        ICommandHandler<SaveCommandArgs>
    {
        public string DisplayName => "Automatic Formatting";
        
        #region Checker

        private Lazy<FSharpChecker> _checker = new Lazy<FSharpChecker>(() =>
            FSharpChecker.Create(null, null, null, null, null, null, null, null)
        );

        protected FSharpChecker CheckerInstance => _checker.Value;

        #endregion

        #region Build Options

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

        #endregion

        #region Patching

        protected void FullReplace(Span span, ITextBuffer buffer, string oldText, string newText)
        {
            buffer.Replace(span, newText);
        }

        protected void DiffPatch(Span span, ITextBuffer buffer, string oldText, string newText)
        {
            var snapshot = buffer.CurrentSnapshot;

            using (var edit = buffer.CreateEdit())
            {
                var diff = Differ.Instance.CreateDiffs(oldText, newText, false, false, new DiffPlex.Chunkers.LineEndingsPreservingChunker());
                var lineOffset = snapshot.GetLineNumberFromPosition(span.Start);

                foreach (var current in diff.DiffBlocks)
                {
                    var start = lineOffset + current.DeleteStartA;

                    for (int i = 0; i < current.DeleteCountA; i++)
                    {
                        var ln = snapshot.GetLineFromLineNumber(start + i);
                        edit.Delete(ln.Start, ln.LengthIncludingLineBreak);
                    }

                    for (int i = 0; i < current.InsertCountB; i++)
                    {
                        var ln = snapshot.GetLineFromLineNumber(start);
                        edit.Insert(ln.Start, diff.PiecesNew[current.InsertStartB + i]);
                    }

                }

                edit.Apply();
            }
        } 
        #endregion

        #region Formatting

        public async Task FormatAsync(SnapshotSpan vspan, EditorCommandArgs args, CommandExecutionContext context)
        {
            var token = context.OperationContext.UserCancellationToken;
            var instance = await FantomasVsPackage.Instance.WithCancellation(token);

            await SetStatusAsync("Formatting...", instance, token);
            await Task.Yield();

            var buffer = args.TextView.TextBuffer;
            var caret = args.TextView.Caret.Position;

            var fantopts = instance.Options;
            var defaults = FSharpParsingOptions.Default;
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            var path = document.FilePath;

            var oldText = vspan.GetText();

            var opts = new FSharpParsingOptions(
                sourceFiles: new string[] { path },
                conditionalCompilationDefines: defaults.ConditionalCompilationDefines,
                errorSeverityOptions: defaults.ErrorSeverityOptions,
                isInteractive: defaults.IsInteractive,
                lightSyntax: defaults.LightSyntax,
                compilingFsLib: defaults.CompilingFsLib,
                isExe: true // let's have this on for now
            );

            var origin = SourceOrigin.SourceOrigin.NewSourceString(oldText);
            var checker = CheckerInstance;
            var config = GetOptions(args, fantopts);

            var hasError = false;

            try
            {
                var fsasync = CodeFormatter.FormatDocumentAsync(path, origin, config, opts, checker);
                var newText = await FSharpAsync.StartAsTask(fsasync, null, token);

                if (fantopts.ApplyDiff)
                {
                    DiffPatch(vspan, buffer, oldText, newText);
                }
                else
                {
                    FullReplace(vspan, buffer, oldText, newText);
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                Trace.TraceError(ex.ToString());
                await SetStatusAsync($"Could not format: {ex.Message.Replace(path, "")}", instance, token);
            }

            args.TextView.Caret.MoveTo(
                caret
                .VirtualBufferPosition
                .TranslateTo(buffer.CurrentSnapshot)
            );

            if (args is FormatSelectionCommandArgs)
                args.TextView.Selection.Select(
                    vspan.TranslateTo(args.TextView.TextSnapshot, SpanTrackingMode.EdgeInclusive),
                false);

            if (hasError) await Task.Delay(2000);
            await SetStatusAsync("Ready.", instance, token);
        }

        public Task FormatAsync(EditorCommandArgs args, CommandExecutionContext context)
        {
            var snapshot = args.TextView.TextSnapshot;
            var vspan = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
            return FormatAsync(vspan, args, context);
        }


        protected async Task SetStatusAsync(string text, FantomasVsPackage instance, CancellationToken token)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            var statusBar = instance.Statusbar;
            // Make sure the status bar is not frozen

            if (statusBar.IsFrozen(out var frozen) == VSConstants.S_OK && frozen != 0)
                statusBar.FreezeOutput(0);

            // Set the status bar text and make its display static.
            statusBar.SetText(text);
        }

        #endregion

        #region Logging 
        
        protected void LogTask(Task task)
        {
            var _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Trace.TraceError(t.Exception.ToString());
            }, TaskScheduler.Default);
        } 

        #endregion

        #region Format Document

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            LogTask(FormatAsync(args, executionContext));
            return false;
        }

        public CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return args.TextView.IsClosed ? CommandState.Unavailable : CommandState.Available;
        }

        #endregion

        #region Format Selection

        public CommandState GetCommandState(FormatSelectionCommandArgs args)
        {
            return args.TextView.Selection.IsEmpty ? CommandState.Unavailable : CommandState.Available;
        }

        public bool ExecuteCommand(FormatSelectionCommandArgs args, CommandExecutionContext executionContext)
        {
            var selections = args.TextView.Selection.SelectedSpans;

            // This command shouldn't be executed 
            // if there's no selection, but it's bad practice
            // to surface exceptions to VS
            if (selections.Count == 0)
                return false;

            var vspan = new SnapshotSpan(args.TextView.TextSnapshot, selections.Single().Span);
            LogTask(FormatAsync(vspan, args, executionContext));
            return false;
        }

        #endregion

        #region Format On Save

        public CommandState GetCommandState(SaveCommandArgs args)
        {
            return CommandState.Unavailable;
        }

        public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            LogTask(FormatOnSaveAsync(args, executionContext));
            return false;
        }

        protected async Task FormatOnSaveAsync(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            var instance = await FantomasVsPackage.Instance;
            if (!instance.Options.FormatOnSave)
                return;

            await FormatAsync(args, executionContext);
        } 

        #endregion
    }

}
