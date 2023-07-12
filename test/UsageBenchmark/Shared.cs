using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations.Schema;
using UsageBenchmark;

namespace Dapper;

public class Customer
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [DbValue(Size = 400)]
    public string Name { get; set; } = "";
}

public class MyContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseSqlServer(Program.ConnectionString)
        .EnableSensitiveDataLogging();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("BenchmarkCustomers");
            entity.Property<int>(nameof(Customer.Id)).ValueGeneratedOnAdd().Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property<string>(nameof(Customer.Name));
        });
}
