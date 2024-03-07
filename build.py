#!/usr/bin/env python
import glob
import os
import shutil
import subprocess
import sys

cfg = {
    "packages": [
        "Microsoft.KernelMemory.Abstractions",
        "Microsoft.KernelMemory.Core",
        "Microsoft.KernelMemory.WebClient",
        "Microsoft.KernelMemory.SemanticKernelPlugin",
        "Microsoft.KernelMemory.ContentStorage.AzureBlobs",
        "Microsoft.KernelMemory.DataFormats.AzureAIDocIntel",
        "Microsoft.KernelMemory.Orchestration.AzureQueues",
        "Microsoft.KernelMemory.Orchestration.RabbitMQ",
        "Microsoft.KernelMemory.AI.AzureOpenAI",
        "Microsoft.KernelMemory.AI.OpenAI",
        "Microsoft.KernelMemory.AI.LlamaSharp",
        "Microsoft.KernelMemory.MemoryDb.AzureAISearch",
        "Microsoft.KernelMemory.MemoryDb.Postgres",
        "Microsoft.KernelMemory.MemoryDb.Qdrant",
    ],
    "clear_dotnet_cache": False,
    # Enable this if using Bash and you want to see colors, though shell output won't stream in realtime
    "bash_with_colors_no_stream": False,
}


def main():

    # Move into project dir
    change_working_dir_to_project()
    print("Directory: ", os.getcwd())

    # Delete and rebuild packages
    print(bold("### Deleting previous builds"))
    delete_files_by_extension(os.getcwd(), [".nupkg", ".snupkg"])

    # Find SLN file
    print(bold("### Build solution"))
    solution = find_sln_file(os.getcwd())
    if not solution or solution.isspace():
        print(bold("# Error:"), red("No valid .sln file found."))
        exit(1)

    # Clear .NET cache
    if cfg["clear_dotnet_cache"]:
        print(bold("# Deleting .NET cache"))
        run_shell_command(f"dotnet clean {solution} --nologo -c Debug --verbosity minimal")
        run_shell_command(f"dotnet clean {solution} --nologo -c Release --verbosity minimal")

    # Build SLN
    run_shell_command(f"dotnet build {solution} --nologo -c Release")

    print(bold("### Verify packages have been built"))
    verify_packages_build(os.getcwd())

    print(bold("### Clearing Nuget cache"))
    delete_cached_packages()

    print(bold("### Copy Nuget packages to /packages"))
    move_packages_build_to(os.getcwd(), os.path.join(os.getcwd(), "packages"))

    # print(bold("# Clean .NET build"))
    # run_shell_command(f"dotnet restore {solution} --nologo --no-cache")
    # run_shell_command(f"dotnet clean {solution} --nologo -c Debug --verbosity minimal")
    # run_shell_command(f"dotnet clean {solution} --nologo -c Release --verbosity minimal")


def change_working_dir_to_project():
    os.chdir(os.path.dirname(os.path.abspath(__file__)))


def delete_files_by_extension(root_dir, extensions):
    count = 0
    if isinstance(extensions, str):
        extensions = [extensions]

    print("Deleting files from dir:", root_dir)
    for dir_name, sub_dirs, filenames in os.walk(root_dir):
        for filename in filenames:
            if any(filename.endswith(ext) for ext in extensions):
                file_path = os.path.join(dir_name, filename)
                os.remove(file_path)
                count += 1
                print(f"Deleted: {file_path[len(root_dir) + 1:]}")
    print("Files deleted:", count)


def find_sln_file(root_folder):
    for dir_name, sub_dirs, filenames in os.walk(root_folder):
        for filename in filenames:
            if filename.endswith(".sln") and "dev" not in filename.lower():
                return filename


def run_shell_command(command):
    if cfg["bash_with_colors_no_stream"]:
        run_shell_command_with_colors(command)
    else:
        run_shell_command_with_stream(command)


def bold(text):
    return f"\033[1m{text}\033[0m"


def red(text):
    return f"\033[91m{text}\033[0m"


def yellow(text):
    return f"\033[93m{text}\033[0m"


def run_shell_command_with_stream(command):
    try:
        # Run the shell command printing output as it occurs. Note: this might strip colors
        print(bold("# Command: "), command)

        process = subprocess.Popen(
            command,
            shell=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1,
            universal_newlines=True,
            env={**os.environ, "TERM": "xterm-256color"},
        )

        # Print the command output in real-time for stdout and stderr
        for line in process.stdout:
            sys.stdout.write(line)
            sys.stdout.flush()

        for line in process.stderr:
            sys.stdout.write(f"# Error: {line}")
            sys.stdout.flush()

        # Wait for the command to complete
        process.wait()

        # Check the return code
        if process.returncode != 0:
            print(bold("# Return Code: "), process.returncode)
            exit(1)

    except Exception as e:
        print(bold("# Error: "), e)
        exit(1)


def run_shell_command_with_colors(command):
    try:
        # Run the shell command and capture the output along with color codes
        print(bold("# Command: "), command)
        result = subprocess.run(
            ["script", "-q", "/dev/null", "bash", "-c", command],
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )

        if result.stdout and not result.stdout.isspace():
            print("# Command Output:")
            print(result.stdout)

    except subprocess.CalledProcessError as e:
        # Handle errors with original colors
        print(bold("# Error Occurred: "))
        print(bold("# Return Code: "), e.returncode)
        print(bold("# Standard Output: "))
        print(e.stdout)
        if e.stderr and not e.stderr.isspace():
            print(bold("# Standard Error: "))
            print(e.stderr)

        # Exit the script with an error code
        exit(1)


def verify_packages_build(root_dir):
    for package in cfg["packages"]:
        # Generate the expected file path
        expected_file_path = os.path.join(root_dir, "**", "bin", "Release", f"{package}.*.nupkg")

        # Use glob to check if any file matches the pattern
        matches = glob.glob(expected_file_path, recursive=True)

        if not matches:
            print(f"# Error: {package}: package not found")
            exit(1)
        else:
            for match in matches:
                print(f"{match[len(root_dir) + 1:]}")


def move_packages_build_to(root_dir, destination_dir):
    for package in cfg["packages"]:
        # Generate the expected file path
        expected_file_path = os.path.join(root_dir, "**", "bin", "Release", f"{package}.*.nupkg")

        # Use glob to check if any file matches the pattern
        matches = glob.glob(expected_file_path, recursive=True)

        if not matches:
            print(f"# Error: {package}: package not found")
            exit(1)
        else:
            for match in matches:
                shutil.move(match, destination_dir)


def delete_cached_packages():
    for package in cfg["packages"]:
        nuget_packages_dir = os.path.join(os.environ["HOME"], ".nuget", "packages", package)

        if os.path.isdir(nuget_packages_dir):
            print("Cache content before purge")
            print(f"dir: {nuget_packages_dir}")
            for entry in os.listdir(nuget_packages_dir):
                print(entry)

            try:
                shutil.rmtree(nuget_packages_dir)
            except Exception as e:
                print(f"ERROR: unable to clear cache at {nuget_packages_dir}")
                print(e)
                exit(1)

            if os.path.isdir(nuget_packages_dir):
                print(f"ERROR: unable to clear cache at {nuget_packages_dir}")
                exit(1)


def green(text):
    return f"\033[92m{text}\033[0m"


main()
