using InventarioTI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventarioTI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AgenteRegistrado> AgentesRegistrados => Set<AgenteRegistrado>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<SwitchDispositivo> Switches => Set<SwitchDispositivo>();
    public DbSet<SwitchPuerto> SwitchPuertos => Set<SwitchPuerto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // ------------------ agentes_registrados ------------------
        modelBuilder.Entity<AgenteRegistrado>(e =>
        {
            e.ToTable("agentes_registrados");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.NombreEquipo).HasMaxLength(100).IsRequired();
            e.Property(x => x.ApiKeyHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
            e.HasIndex(x => x.NombreEquipo).IsUnique();
        });

        // ------------------ equipos ------------------
        modelBuilder.Entity<Equipo>(e =>
        {
            e.ToTable("equipos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.NombreEquipo).HasMaxLength(100).IsRequired();
            e.Property(x => x.UsuarioConectado).HasMaxLength(100);
            e.Property(x => x.Dominio).HasMaxLength(100);
            e.Property(x => x.Ip).HasMaxLength(45);
            e.Property(x => x.Mac).HasMaxLength(17);
            e.Property(x => x.SistemaOperativo).HasMaxLength(100);
            e.Property(x => x.VersionSo).HasMaxLength(50);
            e.Property(x => x.RamGb).HasPrecision(5, 2);
            e.Property(x => x.Procesador).HasMaxLength(150);
            e.Property(x => x.DiscoGb).HasPrecision(6, 2);
            e.Property(x => x.EspacioLibreGb).HasPrecision(6, 2);
            e.Property(x => x.NumeroSerie).HasMaxLength(100);
            e.Property(x => x.Fabricante).HasMaxLength(100);
            e.Property(x => x.Modelo).HasMaxLength(100);
            e.Property(x => x.Antivirus).HasMaxLength(150);
            e.Property(x => x.Programas)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => System.Text.Json.Nodes.JsonNode.Parse(v) ?? null);
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
            e.HasIndex(x => x.NombreEquipo).IsUnique();
        });

        // ------------------ switches ------------------
        modelBuilder.Entity<SwitchDispositivo>(e =>
        {
            e.ToTable("switches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Ip).HasMaxLength(45).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Marca).HasMaxLength(50);
            e.Property(x => x.Modelo).HasMaxLength(100);
            e.Property(x => x.Serial).HasMaxLength(100);
            e.Property(x => x.Firmware).HasMaxLength(50);
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
            e.HasIndex(x => x.Ip).IsUnique();
        });

        // ------------------ switch_puertos ------------------
        modelBuilder.Entity<SwitchPuerto>(e =>
        {
            e.ToTable("switch_puertos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.NumeroPuerto).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.TraficoInMb).HasPrecision(12, 2);
            e.Property(x => x.TraficoOutMb).HasPrecision(12, 2);
            e.HasIndex(x => new { x.SwitchId, x.NumeroPuerto }).IsUnique();
            e.HasOne(x => x.Switch)
                .WithMany(s => s.Puertos)
                .HasForeignKey(x => x.SwitchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------ usuarios ------------------
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Rol).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
