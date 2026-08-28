using GerenciadorDeFinancasASPNET.Services;

namespace GerenciadorDeFinancasASPNET.Models.ViewModels {
    public class ImportPreviewViewModel {
        public ImportSession Session { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public int Sugeridas => Session.Transactions.Count(t => t.SuggestedCategoryId.HasValue);
        public int SemSugestao => Session.Transactions.Count - Sugeridas;
    }
}
