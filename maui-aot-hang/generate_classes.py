import sys
import random
import argparse


def generate_random_body(n):
    operations = ['+', '-', '*', '/']
    op = random.choice(operations)
    num = random.randint(1, 10)
    delay = random.randint(100, 1000)
    runtime_struct_types = ['int', 'float', 'double', 'long']
    runtime_struct_type = random.choice(runtime_struct_types)
    custiom_struct_id = random.randint(1, n)
    collection_types = ['List<T>', 'HashSet<T>', 'LinkedList<T>', 'Queue<T>', 'Stack<T>', 'SortedSet<T>']
    collection_type = random.choice(collection_types)
    body = f"""
        if (typeof(T) == typeof({runtime_struct_type}))
        {{
            {runtime_struct_type} val = ({runtime_struct_type})(object)input;
            val {op}= {num};
            await Task.Delay({delay});
            var customStruct = new DummyStruct{custiom_struct_id} {{ Value = (int)val }};
    """
    for i in range(1, 100):
        custiom_struct_id_2 = random.randint(1, n)
        body += f"""
                var customStruct{i} = new DummyStruct{custiom_struct_id_2} {{ Value = (int)val }};
                var result{i} = await HelperCalculator.Add(customStruct{i}, {num});
                customStruct.Value += result{i}.First().Value;
        """
    body += f"""
            var list = new List<T>() {{ (T)(object)customStruct.Value }};
            return new {collection_type}(list);
        }}
        else
        {{
            await Task.Delay({delay});
            var list = new List<T>() {{ (T)(object)input }};
            return new {collection_type}(list);
        }}"""
    return body


def generate_class(i, n):
    body = generate_random_body(n)
    class_code = f"""
public class SomeClass{i}<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{{
    public async Task<IEnumerable<T>> Calculate<T>(T input) where T : struct
    {{{body}
    }}
}}"""
    return class_code


def generate_call_all_classes(n, m):
    code = f"""
public static class Caller
{{
{generate_generic_caller_method(n)}
"""

    code += generate_100_class_caller_methods(m)
    code += generate_one_large_caller_method(m)

    code += "\n}"
    return code


def generate_generic_caller_method(n):
    code = f"""
    public static async Task<IEnumerable<T>> All<T>(T input) where T : struct
    {{
        if (input is int number && number == 42)
        {{
            return new List<T> {{ (T)(object)number }};
        }}
        var result = new List<T>();
"""
    for i in range(1, n + 1):
        code += f"""
        var result{i} = await new SomeClass{i}<
        """

        code += ",\n".join([f"DummyEnum{random.randint(1, n)}" for _ in range(10)])

        code += f""">().Calculate(input);
        var genericArgs{i} = new Type[] {{"""

        code += ",\n".join([f"typeof(DummyStruct{random.randint(1, n)})" for _ in range(10)])

        code += f"""}};
        var genericType{i} = typeof(SomeClass1<,,,,,,,,,>).MakeGenericType(genericArgs{i});
        var refletionInstance{i} = Activator.CreateInstance(genericType{i});
        var method{i} = refletionInstance{i}.GetType().GetMethod("Calculate");
        var resultReflected{i} = await (Task<IEnumerable<T>>)method{i}.Invoke(refletionInstance{i}, [ input ]);
        Console.WriteLine($"{{result{i}.First()}} {{resultReflected{i}.First()}}");
        """

    code += """
        return result;
    }"""
    return code


def generate_100_class_caller_methods(n):
    code = ""
    for j in range(100):
        code += f"""
        public static int AllTypes{j}(int input)
        {{
            if (input is int number && number == 42)
            {{
                return number;
            }}
            var res = 0;
        """
        for i in range(1, max(n // 100 + 1, 2)):
            dummy_class_id = min(n, max(1, j * (n // 100) + i))
            code += f"""
            res +=(Activator.CreateInstance(typeof(DummyClass{dummy_class_id})) as DummyClass{dummy_class_id}).Value;"""
        code += """
            return res;
        }"""
    return code


def generate_one_large_caller_method(n):
    code = f"""
    public static int AllTypesLarge(int input)
        {{
            if (input is int number && number == 42)
            {{
                return number;
            }}
            var res = 0;
    """

    for i in range(1, n + 1):
        code += f"""
        res +=(Activator.CreateInstance(typeof(DummyClass{i})) as DummyClass{i}).Value;"""
    code += """
        return res;
    }"""
    return code


def generate_dummy_struct(id):
    return f"""
public struct DummyStruct{id}: IHasValue
{{
    public int Value {{ get; set; }}
}}
    """


def generate_dummy_enum(id):
    return f"""
public enum DummyEnum{id}
{{
    Value{id} = {id}
}}
    """


def generate_dummy_class(id):
    return f"""
public record DummyClass{id}: IHasValue
{{
    public int Value {{ get; set; }}
}}
"""


def generate_helper_calculator():
    return f"""
public interface IHasValue
{{
    int Value {{ get; set; }}
}}

public static class HelperCalculator
{{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<IEnumerable<T>> Add<T>(T input, int value) where T : struct, IHasValue
    {{
        IHasValue input1 = input as IHasValue;
        input1.Value += value;
        await Task.Yield();
        return new List<T> {{ (T)(object)input1.Value }};
    }}
}}"""


def generate_header():
    return """
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GeneratedClasses;
"""


def main():
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="mode")

    parser.add_argument("-o", type=str, default="sample.cs")

    simple_parser = subparsers.add_parser("simple", help="Generate simple classes")
    simple_parser.add_argument("-n", type=int, default=100)

    advanced_parser = subparsers.add_parser("advanced", help="Generate advanced classes")
    advanced_parser.add_argument("-n", type=int, default=100)
    advanced_parser.add_argument("-m", type=int, default=100)

    args = parser.parse_args()

    if args.mode == "simple":
        with open(args.o, 'w') as f:
            f.write(generate_header())

            f.write(generate_helper_calculator())

            for i in range(1, args.n + 1):
                f.write(generate_dummy_class(i))
    elif args.mode == "advanced":
        with open(args.o, 'w') as f:
            f.write(generate_header())

            f.write(generate_call_all_classes(args.n, args.m))
            f.write(generate_helper_calculator())

            for i in range(1, args.n + 1):
                f.write(generate_dummy_struct(i))
                f.write(generate_dummy_enum(i))
                f.write(generate_class(i, args.n))

            for i in range(1, args.m + 1):
                f.write(generate_dummy_class(i))


if __name__ == "__main__":
    main()
