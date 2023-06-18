using Dapper;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        _ = connection.Execute("def", new List<Customer> { new Customer(), new Customer() });
        _ = connection.Execute("def", (new[] { new { Foo = 12, bar }, new { Foo = 53, bar = "abc" } }).AsList());
        _ = connection.Execute("def", new Customer[0], commandType: CommandType.StoredProcedure);
        _ = connection.Execute("def @x", ImmutableArray<Customer>.Empty, commandType: CommandType.Text);
        _ = connection.Execute("def", new CustomerEnumerable());
        _ = connection.Execute("def", new CustomerICollection());
        _ = connection.Execute("def", new CustomerIList());
        _ = connection.Execute("def", new CustomerIReadOnlyCollection());
        _ = connection.Execute("def", new CustomerIReadOnlyList());
    }

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }

    public class CustomerIList : CustomerICollection, IList<Customer>
    {
        public int IndexOf(Customer item) => 1;
        public void Insert(int index, Customer item) { }
        public void RemoveAt(int index) { }
        public Customer this[int index] { get => null; set { } }
    }

    public class CustomerICollection : ICollection<Customer>
    {
        private ICollection<Customer> _collectionImplementation;
        public IEnumerator<Customer> GetEnumerator() => _collectionImplementation.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collectionImplementation).GetEnumerator();
        public void Add(Customer item) { _collectionImplementation.Add(item); }
        public void Clear() { _collectionImplementation.Clear(); }
        public bool Contains(Customer item) => _collectionImplementation.Contains(item);
        public void CopyTo(Customer[] array, int arrayIndex) { _collectionImplementation.CopyTo(array, arrayIndex); }
        public bool Remove(Customer item) { return _collectionImplementation.Remove(item); }
        public int Count => _collectionImplementation.Count;
        public bool IsReadOnly => _collectionImplementation.IsReadOnly;
    }

    public class CustomerIReadOnlyList : CustomerIReadOnlyCollection, IReadOnlyList<Customer>
    {
        public Customer this[int index] => throw new System.NotImplementedException();
    }

    public class CustomerIReadOnlyCollection : IReadOnlyCollection<Customer>
    {
        public IEnumerator<Customer> GetEnumerator() => null!;
        IEnumerator IEnumerable.GetEnumerator() => null!;
        public int Count => 0;
    }

    public class CustomerEnumerable : IEnumerable<Customer>
    {
        public IEnumerator<Customer> GetEnumerator() => null!;
        IEnumerator IEnumerable.GetEnumerator() => null!;
    }
}