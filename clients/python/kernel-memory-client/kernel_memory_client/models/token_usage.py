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

import datetime
from collections.abc import Mapping
from typing import Any, TypeVar, Union, cast

from attrs import define as _attrs_define
from dateutil.parser import isoparse

from ..types import UNSET, Unset

T = TypeVar("T", bound="TokenUsage")


@_attrs_define
class TokenUsage:
    """
    Attributes:
        timestamp (Union[Unset, datetime.datetime]):
        service_type (Union[None, Unset, str]):
        model_type (Union[None, Unset, str]):
        model_name (Union[None, Unset, str]):
        tokenizer_tokens_in (Union[None, Unset, int]):
        tokenizer_tokens_out (Union[None, Unset, int]):
        service_tokens_in (Union[None, Unset, int]):
        service_tokens_out (Union[None, Unset, int]):
        service_reasoning_tokens (Union[None, Unset, int]):
    """

    timestamp: Unset | datetime.datetime = UNSET
    service_type: None | Unset | str = UNSET
    model_type: None | Unset | str = UNSET
    model_name: None | Unset | str = UNSET
    tokenizer_tokens_in: None | Unset | int = UNSET
    tokenizer_tokens_out: None | Unset | int = UNSET
    service_tokens_in: None | Unset | int = UNSET
    service_tokens_out: None | Unset | int = UNSET
    service_reasoning_tokens: None | Unset | int = UNSET

    def to_dict(self) -> dict[str, Any]:
        timestamp: Unset | str = UNSET
        if not isinstance(self.timestamp, Unset):
            timestamp = self.timestamp.isoformat()

        service_type: None | Unset | str
        if isinstance(self.service_type, Unset):
            service_type = UNSET
        else:
            service_type = self.service_type

        model_type: None | Unset | str
        if isinstance(self.model_type, Unset):
            model_type = UNSET
        else:
            model_type = self.model_type

        model_name: None | Unset | str
        if isinstance(self.model_name, Unset):
            model_name = UNSET
        else:
            model_name = self.model_name

        tokenizer_tokens_in: None | Unset | int
        if isinstance(self.tokenizer_tokens_in, Unset):
            tokenizer_tokens_in = UNSET
        else:
            tokenizer_tokens_in = self.tokenizer_tokens_in

        tokenizer_tokens_out: None | Unset | int
        if isinstance(self.tokenizer_tokens_out, Unset):
            tokenizer_tokens_out = UNSET
        else:
            tokenizer_tokens_out = self.tokenizer_tokens_out

        service_tokens_in: None | Unset | int
        if isinstance(self.service_tokens_in, Unset):
            service_tokens_in = UNSET
        else:
            service_tokens_in = self.service_tokens_in

        service_tokens_out: None | Unset | int
        if isinstance(self.service_tokens_out, Unset):
            service_tokens_out = UNSET
        else:
            service_tokens_out = self.service_tokens_out

        service_reasoning_tokens: None | Unset | int
        if isinstance(self.service_reasoning_tokens, Unset):
            service_reasoning_tokens = UNSET
        else:
            service_reasoning_tokens = self.service_reasoning_tokens

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if timestamp is not UNSET:
            field_dict["timestamp"] = timestamp
        if service_type is not UNSET:
            field_dict["serviceType"] = service_type
        if model_type is not UNSET:
            field_dict["modelType"] = model_type
        if model_name is not UNSET:
            field_dict["modelName"] = model_name
        if tokenizer_tokens_in is not UNSET:
            field_dict["tokenizerTokensIn"] = tokenizer_tokens_in
        if tokenizer_tokens_out is not UNSET:
            field_dict["tokenizerTokensOut"] = tokenizer_tokens_out
        if service_tokens_in is not UNSET:
            field_dict["serviceTokensIn"] = service_tokens_in
        if service_tokens_out is not UNSET:
            field_dict["serviceTokensOut"] = service_tokens_out
        if service_reasoning_tokens is not UNSET:
            field_dict["serviceReasoningTokens"] = service_reasoning_tokens

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        d = dict(src_dict)
        _timestamp = d.pop("timestamp", UNSET)
        timestamp: Unset | datetime.datetime
        if isinstance(_timestamp, Unset):
            timestamp = UNSET
        else:
            timestamp = isoparse(_timestamp)

        def _parse_service_type(data: object) -> None | Unset | str:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        service_type = _parse_service_type(d.pop("serviceType", UNSET))

        def _parse_model_type(data: object) -> None | Unset | str:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        model_type = _parse_model_type(d.pop("modelType", UNSET))

        def _parse_model_name(data: object) -> None | Unset | str:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        model_name = _parse_model_name(d.pop("modelName", UNSET))

        def _parse_tokenizer_tokens_in(data: object) -> None | Unset | int:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, int], data)

        tokenizer_tokens_in = _parse_tokenizer_tokens_in(d.pop("tokenizerTokensIn", UNSET))

        def _parse_tokenizer_tokens_out(data: object) -> None | Unset | int:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, int], data)

        tokenizer_tokens_out = _parse_tokenizer_tokens_out(d.pop("tokenizerTokensOut", UNSET))

        def _parse_service_tokens_in(data: object) -> None | Unset | int:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, int], data)

        service_tokens_in = _parse_service_tokens_in(d.pop("serviceTokensIn", UNSET))

        def _parse_service_tokens_out(data: object) -> None | Unset | int:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, int], data)

        service_tokens_out = _parse_service_tokens_out(d.pop("serviceTokensOut", UNSET))

        def _parse_service_reasoning_tokens(data: object) -> None | Unset | int:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, int], data)

        service_reasoning_tokens = _parse_service_reasoning_tokens(d.pop("serviceReasoningTokens", UNSET))

        token_usage = cls(
            timestamp=timestamp,
            service_type=service_type,
            model_type=model_type,
            model_name=model_name,
            tokenizer_tokens_in=tokenizer_tokens_in,
            tokenizer_tokens_out=tokenizer_tokens_out,
            service_tokens_in=service_tokens_in,
            service_tokens_out=service_tokens_out,
            service_reasoning_tokens=service_reasoning_tokens,
        )

        return token_usage
