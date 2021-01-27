namespace FantomasVs.Editor
{
    using Fantomas;
    using FSharp.Compiler.SourceCodeServices;
    using Microsoft.FSharp.Collections;
    using System.Reflection.Metadata.Ecma335;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Data;

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

        public FantomasOptionsEditor()
        {
            InitializeComponent();
            FantomasOptions = new FantomasOptionsPage();
            Editor = new EditorPage<FantomasOptionsPage>(FantomasOptions);
            Editor.PropertyEdited += OnPropertyEdited;
            PropertyView.ItemsSource = Editor.View;
            DataContext = Editor;
            FSharpCheckerInstance = FSharpChecker.Instance;
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

            var fsasync = Fantomas.CodeFormatter.FormatDocumentAsync("sample.fsx", Fantomas.SourceOrigin.SourceOrigin.NewSourceString(SampleText), config, opts, FSharpCheckerInstance);

            tokenSource?.Cancel();
            tokenSource?.Dispose();            

            tokenSource = new();

            var token = tokenSource.Token;
            var task = Microsoft.FSharp.Control.FSharpAsync.StartAsTask(fsasync, null, token);
            task.ContinueWith(t => Dispatcher.Invoke(() => SourcePreview.SourceText = t.Result), token);
        }
    }
}
