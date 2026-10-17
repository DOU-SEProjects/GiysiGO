namespace GiysiGo.Modules.Identity.Tests
{
    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GIYSIGO_IDENTITY_TEST_CONNECTION")))
            {
                Skip = "Run scripts/Test-IdentityData.ps1 against an isolated LocalDB database.";
            }
        }
    }
}
