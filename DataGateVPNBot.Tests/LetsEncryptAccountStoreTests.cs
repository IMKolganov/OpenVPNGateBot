using DataGateVPNBot.Services.LetsEncrypt;
using Xunit;

namespace DataGateVPNBot.Tests;

public class LetsEncryptAccountStoreTests
{
    [Fact]
    public void LoadOrCreateAccountKey_In_Temp_Directory_Creates_Key_And_File()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LetsEncryptAccountStoreTests_" + Guid.NewGuid().ToString("N"));
        var certsDir = Path.Combine(tempDir, "certs");
        try
        {
            Directory.CreateDirectory(certsDir);
            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(tempDir);
                var key = LetsEncryptAccountStore.LoadOrCreateAccountKey();
                Assert.NotNull(key);
                var accountFile = Path.Combine(tempDir, "certs", "account.pem");
                Assert.True(File.Exists(accountFile));
                var pem = File.ReadAllText(accountFile);
                Assert.False(string.IsNullOrWhiteSpace(pem));
                Assert.Contains("-----", pem);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrCreateAccountKey_When_File_Exists_Loads_Key()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LetsEncryptAccountStoreTests_" + Guid.NewGuid().ToString("N"));
        var certsDir = Path.Combine(tempDir, "certs");
        try
        {
            Directory.CreateDirectory(certsDir);
            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(tempDir);
                var key1 = LetsEncryptAccountStore.LoadOrCreateAccountKey();
                var key2 = LetsEncryptAccountStore.LoadOrCreateAccountKey();
                Assert.NotNull(key1);
                Assert.NotNull(key2);
                Assert.Equal(key1.ToPem(), key2.ToPem());
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
