using System;
using System.Collections.Generic;
using Xunit;

namespace Dapper.AOT.Test.Accessors;

public class AccessorTests
{
    [Fact]
    public void AccessorUsage()
    {
        var customer = new Customer
        {
            Id = 42,
            Name = "abc",
            Description = "def",
            Foo = SomeEnum.Three
        };

        TypeAccessor<Customer> accessor = new HandWrittenCustomerAccessor();
        var wrapped = TypeAccessor.CreateAccessor(customer, accessor);

        Assert.Equal(4, wrapped.MemberCount);

        // get names
        Assert.Equal("Id", wrapped.GetName(0));
        Assert.Equal("Name", wrapped.GetName(1));
        Assert.Equal("Description", wrapped.GetName(2));
        Assert.Equal("Foo", wrapped.GetName(3));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.GetName(-1));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.GetName(4));

        // get types
        Assert.Equal(typeof(int), wrapped.GetType(0));
        Assert.Equal(typeof(string), wrapped.GetType(1));
        Assert.Equal(typeof(string), wrapped.GetType(2));
        Assert.Equal(typeof(SomeEnum), wrapped.GetType(3));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.GetType(-1));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.GetType(4));

        // get nullable
        Assert.False(wrapped.IsNullable(0));
        Assert.False(wrapped.IsNullable(1));
        Assert.True(wrapped.IsNullable(2));
        Assert.False(wrapped.IsNullable(3));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.IsNullable(-1));
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped.IsNullable(4));

        // get by name
        Assert.Equal(42, wrapped["Id"]);
        Assert.Equal("abc", wrapped["Name"]);
        Assert.Equal("def", wrapped["Description"]);
        Assert.Equal(SomeEnum.Three, wrapped["Foo"]);
        Assert.Throws<KeyNotFoundException>(() => _ = wrapped["bla"]);

        // get by index
        Assert.Equal(42, wrapped[0]);
        Assert.Equal("abc", wrapped[1]);
        Assert.Equal("def", wrapped[2]);
        Assert.Equal(SomeEnum.Three, wrapped[3]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = wrapped[4]);

        // set by name
        wrapped["Id"] = 43;
        wrapped["Name"] = "abc2";
        wrapped["Description"] = "def2";
        wrapped["Foo"] = SomeEnum.One;
        Assert.Throws<KeyNotFoundException>(() => wrapped["bla"] = true);

        Assert.Equal(43, wrapped[0]);
        Assert.Equal("abc2", wrapped[1]);
        Assert.Equal("def2", wrapped[2]);
        Assert.Equal(SomeEnum.One, wrapped[3]);

        // set by index
        wrapped[0] = 44;
        wrapped[1] = "abc3";
        wrapped[2] = "def3";
        wrapped[3] = SomeEnum.Two;
        Assert.Throws<IndexOutOfRangeException>(() => wrapped[-1] = true);
        Assert.Throws<IndexOutOfRangeException>(() => wrapped[4] = true);

        Assert.Equal(44, wrapped[0]);
        Assert.Equal("abc3", wrapped[1]);
        Assert.Equal("def3", wrapped[2]);
        Assert.Equal(SomeEnum.Two, wrapped[3]);

        // enum pun
        Assert.Equal(SomeEnum.Two, wrapped.GetValue<SomeEnum>(3));
        Assert.Equal(2, wrapped.GetValue<int>(3)); // get punned
        wrapped.SetValue(3, 3); // set punned
        Assert.Equal(SomeEnum.Three, wrapped.GetValue<SomeEnum>(3));
        wrapped[3] = 1; // set as integer
        Assert.Equal(SomeEnum.One, wrapped[3]); // get always well-typed
    }

    public class DbObject
    {
        public int Id { get; set; }
    }
    public sealed class Customer : DbObject
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public SomeEnum Foo { get; set; }
    }

    public enum SomeEnum
    {
        Zero, One, Two, Three
    }

    private sealed class HandWrittenCustomerAccessor : TypeAccessor<Customer>
    {
        public override int MemberCount => 4;

        public override int? TryIndex(string name, bool exact = false) =>
            // hash version not implemented yet
            name switch
            {
                nameof(Customer.Id) => 0,
                nameof(Customer.Name) => 1,
                nameof(Customer.Description) => 2,
                nameof(Customer.Foo) => 3,
                _ => base.TryIndex(name, exact),
            };

        public override string GetName(int index) => index switch
        {
            0 => nameof(Customer.Id),
            1 => nameof(Customer.Name),
            2 => nameof(Customer.Description),
            3 => nameof(Customer.Foo),
            _ => base.GetName(index),
        };

        public override object? this[Customer obj, int index]
        {
            get => index switch
            {
                0 => obj.Id,
                1 => obj.Name,
                2 => obj.Description,
                3 => obj.Foo,
                _ => base[obj, index]
            };
            set
            {
                switch (index)
                {
                    case 0: obj.Id = (int)value!; break;
                    case 1: obj.Name = (string)value!; break;
                    case 2: obj.Description = (string?)value; break;
                    case 3: obj.Foo = (SomeEnum)value!; break;
                    default: base[obj, index] = value; break;
                }
            }
        }
        public override bool IsNullable(int index) => index switch
        {
            0 or 1 or 3 => false,
            2 => true,
            _ => base.IsNullable(index),
        };

        public override Type GetType(int index) => index switch
        {
            0 => typeof(int),
            1 or 2 => typeof(string),
            3 => typeof(SomeEnum),
            _ => base.GetType(index),
        };

        public override TValue GetValue<TValue>(Customer obj, int index) => index switch
        {
            0 when typeof(TValue) == typeof(int) => UnsafePun<int, TValue>(obj.Id),
            1 when typeof(TValue) == typeof(string) => UnsafePun<string, TValue>(obj.Name),
            2 when typeof(TValue) == typeof(string) => UnsafePun<string?, TValue>(obj.Description),
            // important: we need to support integers for enums, using the correct underlying type
            3 when typeof(TValue) == typeof(SomeEnum) || typeof(TValue) == typeof(int) => UnsafePun<SomeEnum, TValue>(obj.Foo),
            _ => base.GetValue<TValue>(obj, index),
        };

        public override void SetValue<TValue>(Customer obj, int index, TValue value)
        {
            switch (index)
            {
                case 0 when typeof(TValue) == typeof(int):
                    obj.Id = UnsafePun<TValue, int>(value);
                    break;
                case 1 when typeof(TValue) == typeof(string):
                    obj.Name = UnsafePun<TValue, string>(value);
                    break;
                case 2 when typeof(TValue) == typeof(string):
                    obj.Description = UnsafePun<TValue, string?>(value);
                    break;
                // important: we need to support integers for enums, using the correct underlying type
                case 3 when typeof(TValue) == typeof(SomeEnum) || typeof(TValue) == typeof(int):
                    obj.Foo = UnsafePun<TValue, SomeEnum>(value);
                    break;
                default:
                    base.SetValue(obj, index, value);
                    break;
            }
        }
    }
}