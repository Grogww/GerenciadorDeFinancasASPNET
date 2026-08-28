namespace GerenciadorDeFinancasASPNET.Services {
    /// <summary>
    /// Importação pendente de confirmação: o extrato já foi lido e deduplicado,
    /// mas o usuário ainda não revisou as categorias. Fica serializada em disco
    /// (TempData/imports) até o Confirmar/Descartar, então sobrevive a restarts.
    /// </summary>
    public class ImportSession {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Quantas linhas do arquivo já existiam no banco e foram ignoradas.</summary>
        public int DuplicatesSkipped { get; set; }

        public List<PendingTransaction> Transactions { get; set; } = new();
    }

    public class PendingTransaction {
        public string UniqueHash { get; set; } = "";
        public DateOnly Date { get; set; }
        public string? Lancamento { get; set; }
        public string? Detalhes { get; set; }
        public string? NumeroDocumento { get; set; }
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = "";
        public string MatchKey { get; set; } = "";

        /// <summary>Categoria sugerida pelas regras aprendidas (null = primeira vez que vemos essa MatchKey).</summary>
        public int? SuggestedCategoryId { get; set; }
    }
}
