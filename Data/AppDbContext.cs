using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WebformularFuerMit.Models;

public class AppDbContext : DbContext
{
    public DbSet<Incident> Incidents { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}