from http import HTTPStatus
from typing import Any, Optional, Union

import httpx

from ... import errors
from ...client import AuthenticatedClient, Client
from ...models.delete_accepted import DeleteAccepted
from ...models.problem_details import ProblemDetails
from ...types import UNSET, Response, Unset


def _get_kwargs(
    *,
    index: Union[Unset, str] = UNSET,
    document_id: str,
) -> dict[str, Any]:
    params: dict[str, Any] = {}

    params["index"] = index

    params["documentId"] = document_id

    params = {k: v for k, v in params.items() if v is not UNSET and v is not None}

    _kwargs: dict[str, Any] = {
        "method": "delete",
        "url": "/documents",
        "params": params,
    }

    return _kwargs


def _parse_response(
    *, client: Union[AuthenticatedClient, Client], response: httpx.Response
) -> Optional[Union[DeleteAccepted, ProblemDetails]]:
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
    *, client: Union[AuthenticatedClient, Client], response: httpx.Response
) -> Response[Union[DeleteAccepted, ProblemDetails]]:
    return Response(
        status_code=HTTPStatus(response.status_code),
        content=response.content,
        headers=response.headers,
        parsed=_parse_response(client=client, response=response),
    )


def sync_detailed(
    *,
    client: Union[AuthenticatedClient, Client],
    index: Union[Unset, str] = UNSET,
    document_id: str,
) -> Response[Union[DeleteAccepted, ProblemDetails]]:
    """Delete a document from the knowledge base. When deleting a document, which can consist of multiple
    files, all the memories previously extracted are deleted too.

     Delete a document from the knowledge base.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DeleteAccepted, ProblemDetails]]
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
    client: Union[AuthenticatedClient, Client],
    index: Union[Unset, str] = UNSET,
    document_id: str,
) -> Optional[Union[DeleteAccepted, ProblemDetails]]:
    """Delete a document from the knowledge base. When deleting a document, which can consist of multiple
    files, all the memories previously extracted are deleted too.

     Delete a document from the knowledge base.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[DeleteAccepted, ProblemDetails]
    """

    return sync_detailed(
        client=client,
        index=index,
        document_id=document_id,
    ).parsed


async def asyncio_detailed(
    *,
    client: Union[AuthenticatedClient, Client],
    index: Union[Unset, str] = UNSET,
    document_id: str,
) -> Response[Union[DeleteAccepted, ProblemDetails]]:
    """Delete a document from the knowledge base. When deleting a document, which can consist of multiple
    files, all the memories previously extracted are deleted too.

     Delete a document from the knowledge base.

    Args:
        index (Union[Unset, str]):
        document_id (str):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[DeleteAccepted, ProblemDetails]]
    """

    kwargs = _get_kwargs(
        index=index,
        document_id=document_id,
    )

    response = await client.get_async_httpx_client().request(**kwargs)

    return _build_response(client=client, response=response)


async def asyncio(
    *,
    client: Union[AuthenticatedClient, Client],
    index: Union[Unset, str] = UNSET,
    document_id: str,
) -> Optional[Union[DeleteAccepted, ProblemDetails]]:
    """Delete a document from the knowledge base. When deleting a document, which can consist of multiple
    files, all the memories previously extracted are deleted too.

     Delete a document from the knowledge base.

    Args:
        index (Union[Unset, str]):
        document_id (str):

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
            document_id=document_id,
        )
    ).parsed
