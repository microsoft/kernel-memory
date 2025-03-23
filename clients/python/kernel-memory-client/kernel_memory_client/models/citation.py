from collections.abc import Mapping
from typing import TYPE_CHECKING, Any, TypeVar, Union, cast

from attrs import define as _attrs_define

from ..types import UNSET, Unset

if TYPE_CHECKING:
    from ..models.partition import Partition


T = TypeVar("T", bound="Citation")


@_attrs_define
class Citation:
    """
    Attributes:
        link (Union[None, Unset, str]):
        index (Union[None, Unset, str]):
        document_id (Union[None, Unset, str]):
        file_id (Union[None, Unset, str]):
        source_content_type (Union[None, Unset, str]):
        source_name (Union[None, Unset, str]):
        source_url (Union[None, Unset, str]):
        partitions (Union[None, Unset, list['Partition']]):
    """

    link: Union[None, Unset, str] = UNSET
    index: Union[None, Unset, str] = UNSET
    document_id: Union[None, Unset, str] = UNSET
    file_id: Union[None, Unset, str] = UNSET
    source_content_type: Union[None, Unset, str] = UNSET
    source_name: Union[None, Unset, str] = UNSET
    source_url: Union[None, Unset, str] = UNSET
    partitions: Union[None, Unset, list["Partition"]] = UNSET

    def to_dict(self) -> dict[str, Any]:
        link: Union[None, Unset, str]
        if isinstance(self.link, Unset):
            link = UNSET
        else:
            link = self.link

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

        file_id: Union[None, Unset, str]
        if isinstance(self.file_id, Unset):
            file_id = UNSET
        else:
            file_id = self.file_id

        source_content_type: Union[None, Unset, str]
        if isinstance(self.source_content_type, Unset):
            source_content_type = UNSET
        else:
            source_content_type = self.source_content_type

        source_name: Union[None, Unset, str]
        if isinstance(self.source_name, Unset):
            source_name = UNSET
        else:
            source_name = self.source_name

        source_url: Union[None, Unset, str]
        if isinstance(self.source_url, Unset):
            source_url = UNSET
        else:
            source_url = self.source_url

        partitions: Union[None, Unset, list[dict[str, Any]]]
        if isinstance(self.partitions, Unset):
            partitions = UNSET
        elif isinstance(self.partitions, list):
            partitions = []
            for partitions_type_0_item_data in self.partitions:
                partitions_type_0_item = partitions_type_0_item_data.to_dict()
                partitions.append(partitions_type_0_item)

        else:
            partitions = self.partitions

        field_dict: dict[str, Any] = {}
        field_dict.update({})
        if link is not UNSET:
            field_dict["link"] = link
        if index is not UNSET:
            field_dict["index"] = index
        if document_id is not UNSET:
            field_dict["documentId"] = document_id
        if file_id is not UNSET:
            field_dict["fileId"] = file_id
        if source_content_type is not UNSET:
            field_dict["sourceContentType"] = source_content_type
        if source_name is not UNSET:
            field_dict["sourceName"] = source_name
        if source_url is not UNSET:
            field_dict["sourceUrl"] = source_url
        if partitions is not UNSET:
            field_dict["partitions"] = partitions

        return field_dict

    @classmethod
    def from_dict(cls: type[T], src_dict: Mapping[str, Any]) -> T:
        from ..models.partition import Partition

        d = dict(src_dict)

        def _parse_link(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        link = _parse_link(d.pop("link", UNSET))

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

        def _parse_file_id(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        file_id = _parse_file_id(d.pop("fileId", UNSET))

        def _parse_source_content_type(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        source_content_type = _parse_source_content_type(d.pop("sourceContentType", UNSET))

        def _parse_source_name(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        source_name = _parse_source_name(d.pop("sourceName", UNSET))

        def _parse_source_url(data: object) -> Union[None, Unset, str]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            return cast(Union[None, Unset, str], data)

        source_url = _parse_source_url(d.pop("sourceUrl", UNSET))

        def _parse_partitions(data: object) -> Union[None, Unset, list["Partition"]]:
            if data is None:
                return data
            if isinstance(data, Unset):
                return data
            try:
                if not isinstance(data, list):
                    raise TypeError()
                partitions_type_0 = []
                _partitions_type_0 = data
                for partitions_type_0_item_data in _partitions_type_0:
                    partitions_type_0_item = Partition.from_dict(partitions_type_0_item_data)

                    partitions_type_0.append(partitions_type_0_item)

                return partitions_type_0
            except:  # noqa: E722
                pass
            return cast(Union[None, Unset, list["Partition"]], data)

        partitions = _parse_partitions(d.pop("partitions", UNSET))

        citation = cls(
            link=link,
            index=index,
            document_id=document_id,
            file_id=file_id,
            source_content_type=source_content_type,
            source_name=source_name,
            source_url=source_url,
            partitions=partitions,
        )

        return citation
