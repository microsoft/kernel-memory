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
    from ..models.citation import Citation


T = TypeVar("T", bound="SearchResult")


@_attrs_define
class SearchResult:
    """
    Attributes:
        query (Union[None, Unset, str]):
        no_result (Union[Unset, bool]):
        results (Union[None, Unset, list['Citation']]):
    """

    query: None | Unset | str = UNSET
    no_result: Unset | bool = UNSET
    results: None | Unset | list["Citation"] = UNSET

    def to_dict(self) -> dict[str, Any]:
        query: None | Unset | str
        if isinstance(self.query, Unset):
            query = UNSET
        else:
            query = self.query

        no_result = self.no_result

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
        if query is not UNSET:
            field_dict["query"] = query
        if no_result is not UNSET:
            field_dict["noResult"] = no_result
        if results is not UNSET:
            field_dict["results"] = results

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.citation import Citation

        d = dict(src_dict)

        def _parse_query(data: object) -> None | Unset | str:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        query = _parse_query(d.pop("query", UNSET))

        no_result = d.pop("noResult", UNSET)

        def _parse_results(data: object) -> None | Unset | list["Citation"]:
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
                    results_type_0_item = Citation.from_dict(results_type_0_item_data)

                    results_type_0.append(results_type_0_item)

                return results_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["Citation"]], data)

        results = _parse_results(d.pop("results", UNSET))

        search_result = cls(
            query=query,
            no_result=no_result,
            results=results,
        )

        return search_result
