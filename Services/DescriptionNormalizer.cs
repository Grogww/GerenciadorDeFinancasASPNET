using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GerenciadorDeFinancasASPNET.Services {
    /// <summary>
    /// Extrai de uma transação a "chave" que identifica o estabelecimento/contraparte.
    ///
    /// O campo Detalhes do extrato do BB vem como "02/06 14:11 Nome" ou
    /// "07/06 16:28 Num Nome": data/hora + (às vezes) CPF/CNPJ + nome.
    /// Só o nome interessa para reconhecer a mesma despesa/receita em extratos futuros,
    /// então removemos o prefixo de data/hora, os tokens só-numéricos e os acentos.
    /// </summary>
    public static partial class DescriptionNormalizer {
        [GeneratedRegex(@"^\s*\d{2}/\d{2}\s+\d{2}:\d{2}\s*")]
        private static partial Regex DateTimePrefix();

        public static string BuildMatchKey(string? lancamento, string? detalhes) {
            var texto = DateTimePrefix().Replace((detalhes ?? "").Trim(), "");

            // Descarta tokens sem nenhuma letra (CPF/CNPJ, códigos, "56 876 456" etc.).
            var tokens = texto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(tok => tok.Any(char.IsLetter));
            texto = string.Join(' ', tokens);

            // Lançamentos sem Detalhes (ex.: "Aplicação BB CDB DI") usam o próprio Lançamento.
            if (string.IsNullOrWhiteSpace(texto)) {
                texto = (lancamento ?? "").Trim();
            }

            return RemoveDiacritics(texto).ToUpperInvariant();
        }

        private static string RemoveDiacritics(string texto) {
            var decomposto = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposto.Length);
            foreach (var c in decomposto) {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
