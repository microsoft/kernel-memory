using Visus.Cuid;

namespace KernelMemory.Core.Storage;

/// <summary>
/// Default implementation of ICuidGenerator using Cuid.Net library.
/// </summary>
public class CuidGenerator : ICuidGenerator
{
    /// <summary>
    /// Generates a new lowercase Cuid2 identifier.
    /// </summary>
    /// <returns>A unique lowercase Cuid2 string.</returns>
    public string Generate()
    {
        // Create new Cuid2 with default length (24 characters)
        // Cuid2 generates lowercase IDs by default
        var cuid = new Cuid2();
        return cuid.ToString();
    }
}
