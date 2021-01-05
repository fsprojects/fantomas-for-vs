namespace FantomasVs
{
    using Microsoft.VisualStudio.Shell;
    using System.Windows;

    public class FantomasOptionsDialogPage : UIElementDialogPage
    {
        private FantomasOptionsPage2 _page;
        protected override UIElement Child => _page ??= new FantomasOptionsPage2();
    }
}
