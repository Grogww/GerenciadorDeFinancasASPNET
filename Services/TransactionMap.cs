using CsvHelper.Configuration;
using GerenciadorDeFinancasASPNET.Models;

namespace GerenciadorDeFinancasASPNET.Services {
    /// <summary>
    /// Mapeia as colunas do CSV do extrato (BB) para a entidade Transaction.
    /// Mapeamento por ÍNDICE (posição fixa), mais robusto que por nome por causa
    /// dos acentos/símbolos nos cabeçalhos (Lançamento, N°, Tipo Lançamento).
    /// Ordem do extrato: Data, Lançamento, Detalhes, N° documento, Valor, Tipo Lançamento.
    /// </summary>
    public sealed class TransactionMap : ClassMap<Transaction> {
        public TransactionMap() {
            Map(m => m.Date).Index(0).TypeConverterOption.Format("dd/MM/yyyy"); // Data
            Map(m => m.Lancamento).Index(1);                                    // Lançamento
            Map(m => m.Detalhes).Index(2);                                      // Detalhes
            Map(m => m.NumeroDocumento).Index(3);                               // N° documento
            Map(m => m.Valor).Index(4);                                         // Valor
            Map(m => m.Tipo).Index(5);                                          // Tipo Lançamento

            // Campos que NÃO vêm do CSV (gerados ou definidos depois):
            Map(m => m.Id).Ignore();
            Map(m => m.UniqueHash).Ignore();
            Map(m => m.MatchKey).Ignore();
            Map(m => m.ImportedAt).Ignore();
            Map(m => m.Origem).Ignore();
            Map(m => m.CategoryId).Ignore();
            Map(m => m.Category).Ignore();
        }
    }
}
