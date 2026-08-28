using GerenciadorDeFinancasASPNET.Models;

namespace GerenciadorDeFinancasASPNET.Services {
    public interface ITransactionImportService {
        /// <summary>
        /// Lê o arquivo CSV de extrato e mapeia cada linha para uma Transaction.
        /// Ainda NÃO persiste no banco — apenas devolve a lista em memória.
        /// </summary>
        IReadOnlyList<Transaction> LoadFromCsv(string filePath);

        /// <summary>Mesma leitura, mas a partir de um stream (upload da interface).</summary>
        IReadOnlyList<Transaction> LoadFromCsv(Stream stream);
    }
}
