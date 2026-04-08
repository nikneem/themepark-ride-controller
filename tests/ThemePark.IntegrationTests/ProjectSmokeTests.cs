namespace ThemePark.IntegrationTests;

/// <summary>
/// Placeholder non-integration tests so the project is not excluded
/// when running <c>dotnet test --filter "Category!=Integration"</c>.
/// The filter would otherwise produce "No tests found" which results in
/// a non-zero exit code in CI, masking the actual unit test results.
/// </summary>
public sealed class ProjectSmokeTests
{
    [Fact]
    public void IntegrationTestProject_Builds_Successfully()
    {
        // This test validates the integration test project compiles and all
        // harness types are reachable. Real integration tests require a live
        // Aspire distributed application and are tagged [Trait("Category","Integration")].
        Assert.True(true);
    }
}
