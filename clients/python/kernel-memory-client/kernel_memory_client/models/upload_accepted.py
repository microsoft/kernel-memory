from collections.abc import Mapping
from typing import Any, TypeVar, Union, cast

from attrs import define as _attrs_define

from ..types import UNSET, Unset

T = TypeVar("T", bound="UploadAccepted")


@_attrs_define
class UploadAccepted:
    """
    Attributes:
        index (Union[None, Unset, str]):
        document_id (Union[None, Unset, str]):
        message (Union[None, Unset, str]):
    """

    index: Union[None, Unset, str] = UNSET
    document_id: Union[None, Unset, str] = UNSET
    message: Union[None, Unset, str] = UNSET

    def to_dict(self) -> dict[str, Any]:
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

        message: Union[None, Unset, str]
        if isinstance(self.message, Unset):
            message = UNSET
        else:
            message = self.message

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if index is not UNSET:
            field_dict["index"] = index
        if document_id is not UNSET:
            field_dict["documentId"] = document_id
        if message is not UNSET:
            field_dict["message"] = message

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        d = dict(src_dict)

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

        document_id = _parse_document_id(d.pop("documentId", UNSET))

        def _parse_message(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        message = _parse_message(d.pop("message", UNSET))

        upload_accepted = cls(
            index=index,
            document_id=document_id,
            message=message,
        )

        return upload_accepted
