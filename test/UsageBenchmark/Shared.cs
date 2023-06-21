using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsageBenchmark;

namespace Dapper;

public class Customer
{
    public int Id { get; set; }

    [DbValue(Size = 400)]
    public string Name { get; set; } = "";
}

public class MyContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(Program.ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("BenchmarkCustomers");
            entity.Property<int>(nameof(Customer.Id)).ValueGeneratedOnAdd().UseIdentityColumn(1, 1);
            entity.Property<string>(nameof(Customer.Name));
        });
}
