import json
import os
from pathlib import Path
from typing import List, Optional, Dict, Any, Union
import typer
import httpx
from typing_extensions import Annotated
from kernel_memory_client.client import Client, AuthenticatedClient
from kernel_memory_client.types import File
from kernel_memory_client.models.upload_document_body import UploadDocumentBody
from kernel_memory_client.models.upload_document_body_tags import UploadDocumentBodyTags
from kernel_memory_client.models.search_query import SearchQuery
from kernel_memory_client.models.memory_query import MemoryQuery
from kernel_memory_client.models.search_query_filters_type_0_item import SearchQueryFiltersType0Item
from kernel_memory_client.models.memory_query_filters_type_0_item import MemoryQueryFiltersType0Item
from kernel_memory_client.api.microsoft_kernel_memory_service_assembly.upload_document import sync as upload_document
from kernel_memory_client.api.microsoft_kernel_memory_service_assembly.search_document_snippets import sync as search_document_snippets
from kernel_memory_client.api.microsoft_kernel_memory_service_assembly.answer_question import sync as answer_question
from kernel_memory_client.api.microsoft_kernel_memory_service_assembly.list_indexes import sync as list_indexes
import mimetypes

#!/usr/bin/env python3
"""
Kernel Memory CLI

A command line interface for Microsoft Kernel Memory.
"""

app = typer.Typer(help="Kernel Memory CLI")

CONFIG_PATH = Path(
    os.environ.get("KM_CONFIG_PATH",
                   Path.home() / ".config" / "kernel-memory" / "config.json"))


def load_config(config_file: Optional[Path] = None) -> Dict[str, Any]:
    """Load configuration from file or environment variables."""
    config = {
        "base_url": os.environ.get("KM_BASE_URL", "http://localhost:9001"),
        "token": os.environ.get("KM_TOKEN", ""),
        "prefix": os.environ.get("KM_TOKEN_PREFIX", ""),
        "default_index": os.environ.get("KM_DEFAULT_INDEX", "default"),
        "timeout": float(os.environ.get("KM_TIMEOUT", "30")),
        "verify_ssl": os.environ.get("KM_VERIFY_SSL",
                                     "true").lower() == "true",
    }

    if config_file and config_file.exists():
        try:
            with open(config_file, "r") as f:
                file_config = json.load(f)
                config.update(file_config)
        except Exception as e:
            typer.echo(f"Error loading config file: {e}", err=True)

    return config


def create_client(
        config: Dict[str, Any]) -> Union[Client, AuthenticatedClient]:
    """Create and configure a client based on the provided configuration."""
    base_url = config.get("base_url")
    timeout = httpx.Timeout(config.get("timeout", 30))
    verify_ssl = config.get("verify_ssl", True)

    token = config.get("token")
    if token:
        return AuthenticatedClient(
            base_url=base_url,
            token=token,
            prefix=config.get("prefix", ""),
            timeout=timeout,
            verify_ssl=verify_ssl,
        )
    else:
        return Client(
            base_url=base_url,
            timeout=timeout,
            verify_ssl=verify_ssl,
        )


@app.command()
def config(
    base_url: Annotated[
        Optional[str],
        typer.Option(help="Base URL for the API")] = "http://localhost:9001",
    prefix: Annotated[Optional[str],
                      typer.Option(help="Token prefix (empty)")] = "",
    default_index: Annotated[Optional[str],
                             typer.Option(
                                 help="Default index to use")] = "default",
    token: Annotated[Optional[str],
                     typer.Option(help="Authentication token")] = None,
    timeout: Annotated[Optional[float],
                       typer.Option(help="Request timeout in seconds")] = None,
    verify_ssl: Annotated[Optional[bool],
                          typer.Option(help="Verify SSL certificates")] = None,
    config_file: Annotated[Optional[Path],
                           typer.Option(help="Path to config file")] = None,
) -> None:
    """Configure the Kernel Memory CLI."""
    config_file = config_file or CONFIG_PATH

    # Ensure directory exists
    config_file.parent.mkdir(parents=True, exist_ok=True)

    # Load existing config or create new one
    if config_file.exists():
        try:
            with open(config_file, "r") as f:
                config = json.load(f)
        except Exception:
            config = {}
    else:
        config = {}

    # Update config with provided values
    if base_url is not None:
        config["base_url"] = base_url
    if token is not None:
        config["token"] = token
    if prefix is not None:
        config["prefix"] = prefix
    if default_index is not None:
        config["default_index"] = default_index
    if timeout is not None:
        config["timeout"] = timeout
    if verify_ssl is not None:
        config["verify_ssl"] = verify_ssl

    # Save config
    with open(config_file, "w") as f:
        json.dump(config, f, indent=2)

    typer.echo(f"Configuration saved to {config_file}")


