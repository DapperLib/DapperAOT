using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser]
public class ListIterationBenchmarks
{
    private readonly List<Customer> customers = new();

    [Params(0, 1, 10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        customers.Clear();
        for (int i = 0; i < Count; i++)
        {
            customers.Add(new Customer { Id = i, Name = "Name " + i });
        }
    }

    [Benchmark(Baseline = true)]
    public int List()
    {
        int sum = 0;
        foreach (var customer in customers)
        {
            sum += customer.Id;
        }
        return sum;
    }

    [Benchmark]
    public int Span()
    {
        int sum = 0;
        foreach (var customer in CollectionsMarshal.AsSpan(customers))
        {
            sum += customer.Id;
        }
        return sum;
    }

    [Benchmark]
    public int Enumerable()
    {
        int sum = 0;
        foreach (var customer in (IEnumerable<Customer>)customers)
        {
            sum += customer.Id;
        }
        return sum;
    }
}
