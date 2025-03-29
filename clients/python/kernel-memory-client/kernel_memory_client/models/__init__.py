# Copyright (c) 2025 Microsoft
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of
# this software and associated documentation files (the "Software"), to deal in
# the Software without restriction, including without limitation the rights to
# use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
# the Software, and to permit persons to whom the Software is furnished to do so,
# subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
# FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
# COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
# IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
# CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

"""Contains all the data models used in inputs/outputs"""

from .citation import Citation
from .data_pipeline_status import DataPipelineStatus
from .data_pipeline_status_tags_type_0 import DataPipelineStatusTagsType0
from .delete_accepted import DeleteAccepted
from .index_collection import IndexCollection
from .index_details import IndexDetails
from .memory_answer import MemoryAnswer
from .memory_query import MemoryQuery
from .memory_query_args_type_0 import MemoryQueryArgsType0
from .memory_query_filters_type_0_item import MemoryQueryFiltersType0Item
from .partition import Partition
from .partition_tags_type_0 import PartitionTagsType0
from .problem_details import ProblemDetails
from .search_query import SearchQuery
from .search_query_args_type_0 import SearchQueryArgsType0
from .search_query_filters_type_0_item import SearchQueryFiltersType0Item
from .search_result import SearchResult
from .stream_states import StreamStates
from .token_usage import TokenUsage
from .upload_accepted import UploadAccepted
from .upload_document_body import UploadDocumentBody
from .upload_document_body_tags import UploadDocumentBodyTags

__all__ = (
    "Citation",
    "DataPipelineStatus",
    "DataPipelineStatusTagsType0",
    "DeleteAccepted",
    "IndexCollection",
    "IndexDetails",
    "MemoryAnswer",
    "MemoryQuery",
    "MemoryQueryArgsType0",
    "MemoryQueryFiltersType0Item",
    "Partition",
    "PartitionTagsType0",
    "ProblemDetails",
    "SearchQuery",
    "SearchQueryArgsType0",
    "SearchQueryFiltersType0Item",
    "SearchResult",
    "StreamStates",
    "TokenUsage",
    "UploadAccepted",
    "UploadDocumentBody",
    "UploadDocumentBodyTags",
)
