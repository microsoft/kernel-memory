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
from ...models.delete_accepted import DeleteAccepted
from ...models.problem_details import ProblemDetails
from ...types import UNSET, Response, Unset


def _get_kwargs(
    *,
    index: Unset | str = UNSET,
) -> dict[str, Any]:
    params: dict[str, Any] = {}

    params["index"] = index

    params = {k: v for k, v in params.items() if v is not UNSET and v is not None}

    _kwargs: dict[str, Any] = {
        "method": "delete",
        "url": "/indexes",
        "params": params,
    }

    return _kwargs


def _parse_response(
    *, client: AuthenticatedClient | Client, response: httpx.Response
) -> DeleteAccepted | ProblemDetails | None:
    if response.status_code == 202:
        response_202 = DeleteAccepted.from_dict(response.json())

        return response_202
    if response.status_code == 401:
        response_401 = ProblemDetails.from_dict(response.json())

        return response_401
    if response.status_code == 403:
        response_403 = ProblemDetails.from_dict(response.json())

        return response_403
    if client.raise_on_unexpected_status:
        raise errors.UnexpectedStatus(response.status_code, response.content)
    else:
        return None


def _build_response(
    *, client: AuthenticatedClient | Client, response: httpx.Response
) -> Response[DeleteAccepted | ProblemDetails]:
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
) -> Response[DeleteAccepted | ProblemDetails]:
    """Delete a container of documents (aka 'index') from the knowledge base. Indexes are collections of
    memories extracted from the documents uploaded.

     Delete a container of documents (aka 'index') from the knowledge base.

    Args:
        index (Union[Unset, str]):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DeleteAccepted, ProblemDetails]]
    """

    kwargs = _get_kwargs(
        index=index,
    )

    response = client.get_httpx_client().request(
        **kwargs,
    )

    return _build_response(client=client, response=response)


def sync(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
) -> DeleteAccepted | ProblemDetails | None:
    """Delete a container of documents (aka 'index') from the knowledge base. Indexes are collections of
    memories extracted from the documents uploaded.

     Delete a container of documents (aka 'index') from the knowledge base.

    Args:
        index (Union[Unset, str]):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[DeleteAccepted, ProblemDetails]
    """

    return sync_detailed(
        client=client,
        index=index,
    ).parsed


async def asyncio_detailed(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
) -> Response[DeleteAccepted | ProblemDetails]:
    """Delete a container of documents (aka 'index') from the knowledge base. Indexes are collections of
    memories extracted from the documents uploaded.

     Delete a container of documents (aka 'index') from the knowledge base.

    Args:
        index (Union[Unset, str]):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DeleteAccepted, ProblemDetails]]
    """

    kwargs = _get_kwargs(
        index=index,
    )

    response = await client.get_async_httpx_client().request(**kwargs)

    return _build_response(client=client, response=response)


async def asyncio(
    *,
    client: AuthenticatedClient | Client,
    index: Unset | str = UNSET,
) -> DeleteAccepted | ProblemDetails | None:
    """Delete a container of documents (aka 'index') from the knowledge base. Indexes are collections of
    memories extracted from the documents uploaded.

     Delete a container of documents (aka 'index') from the knowledge base.

    Args:
        index (Union[Unset, str]):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[DeleteAccepted, ProblemDetails]
    """

    return (
        await asyncio_detailed(
            client=client,
            index=index,
        )
    ).parsed
