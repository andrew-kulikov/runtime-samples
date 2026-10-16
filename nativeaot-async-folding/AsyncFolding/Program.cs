using System;
using System.Threading.Tasks;

namespace AsyncFolding
{
    internal static class Program
    {
        private static async Task<int> Main()
        {
            long sum = 0;
            foreach (Func<int, Task<int>> method in Generated.All())
                sum += await method(1);

            Console.WriteLine($"{Generated.Count} async methods, sum = {sum}");
            return 0;
        }
    }
}
