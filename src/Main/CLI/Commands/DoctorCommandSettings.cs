// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Main.CLI.Commands;

/// <summary>
/// Settings for the doctor command.
/// The doctor command validates configuration dependencies and checks system health.
/// </summary>
public sealed class DoctorCommandSettings : GlobalOptions
{
    // Doctor command has no additional settings beyond global options
    // Uses config file, node selection, and output format from GlobalOptions
}
