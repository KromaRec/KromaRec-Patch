using System;
using System.IO;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: VerifyInterop <20230414 game or reference directory>");
    return 2;
}
try { return InteropVerifier.Run(args[0]); }
catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
{
    Console.Error.WriteLine($"KRBUILD_METADATA_READ type={error.GetType().Name}; check local reference access.");
    return 3;
}
