import datetime
from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define
from dateutil.parser import isoparse

from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.data_pipeline_status_tags_type_0 import DataPipelineStatusTagsType0


T = TypeVar("T", bound="DataPipelineStatus")


@_attrs_define
class DataPipelineStatus:
    """
    Attributes:
        completed (Union[Unset, bool]):
        empty (Union[Unset, bool]):
        index (Union[None, Unset, str]):
        document_id (Union[None, Unset, str]):
        tags (Union['DataPipelineStatusTagsType0', None, Unset]):
        creation (Union[Unset, datetime.datetime]):
        last_update (Union[Unset, datetime.datetime]):
        steps (Union[None, Unset, list[str]]):
        remaining_steps (Union[None, Unset, list[str]]):
        completed_steps (Union[None, Unset, list[str]]):
    """

    completed: Union[Unset, bool] = UNSET
    empty: Union[Unset, bool] = UNSET
    index: Union[None, Unset, str] = UNSET
    document_id: Union[None, Unset, str] = UNSET
    tags: Union["DataPipelineStatusTagsType0", None, Unset] = UNSET
    creation: Union[Unset, datetime.datetime] = UNSET
    last_update: Union[Unset, datetime.datetime] = UNSET
    steps: Union[None, Unset, list[str]] = UNSET
    remaining_steps: Union[None, Unset, list[str]] = UNSET
    completed_steps: Union[None, Unset, list[str]] = UNSET

    def to_dict(self) -> dict[str, Any]:
        from ..models.data_pipeline_status_tags_type_0 import DataPipelineStatusTagsType0

        completed = self.completed

        empty = self.empty

        index: Union[None, Unset, str]
        if isinstance(self.index, Unset):
            index = UNSET
        else:
            index = self.index

        document_id: Union[None, Unset, str]
        if isinstance(self.document_id, Unset):
            document_id = UNSET
        else:
            document_id = self.document_id

        tags: Union[None, Unset, dict[str, Any]]
        if isinstance(self.tags, Unset):
            tags = UNSET
        elif isinstance(self.tags, DataPipelineStatusTagsType0):
            tags = self.tags.to_dict()
        else:
            tags = self.tags

        creation: Union[Unset, str] = UNSET
        if not isinstance(self.creation, Unset):
            creation = self.creation.isoformat()

        last_update: Union[Unset, str] = UNSET
        if not isinstance(self.last_update, Unset):
            last_update = self.last_update.isoformat()

        steps: Union[None, Unset, list[str]]
        if isinstance(self.steps, Unset):
            steps = UNSET
        elif isinstance(self.steps, list):
            steps = self.steps

        else:
            steps = self.steps

        remaining_steps: Union[None, Unset, list[str]]
        if isinstance(self.remaining_steps, Unset):
            remaining_steps = UNSET
        elif isinstance(self.remaining_steps, list):
            remaining_steps = self.remaining_steps

        else:
            remaining_steps = self.remaining_steps

        completed_steps: Union[None, Unset, list[str]]
        if isinstance(self.completed_steps, Unset):
            completed_steps = UNSET
        elif isinstance(self.completed_steps, list):
            completed_steps = self.completed_steps

        else:
            completed_steps = self.completed_steps

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if completed is not UNSET:
            field_dict["completed"] = completed
        if empty is not UNSET:
            field_dict["empty"] = empty
        if index is not UNSET:
            field_dict["index"] = index
        if document_id is not UNSET:
            field_dict["document_id"] = document_id
        if tags is not UNSET:
            field_dict["tags"] = tags
        if creation is not UNSET:
            field_dict["creation"] = creation
        if last_update is not UNSET:
            field_dict["last_update"] = last_update
        if steps is not UNSET:
            field_dict["steps"] = steps
        if remaining_steps is not UNSET:
            field_dict["remaining_steps"] = remaining_steps
        if completed_steps is not UNSET:
            field_dict["completed_steps"] = completed_steps

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.data_pipeline_status_tags_type_0 import DataPipelineStatusTagsType0

        d = dict(src_dict)
        completed = d.pop("completed", UNSET)

        empty = d.pop("empty", UNSET)

        def _parse_index(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        index = _parse_index(d.pop("index", UNSET))

        def _parse_document_id(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        document_id = _parse_document_id(d.pop("document_id", UNSET))

        def _parse_tags(data: object) -> Union["DataPipelineStatusTagsType0", None, Unset]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, dict):
                    raise TypeError()
                tags_type_0 = DataPipelineStatusTagsType0.from_dict(data)

                return tags_type_0
            except:  # noqa: E722
                pass
            return cast(Union["DataPipelineStatusTagsType0", None, Unset], data)

        tags = _parse_tags(d.pop("tags", UNSET))

        _creation = d.pop("creation", UNSET)
        creation: Union[Unset, datetime.datetime]
        if isinstance(_creation, Unset):
            creation = UNSET
        else:
            creation = isoparse(_creation)

        _last_update = d.pop("last_update", UNSET)
        last_update: Union[Unset, datetime.datetime]
        if isinstance(_last_update, Unset):
            last_update = UNSET
        else:
            last_update = isoparse(_last_update)

        def _parse_steps(data: object) -> Union[None, Unset, list[str]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                steps_type_0 = cast(list[str], data)

                return steps_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list[str]], data)

        steps = _parse_steps(d.pop("steps", UNSET))

        def _parse_remaining_steps(data: object) -> Union[None, Unset, list[str]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                remaining_steps_type_0 = cast(list[str], data)

                return remaining_steps_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list[str]], data)

        remaining_steps = _parse_remaining_steps(d.pop("remaining_steps", UNSET))

        def _parse_completed_steps(data: object) -> Union[None, Unset, list[str]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                completed_steps_type_0 = cast(list[str], data)

                return completed_steps_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list[str]], data)

        completed_steps = _parse_completed_steps(d.pop("completed_steps", UNSET))

        data_pipeline_status = cls(
            completed=completed,
            empty=empty,
            index=index,
            document_id=document_id,
            tags=tags,
            creation=creation,
            last_update=last_update,
            steps=steps,
            remaining_steps=remaining_steps,
            completed_steps=completed_steps,
        )

        return data_pipeline_status
