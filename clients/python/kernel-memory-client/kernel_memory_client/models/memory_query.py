from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define

from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.memory_query_args_type_0 import MemoryQueryArgsType0
    from ..models.memory_query_filters_type_0_item import MemoryQueryFiltersType0Item


T = TypeVar("T", bound="MemoryQuery")


@_attrs_define
class MemoryQuery:
    """
    Attributes:
        index (Union[None, Unset, str]):
        question (Union[None, Unset, str]):
        filters (Union[None, Unset, list['MemoryQueryFiltersType0Item']]):
        min_relevance (Union[Unset, float]):
        stream (Union[Unset, bool]):
        args (Union['MemoryQueryArgsType0', None, Unset]):
    """

    index: Union[None, Unset, str] = UNSET
    question: Union[None, Unset, str] = UNSET
    filters: Union[None, Unset, list["MemoryQueryFiltersType0Item"]] = UNSET
    min_relevance: Union[Unset, float] = UNSET
    stream: Union[Unset, bool] = UNSET
    args: Union["MemoryQueryArgsType0", None, Unset] = UNSET

    def to_dict(self) -> dict[str, Any]:
        from ..models.memory_query_args_type_0 import MemoryQueryArgsType0

        index: Union[None, Unset, str]
        if isinstance(self.index, Unset):
            index = UNSET
        else:
            index = self.index

        question: Union[None, Unset, str]
        if isinstance(self.question, Unset):
            question = UNSET
        else:
            question = self.question

        filters: Union[None, Unset, list[dict[str, Any]]]
        if isinstance(self.filters, Unset):
            filters = UNSET
        elif isinstance(self.filters, list):
            filters = []
            for filters_type_0_item_data in self.filters:
                filters_type_0_item = filters_type_0_item_data.to_dict()
                filters.append(filters_type_0_item)

        else:
            filters = self.filters

        min_relevance = self.min_relevance

        stream = self.stream

        args: Union[None, Unset, dict[str, Any]]
        if isinstance(self.args, Unset):
            args = UNSET
        elif isinstance(self.args, MemoryQueryArgsType0):
            args = self.args.to_dict()
        else:
            args = self.args

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if index is not UNSET:
            field_dict["index"] = index
        if question is not UNSET:
            field_dict["question"] = question
        if filters is not UNSET:
            field_dict["filters"] = filters
        if min_relevance is not UNSET:
            field_dict["minRelevance"] = min_relevance
        if stream is not UNSET:
            field_dict["stream"] = stream
        if args is not UNSET:
            field_dict["args"] = args

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.memory_query_args_type_0 import MemoryQueryArgsType0
        from ..models.memory_query_filters_type_0_item import MemoryQueryFiltersType0Item

        d = dict(src_dict)

        def _parse_index(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        index = _parse_index(d.pop("index", UNSET))

        def _parse_question(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        question = _parse_question(d.pop("question", UNSET))

        def _parse_filters(data: object) -> Union[None, Unset, list["MemoryQueryFiltersType0Item"]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                filters_type_0 = []
                _filters_type_0 = data
                for filters_type_0_item_data in _filters_type_0:
                    filters_type_0_item = MemoryQueryFiltersType0Item.from_dict(filters_type_0_item_data)

                    filters_type_0.append(filters_type_0_item)

                return filters_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["MemoryQueryFiltersType0Item"]], data)

        filters = _parse_filters(d.pop("filters", UNSET))

        min_relevance = d.pop("minRelevance", UNSET)

        stream = d.pop("stream", UNSET)

        def _parse_args(data: object) -> Union["MemoryQueryArgsType0", None, Unset]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, dict):
                    raise TypeError()
                args_type_0 = MemoryQueryArgsType0.from_dict(data)

                return args_type_0
            except:  # noqa: E722
                pass
            return cast(Union["MemoryQueryArgsType0", None, Unset], data)

        args = _parse_args(d.pop("args", UNSET))

        memory_query = cls(
            index=index,
            question=question,
            filters=filters,
            min_relevance=min_relevance,
            stream=stream,
            args=args,
        )

        return memory_query
