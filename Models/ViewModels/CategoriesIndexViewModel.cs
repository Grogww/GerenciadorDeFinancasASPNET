namespace GerenciadorDeFinancasASPNET.Models.ViewModels {
    public class CategoryListItem {
        public Category Category { get; set; } = new();
        public int TransactionCount { get; set; }
        public int RuleCount { get; set; }
        public decimal Total { get; set; }
    }

    public class CategoriesIndexViewModel {
        public List<CategoryListItem> Items { get; set; } = new();
    }
}
