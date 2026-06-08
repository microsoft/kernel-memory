"""CLI execution wrapper for testing km commands."""
import subprocess
import json
import os
from pathlib import Path
from typing import Optional


class KmResult:
    """Result of executing a km command."""

    def __init__(self, stdout: str, stderr: str, exit_code: int):
        self.stdout = stdout
        self.stderr = stderr
        self.exit_code = exit_code

    @property
    def stdout_json(self):
        """Parse stdout as JSON."""
        return json.loads(self.stdout)

    def assert_success(self):
        """Assert command succeeded."""
        assert self.exit_code == 0, f"Command failed with exit code {self.exit_code}\nstderr: {self.stderr}"


class KmCli:
    """Wrapper for executing km CLI commands."""

    def __init__(self, config_path: Optional[str] = None, km_binary: Optional[str] = None):
        self.config_path = config_path

        # Find km binary (Main.dll)
        if km_binary:
            self.km_binary = Path(km_binary)
        else:
            self.km_binary = locate_km_binary()

    def run(self, *args, timeout: int = 30) -> KmResult:
        """Execute km command and return result."""
        cmd = ["dotnet", str(self.km_binary)]
        cmd.extend(args)

        # Add config if specified
        if self.config_path and "--config" not in args:
            cmd.extend(["--config", self.config_path])

        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout
        )

        return KmResult(result.stdout, result.stderr, result.returncode)

    def get_database_path(self, node: str = "personal") -> Optional[Path]:
        """Get path to node's content database from config."""
        if not self.config_path:
            return None

        with open(self.config_path) as f:
            config = json.load(f)

        node_config = config["nodes"].get(node)
        if not node_config:
            return None

        db_path = node_config["contentIndex"]["path"]
        return Path(db_path)


def locate_km_binary() -> Path:
    """
    Locate the built km CLI (KernelMemory.Main.dll).

    Priority:
    1) KM_BIN environment variable
    2) Debug output
    3) Release output
    """
    env_bin = os.environ.get("KM_BIN")
    if env_bin:
        path = Path(env_bin)
        if path.exists():
            return path
        raise FileNotFoundError(f"KM_BIN is set but does not exist: {path}")

    repo_root = Path(__file__).parent.parent.parent.parent
    candidates = [
        repo_root / "src/Main/bin/Debug/net10.0/KernelMemory.Main.dll",
        repo_root / "src/Main/bin/Release/net10.0/KernelMemory.Main.dll",
    ]

    for candidate in candidates:
        if candidate.exists():
            return candidate

    raise FileNotFoundError(
        "km binary not found. Set KM_BIN to the path of KernelMemory.Main.dll or build the project."
    )