@app.command(name="show")
def show_config(
    config_file: Annotated[Optional[Path],
                           typer.Option(
                               help="Path to config file")] = CONFIG_PATH,
) -> None:
    """Show the current Kernel Memory CLI configuration."""
    try:
        if not config_file.exists():
            typer.echo(f"Configuration file not found: {config_file}")
            return

        with open(config_file, "r") as f:
            config = json.load(f)

        typer.echo(f"Current configuration ({config_file}):")
        for key, value in config.items():
            if key == "token" and value:
                value = f"{value[:5]}..." if len(value) > 5 else "***"
            typer.echo(f"  {key}: {value}")

    except Exception as e:
        typer.echo(f"Error reading configuration: {e}", err=True)


@app.command()
def upload(
    file_paths: Annotated[List[Path],
                          typer.Argument(
                              help="Path to the file(s) to upload")],
    index: Annotated[Optional[str],
                     typer.Option(help="The index to upload to")] = None,
    document_id: Annotated[Optional[str],
                           typer.Option(help="The document ID")] = None,
    tags: Annotated[Optional[List[str]],
                    typer.Option(help="Tags in format key:value")] = None,
    steps: Annotated[Optional[List[str]],
                     typer.Option(help="Pipeline steps")] = None,
    config_file: Annotated[Optional[Path],
                           typer.Option(
                               help="Path to config file")] = CONFIG_PATH,
) -> None:
    """Upload a document or documents to Kernel Memory."""
    config = load_config(config_file)
    client = create_client(config)

    if not index:
        index = config.get("default_index", "")
        if not index:
            typer.echo(
                "Error: No index specified. Use --index or set a default index in config.",
                err=True)
            raise typer.Exit(1)

    for path in file_paths:
        if path.is_dir():
            # Handle directory upload
            for file in path.glob("**/*"):
                if file.is_file():
                    _upload_single_file(client, file, index, document_id, tags,
                                        steps)
        else:
            # Handle single file upload
            _upload_single_file(client, path, index, document_id, tags, steps)


def _upload_single_file(
    client: Union[Client, AuthenticatedClient],
    file_path: Path,
    index: str,
    document_id: Optional[str] = None,
    tags: Optional[List[str]] = None,
    steps: Optional[List[str]] = None,
) -> None:
    """Upload a single file to Kernel Memory."""
    try:
        # Parse tags into dict format
        tags_dict = {}

        if tags:
            for tag in tags:
                if ":" in tag:
                    key, value = tag.split(":", 1)
                    tags_dict[key] = value
                else:
                    tags_dict[tag] = "true"

        tags = UploadDocumentBodyTags.from_dict(tags_dict)

        # Create the upload document body
        with open(file_path, "rb") as f:
            mime_type = mimetypes.guess_type(
                file_path)[0] or 'application/octet-stream'

            file = File(file_name=file_path.name,
                        payload=f,
                        mime_type=mime_type)

            upload_body = UploadDocumentBody(
                files=[file],  # Use the proper file model
                index=index,
                document_id=document_id or str(file_path),
                tags=tags,
                steps=steps or [],
            )

            result = upload_document(client=client, body=upload_body)

            if result and hasattr(result, "document_id"):
                typer.echo(
                    f"✅ Uploaded {file_path} - Document ID: {result.document_id}"
                )
            else:
                typer.echo(f"✅ Uploaded {file_path}")
    except Exception as e:
        typer.echo(f"❌ Error uploading {file_path}: {e}", err=True)


