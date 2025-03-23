from datetime import timedelta
import json
from typing import Annotated, List, Dict, Any, Optional, Union

from semantic_kernel.functions import kernel_function

from kernel_memory_client import Client, AuthenticatedClient
from kernel_memory_client.models import SearchQuery, SearchQueryFiltersType0Item
from kernel_memory_client.api.microsoft_kernel_memory_service_assembly import search_document_snippets


class MemoryPlugin:
    """
    Kernel Memory Plugin
    
    Recommended name: "memory"
    
    Functions:
    * memory.search
    """

    def __init__(self,
                 memory_client: Union[Client, AuthenticatedClient, str],
                 default_index: str = "",
                 default_retrieval_tags: Dict[str, List[str]] = None):
        """
        Initialize a new instance of the MemoryPlugin class.
        
        Args:
            memory_client: The memory client or service URL
            default_index: Default Memory Index to use when none is specified
            default_retrieval_tags: Default Tags to require when searching memories
        """
        self._default_index = default_index
        self._default_retrieval_tags = default_retrieval_tags or {}

        # Initialize the memory client
        if isinstance(memory_client, str):
            self._memory = Client(base_url=memory_client)
        else:
            self._memory = memory_client

    @kernel_function(
        name="search",
        description="Return up to N memories related to the input text")
    def search(
        self,
        query: Annotated[str, "The text to search in memory"],
        index: Annotated[str,
                         "Memories index container to search for information"],
        min_relevance: Annotated[
            float, "Minimum relevance of the memories to return"] = 0.0,
        limit: Annotated[int, "Maximum number of memories to return"] = 1,
        tags: Annotated[Optional[Dict[str, List[str]]],
                        "Memories key-value tags to filter information"] = None
    ) -> str:
        """
        Return up to N memories related to the input text.
        
        Returns:
            str: JSON string containing the list of memories
        """
        index = index or self._default_index
        tags = tags or self._default_retrieval_tags

        # Create search filters from tags
        filter_items = []
        if tags:
            for key, values in tags.items():
                for value in values:
                    filter_items.append(
                        SearchQueryFiltersType0Item(key=key, value=value))

        # Create the search query
        search_query = SearchQuery(index=index,
                                   query=query,
                                   filters=filter_items,
                                   min_relevance=min_relevance,
                                   limit=limit)

        # Execute the search
        result = search_document_snippets.sync(client=self._memory,
                                               body=search_query)

        # Check if we have results
        if not result or not hasattr(result, 'results') or len(
                result.results) == 0:
            return json.dumps({"results": []})

        # Return the results as JSON
        return json.dumps(result.to_dict())
