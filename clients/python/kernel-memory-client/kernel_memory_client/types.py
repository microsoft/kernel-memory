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

"""Contains some shared types for properties"""

from collections.abc import MutableMapping
from http import HTTPStatus
from typing import BinaryIO, Generic, Literal, Optional, TypeVar

from attrs import define


class Unset:
    def __bool__(self) -> Literal[False]:
        return False


UNSET: Unset = Unset()

FileJsonType = tuple[Optional[str], BinaryIO, Optional[str]]


@define
class File:
    """Contains information for file uploads"""

    payload: BinaryIO
    file_name: str | None = None
    mime_type: str | None = None

    def to_tuple(self) -> FileJsonType:
        """Return a tuple representation that httpx will accept for multipart/form-data"""
        return self.file_name, self.payload, self.mime_type


T = TypeVar("T")


@define
class Response(Generic[T]):
    """A response from an endpoint"""

    status_code: HTTPStatus
    content: bytes
    headers: MutableMapping[str, str]
    parsed: T | None


__all__ = ["UNSET", "File", "FileJsonType", "Response", "Unset"]
