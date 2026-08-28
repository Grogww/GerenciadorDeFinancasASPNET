namespace GerenciadorDeFinancasASPNET.Models {
    /// <summary>
    /// Regra aprendida de categorização: quando o usuário categoriza uma transação,
    /// gravamos "MatchKey → Category". Nas próximas importações, transações com a
    /// mesma MatchKey recebem a categoria automaticamente (como sugestão no preview).
    /// </summary>
    public class CategoryRule {
        public int Id { get; set; }
        public string MatchKey { get; set; } = "";
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
