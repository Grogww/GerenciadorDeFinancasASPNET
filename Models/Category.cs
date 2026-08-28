namespace GerenciadorDeFinancasASPNET.Models {
    public class Category {
        public int Id { get; set; }
        public string Nome { get; set; } = "";

        /// <summary>Cor em hex (ex.: "#22c55e") usada nos badges e, futuramente, nos dashboards.</summary>
        public string Cor { get; set; } = "#64748b";

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<CategoryRule> Rules { get; set; } = new List<CategoryRule>();
    }
}
