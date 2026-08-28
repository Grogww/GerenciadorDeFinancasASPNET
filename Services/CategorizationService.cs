using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancasASPNET.Services {
    public class CategorizationService : ICategorizationService {
        private readonly AppDbContext _db;

        public CategorizationService(AppDbContext db) {
            _db = db;
        }

        public async Task ApplySuggestionsAsync(IEnumerable<PendingTransaction> transactions) {
            var list = transactions.ToList();
            var keys = list.Select(t => t.MatchKey)
                           .Where(k => !string.IsNullOrEmpty(k))
                           .Distinct()
                           .ToList();
            if (keys.Count == 0) return;

            var rules = await _db.CategoryRules
                .Where(r => keys.Contains(r.MatchKey))
                .ToDictionaryAsync(r => r.MatchKey, r => r.CategoryId);

            foreach (var t in list) {
                if (rules.TryGetValue(t.MatchKey, out var categoryId)) {
                    t.SuggestedCategoryId = categoryId;
                }
            }
        }

        public async Task LearnAsync(string matchKey, int categoryId) {
            if (string.IsNullOrWhiteSpace(matchKey)) return;

            // Inclui regras recém-adicionadas (Local) para não duplicar quando o mesmo
            // estabelecimento aparece mais de uma vez na mesma confirmação de importação.
            var rule = _db.CategoryRules.Local.FirstOrDefault(r => r.MatchKey == matchKey)
                ?? await _db.CategoryRules.FirstOrDefaultAsync(r => r.MatchKey == matchKey);

            if (rule is null) {
                _db.CategoryRules.Add(new CategoryRule {
                    MatchKey = matchKey,
                    CategoryId = categoryId,
                    UpdatedAt = DateTime.Now,
                });
            } else if (rule.CategoryId != categoryId) {
                rule.CategoryId = categoryId;
                rule.UpdatedAt = DateTime.Now;
            }
        }
    }
}
