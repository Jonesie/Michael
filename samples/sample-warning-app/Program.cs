using System;

namespace SampleWarningApp;

internal static class Program
{
    [Obsolete("Use NewMethod instead.")]
    private static void OldMethod()
    {
        Console.WriteLine("Using obsolete path.");
    }

    private static void Main()
    {
        Console.WriteLine("Hello from SampleWarningApp");

        int declaredButUnused;
        int assignedButUnused = 42;

        string? maybeNull = null;
        Console.WriteLine(maybeNull.Length);

        OldMethod();

        if (DateTime.UtcNow.Year > 1900)
        {
            return;
            Console.WriteLine("Unreachable line.");
        }
    }
}
