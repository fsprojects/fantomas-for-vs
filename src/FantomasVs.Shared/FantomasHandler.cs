using System;
using DiffPlex;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

using Fantomas.Client;
using FantomasResponseCode = Fantomas.Client.LSPFantomasServiceTypes.FantomasResponseCode;
using Microsoft.VisualStudio.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

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
        [ImportingConstructor]
        public FantomasHandler(Contracts.FantomasService fantomasService)
        {
            service = fantomasService;
        }

        private Contracts.FantomasService service;

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
            await VS.StatusBar.ShowMessageAsync("Formatting...");
            await Task.Yield();

            var buffer = args.TextView.TextBuffer;
            var caret = args.TextView.Caret.Position;

            var fantopts = await Formatting.GetLiveInstanceAsync();
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            var path = document.FilePath;
            var workingDir = System.IO.Path.GetDirectoryName(path);
            var hasDiff = false;
            var hasError = false;

            try
            {
                var originText = kind switch
                {
                    FormatKind.Document => buffer.CurrentSnapshot.GetText(),
                    FormatKind.Selection => buffer.CurrentSnapshot.GetText(),
                    FormatKind.IsolatedSelection => vspan.GetText(),
                    _ => throw new NotSupportedException($"Operation {kind} is not supported")
                };

                var response = await (kind switch
                {
                    FormatKind.Document or FormatKind.IsolatedSelection =>
                        service.FormatDocumentAsync(new Contracts.FormatDocumentRequest(originText, path, null), token),
                    FormatKind.Selection =>
                        service.FormatSelectionAsync(new Contracts.FormatSelectionRequest(originText, path, null, MakeRange(vspan, path)), token),
                    _ => throw new NotSupportedException($"Operation {kind} is not supported")
                });

                switch ((FantomasResponseCode)response.Code)
                {
                    case FantomasResponseCode.Formatted:
                        {
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
                        }
                    case FantomasResponseCode.UnChanged:
                    case FantomasResponseCode.Ignored:
                        break;
                    case FantomasResponseCode.ToolNotFound:
                        {
                            await VS.StatusBar.ShowMessageAsync("Fantomas tool not installed");
                            // prevent editor command wait dialog
                            context.OperationContext.TakeOwnership();
                            var view = new InstallChoiceWindow();
                            var result = await InstallAsync(view.GetDialogAction(), workingDir, token);
                            await VS.StatusBar.ClearAsync();
                            switch (result)
                            {
                                case InstallResult.Succeded:
                                    {
                                        InstallResultDialog.ShowDialog("Fantomas Tool was succesfully installed!");
                                        await VS.StatusBar.ShowMessageAsync("Starting Fantomas instance...");
                                        await FormatAsync(vspan, args, context, kind);
                                        break;
                                    }
                                case InstallResult.Failed:
                                    {
                                        InstallResultDialog.ShowDialog("Fantomas Tool could not be installed. You may not have a tool manifest set up. Please check the log for details.");
                                        await FocusLogAsync();
                                        break;
                                    }
                            }
                            break;
                        }

                    case FantomasResponseCode.Error:
                    case FantomasResponseCode.FileNotFound:
                    case FantomasResponseCode.FilePathIsNotAbsolute:
                        {
                            hasError = true;
                            var error = response.Content.Value;
                            await VS.StatusBar.ShowMessageAsync($"Could not format: {error.Replace(path, "")}");
                            await WriteLogAsync(error);
                            await FocusLogAsync();
                            break;
                        }
                    case FantomasResponseCode.DaemonCreationFailed:
                        {
                            await WriteLogAsync($"Creating the Fantomas Daemon failed:\n{response.Content?.Value}");
                            await FocusLogAsync();
                            hasError = true;
                            break;
                        }
                    default:
                        throw new NotSupportedException($"The {nameof(FantomasResponseCode)} value '{response.Code}' is unexpected.\n Error: {response.Content?.Value}");
                }

                if (hasError)
                {
                    await WriteLogAsync("Attempting to find Fantomas Tool...");
                    var folder = LSPFantomasServiceTypes.Folder.NewFolder(workingDir);
                    var toolLocation = FantomasToolLocator.findFantomasTool(folder);
                    var result = toolLocation.IsError ? $"Failed to find tool: {toolLocation.ErrorValue}" : $"Found at: {toolLocation.ResultValue}";
                    await WriteLogAsync(result);
                }
            }
            catch (NotSupportedException ex)
            {
                await WriteLogAsync($"The operation is not supported:\n {ex.Message}");
            }
            catch (Exception ex)
            {
                hasError = true;
                await WriteLogAsync($"The formatting operation failed:\n {ex}");
                await VS.StatusBar.ShowMessageAsync($"Could not format: {ex.Message.Replace(path, "")}");
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

            if (!hasError) { await VS.StatusBar.ClearAsync(); }

            return hasDiff;
        }

        protected async Task<(bool, string)> RunProcessAsync(string name, string args, string workingDir, CancellationToken token)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = name,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            try
            {
                using var process = Process.Start(startInfo);
                var exitCode = await process.WaitForExitAsync(token);

                token.ThrowIfCancellationRequested();

                var output = exitCode switch
                {
                    0 => await process.StandardOutput.ReadToEndAsync().WithCancellation(token),
                    _ => await process.StandardError.ReadToEndAsync().WithCancellation(token)
                };

                return (exitCode == 0, output);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to run dotnet tool {ex}");
            }
        }

        public async Task<InstallResult> InstallAsync(InstallAction installAction, string workingDir, CancellationToken token)
        {
            await VS.StatusBar.ShowMessageAsync("Installing Fantomas tool...");
            async Task<InstallResult> LaunchUrl(string uri)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    await WriteLogAsync($"Failed to launch url: {uri}\n{ex}");
                }

                return InstallResult.Skipped;
            }

            async Task<InstallResult> LaunchDotnet(string caption, string args)
            {
                await WriteLogAsync(caption);
                await WriteLogAsync("Running dotnet installation...");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                var fac = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();
                using var session = fac.StartWaitDialog(caption);
                var (success, output) = await RunProcessAsync("dotnet", args, workingDir, session.UserCancellationToken);
                await WriteLogAsync(output);
                return success ? InstallResult.Succeded : InstallResult.Failed;
            }

            return installAction switch
            {
                InstallAction.Global => await LaunchDotnet("Installing tool globally", "tool install --verbosity normal --global fantomas"),
                InstallAction.Local => await LaunchDotnet("Installing tool locally", "tool install --verbosity normal fantomas"),
                InstallAction.ShowDocs => await LaunchUrl("https://fsprojects.github.io/fantomas/docs/index.html"),
                _ => InstallResult.Skipped, // do nothing
            };
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

            var range = new Contracts.FormatSelectionRange(
                startLine: startLine,
                startColumn: startCol,
                endLine: endLine,
                endColumn: endCol);
            return range;
        }

        public Task<bool> FormatAsync(EditorCommandArgs args, CommandExecutionContext context)
        {
            var snapshot = args.TextView.TextSnapshot;
            var vspan = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
            return FormatAsync(vspan, args, context, FormatKind.Document);
        }

        #endregion

        #region Output Window

        public Task WriteLogAsync(string text) => OutputLogging.LogTextAsync(text);

        public Task FocusLogAsync() => OutputLogging.BringToFrontAsync();

        #endregion

        public void LogTask<T>(Func<Task<T>> asyncMethod)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(asyncMethod);
            }
            catch (Exception ex)
            {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await WriteLogAsync(ex.ToString());
                    await Task.Delay(2000);
                    await VS.StatusBar.ClearAsync();
                });
            }
        }

        #region Format Document

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
        {
            LogTask(() => FormatAsync(args, context));
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
            LogTask(() => FormatAsync(vspan, args, executionContext, FormatKind.Selection));
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
            LogTask(() => FormatOnSaveAsync(args, executionContext));
            return false;
        }

        protected async Task<bool> FormatOnSaveAsync(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            var options = await Formatting.GetLiveInstanceAsync();
            if (!options.FormatOnSave)
                return false;

            var hasDiff = await FormatAsync(args, executionContext);

            if (!hasDiff || !options.CommitChanges)
                return false;

            var buffer = args.SubjectBuffer;
            var document = buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));

            document?.Save();
            return true;
        }

        #endregion
    }
}