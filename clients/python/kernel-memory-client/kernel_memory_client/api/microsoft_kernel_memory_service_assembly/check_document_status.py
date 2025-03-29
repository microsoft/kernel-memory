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

from http import HTTPStatus
from typing import Any

import httpx

from ... import errors
from ...client import AuthenticatedClient, Client
from ...models.data_pipeline_status import DataPipelineStatus
from ...models.problem_details import ProblemDetails
from ...types import UNSET, Response, Unset


def _get_kwargs(
    *,
    index: Unset | str = UNSET,
    document_id: str,
) -> dict[str, Any]:
    params: dict[str, Any] = {}

    params["index"] = index

    params["documentId"] = document_id

    params = {k: v for k, v in params.items() if v is not UNSET and v is not None}

    _kwargs: dict[str, Any] = {
        "method": "get",
        "url": "/upload-status",
        "params": params,
    }

    return _kwargs


def _parse_response(
    *, client: AuthenticatedClient | Client, response: httpx.Response
) -> DataPipelineStatus | ProblemDetails | None:
    if response.status_code == 200:
        response_200 = DataPipelineStatus.from_dict(response.json())

        return response_200
    if response.status_code == 400:
        response_400 = ProblemDetails.from_dict(response.json())

        return response_400
    if response.status_code == 401:
        response_401 = ProblemDetails.from_dict(response.json())

        return response_401
    if response.status_code == 403:
        response_403 = ProblemDetails.from_dict(response.json())

        return response_403
    if response.status_code == 404:
        response_404 = ProblemDetails.from_dict(response.json())

        return response_404
    if response.status_code == 413:
        response_413 = ProblemDetails.from_dict(response.json())

        return response_413
    if client.raise_on_unexpected_status:
        raise errors.UnexpectedStatus(response.status_code, response.content)
    else:
        return None


def _build_response(
    *, client: AuthenticatedClient | Client, response: httpx.Response
) -> Response[DataPipelineStatus | ProblemDetails]:
    return Response(
        status_code=HTTPStatus(response.status_code),
        content=response.content,
        headers=response.headers,
        parsed=_parse_response(client=client, response=response),
    )


def sync_detailed(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
    document_id: str,
) -> Response[DataPipelineStatus | ProblemDetails]:
    """Check the status of a file upload in progress. When uploading a document, which can consist of
    multiple files, each file goes through multiple steps. The status include details about which steps
    are completed.

     Check the status of a file upload in progress.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DataPipelineStatus, ProblemDetails]]
    """

    kwargs = _get_kwargs(
        index=index,
        document_id=document_id,
    )

    response = client.get_httpx_client().request(
        **kwargs,
    )

    return _build_response(client=client, response=response)


def sync(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
    document_id: str,
) -> DataPipelineStatus | ProblemDetails | None:
    """Check the status of a file upload in progress. When uploading a document, which can consist of
    multiple files, each file goes through multiple steps. The status include details about which steps
    are completed.

     Check the status of a file upload in progress.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[DataPipelineStatus, ProblemDetails]
    """

    return sync_detailed(
        client=client,
        index=index,
        document_id=document_id,
    ).parsed


async def asyncio_detailed(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
    document_id: str,
) -> Response[DataPipelineStatus | ProblemDetails]:
    """Check the status of a file upload in progress. When uploading a document, which can consist of
    multiple files, each file goes through multiple steps. The status include details about which steps
    are completed.

     Check the status of a file upload in progress.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DataPipelineStatus, ProblemDetails]]
    """

    kwargs = _get_kwargs(
        index=index,
        document_id=document_id,
    )

    response = await client.get_async_httpx_client().request(**kwargs)

    return _build_response(client=client, response=response)


async def asyncio(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
    document_id: str,
) -> DataPipelineStatus | ProblemDetails | None:
    """Check the status of a file upload in progress. When uploading a document, which can consist of
    multiple files, each file goes through multiple steps. The status include details about which steps
    are completed.

     Check the status of a file upload in progress.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[DataPipelineStatus, ProblemDetails]
    """

    return (
        await asyncio_detailed(
            client=client,
            index=index,
            document_id=document_id,
        )
    ).parsed
