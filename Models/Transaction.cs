namespace GerenciadorDeFinancasASPNET.Models {
    public class Transaction {
        public int Id { get; set; }
        public string UniqueHash { get; set; } = "";
        public DateOnly Date { get; set; }
        public string? Lancamento { get; set; }
        public string? Detalhes { get; set; }

        // String (e não int): documentos de Pix têm 14+ dígitos e estouram int32.
        public string? NumeroDocumento { get; set; }

        public decimal Valor { get; set; } = 0;
        public string Tipo { get; set; } = "";

        /// <summary>
        /// Descrição normalizada (sem data/hora, CPF/CNPJ e acentos — ex.: "SUBWAY FRAIBURGO").
        /// É por ela que o sistema reconhece o mesmo estabelecimento em extratos
        /// futuros e aplica a categoria aprendida (ver CategoryRule).
        /// </summary>
        public string MatchKey { get; set; } = "";

        public DateTime ImportedAt { get; set; }

        /// <summary>
        /// "Extrato" = veio da importação do CSV; "Manual" = cadastrada pelo usuário
        /// (dinheiro físico, ajustes). Só transações manuais podem ser editadas/excluídas —
        /// as do extrato são o espelho fiel do banco.
        /// TODO futuro: origem "Foto" (leitura de comprovantes por IA).
        /// </summary>
        public string Origem { get; set; } = OrigemExtrato;

        public const string OrigemExtrato = "Extrato";
        public const string OrigemManual = "Manual";

        public bool IsManual => Origem == OrigemManual;

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
