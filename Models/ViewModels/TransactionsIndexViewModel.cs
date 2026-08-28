namespace GerenciadorDeFinancasASPNET.Models.ViewModels {
    public class TransactionsIndexViewModel {
        public List<Transaction> Transactions { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Filtros ativos
        public string? Periodo { get; set; }      // "yyyy-MM" ou null = todos
        public int? CategoriaId { get; set; }     // null = todas; -1 = sem categoria
        public string? Tipo { get; set; }         // "Entrada" | "Saída" | null
        public string? Busca { get; set; }

        /// <summary>Meses (yyyy-MM) que possuem transações, para o dropdown de período.</summary>
        public List<string> PeriodosDisponiveis { get; set; } = new();

        // Resumo do conjunto filtrado
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public decimal Saldo => TotalEntradas + TotalSaidas; // saídas já são negativas
        public int SemCategoria { get; set; }
    }
}
