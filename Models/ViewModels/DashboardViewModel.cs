namespace GerenciadorDeFinancasASPNET.Models.ViewModels {
    /// <summary>
    /// Dados agregados do dashboard. Tudo é calculado sobre a MESMA fatia de
    /// período (filtro no topo da página), para os números sempre baterem entre si.
    /// Valores de gasto ficam em módulo (positivos) — o sinal vira rótulo/cor na view.
    /// </summary>
    public class DashboardViewModel {
        public string Periodo { get; set; } = "12m";  // "6m" | "12m" | "ano" | "tudo"
        public string PeriodoRotulo { get; set; } = "";

        // KPIs
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }      // módulo
        public decimal Saldo => TotalEntradas - TotalSaidas;
        public decimal MediaMensalSaidas { get; set; }
        public int TotalTransacoes { get; set; }

        public List<string> Insights { get; set; } = new();

        public List<PontoMensal> Meses { get; set; } = new();
        public List<GastoCategoria> Categorias { get; set; } = new();
        public List<TopEstabelecimento> Estabelecimentos { get; set; } = new();
        public List<GastoDiaSemana> DiasSemana { get; set; } = new();

        public class PontoMensal {
            public string Rotulo { get; set; } = "";       // "jun/26"
            public decimal Entradas { get; set; }
            public decimal Saidas { get; set; }            // módulo
            public decimal SaldoAcumulado { get; set; }    // acumulado dentro do período filtrado
        }

        public class GastoCategoria {
            public string Nome { get; set; } = "";
            public string Cor { get; set; } = "";
            public decimal Total { get; set; }             // módulo
            public decimal Percentual { get; set; }        // % do total de saídas
        }

        public class TopEstabelecimento {
            public string Nome { get; set; } = "";
            public decimal Total { get; set; }             // módulo
            public int Quantidade { get; set; }
        }

        public class GastoDiaSemana {
            public string Rotulo { get; set; } = "";       // "dom", "seg", …
            public decimal Total { get; set; }             // módulo
        }
    }
}
