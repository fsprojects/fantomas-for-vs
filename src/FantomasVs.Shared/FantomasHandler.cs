using System;
using DiffPlex;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Threading;

using Fantomas.Client;

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

        #region Patching

        protected bool ReplaceAll(Span span, ITextBuffer buffer, string oldText, string newText)
        {
            if (oldText == newText)
                return false;

            buffer.Replace(span, newText);
            return true;
        }

        private (int, int) ShrinkDiff(string currentText, string replaceWith)
        {
            int startOffset = 0, endOffset = 0;
            var currentLength = currentText.Length;
            var replaceLength = replaceWith.Length;

            var length = Math.Min(currentLength, replaceLength);

            for (int i = 0; i < length; i++)
            {
                if (currentText[i] == replaceWith[i])
                    startOffset++;
                else
                    break;
            }

            for (int i = 1; i < length; i++)
            {
                if ((startOffset + endOffset) >= length)
                    break;

                if (currentText[currentLength - i] == replaceWith[replaceLength - i])
                    endOffset++;
                else
                    break;
            }

            return (startOffset, endOffset);
        }

        protected bool DiffPatch(Span span, ITextBuffer buffer, string oldText, string newText)
        {
            var snapshot = buffer.CurrentSnapshot;

            using var edit = buffer.CreateEdit();
            var diff = Differ.Instance.CreateDiffs(oldText, newText, false, false, AgnosticChunker.Instance);
            var lineOffset = snapshot.GetLineNumberFromPosition(span.Start);

            int StartOf(int line) =>
                snapshot
                .GetLineFromLineNumber(line)
                .Start
                .Position;

            foreach (var current in diff.DiffBlocks)
            {
                var start = lineOffset + current.DeleteStartA;

                if (current.DeleteCountA == current.InsertCountB &&
                   (current.DeleteStartA + current.DeleteCountA) < snapshot.LineCount)
                {
                    var count = current.InsertCountB;
                    var lstart = StartOf(start);
                    var lend = StartOf(start + count);
                    var currentText = snapshot.GetText(lstart, lend - lstart);
                    var replaceWith = count == 1 ?
                            diff.PiecesNew[current.InsertStartB] :
                            string.Join("", diff.PiecesNew, current.InsertStartB, current.InsertCountB);
                    var (startOffset, endOffset) = ShrinkDiff(currentText, replaceWith);
                    var totalOffset = startOffset + endOffset;

                    var minReplaceWith = replaceWith.Substring(startOffset, replaceWith.Length - totalOffset);

                    edit.Replace(lstart + startOffset, Math.Max(0, lend - lstart - totalOffset), minReplaceWith);
                }
                else
                {

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
            }

            edit.Apply();

            return diff.DiffBlocks.Any();
        }

        #endregion


        #region Formatting

        public enum FormatKind
        {
            Document,
            Selection,
            IsolatedSelection
        }

        public bool CommandHandled => true;

        public async Task<bool> FormatAsync(SnapshotSpan vspan, EditorCommandArgs args, CommandExecutionContext context, FormatKind kind)
        {
            var token = context.OperationContext.UserCancellationToken;
            var instance = await FantomasVsPackage.Instance.WithCancellation(token);

            await SetStatusAsync("Formatting...", instance, token);
            await Task.Yield();

            var buffer = args.TextView.TextBuffer;
            var caret = args.TextView.Caret.Position;

            var service = instance.FantomasService;
            var fantopts = instance.Options;
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            var path = document.FilePath;
            var hasDiff = false;
            var hasError = false;

            try
            {
                var originText = kind switch
                {
                    FormatKind.Document => buffer.CurrentSnapshot.GetText(),
                    FormatKind.Selection => buffer.CurrentSnapshot.GetText(),
                    FormatKind.IsolatedSelection => vspan.GetText(),
                    _ => throw new NotSupportedException()
                };

                var response = await (kind switch
                {
                    FormatKind.Document or FormatKind.IsolatedSelection =>
                        service.FormatDocumentAsync(new Contracts.FormatDocumentRequest(originText, path, null), token),
                    FormatKind.Selection =>
                        service.FormatSelectionAsync(new Contracts.FormatSelectionRequest(originText, path, null, MakeRange(vspan, path)), token),
                    _ => throw new NotSupportedException()
                });

                switch ((LSPFantomasServiceTypes.FantomasResponseCode)response.Code)
                {
                    case LSPFantomasServiceTypes.FantomasResponseCode.Formatted:
                        var newText = response.Content.Value;
                        var oldText = vspan.GetText();

                        if (fantopts.ApplyDiff)
                        {
                            hasDiff = DiffPatch(vspan, buffer, oldText, newText);
                        }
                        else
                        {
                            hasDiff = ReplaceAll(vspan, buffer, oldText, newText);
                        }
                        break;
                    case LSPFantomasServiceTypes.FantomasResponseCode.UnChanged:
                    case LSPFantomasServiceTypes.FantomasResponseCode.Ignored:
                        break;
                    case LSPFantomasServiceTypes.FantomasResponseCode.Error:
                    case LSPFantomasServiceTypes.FantomasResponseCode.ToolNotFound:
                    case LSPFantomasServiceTypes.FantomasResponseCode.FileNotFound:
                    case LSPFantomasServiceTypes.FantomasResponseCode.FilePathIsNotAbsolute:
                        hasError = true;
                        var error = response.Content.Value;
                        Trace.TraceError(error);
                        await SetStatusAsync($"Could not format: {error.Replace(path, "")}", instance, token);
                        break;
                    default:
                        throw new NotSupportedException($"The {nameof(LSPFantomasServiceTypes.FantomasResponseCode)} value '{response.Code}' is unexpected.");
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
                .BufferPosition
                .TranslateTo(buffer.CurrentSnapshot, PointTrackingMode.Positive)
            );

            if (kind == FormatKind.Selection || kind == FormatKind.IsolatedSelection)
                args.TextView.Selection.Select(
                    vspan.TranslateTo(args.TextView.TextSnapshot, SpanTrackingMode.EdgeInclusive),
                false);

            if (hasError) await Task.Delay(2000);
            await SetStatusAsync("Ready.", instance, token);

            return hasDiff;
        }

        public static Contracts.FormatSelectionRange MakeRange(SnapshotSpan vspan, string path)
        {
            // Beware that the range argument is inclusive.
            // If the range has a trailing newline, it will appear in the formatted result.

            var start = vspan.Start.GetContainingLine();
            var end = vspan.End.GetContainingLine();
            var startLine = start.LineNumber + 1;
            var startCol = Math.Max(0, vspan.Start.Position - start.Start.Position - 1);
            var endLine = end.LineNumber + 1;
            var endCol = Math.Max(0, vspan.End.Position - end.Start.Position - 1);

            var range = new Contracts.FormatSelectionRange(startLine, startCol, endLine, endCol);
            return range;
        }

        public Task<bool> FormatAsync(EditorCommandArgs args, CommandExecutionContext context)
        {
            var snapshot = args.TextView.TextSnapshot;
            var vspan = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
            return FormatAsync(vspan, args, context, FormatKind.Document);
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
            return CommandHandled;
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

            // This command shouldn't be called
            // if there's no selection, but it's bad practice
            // to surface exceptions to VS
            if (selections.Count == 0)
                return false;

            var vspan = new SnapshotSpan(args.TextView.TextSnapshot, selections.Single().Span);
            LogTask(FormatAsync(vspan, args, executionContext, FormatKind.Selection));
            return CommandHandled;
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

            var hasDiff = await FormatAsync(args, executionContext);

            if (!hasDiff || !instance.Options.CommitChanges)
                return;

            var buffer = args.SubjectBuffer;
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));

            document?.Save();
        }

        #endregion
    }
}

