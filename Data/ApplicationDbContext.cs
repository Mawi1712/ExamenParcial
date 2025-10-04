using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Curso>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                entity.HasIndex(c => c.Codigo)
                    .IsUnique();

                entity.Property(c => c.Creditos)
                    .IsRequired();

            });

            modelBuilder.Entity<Matricula>(entity =>
{
    entity.HasKey(m => m.Id);

    entity.HasOne(m => m.Curso)
        .WithMany(c => c.Matriculas)
        .HasForeignKey(m => m.CursoId)
        .OnDelete(DeleteBehavior.Restrict);

    // Índice único: Un usuario no puede matricularse más de una vez en el mismo curso
    entity.HasIndex(m => new { m.CursoId, m.UsuarioId })
        .IsUnique()
        .HasFilter("[Estado] != 2"); // Solo si Estado != Cancelada (2)

    entity.Property(m => m.Estado)
        .HasConversion<int>();
});
        }
    }
}