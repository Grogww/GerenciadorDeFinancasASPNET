using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using GerenciadorDeFinancasASPNET.Models;

namespace GerenciadorDeFinancasASPNET.Services {
    public class TransactionImportService : ITransactionImportService {
        // TODO: Implementar validação com o Saldo do Dia (para segurança)

        public IReadOnlyList<Transaction> LoadFromCsv(string filePath) {
            using var stream = File.OpenRead(filePath);
            return LoadFromCsv(stream);
        }

        public IReadOnlyList<Transaction> LoadFromCsv(Stream stream) {
            var config = new CsvConfiguration(new CultureInfo("pt-BR")) {
                Delimiter = ",",          // extrato do BB: vírgula (valores decimais vêm entre aspas)
                HasHeaderRecord = true,
                MissingFieldFound = null, // não quebra se faltar coluna opcional
                HeaderValidated = null,   // não valida cabeçalho (mapeamos por índice)

                // Pula as linhas de saldo (Saldo Anterior / Saldo do dia / S A L D O): elas têm
                // "Tipo Lançamento" (índice 5) vazio e não são transações reais.
                // Filtrar aqui evita tentar converter a data "00/00/0000" e o doc vazio.
                ShouldSkipRecord = args => {
                    var fields = args.Row.Parser.Record;
                    return fields is null || fields.Length < 6 || string.IsNullOrWhiteSpace(fields[5]);
                },
            };

            // Latin1 (Windows-1252) preserva acentos do extrato do BB.
            // Se o seu arquivo for UTF-8, troque para Encoding.UTF8.
            using var reader = new StreamReader(stream, Encoding.Latin1);
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<TransactionMap>();

            var transactions = csv.GetRecords<Transaction>().ToList();

            foreach (var t in transactions) {
                t.UniqueHash = GenerateUniqueHash(t);
                t.MatchKey = DescriptionNormalizer.BuildMatchKey(t.Lancamento, t.Detalhes);
            }

            return transactions;
        }

        /// <summary>
        /// Gera a "impressão digital" da transação para deduplicação na importação.
        /// Monta um texto canônico com os campos que identificam o lançamento (formatos
        /// fixos via InvariantCulture, separados por '|') e calcula o SHA256 em hex.
        /// O mesmo lançamento sempre gera o mesmo hash, então o índice único de
        /// UniqueHash recusa importar a mesma transação duas vezes.
        ///
        /// OBS.: a unicidade depende do NumeroDocumento. Dois lançamentos genuinamente
        /// idênticos no mesmo dia SEM número de documento gerariam o mesmo hash — caso
        /// raro, a ser tratado depois junto com a validação do saldo do dia.
        /// </summary>
        private static string GenerateUniqueHash(Transaction t) {
            var canonical = string.Join("|",
                t.Date.ToString("yyyy-MM-dd"),                          // data sem ambiguidade
                t.Valor.ToString("F2", CultureInfo.InvariantCulture),   // sempre 2 casas, ponto decimal
                (t.NumeroDocumento ?? "").Trim(),                       // identificador principal
                t.Tipo.Trim(),
                (t.Lancamento ?? "").Trim(),
                (t.Detalhes ?? "").Trim()
            );

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
