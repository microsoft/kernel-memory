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
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define
from dateutil.parser import isoparse

from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.partition_tags_type_0 import PartitionTagsType0


T = TypeVar("T", bound="Partition")


@_attrs_define
class Partition:
    """
    Attributes:
        text (Union[None, Unset, str]):
        relevance (Union[Unset, float]):
        partition_number (Union[Unset, int]):
        section_number (Union[Unset, int]):
        last_update (Union[Unset, datetime.datetime]):
        tags (Union['PartitionTagsType0', None, Unset]):
    """

    text: None | Unset | str = UNSET
    relevance: Unset | float = UNSET
    partition_number: Unset | int = UNSET
    section_number: Unset | int = UNSET
    last_update: Unset | datetime.datetime = UNSET
    tags: Union["PartitionTagsType0", None, Unset] = UNSET

    def to_dict(self) -> dict[str, Any]:
        from ..models.partition_tags_type_0 import PartitionTagsType0

        text: None | Unset | str
        if isinstance(self.text, Unset):
            text = UNSET
        else:
            text = self.text

        relevance = self.relevance

        partition_number = self.partition_number

        section_number = self.section_number

        last_update: Unset | str = UNSET
        if not isinstance(self.last_update, Unset):
            last_update = self.last_update.isoformat()

        tags: None | Unset | dict[str, Any]
        if isinstance(self.tags, Unset):
            tags = UNSET
        elif isinstance(self.tags, PartitionTagsType0):
            tags = self.tags.to_dict()
        else:
            tags = self.tags

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if text is not UNSET:
            field_dict["text"] = text
        if relevance is not UNSET:
            field_dict["relevance"] = relevance
        if partition_number is not UNSET:
            field_dict["partitionNumber"] = partition_number
        if section_number is not UNSET:
            field_dict["sectionNumber"] = section_number
        if last_update is not UNSET:
            field_dict["lastUpdate"] = last_update
        if tags is not UNSET:
            field_dict["tags"] = tags

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.partition_tags_type_0 import PartitionTagsType0

        d = dict(src_dict)

        def _parse_text(data: object) -> None | Unset | str:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        text = _parse_text(d.pop("text", UNSET))

        relevance = d.pop("relevance", UNSET)

        partition_number = d.pop("partitionNumber", UNSET)

        section_number = d.pop("sectionNumber", UNSET)

        _last_update = d.pop("lastUpdate", UNSET)
        last_update: Unset | datetime.datetime
        if isinstance(_last_update, Unset):
            last_update = UNSET
        else:
            last_update = isoparse(_last_update)

        def _parse_tags(data: object) -> Union["PartitionTagsType0", None, Unset]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, dict):
                    raise TypeError()
                tags_type_0 = PartitionTagsType0.from_dict(data)

                return tags_type_0
            except:  # noqa: E722
                pass
            return cast(Union["PartitionTagsType0", None, Unset], data)

        tags = _parse_tags(d.pop("tags", UNSET))

        partition = cls(
            text=text,
            relevance=relevance,
            partition_number=partition_number,
            section_number=section_number,
            last_update=last_update,
            tags=tags,
        )

        return partition
