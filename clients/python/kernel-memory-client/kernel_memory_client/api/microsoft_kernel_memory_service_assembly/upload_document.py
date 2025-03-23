from http import HTTPStatus
from typing import Any, Optional, Union, cast

import httpx

from ... import errors
from ...client import AuthenticatedClient, Client
from ...models.problem_details import ProblemDetails
from ...models.upload_accepted import UploadAccepted
from ...models.upload_document_body import UploadDocumentBody
from ...types import Response


def _get_kwargs(
    *,
    body: UploadDocumentBody,
) -> dict[str, Any]:
    headers: dict[str, Any] = {}

    _kwargs: dict[str, Any] = {
        "method": "post",
        "url": "/upload",
    }

    _body = body.to_multipart()

    _kwargs["files"] = _body

    _kwargs["headers"] = headers
    return _kwargs


def _parse_response(
    *, client: Union[AuthenticatedClient, Client], response: httpx.Response
) -> Optional[Union[Any, ProblemDetails, UploadAccepted]]:
    if response.status_code == 200:
        response_200 = cast(Any, None)
        return response_200
    if response.status_code == 202:
        response_202 = UploadAccepted.from_dict(response.json())

        return response_202
    if response.status_code == 400:
        response_400 = ProblemDetails.from_dict(response.json())

        return response_400
    if response.status_code == 401:
        response_401 = ProblemDetails.from_dict(response.json())

        return response_401
    if response.status_code == 403:
        response_403 = ProblemDetails.from_dict(response.json())

        return response_403
    if response.status_code == 503:
        response_503 = ProblemDetails.from_dict(response.json())

        return response_503
    if client.raise_on_unexpected_status:
        raise errors.UnexpectedStatus(response.status_code, response.content)
    else:
        return None


def _build_response(
    *, client: Union[AuthenticatedClient, Client], response: httpx.Response
) -> Response[Union[Any, ProblemDetails, UploadAccepted]]:
    return Response(
        status_code=HTTPStatus(response.status_code),
        content=response.content,
        headers=response.headers,
        parsed=_parse_response(client=client, response=response),
    )


def sync_detailed(
    *,
    client: Union[AuthenticatedClient, Client],
    body: UploadDocumentBody,
) -> Response[Union[Any, ProblemDetails, UploadAccepted]]:
    """Upload a new document to the knowledge base

     Upload a document consisting of one or more files to extract memories from. The extraction process
    happens asynchronously. If a document with the same ID already exists, it will be overwritten and
    the memories previously extracted will be updated.

    Args:
        body (UploadDocumentBody):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[Any, ProblemDetails, UploadAccepted]]
    """

    kwargs = _get_kwargs(
        body=body,
    )

    response = client.get_httpx_client().request(
        **kwargs,
    )

    return _build_response(client=client, response=response)


def sync(
    *,
    client: Union[AuthenticatedClient, Client],
    body: UploadDocumentBody,
) -> Optional[Union[Any, ProblemDetails, UploadAccepted]]:
    """Upload a new document to the knowledge base

     Upload a document consisting of one or more files to extract memories from. The extraction process
    happens asynchronously. If a document with the same ID already exists, it will be overwritten and
    the memories previously extracted will be updated.

    Args:
        body (UploadDocumentBody):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[Any, ProblemDetails, UploadAccepted]
    """

    return sync_detailed(
        client=client,
        body=body,
    ).parsed


async def asyncio_detailed(
    *,
    client: Union[AuthenticatedClient, Client],
    body: UploadDocumentBody,
) -> Response[Union[Any, ProblemDetails, UploadAccepted]]:
    """Upload a new document to the knowledge base

     Upload a document consisting of one or more files to extract memories from. The extraction process
    happens asynchronously. If a document with the same ID already exists, it will be overwritten and
    the memories previously extracted will be updated.

    Args:
        body (UploadDocumentBody):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Response[Union[Any, ProblemDetails, UploadAccepted]]
    """

    kwargs = _get_kwargs(
        body=body,
    )

    response = await client.get_async_httpx_client().request(**kwargs)

    return _build_response(client=client, response=response)


async def asyncio(
    *,
    client: Union[AuthenticatedClient, Client],
    body: UploadDocumentBody,
) -> Optional[Union[Any, ProblemDetails, UploadAccepted]]:
    """Upload a new document to the knowledge base

     Upload a document consisting of one or more files to extract memories from. The extraction process
    happens asynchronously. If a document with the same ID already exists, it will be overwritten and
    the memories previously extracted will be updated.

    Args:
        body (UploadDocumentBody):

    Raises:
        errors.UnexpectedStatus: If the server returns an undocumented status code and Client.raise_on_unexpected_status is True.
        httpx.TimeoutException: If the request takes longer than Client.timeout.

    Returns:
        Union[Any, ProblemDetails, UploadAccepted]
    """

    return (
        await asyncio_detailed(
            client=client,
            body=body,
        )
    ).parsed
