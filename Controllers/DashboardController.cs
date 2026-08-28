using GerenciadorDeFinancasASPNET.data;
using GerenciadorDeFinancasASPNET.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancasASPNET.Controllers {
    public class DashboardController : Controller {
        private readonly AppDbContext _db;

        // Domingo primeiro, convenção dos calendários brasileiros (índice = DayOfWeek).
        private static readonly string[] DiasSemanaRotulos = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];

        private static readonly string[] MesesAbreviados =
            ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

        private static readonly string[] MesesExtensos =
            ["janeiro", "fevereiro", "março", "abril", "maio", "junho",
             "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

        public DashboardController(AppDbContext db) {
            _db = db;
        }

        // GET /Dashboard?periodo=12m
        [HttpGet]
        public async Task<IActionResult> Index(string periodo = "12m") {
            var mesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

            (DateOnly? inicio, string rotulo) = periodo switch {
                "6m" => (mesAtual.AddMonths(-5), "últimos 6 meses"),
                "ano" => (new DateOnly(mesAtual.Year, 1, 1), $"ano de {mesAtual.Year}"),
                "tudo" => ((DateOnly?)null, "todo o histórico"),
                _ => (mesAtual.AddMonths(-11), "últimos 12 meses"),
            };
            if (periodo is not ("6m" or "12m" or "ano" or "tudo")) periodo = "12m";

            var query = _db.Transactions.AsQueryable();
            if (inicio.HasValue) {
                query = query.Where(t => t.Date >= inicio.Value);
            }

            // Volume de finanças pessoais é pequeno: agregar em memória é suficiente.
            var transactions = await query
                .Select(t => new { t.Date, t.Valor, t.MatchKey, t.CategoryId, CategoriaNome = t.Category!.Nome, CategoriaCor = t.Category.Cor })
                .ToListAsync();

            var vm = new DashboardViewModel {
                Periodo = periodo,
                PeriodoRotulo = rotulo,
                TotalTransacoes = transactions.Count,
                TotalEntradas = transactions.Where(t => t.Valor > 0).Sum(t => t.Valor),
                TotalSaidas = -transactions.Where(t => t.Valor < 0).Sum(t => t.Valor),
            };

            if (transactions.Count == 0) {
                return View(vm);
            }

            // ----- Evolução mensal (sequência contínua, meses sem dados entram zerados) -----
            var porMes = transactions
                .GroupBy(t => new DateOnly(t.Date.Year, t.Date.Month, 1))
                .ToDictionary(g => g.Key, g => new {
                    Entradas = g.Where(t => t.Valor > 0).Sum(t => t.Valor),
                    Saidas = -g.Where(t => t.Valor < 0).Sum(t => t.Valor),
                });

            // Começa no primeiro mês COM dados (uma janela de 12 meses num histórico de 6
            // mostraria meia tela de barras zeradas); buracos internos continuam zerados.
            var primeiroMes = porMes.Keys.Min();
            // Com filtro "tudo" não força até o mês atual: termina no último mês com dados.
            var ultimoMes = inicio.HasValue ? mesAtual : porMes.Keys.Max();

            decimal acumulado = 0;
            for (var mes = primeiroMes; mes <= ultimoMes; mes = mes.AddMonths(1)) {
                porMes.TryGetValue(mes, out var dados);
                var entradas = dados?.Entradas ?? 0;
                var saidas = dados?.Saidas ?? 0;
                acumulado += entradas - saidas;
                vm.Meses.Add(new DashboardViewModel.PontoMensal {
                    Rotulo = $"{MesesAbreviados[mes.Month - 1]}/{mes.Year % 100:D2}",
                    Entradas = entradas,
                    Saidas = saidas,
                    SaldoAcumulado = acumulado,
                });
            }

            var mesesComDados = porMes.Count;
            vm.MediaMensalSaidas = mesesComDados > 0 ? vm.TotalSaidas / mesesComDados : 0;

            // ----- Gastos por categoria (só saídas) -----
            vm.Categorias = transactions
                .Where(t => t.Valor < 0)
                .GroupBy(t => t.CategoryId == null
                    ? (Nome: "Sem categoria", Cor: "#94a3b8")
                    : (Nome: t.CategoriaNome, Cor: t.CategoriaCor))
                .Select(g => new DashboardViewModel.GastoCategoria {
                    Nome = g.Key.Nome,
                    Cor = g.Key.Cor,
                    Total = -g.Sum(t => t.Valor),
                    Percentual = vm.TotalSaidas > 0 ? Math.Round(-g.Sum(t => t.Valor) / vm.TotalSaidas * 100, 1) : 0,
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            // ----- Top estabelecimentos (só saídas, pela MatchKey aprendida) -----
            vm.Estabelecimentos = transactions
                .Where(t => t.Valor < 0 && !string.IsNullOrEmpty(t.MatchKey))
                .GroupBy(t => t.MatchKey)
                .Select(g => new DashboardViewModel.TopEstabelecimento {
                    Nome = g.Key,
                    Total = -g.Sum(t => t.Valor),
                    Quantidade = g.Count(),
                })
                .OrderByDescending(e => e.Total)
                .Take(10)
                .ToList();

            // ----- Gasto por dia da semana (só saídas) -----
            var porDia = transactions
                .Where(t => t.Valor < 0)
                .GroupBy(t => (int)t.Date.DayOfWeek)
                .ToDictionary(g => g.Key, g => -g.Sum(t => t.Valor));
            vm.DiasSemana = Enumerable.Range(0, 7)
                .Select(d => new DashboardViewModel.GastoDiaSemana {
                    Rotulo = DiasSemanaRotulos[d],
                    Total = porDia.GetValueOrDefault(d),
                })
                .ToList();

            MontarInsights(vm, mesAtual, transactions.Count(t => t.Valor < 0 && t.CategoryId == null));
            return View(vm);
        }

        /// <summary>Frases de destaque calculadas sobre a mesma fatia dos gráficos.</summary>
        private static void MontarInsights(DashboardViewModel vm, DateOnly mesAtual, int saidasSemCategoria) {
            var mesesComGasto = vm.Meses.Where(m => m.Saidas > 0).ToList();

            if (mesesComGasto.Count > 0) {
                var pior = mesesComGasto.MaxBy(m => m.Saidas)!;
                vm.Insights.Add($"Seu mês de maior gasto foi {NomeCompleto(pior.Rotulo)}, com {pior.Saidas:C2} em saídas.");
            }

            // Compara os dois meses completos mais recentes (o mês atual, parcial, distorceria).
            var rotuloMesAtual = $"{MesesAbreviados[mesAtual.Month - 1]}/{mesAtual.Year % 100:D2}";
            var completos = mesesComGasto.Where(m => m.Rotulo != rotuloMesAtual).TakeLast(2).ToList();
            if (completos.Count == 2 && completos[0].Saidas > 0) {
                var variacao = (completos[1].Saidas - completos[0].Saidas) / completos[0].Saidas * 100;
                if (Math.Abs(variacao) >= 1) {
                    vm.Insights.Add($"Em {NomeCompleto(completos[1].Rotulo)} você gastou {Math.Abs(variacao):F0}% " +
                        $"{(variacao > 0 ? "a mais" : "a menos")} que em {NomeCompleto(completos[0].Rotulo)}.");
                }
            }

            var topCategoria = vm.Categorias.FirstOrDefault(c => c.Nome != "Sem categoria");
            if (topCategoria is not null) {
                vm.Insights.Add($"{topCategoria.Nome} é a categoria que mais pesa: " +
                    $"{topCategoria.Percentual:F1}% das saídas ({topCategoria.Total:C2}).");
            }

            var topEstabelecimento = vm.Estabelecimentos.FirstOrDefault();
            if (topEstabelecimento is not null) {
                vm.Insights.Add($"Onde você mais gastou: {topEstabelecimento.Nome} — " +
                    $"{topEstabelecimento.Total:C2} em {topEstabelecimento.Quantidade} lançamentos.");
            }

            var piorDia = vm.DiasSemana.MaxBy(d => d.Total);
            if (piorDia is not null && piorDia.Total > 0) {
                var nomes = new Dictionary<string, string> {
                    ["dom"] = "domingo", ["seg"] = "segunda-feira", ["ter"] = "terça-feira",
                    ["qua"] = "quarta-feira", ["qui"] = "quinta-feira", ["sex"] = "sexta-feira", ["sáb"] = "sábado",
                };
                vm.Insights.Add($"O dia da semana em que você mais gasta é {nomes[piorDia.Rotulo]} ({piorDia.Total:C2} no período).");
            }

            if (saidasSemCategoria > 0) {
                vm.Insights.Add($"{saidasSemCategoria} saídas ainda estão sem categoria — " +
                    "categorizá-las deixa o gráfico de categorias mais fiel.");
            }
        }

        /// <summary>"jun/26" → "junho de 2026", para as frases de insight.</summary>
        private static string NomeCompleto(string rotulo) {
            var partes = rotulo.Split('/');
            var indice = Array.IndexOf(MesesAbreviados, partes[0]);
            return indice >= 0 ? $"{MesesExtensos[indice]} de 20{partes[1]}" : rotulo;
        }
    }
}
