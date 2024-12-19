using System;
using System.Linq;

namespace Dapper;

public readonly partial struct Command
{
    private abstract class ParameterBag
    {
        public abstract void AddParameters(in UnifiedCommand command);

        public static ParameterBag Create(Parameter[]? parameters) =>
            parameters is null ? Bag0.Instance
            : parameters.Length switch
            {
                0 => Bag0.Instance,
                1 => new Bag1(parameters[0]),
                2 => new Bag2(in parameters[0], in parameters[1]),
                3 => new Bag3(in parameters[0], in parameters[1], in parameters[2]),
                4 => new Bag4(in parameters[0], in parameters[1], in parameters[2], in parameters[3]),
                _ => new BagN(parameters),
            };

        public static ParameterBag Create(scoped ReadOnlySpan<Parameter> parameters) => parameters.Length switch
        {
            0 => Bag0.Instance,
            1 => new Bag1(parameters[0]),
            2 => new Bag2(in parameters[0], in parameters[1]),
            3 => new Bag3(in parameters[0], in parameters[1], in parameters[2]),
            4 => new Bag4(in parameters[0], in parameters[1], in parameters[2], in parameters[3]),
            _ => new BagN(parameters.ToArray()),
        };

        internal abstract Parameter[] ToArray();

        private sealed class Bag0 : ParameterBag
        {
            private Bag0() { }
            internal static readonly Bag0 Instance = new();
            public override void AddParameters(in UnifiedCommand command) { }

            internal override Parameter[] ToArray() => [];
        }

        private sealed class Bag1(in Parameter value0) : ParameterBag
        {
            private readonly Parameter value0 = value0;

            public override void AddParameters(in UnifiedCommand command)
                => value0.AddParameter(in command);

            internal override Parameter[] ToArray() => [value0];
        }

        private sealed class Bag2(in Parameter value0, in Parameter value1) : ParameterBag
        {
            private readonly Parameter value0 = value0;
            private readonly Parameter value1 = value1;

            public override void AddParameters(in UnifiedCommand command)
            {
                value0.AddParameter(in command);
                value1.AddParameter(in command);
            }

            internal override Parameter[] ToArray() => [value0, value1];
        }

        private sealed class Bag3(in Parameter value0, in Parameter value1, in Parameter value2) : ParameterBag
        {
            private readonly Parameter value0 = value0;
            private readonly Parameter value1 = value1;
            private readonly Parameter value2 = value2;

            public override void AddParameters(in UnifiedCommand command)
            {
                value0.AddParameter(in command);
                value1.AddParameter(in command);
                value2.AddParameter(in command);
            }

            internal override Parameter[] ToArray() => [value0, value1, value2];
        }

        private sealed class Bag4(in Parameter value0, in Parameter value1, in Parameter value2, in Parameter value3) : ParameterBag
        {
            private readonly Parameter value0 = value0;
            private readonly Parameter value1 = value1;
            private readonly Parameter value2 = value2;
            private readonly Parameter value3 = value3;

            internal override Parameter[] ToArray() => [value0, value1, value2, value3];

            public override void AddParameters(in UnifiedCommand command)
            {
                value0.AddParameter(in command);
                value1.AddParameter(in command);
                value2.AddParameter(in command);
                value3.AddParameter(in command);
            }
        }

        private sealed class BagN(Parameter[] parameters) : ParameterBag
        {
            public override void AddParameters(in UnifiedCommand command)
            {
                var snapshot = parameters;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i].AddParameter(in command);
                }
            }

            internal override Parameter[] ToArray() => parameters.ToArray(); // defensive copy, debug/test only
        }

        internal static CommandFactory<ParameterBag> CommandFactory => ParameterBagFactory.Instance;

        private sealed class ParameterBagFactory : CommandFactory<ParameterBag>
        {
            public static readonly ParameterBagFactory Instance = new();
            private ParameterBagFactory() { }

            public override void AddParameters(in UnifiedCommand command, ParameterBag args)
                => args.AddParameters(in command);
        }
    }
}
