using System.Threading;

namespace ClipboardMerge;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\ClipboardMerge.SingleInstance", out var createdNew);
        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new ClipboardApplicationContext());
    }
}
