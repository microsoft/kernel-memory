from enum import Enum


class StreamStates(str, Enum):
    APPEND = "append"
    ERROR = "error"
    LAST = "last"
    RESET = "reset"

    def __str__(self) -> str:
        return str(self.value)
