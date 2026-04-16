using System.Diagnostics;

namespace UnitTests;

public class JsTestsRunner
{
    [Fact]
    public void RunChecklistSyncJsTests()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var jsTestPath = Path.GetFullPath(Path.Combine(basePath, "../../../../JsTests/checklist-sync.test.js"));

        Assert.True(File.Exists(jsTestPath), $"JS test file not found at: {jsTestPath}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"--test \"{jsTestPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        Console.WriteLine("\n================== RUNNING JS TESTS ==================");

        process.Start();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"JS tests failed with exit code {process.ExitCode}");
    }
}
