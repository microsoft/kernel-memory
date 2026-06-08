// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Redis;

internal static class Scripts
{
    /// <summary>
    /// Script to Upsert a Record. This script checks to see if the index exists
    /// If the script does not exist, the script returns false
    /// </summary>
    internal const string CheckIndexAndUpsert = """
local result = redis.pcall("FT.INFO",ARGV[1])

if type(result) == "string" and string.sub(result,7) == "(error)" then
    if result:lower() == "(error) unknown index name" then
        return false
    else
        return result
    end
end

redis.call("UNLINK", KEYS[1])
return redis.call("HSET", KEYS[1], unpack(ARGV,2))
""";
}
