using Microsoft.EntityFrameworkCore;
using GerenciadorDeFinancasASPNET.Models;

namespace GerenciadorDeFinancasASPNET.data {
    public class AppDbContext : DbContext{
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryRule> CategoryRules { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Transaction>().HasKey(t => t.Id);

            // Impede importar a mesma transação duas vezes (idempotência na importação do extrato).
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.UniqueHash)
                .IsUnique();

            // Usado para localizar transações "semelhantes" ao aplicar/aprender categorias.
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.MatchKey);

            // Linhas antigas (só extrato) ganham "Extrato" ao aplicar a migration.
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Origem)
                .HasDefaultValue(Transaction.OrigemExtrato);

            // Excluir uma categoria não apaga as transações: elas voltam a "sem categoria".
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Nome)
                .IsUnique();

            // Uma MatchKey só pode apontar para uma categoria (a regra mais recente vence).
            modelBuilder.Entity<CategoryRule>()
                .HasIndex(r => r.MatchKey)
                .IsUnique();

            // As regras da categoria morrem junto com ela.
            modelBuilder.Entity<CategoryRule>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Rules)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
