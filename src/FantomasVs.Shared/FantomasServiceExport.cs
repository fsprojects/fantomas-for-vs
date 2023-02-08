using System.ComponentModel.Composition;

using static Fantomas.Client.Contracts;
using static Fantomas.Client.LSPFantomasService;

namespace FantomasVs
{
    internal class FantomasServiceExport
    {
        [Export(typeof(FantomasService))]
        public FantomasService FantomasService => new LSPFantomasService();
    }
}
