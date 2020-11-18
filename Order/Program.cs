using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;

var factory = new OrderContextFactory();
using var context = factory.CreateDbContext(args);

switch (args[0])
{
    case "import":
        ImportMode(args[1], args[2]);
        break;
    case "clean":
        CleanMode();
        break;
    case "check":
        CheckMode();
        break;
    case "full":
        CleanMode();
        ImportMode(args[1], args[2]);
        CheckMode();
        break;
    default:
        break;
}

async void ImportMode(string customerPath, string orderPath)
{
    var tempLines = await File.ReadAllLinesAsync(customerPath);
    var customerLines = tempLines.Skip(1).Select(x => x.Split('\t')).ToList();

    tempLines = await File.ReadAllLinesAsync(orderPath);
    var orderLines = tempLines.Skip(1).Select(x => x.Split('\t')).ToList();

    Customer newCustomer = null;
    foreach (var item in customerLines)
    {
        newCustomer = new Customer { Name = item[0].ToString(), CreditLimit = Convert.ToDecimal(item[1]), Orders = new List<OrderTest>()};

        var orderList = orderLines.Where(o => o[0] == item[0])
            .Select(o => new OrderTest { Customer = newCustomer, OrderDate = Convert.ToDateTime(o[1]), OrderValue = Convert.ToDecimal(o[2]) })
            .ToList();
        newCustomer.Orders = orderList;

        context.Customers.Add(newCustomer);
        await context.SaveChangesAsync();
    }

    context.Customers.Add(newCustomer);
    await context.SaveChangesAsync();
}

async void CleanMode()
{
    context.Customers.RemoveRange(context.Customers);
    context.Orders.RemoveRange(context.Orders);

    await context.SaveChangesAsync();
}

async void CheckMode()
{
    var result = await context.Customers.Where(c => c.Orders.Sum(s => s.OrderValue) > c.CreditLimit)
        .ToListAsync();

    foreach (var item in result)
    {
        Console.WriteLine($"Customer {item.Name}");
    }
}



class Customer
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [Column(TypeName ="decimal(8,2)")]
    public decimal CreditLimit { get; set; }

    public List<OrderTest> Orders { get; set; }
}

class OrderTest
{
    public int Id { get; set; }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Customer? Customer { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal OrderValue { get; set; }
}

class OrderContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    public DbSet<OrderTest> Orders { get; set; }

    public OrderContext(DbContextOptions<OrderContext> options)
        : base(options)
    {

    }
}

class OrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    public OrderContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new OrderContext(optionsBuilder.Options);
    }
}