@app.command()
def search(
    query: Annotated[str, typer.Argument(help="The search query")],
    index: Annotated[Optional[str],
                     typer.Option(help="The index to search in")] = None,
    limit: Annotated[int, typer.Option(help="Maximum number of results")] = 3,
    min_relevance: Annotated[float,
                             typer.Option(
                                 help="Minimum relevance score")] = 0.0,
    filter_tags: Annotated[
        Optional[List[str]],
        typer.Option(help="Filter by tags in format key:value")] = None,
    config_file: Annotated[Optional[Path],
                           typer.Option(
                               help="Path to config file")] = CONFIG_PATH,
) -> None:
    """Search for document snippets in Kernel Memory."""
    config = load_config(config_file)
    client = create_client(config)

    if not index:
        index = config.get("default_index", "")
        if not index:
            typer.echo(
                "Error: No index specified. Use --index or set a default index in config.",
                err=True)
            raise typer.Exit(1)

    try:
        filters = []
        if filter_tags:
            for tag in filter_tags:
                if ":" in tag:
                    key, value = tag.split(":", 1)
                else:
                    key, value = tag, "true"
                filters.append(
                    SearchQueryFiltersType0Item(key=key, value=value))

        search_query = SearchQuery(
            query=query,
            index=index,
            limit=limit,
            min_relevance=min_relevance,
            filters=filters if filters else None,
        )

        result = search_document_snippets(client=client, body=search_query)

        if result and result.results:
            typer.echo(
                typer.style(f"Search results for: '{query}'",
                            fg="blue",
                            bold=True))
            for i, item in enumerate(result.results, 1):
                # Citation objects have partitions that contain relevance score
                relevance = item.partitions[
                    0].relevance if item.partitions and hasattr(
                        item.partitions[0], "relevance") else 0.0

                # Add colored header for each result
                typer.echo(f"\n{typer.style('─' * 50, fg='bright_black')}")
                typer.echo(
                    typer.style(f"Result {i}", fg="green", bold=True) +
                    typer.style(f" (Relevance: {relevance:.4f})", fg="cyan"))
                typer.echo(f"{typer.style('─' * 50, fg='bright_black')}")

                # Document ID with label
                typer.echo(
                    typer.style("Document ID: ", fg="bright_black", bold=True)
                    + typer.style(item.document_id, fg="yellow"))

                # Content with label
                content_text = item.partitions[
                    0].text if item.partitions else 'No content'
                typer.echo(
                    typer.style("Content: ", fg="bright_black", bold=True) +
                    typer.style(content_text, fg="white"))

                # Tags with label if available
                if item.partitions and hasattr(
                        item.partitions[0],
                        "tags") and item.partitions[0].tags:
                    tags_str = ", ".join([
                        f"{k}={v}" for k, v in
                        item.partitions[0].tags.additional_properties.items()
                    ])
                    typer.echo(
                        typer.style("Tags: ", fg="bright_black", bold=True) +
                        typer.style(tags_str, fg="magenta"))
        else:
            typer.echo(typer.style("No results found.", fg="red", bold=True))
    except Exception as e:
        typer.echo(f"❌ Error searching: {e}, {e.with_traceback()}", err=True)


@app.command()
def ask(
    question: Annotated[str, typer.Argument(help="The question to ask")],
    index: Annotated[Optional[str],
                     typer.Option(help="The index to search in")] = None,
    filter_tags: Annotated[
        Optional[List[str]],
        typer.Option(help="Filter by tags in format key:value")] = None,
    config_file: Annotated[Optional[Path],
                           typer.Option(
                               help="Path to config file")] = CONFIG_PATH,
) -> None:
    """Ask a question to Kernel Memory."""
    config = load_config(config_file)
    client = create_client(config)

    if not index:
        index = config.get("default_index", "")
        if not index:
            typer.echo(
                "Error: No index specified. Use --index or set a default index in config.",
                err=True)
            raise typer.Exit(1)

    try:
        filters = []
        if filter_tags:
            for tag in filter_tags:
                if ":" in tag:
                    key, value = tag.split(":", 1)
                else:
                    key, value = tag, "true"
                filters.append(
                    MemoryQueryFiltersType0Item(key=key, value=value))

        memory_query = MemoryQuery(
            question=question,
            index=index,
            filters=filters if filters else None,
        )

        result = answer_question(client=client, body=memory_query)

        if result:
            typer.echo(
                typer.style(f"Question: ", fg="blue", bold=True) + question)

            if hasattr(result, "text") and result.text:
                typer.echo(
                    typer.style("\nAnswer: ", fg="green", bold=True) +
                    result.text)

            if hasattr(result, "relevant_sources") and result.relevant_sources:
                typer.echo(typer.style("\nSources:", fg="yellow", bold=True))
                for i, citation in enumerate(result.relevant_sources, 1):
                    typer.echo(
                        f"  {typer.style(str(i) + '.', fg='yellow')} Document: {typer.style(citation.document_id, fg='cyan')}"
                    )
                    if hasattr(citation, "text") and citation.text:
                        typer.echo(
                            f"     Extract: {typer.style(f'\"{citation.text}\"', fg='white')}"
                        )
        else:
            typer.echo("No answer found.")
    except Exception as e:
        typer.echo(f"❌ Error asking question: {e}", err=True)


@app.command(name="list")
def list_indices(
    config_file: Annotated[Optional[Path],
                           typer.Option(
                               help="Path to config file")] = CONFIG_PATH,
) -> None:
    """List all available indices in Kernel Memory."""
    config = load_config(config_file)
    client = create_client(config)

    try:
        result = list_indexes(client=client)

        if result and result.results:
            typer.echo(typer.style("Available indices:", fg="blue", bold=True))
            for index_detail in result.results:
                if index_detail.name:
                    typer.echo(
                        f"  - {typer.style(index_detail.name, fg='green')}")
        else:
            typer.echo(typer.style("No indices found.", fg="red", bold=True))
    except Exception as e:
        typer.echo(f"❌ Error listing indices: {e}", err=True)


def main():
    """Main entry point for the CLI."""
    app()


if __name__ == "__main__":
    main()
