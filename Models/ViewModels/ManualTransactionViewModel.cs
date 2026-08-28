namespace GerenciadorDeFinancasASPNET.Models.ViewModels {
    /// <summary>
    /// Formulário de transação manual (dinheiro físico, ajustes de saldo).
    /// O valor vem como texto porque o input numérico envia ponto decimal e a
    /// cultura pt-BR espera vírgula — o controller aceita os dois formatos.
    /// </summary>
    public class ManualTransactionViewModel {
        public int? Id { get; set; }  // null = criação; preenchido = edição

        public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string Lancamento { get; set; } = "";
        public string? Detalhes { get; set; }
        public string ValorTexto { get; set; } = "";
        public string Tipo { get; set; } = "Saída";  // "Entrada" | "Saída"
        public int? CategoryId { get; set; }

        public List<Category> Categories { get; set; } = new();
    }
}
