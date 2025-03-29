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

from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define

from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.index_details import IndexDetails


T = TypeVar("T", bound="IndexCollection")


@_attrs_define
class IndexCollection:
    """
    Attributes:
        results (Union[None, Unset, list['IndexDetails']]):
    """

    results: None | Unset | list["IndexDetails"] = UNSET

    def to_dict(self) -> dict[str, Any]:
        results: None | Unset | list[dict[str, Any]]
        if isinstance(self.results, Unset):
            results = UNSET
        elif isinstance(self.results, list):
            results = []
            for results_type_0_item_data in self.results:
                results_type_0_item = results_type_0_item_data.to_dict()
                results.append(results_type_0_item)

        else:
            results = self.results

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if results is not UNSET:
            field_dict["results"] = results

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.index_details import IndexDetails

        d = dict(src_dict)

        def _parse_results(data: object) -> None | Unset | list["IndexDetails"]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                results_type_0 = []
                _results_type_0 = data
                for results_type_0_item_data in _results_type_0:
                    results_type_0_item = IndexDetails.from_dict(results_type_0_item_data)

                    results_type_0.append(results_type_0_item)

                return results_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["IndexDetails"]], data)

        results = _parse_results(d.pop("results", UNSET))

        index_collection = cls(
            results=results,
        )

        return index_collection
