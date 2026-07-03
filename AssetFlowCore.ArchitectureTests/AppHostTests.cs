using Xunit;

namespace AssetFlowCore.ArchitectureTests
{
    public class AppHostTests
    {
        [Fact]
        public void AppHost_source_file_contains_expected_builder_call()
        {
            // Resolve the path to the AppHost source file in the repository
            var repoRoot = Directory.GetCurrentDirectory();
            // Tests run from the test project's bin folder; walk up until solution root is found
            // Try multiple parent levels to be robust across environments
            string solutionRoot = repoRoot;
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(solutionRoot, "AssetFlowCore.Aspire", "AssetFlowCore.Aspire.AppHost", "AppHost.cs")))
                    break;
                solutionRoot = Path.GetDirectoryName(solutionRoot) ?? solutionRoot;
            }

            var appHostPath = Path.Combine(solutionRoot, "AssetFlowCore.Aspire", "AssetFlowCore.Aspire.AppHost", "AppHost.cs");

            Assert.True(File.Exists(appHostPath), $"AppHost.cs not found at: {appHostPath}");

            var content = File.ReadAllText(appHostPath);

            Assert.Contains("DistributedApplication.CreateBuilder", content);
            Assert.Contains("AddSqlServer", content);
            Assert.Contains("AddProject<Projects.AssetFlowCore_WebApi>", content);
        }
    }
}
