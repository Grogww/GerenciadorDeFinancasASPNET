using System.Globalization;
using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Models;
using GerenciadorDeFinancasASPNET.Models.ViewModels;
using GerenciadorDeFinancasASPNET.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancasASPNET.Controllers {
    public class TransactionsController : Controller {
        private readonly AppDbContext _db;
        private readonly ICategorizationService _categorization;

        public TransactionsController(AppDbContext db, ICategorizationService categorization) {
            _db = db;
            _categorization = categorization;
        }

        // GET /Transactions?periodo=2026-06&categoriaId=-1&tipo=Saída&busca=subway
        [HttpGet]
        public async Task<IActionResult> Index(string? periodo, int? categoriaId, string? tipo, string? busca) {
            var query = _db.Transactions.Include(t => t.Category).AsQueryable();

            if (!string.IsNullOrEmpty(periodo) && TryParsePeriodo(periodo, out var inicio)) {
                var fim = inicio.AddMonths(1);
                query = query.Where(t => t.Date >= inicio && t.Date < fim);
            } else {
                periodo = null;
            }

            if (categoriaId == -1) {
                query = query.Where(t => t.CategoryId == null);
            } else if (categoriaId > 0) {
                query = query.Where(t => t.CategoryId == categoriaId);
            }

            if (!string.IsNullOrEmpty(tipo)) {
                query = query.Where(t => t.Tipo == tipo);
            }

            if (!string.IsNullOrWhiteSpace(busca)) {
                var termo = busca.Trim();
                query = query.Where(t =>
                    (t.Detalhes != null && EF.Functions.Like(t.Detalhes, $"%{termo}%")) ||
                    (t.Lancamento != null && EF.Functions.Like(t.Lancamento, $"%{termo}%")));
            }

            var transactions = await query
                .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                .ToListAsync();

            // Volume de finanças pessoais é pequeno: agregar em memória é suficiente.
            var vm = new TransactionsIndexViewModel {
                Transactions = transactions,
                Categories = await _db.Categories.OrderBy(c => c.Nome).ToListAsync(),
                Periodo = periodo,
                CategoriaId = categoriaId,
                Tipo = tipo,
                Busca = busca,
                TotalEntradas = transactions.Where(t => t.Valor > 0).Sum(t => t.Valor),
                TotalSaidas = transactions.Where(t => t.Valor < 0).Sum(t => t.Valor),
                SemCategoria = transactions.Count(t => t.CategoryId == null),
                PeriodosDisponiveis = (await _db.Transactions.Select(t => t.Date).ToListAsync())
                    .Select(d => $"{d.Year:D4}-{d.Month:D2}")
                    .Distinct()
                    .OrderByDescending(p => p)
                    .ToList(),
            };
            return View(vm);
        }

        // ---------- Transações manuais (dinheiro físico, ajustes) ----------

        // GET /Transactions/Create
        [HttpGet]
        public async Task<IActionResult> Create() {
            return View("Manual", new ManualTransactionViewModel {
                Categories = await _db.Categories.OrderBy(c => c.Nome).ToListAsync(),
            });
        }

        // POST /Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ManualTransactionViewModel vm) {
            if (!await ValidateManualAsync(vm)) return View("Manual", vm);

            var valor = ParseValor(vm.ValorTexto)!.Value;
            var transaction = new Transaction {
                Date = vm.Data,
                Lancamento = vm.Lancamento.Trim(),
                Detalhes = string.IsNullOrWhiteSpace(vm.Detalhes) ? null : vm.Detalhes.Trim(),
                Valor = vm.Tipo == "Entrada" ? valor : -valor,
                Tipo = vm.Tipo,
                Origem = Transaction.OrigemManual,
                // Guid no lugar do hash de conteúdo: lançamentos manuais idênticos
                // (dois cafés de R$ 5 no mesmo dia) são legítimos e não podem colidir.
                UniqueHash = $"manual-{Guid.NewGuid():N}",
                MatchKey = DescriptionNormalizer.BuildMatchKey(vm.Lancamento, vm.Detalhes),
                ImportedAt = DateTime.Now,
                CategoryId = vm.CategoryId,
            };

            if (vm.CategoryId.HasValue) {
                await _categorization.LearnAsync(transaction.MatchKey, vm.CategoryId.Value);
            }

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Transação manual registrada.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Transactions/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id) {
            var transaction = await _db.Transactions.FindAsync(id);
            if (transaction is null || !transaction.IsManual) {
                TempData["Erro"] = "Só transações manuais podem ser editadas — as do extrato são o espelho fiel do banco.";
                return RedirectToAction(nameof(Index));
            }

            return View("Manual", new ManualTransactionViewModel {
                Id = transaction.Id,
                Data = transaction.Date,
                Lancamento = transaction.Lancamento ?? "",
                Detalhes = transaction.Detalhes,
                ValorTexto = Math.Abs(transaction.Valor).ToString("F2"),
                Tipo = transaction.Tipo,
                CategoryId = transaction.CategoryId,
                Categories = await _db.Categories.OrderBy(c => c.Nome).ToListAsync(),
            });
        }

        // POST /Transactions/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManualTransactionViewModel vm) {
            var transaction = vm.Id.HasValue ? await _db.Transactions.FindAsync(vm.Id.Value) : null;
            if (transaction is null || !transaction.IsManual) {
                TempData["Erro"] = "Transação manual não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (!await ValidateManualAsync(vm)) return View("Manual", vm);

            var valor = ParseValor(vm.ValorTexto)!.Value;
            transaction.Date = vm.Data;
            transaction.Lancamento = vm.Lancamento.Trim();
            transaction.Detalhes = string.IsNullOrWhiteSpace(vm.Detalhes) ? null : vm.Detalhes.Trim();
            transaction.Valor = vm.Tipo == "Entrada" ? valor : -valor;
            transaction.Tipo = vm.Tipo;
            transaction.MatchKey = DescriptionNormalizer.BuildMatchKey(vm.Lancamento, vm.Detalhes);
            transaction.CategoryId = vm.CategoryId;

            if (vm.CategoryId.HasValue) {
                await _categorization.LearnAsync(transaction.MatchKey, vm.CategoryId.Value);
            }

            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Transação manual atualizada.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Transactions/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var transaction = await _db.Transactions.FindAsync(id);
            if (transaction is null || !transaction.IsManual) {
                TempData["Erro"] = "Só transações manuais podem ser excluídas — as do extrato são o espelho fiel do banco.";
                return RedirectToAction(nameof(Index));
            }

            _db.Transactions.Remove(transaction);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Transação manual excluída.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Valida o formulário manual. Em caso de erro, repõe a lista de categorias
        /// (perdida no POST) e o aviso no ViewData para a view reexibir o formulário.
        /// </summary>
        private async Task<bool> ValidateManualAsync(ManualTransactionViewModel vm) {
            string? erro = null;
            if (string.IsNullOrWhiteSpace(vm.Lancamento)) {
                erro = "Informe a descrição do lançamento.";
            } else if (vm.Tipo != "Entrada" && vm.Tipo != "Saída") {
                erro = "Tipo inválido.";
            } else if (ParseValor(vm.ValorTexto) is not > 0) {
                erro = "Informe um valor maior que zero (ex.: 25,90).";
            } else if (vm.CategoryId.HasValue &&
                       !await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId.Value)) {
                erro = "Categoria inexistente.";
            }

            if (erro is null) return true;

            ViewData["ErroForm"] = erro;
            vm.Categories = await _db.Categories.OrderBy(c => c.Nome).ToListAsync();
            return false;
        }

        /// <summary>
        /// Aceita "25,90" / "1.234,56" (pt-BR) e "25.90" (teclado numérico / cópia de app).
        /// A cultura é escolhida pelo separador presente: tentar pt-BR primeiro leria
        /// "25.90" como 2590 (ponto = milhar), invertendo o valor em 100x.
        /// </summary>
        private static decimal? ParseValor(string? texto) {
            texto = (texto ?? "").Trim();
            if (texto.Length == 0) return null;

            var cultura = texto.Contains(',') ? new CultureInfo("pt-BR") : CultureInfo.InvariantCulture;
            return decimal.TryParse(texto, NumberStyles.Number, cultura, out var valor) ? valor : null;
        }

        public record SetCategoryRequest(int Id, int? CategoryId, bool AplicarSemelhantes = true);

        /// <summary>
        /// Troca a categoria de uma transação (edição inline na listagem).
        /// Além de atualizar a linha, aprende a regra para os próximos extratos e,
        /// opcionalmente, aplica a mesma categoria às transações semelhantes que
        /// ainda estão sem categoria. Remover a categoria também apaga a regra.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCategory([FromBody] SetCategoryRequest? request) {
            if (request is null) return BadRequest(new { ok = false, erro = "Requisição inválida." });

            var transaction = await _db.Transactions.FindAsync(request.Id);
            if (transaction is null) return NotFound(new { ok = false, erro = "Transação não encontrada." });

            if (request.CategoryId.HasValue &&
                !await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value)) {
                return BadRequest(new { ok = false, erro = "Categoria inexistente." });
            }

            transaction.CategoryId = request.CategoryId;
            int semelhantes = 0;

            if (request.CategoryId.HasValue) {
                await _categorization.LearnAsync(transaction.MatchKey, request.CategoryId.Value);

                if (request.AplicarSemelhantes && !string.IsNullOrEmpty(transaction.MatchKey)) {
                    // Só as sem categoria: não sobrescreve escolhas manuais já feitas.
                    semelhantes = await _db.Transactions
                        .Where(t => t.Id != transaction.Id
                                    && t.MatchKey == transaction.MatchKey
                                    && t.CategoryId == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.CategoryId, request.CategoryId));
                }
            } else {
                var rule = await _db.CategoryRules.FirstOrDefaultAsync(r => r.MatchKey == transaction.MatchKey);
                if (rule is not null) _db.CategoryRules.Remove(rule);
            }

            await _db.SaveChangesAsync();

            var categoria = request.CategoryId.HasValue
                ? await _db.Categories.Where(c => c.Id == request.CategoryId.Value)
                    .Select(c => new { c.Id, c.Nome, c.Cor })
                    .FirstOrDefaultAsync()
                : null;

            return Json(new { ok = true, semelhantes, categoria });
        }

        private static bool TryParsePeriodo(string periodo, out DateOnly inicio) {
            inicio = default;
            var partes = periodo.Split('-');
            if (partes.Length == 2
                && int.TryParse(partes[0], out var ano)
                && int.TryParse(partes[1], out var mes)
                && mes is >= 1 and <= 12 && ano is >= 1 and <= 9999) {
                inicio = new DateOnly(ano, mes, 1);
                return true;
            }
            return false;
        }
    }
}
