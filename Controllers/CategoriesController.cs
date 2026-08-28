using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Models;
using GerenciadorDeFinancasASPNET.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancasASPNET.Controllers {
    public class CategoriesController : Controller {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db) {
            _db = db;
        }

        // GET /Categories
        [HttpGet]
        public async Task<IActionResult> Index() {
            var vm = new CategoriesIndexViewModel {
                Items = await _db.Categories
                    .OrderBy(c => c.Nome)
                    .Select(c => new CategoryListItem {
                        Category = c,
                        TransactionCount = c.Transactions.Count,
                        RuleCount = c.Rules.Count,
                        Total = c.Transactions.Sum(t => (decimal?)t.Valor) ?? 0,
                    })
                    .ToListAsync(),
            };
            return View(vm);
        }

        // POST /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nome, string? cor) {
            nome = (nome ?? "").Trim();
            if (nome.Length == 0) {
                TempData["Erro"] = "Informe o nome da categoria.";
                return RedirectToAction(nameof(Index));
            }
            if (await _db.Categories.AnyAsync(c => c.Nome.ToLower() == nome.ToLower())) {
                TempData["Erro"] = $"Já existe uma categoria chamada \"{nome}\".";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Add(new Category { Nome = nome, Cor = NormalizeCor(cor) });
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = $"Categoria \"{nome}\" criada.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Categories/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string nome, string? cor) {
            var categoria = await _db.Categories.FindAsync(id);
            if (categoria is null) {
                TempData["Erro"] = "Categoria não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            nome = (nome ?? "").Trim();
            if (nome.Length == 0) {
                TempData["Erro"] = "Informe o nome da categoria.";
                return RedirectToAction(nameof(Index));
            }
            if (await _db.Categories.AnyAsync(c => c.Id != id && c.Nome.ToLower() == nome.ToLower())) {
                TempData["Erro"] = $"Já existe uma categoria chamada \"{nome}\".";
                return RedirectToAction(nameof(Index));
            }

            categoria.Nome = nome;
            categoria.Cor = NormalizeCor(cor);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Categoria atualizada.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Categories/Delete
        // Transações da categoria voltam a "sem categoria" (SetNull) e as regras
        // aprendidas são apagadas em cascata — configurado no AppDbContext.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) {
            var categoria = await _db.Categories
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (categoria is null) {
                TempData["Erro"] = "Categoria não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(categoria);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" excluída. " +
                $"{categoria.Transactions.Count} transações ficaram sem categoria.";
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeCor(string? cor) {
            cor = (cor ?? "").Trim();
            return cor.Length == 7 && cor.StartsWith('#') ? cor : "#64748b";
        }
    }
}
