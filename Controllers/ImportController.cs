using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Models;
using GerenciadorDeFinancasASPNET.Models.ViewModels;
using GerenciadorDeFinancasASPNET.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancasASPNET.Controllers {
    /// <summary>
    /// Fluxo de importação em duas etapas:
    ///  1. Upload  → lê o CSV, remove duplicatas (UniqueHash) e sugere categorias
    ///               pelas regras aprendidas; nada é gravado ainda.
    ///  2. Preview → o usuário revisa/ajusta as categorias e Confirma; só então as
    ///               transações entram no banco e as escolhas viram regras (aprendizado).
    /// </summary>
    public class ImportController : Controller {
        private const long MaxFileSize = 5 * 1024 * 1024; // extratos são pequenos; 5 MB é folga

        private readonly ITransactionImportService _importService;
        private readonly ICategorizationService _categorization;
        private readonly IImportSessionService _sessions;
        private readonly AppDbContext _db;

        public ImportController(
            ITransactionImportService importService,
            ICategorizationService categorization,
            IImportSessionService sessions,
            AppDbContext db) {
            _importService = importService;
            _categorization = categorization;
            _sessions = sessions;
            _db = db;
        }

        // GET /Import
        [HttpGet]
        public IActionResult Index() {
            return View();
        }

        // POST /Import/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> Upload(IFormFile? arquivo) {
            if (arquivo is null || arquivo.Length == 0) {
                TempData["Erro"] = "Selecione um arquivo CSV de extrato para importar.";
                return RedirectToAction(nameof(Index));
            }

            IReadOnlyList<Transaction> parsed;
            try {
                using var stream = arquivo.OpenReadStream();
                parsed = _importService.LoadFromCsv(stream);
            } catch (Exception ex) {
                TempData["Erro"] = $"Não foi possível ler o arquivo. Confira se é o CSV do extrato do banco. Detalhe: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            if (parsed.Count == 0) {
                TempData["Erro"] = "Nenhuma transação encontrada no arquivo (apenas linhas de saldo?).";
                return RedirectToAction(nameof(Index));
            }

            // Deduplicação: dentro do próprio arquivo e contra o que já está no banco.
            var distintas = parsed.GroupBy(t => t.UniqueHash).Select(g => g.First()).ToList();
            var hashes = distintas.Select(t => t.UniqueHash).ToList();
            var jaImportados = await _db.Transactions
                .Where(t => hashes.Contains(t.UniqueHash))
                .Select(t => t.UniqueHash)
                .ToListAsync();
            var novas = distintas.Where(t => !jaImportados.Contains(t.UniqueHash)).ToList();

            if (novas.Count == 0) {
                TempData["Info"] = $"Todas as {parsed.Count} transações do arquivo já haviam sido importadas.";
                return RedirectToAction(nameof(Index));
            }

            var session = new ImportSession {
                FileName = arquivo.FileName,
                DuplicatesSkipped = parsed.Count - novas.Count,
                Transactions = novas
                    .OrderBy(t => t.MatchKey).ThenBy(t => t.Date)
                    .Select(t => new PendingTransaction {
                        UniqueHash = t.UniqueHash,
                        Date = t.Date,
                        Lancamento = t.Lancamento,
                        Detalhes = t.Detalhes,
                        NumeroDocumento = t.NumeroDocumento,
                        Valor = t.Valor,
                        Tipo = t.Tipo,
                        MatchKey = t.MatchKey,
                    })
                    .ToList(),
            };

            await _categorization.ApplySuggestionsAsync(session.Transactions);
            _sessions.Save(session);

            return RedirectToAction(nameof(Preview), new { id = session.Id });
        }

        // GET /Import/Preview/{id}
        [HttpGet]
        public async Task<IActionResult> Preview(string id) {
            var session = _sessions.Load(id);
            if (session is null) {
                TempData["Erro"] = "Importação não encontrada ou já concluída. Envie o extrato novamente.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new ImportPreviewViewModel {
                Session = session,
                Categories = await _db.Categories.OrderBy(c => c.Nome).ToListAsync(),
            };
            return View(vm);
        }

        // POST /Import/Confirm
        // "categorias" chega como categorias[UNIQUE_HASH] = "" | "<categoryId>" | "new:Nome|#cor"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string id, Dictionary<string, string> categorias) {
            var session = _sessions.Load(id);
            if (session is null) {
                TempData["Erro"] = "Importação não encontrada ou já concluída. Envie o extrato novamente.";
                return RedirectToAction(nameof(Index));
            }

            categorias ??= new Dictionary<string, string>();

            // 1) Cria primeiro as categorias novas ("new:Nome|#cor") para já termos os Ids.
            var novasCategorias = await CreateNewCategoriesAsync(categorias.Values);

            // 2) Revalida duplicatas (alguém pode ter importado entre o preview e o confirm).
            var hashes = session.Transactions.Select(t => t.UniqueHash).ToList();
            var jaImportados = (await _db.Transactions
                .Where(t => hashes.Contains(t.UniqueHash))
                .Select(t => t.UniqueHash)
                .ToListAsync()).ToHashSet();

            var categoriasValidas = (await _db.Categories.Select(c => c.Id).ToListAsync()).ToHashSet();
            var agora = DateTime.Now;
            int importadas = 0, categorizadas = 0;

            foreach (var p in session.Transactions) {
                if (jaImportados.Contains(p.UniqueHash)) continue;

                var categoryId = ResolveCategoryId(categorias.GetValueOrDefault(p.UniqueHash), novasCategorias, categoriasValidas);

                _db.Transactions.Add(new Transaction {
                    UniqueHash = p.UniqueHash,
                    Date = p.Date,
                    Lancamento = p.Lancamento,
                    Detalhes = p.Detalhes,
                    NumeroDocumento = p.NumeroDocumento,
                    Valor = p.Valor,
                    Tipo = p.Tipo,
                    MatchKey = p.MatchKey,
                    ImportedAt = agora,
                    CategoryId = categoryId,
                });
                importadas++;

                // Aprendizado: cada escolha vira regra para os próximos extratos.
                if (categoryId.HasValue) {
                    categorizadas++;
                    await _categorization.LearnAsync(p.MatchKey, categoryId.Value);
                }
            }

            await _db.SaveChangesAsync();
            _sessions.Delete(id);

            TempData["Sucesso"] = $"{importadas} transações importadas ({categorizadas} categorizadas)." +
                (session.DuplicatesSkipped > 0 ? $" {session.DuplicatesSkipped} duplicadas foram ignoradas." : "");
            return RedirectToAction("Index", "Transactions");
        }

        // POST /Import/Discard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Discard(string id) {
            _sessions.Delete(id);
            TempData["Info"] = "Importação descartada. Nada foi gravado.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cria as categorias marcadas como "new:Nome|#cor" (uma por nome, mesmo que
        /// várias linhas usem a mesma) e devolve o mapa valor-do-form → Id criado.
        /// </summary>
        private async Task<Dictionary<string, int>> CreateNewCategoriesAsync(IEnumerable<string> valores) {
            var resultado = new Dictionary<string, int>();
            var pedidos = valores
                .Where(v => v is not null && v.StartsWith("new:", StringComparison.Ordinal))
                .Distinct()
                .ToList();
            if (pedidos.Count == 0) return resultado;

            var pendentes = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
            foreach (var valor in pedidos) {
                var partes = valor[4..].Split('|', 2);
                var nome = partes[0].Trim();
                var cor = partes.Length > 1 && partes[1].StartsWith('#') ? partes[1] : "#64748b";
                if (nome.Length == 0) continue;

                // Reaproveita se já existir categoria com o mesmo nome (evita violar o índice único).
                var existente = await _db.Categories.FirstOrDefaultAsync(c => c.Nome.ToLower() == nome.ToLower());
                if (existente is not null) {
                    resultado[valor] = existente.Id;
                    continue;
                }

                if (!pendentes.TryGetValue(nome, out var nova)) {
                    nova = new Category { Nome = nome, Cor = cor };
                    _db.Categories.Add(nova);
                    pendentes[nome] = nova;
                }
                resultado[valor] = 0; // Id definitivo só após o SaveChanges abaixo
            }

            if (pendentes.Count > 0) {
                await _db.SaveChangesAsync();
                foreach (var chave in resultado.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList()) {
                    var nome = chave[4..].Split('|', 2)[0].Trim();
                    resultado[chave] = pendentes[nome].Id;
                }
            }
            return resultado;
        }

        private static int? ResolveCategoryId(string? valor, Dictionary<string, int> novas, HashSet<int> validas) {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            if (novas.TryGetValue(valor, out var novoId)) return novoId;
            if (int.TryParse(valor, out var idExistente) && validas.Contains(idExistente)) return idExistente;
            return null;
        }
    }
}
