using InventarioTI.Api.Helpers;
using InventarioTI.Domain.Models;
using InventarioTI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventarioTI.Api.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext db, string adminEmail, string adminPassword)
    {
        db.Database.Migrate();

        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new Usuario
            {
                Email = adminEmail,
                PasswordHash = PasswordHelper.Hash(adminPassword),
                Rol = "Admin"
            });
            db.SaveChanges();
        }
    }
}
