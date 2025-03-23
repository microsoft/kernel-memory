from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define

from ..models.stream_states import StreamStates
from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.citation import Citation
    from ..models.token_usage import TokenUsage


T = TypeVar("T", bound="MemoryAnswer")


@_attrs_define
class MemoryAnswer:
    """
    Attributes:
        stream_state (Union[Unset, StreamStates]):
        question (Union[None, Unset, str]):
        no_result (Union[Unset, bool]):
        no_result_reason (Union[None, Unset, str]):
        text (Union[None, Unset, str]):
        token_usage (Union[None, Unset, list['TokenUsage']]):
        relevant_sources (Union[None, Unset, list['Citation']]):
    """

    stream_state: Union[Unset, StreamStates] = UNSET
    question: Union[None, Unset, str] = UNSET
    no_result: Union[Unset, bool] = UNSET
    no_result_reason: Union[None, Unset, str] = UNSET
    text: Union[None, Unset, str] = UNSET
    token_usage: Union[None, Unset, list["TokenUsage"]] = UNSET
    relevant_sources: Union[None, Unset, list["Citation"]] = UNSET

    def to_dict(self) -> dict[str, Any]:
        stream_state: Union[Unset, str] = UNSET
        if not isinstance(self.stream_state, Unset):
            stream_state = self.stream_state.value

        question: Union[None, Unset, str]
        if isinstance(self.question, Unset):
            question = UNSET
        else:
            question = self.question

        no_result = self.no_result

        no_result_reason: Union[None, Unset, str]
        if isinstance(self.no_result_reason, Unset):
            no_result_reason = UNSET
        else:
            no_result_reason = self.no_result_reason

        text: Union[None, Unset, str]
        if isinstance(self.text, Unset):
            text = UNSET
        else:
            text = self.text

        token_usage: Union[None, Unset, list[dict[str, Any]]]
        if isinstance(self.token_usage, Unset):
            token_usage = UNSET
        elif isinstance(self.token_usage, list):
            token_usage = []
            for token_usage_type_0_item_data in self.token_usage:
                token_usage_type_0_item = token_usage_type_0_item_data.to_dict()
                token_usage.append(token_usage_type_0_item)

        else:
            token_usage = self.token_usage

        relevant_sources: Union[None, Unset, list[dict[str, Any]]]
        if isinstance(self.relevant_sources, Unset):
            relevant_sources = UNSET
        elif isinstance(self.relevant_sources, list):
            relevant_sources = []
            for relevant_sources_type_0_item_data in self.relevant_sources:
                relevant_sources_type_0_item = relevant_sources_type_0_item_data.to_dict()
                relevant_sources.append(relevant_sources_type_0_item)

        else:
            relevant_sources = self.relevant_sources

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if stream_state is not UNSET:
            field_dict["streamState"] = stream_state
        if question is not UNSET:
            field_dict["question"] = question
        if no_result is not UNSET:
            field_dict["noResult"] = no_result
        if no_result_reason is not UNSET:
            field_dict["noResultReason"] = no_result_reason
        if text is not UNSET:
            field_dict["text"] = text
        if token_usage is not UNSET:
            field_dict["tokenUsage"] = token_usage
        if relevant_sources is not UNSET:
            field_dict["relevantSources"] = relevant_sources

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.citation import Citation
        from ..models.token_usage import TokenUsage

        d = dict(src_dict)
        _stream_state = d.pop("streamState", UNSET)
        stream_state: Union[Unset, StreamStates]
        if isinstance(_stream_state, Unset):
            stream_state = UNSET
        else:
            stream_state = StreamStates(_stream_state)

        def _parse_question(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        question = _parse_question(d.pop("question", UNSET))

        no_result = d.pop("noResult", UNSET)

        def _parse_no_result_reason(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        no_result_reason = _parse_no_result_reason(d.pop("noResultReason", UNSET))

        def _parse_text(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        text = _parse_text(d.pop("text", UNSET))

        def _parse_token_usage(data: object) -> Union[None, Unset, list["TokenUsage"]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                token_usage_type_0 = []
                _token_usage_type_0 = data
                for token_usage_type_0_item_data in _token_usage_type_0:
                    token_usage_type_0_item = TokenUsage.from_dict(token_usage_type_0_item_data)

                    token_usage_type_0.append(token_usage_type_0_item)

                return token_usage_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["TokenUsage"]], data)

        token_usage = _parse_token_usage(d.pop("tokenUsage", UNSET))

        def _parse_relevant_sources(data: object) -> Union[None, Unset, list["Citation"]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                relevant_sources_type_0 = []
                _relevant_sources_type_0 = data
                for relevant_sources_type_0_item_data in _relevant_sources_type_0:
                    relevant_sources_type_0_item = Citation.from_dict(relevant_sources_type_0_item_data)

                    relevant_sources_type_0.append(relevant_sources_type_0_item)

                return relevant_sources_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["Citation"]], data)

        relevant_sources = _parse_relevant_sources(d.pop("relevantSources", UNSET))

        memory_answer = cls(
            stream_state=stream_state,
            question=question,
            no_result=no_result,
            no_result_reason=no_result_reason,
            text=text,
            token_usage=token_usage,
            relevant_sources=relevant_sources,
        )

        return memory_answer
