// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KM.Abstractions.UnitTests.Diagnostics;

public sealed class SensitiveDataLoggerTests : IDisposable
{
    private const string AspNetCoreEnvVar = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvVar = "DOTNET_ENVIRONMENT";

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItIsDisabledByDefault()
    {
        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(AspNetCoreEnvVar, "development")]
    [InlineData(AspNetCoreEnvVar, "Development")]
    [InlineData(DotNetEnvVar, "development")]
    [InlineData(DotNetEnvVar, "Development")]
    public void ItCanBeEnabledInDevEnvironments(string envVar, string envType)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(envVar, envType);

        // Act
        SensitiveDataLogger.Enabled = true;

        // Assert
        Assert.True(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(AspNetCoreEnvVar, "staging")]
    [InlineData(AspNetCoreEnvVar, "Staging")]
    [InlineData(AspNetCoreEnvVar, "production")]
    [InlineData(AspNetCoreEnvVar, "Production")]
    [InlineData(AspNetCoreEnvVar, "any")]
    [InlineData(DotNetEnvVar, "staging")]
    [InlineData(DotNetEnvVar, "Staging")]
    [InlineData(DotNetEnvVar, "production")]
    [InlineData(DotNetEnvVar, "Production")]
    [InlineData(DotNetEnvVar, "any")]
    public void ItCannotBeEnabledInNonDevEnvironments(string envVar, string envType)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(envVar, envType);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(AspNetCoreEnvVar, "development")]
    [InlineData(AspNetCoreEnvVar, "Development")]
    [InlineData(AspNetCoreEnvVar, "staging")]
    [InlineData(AspNetCoreEnvVar, "Staging")]
    [InlineData(AspNetCoreEnvVar, "production")]
    [InlineData(AspNetCoreEnvVar, "Production")]
    [InlineData(AspNetCoreEnvVar, "any")]
    [InlineData(DotNetEnvVar, "development")]
    [InlineData(DotNetEnvVar, "Development")]
    [InlineData(DotNetEnvVar, "staging")]
    [InlineData(DotNetEnvVar, "Staging")]
    [InlineData(DotNetEnvVar, "production")]
    [InlineData(DotNetEnvVar, "Production")]
    [InlineData(DotNetEnvVar, "any")]
    public void ItCanBeDisabledInAnyEnvironment(string envVar, string envType)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(envVar, envType);

        // Act
        SensitiveDataLogger.Enabled = false;

        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development", "staging", false)]
    [InlineData("Development", "Staging", false)]
    [InlineData("development", "production", false)]
    [InlineData("Development", "Production", false)]
    [InlineData("staging", "development", true)]
    [InlineData("Staging", "Development", true)]
    [InlineData("production", "development", true)]
    [InlineData("Production", "Development", true)]
    public void AspNetCoreEnvTypeHasThePrecedence(string aspNetCoreEnvType, string dotNetEnvType, bool fail)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(AspNetCoreEnvVar, aspNetCoreEnvType);
        Environment.SetEnvironmentVariable(DotNetEnvVar, dotNetEnvType);

        if (fail)
        {
            Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
        }
        else
        {
            // Act
            SensitiveDataLogger.Enabled = true;

            // Assert
            Assert.True(SensitiveDataLogger.Enabled);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AspNetCoreEnvVar, null);
        Environment.SetEnvironmentVariable(DotNetEnvVar, null);
        SensitiveDataLogger.Enabled = false;
    }
}
