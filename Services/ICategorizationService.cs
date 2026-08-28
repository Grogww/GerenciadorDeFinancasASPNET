namespace GerenciadorDeFinancasASPNET.Services {
    public interface ICategorizationService {
        /// <summary>
        /// Preenche SuggestedCategoryId das transações pendentes usando as regras
        /// aprendidas (CategoryRule) — é isso que torna a categorização automática
        /// a partir do segundo extrato.
        /// </summary>
        Task ApplySuggestionsAsync(IEnumerable<PendingTransaction> transactions);

        /// <summary>
        /// Grava (ou atualiza) a regra "MatchKey → categoria". NÃO chama SaveChanges:
        /// o chamador decide quando persistir, para agrupar com as demais alterações.
        /// </summary>
        Task LearnAsync(string matchKey, int categoryId);
    }
}
