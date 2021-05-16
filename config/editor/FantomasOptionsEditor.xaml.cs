extern alias FantomasLatest;
extern alias FantomasStable;

using FSharp.Compiler.SourceCodeServices;
using System;
using System.Threading;
using System.Windows.Controls;


using StableCodeFormatter = FantomasStable::Fantomas.CodeFormatter;
using LatestCodeFormatter = FantomasLatest::Fantomas.CodeFormatter;

namespace FantomasVs.Editor
{
    /// <summary>
    /// Interaction logic for FantomasOptionsPage2.xaml
    /// </summary>
    public partial class FantomasOptionsEditor : UserControl
    {
        public FantomasOptionsPage FantomasOptions { get; }

        public EditorPage<FantomasOptionsPage> Editor { get; }

        public FSharpChecker FSharpCheckerInstance { get; }

        public string SampleText { get; set; } =
            @"
            let rec distribute e = function
            | [] -> [[e]]
            | x::xs as list ->
                [e::list] @ [for xs in distribute e xs -> x::xs]
            ";

        #region Checker

        private readonly Lazy<FSharpChecker> _checker = new(() =>
            FSharpChecker.Create(null, null, null, null, null, null, null, null, null)
        );

        protected FSharpChecker CheckerInstance => _checker.Value;

        #endregion

        public FantomasOptionsEditor()
        {
            InitializeComponent();
            FantomasOptions = new FantomasOptionsPage();
            Editor = new EditorPage<FantomasOptionsPage>(FantomasOptions);
            Editor.PropertyEdited += OnPropertyEdited;
            PropertyView.ItemsSource = Editor.View;
            DataContext = Editor;
            OnPropertyEdited(nameof(FantomasOptionsPage.CommitChanges));
        }

        CancellationTokenSource tokenSource;

        private void OnPropertyEdited(string name)
        {
            var config = FantomasOptions.ToOptions(null);
            var defaults = FSharpParsingOptions.Default;
            var opts = new FSharpParsingOptions(
              sourceFiles: new string[] { "sample.fsx" },
              conditionalCompilationDefines: defaults.ConditionalCompilationDefines,
              errorSeverityOptions: defaults.ErrorSeverityOptions,
              isInteractive: defaults.IsInteractive,
              lightSyntax: defaults.LightSyntax,
              compilingFsLib: defaults.CompilingFsLib,
              isExe: true // let's have this on for now
            );


            //var fsasync = LatestCodeFormatter.FormatDocumentAsync("sample.fsx", Fantomas.SourceOrigin.SourceOrigin.NewSourceString(SampleText), config, opts, FSharpCheckerInstance);
            var fsasync = StableCodeFormatter.FormatDocumentAsync("sample.fsx", Fantomas.SourceOrigin.SourceOrigin.NewSourceString(SampleText), config, opts, FSharpCheckerInstance);

            tokenSource?.Cancel();
            tokenSource?.Dispose();

            tokenSource = new();

            var token = tokenSource.Token;
            var task = Microsoft.FSharp.Control.FSharpAsync.StartAsTask(fsasync, null, token);
            task.ContinueWith(t => Dispatcher.Invoke(() => SourcePreview.SourceText = t.Result), token);
        }
    }
}
