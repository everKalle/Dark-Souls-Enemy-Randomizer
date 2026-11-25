from byteread import StringFromBytes, AiFunction
from typing import List
import os.path
import struct

class LuaGnl:
    def __init__(self):
        self.mbytes = b'\x00\x00'
        self.offsets = []
        self.aiFunctionNames = []

    def open(self, filename: str):
        """
        Read luagnl from file @filename.
        """

        with open(filename, "rb") as f:
            self.open_bytes(f.read())

    def open_bytes(self, luagnlBytes: bytes):
        """
        Read luagnl from bytes @luagnlBytes.
        """
        
        self.mbytes = b'\x00\x00'
        self.offsets = []
        self.aiFunctionNames = []

        self.mbytes = luagnlBytes

        ptr = 0

        while True:
            val, = struct.unpack_from("<I", self.mbytes, ptr)
            self.offsets.append(val)
            ptr += 4
            if (val == 0):
                break
        i = 0
        while True:
            name, strLen = StringFromBytes(self.mbytes, ptr)
            self.aiFunctionNames.append(AiFunction(name, ptr))
            ptr += (strLen + 1)
            i += 1
            if (i == len(self.offsets)):
                break

    def save(self, filename: str):
        """
        Save the luagnl file as @filename, create a backup if it doesn't exist.
        """
        if (not os.path.isfile(filename + '.bak')):
            with open(filename + '.bak', 'wb') as bakf:
                with open(filename, 'rb') as oldf:
                    bakf.write(oldf.read())

        with open(filename, 'wb') as f:
            f.write(self.save_bytes())

    def save_bytes(self) -> bytes:
        """
        Write luagnl as bytes and return the result.
        """
        retBytes = bytearray()
        offs = 0
        for offset in self.offsets:
            retBytes += struct.pack("<I", offset)
            offs += 4
        for i, ai in enumerate(self.aiFunctionNames):
            retBytes += ai.functionName
            offs += len(ai.functionName) + 1
            retBytes += b'\x00'

        return bytes(retBytes)

    def AddEntry(self, index: int):
        """
        Add a new empty entry at index @index.
        """
        self.aiFunctionNames.insert(index + 1, AiFunction(b'', 0))
        self.UpdateOffsets()

    def DeleteEntry(self, index: int):
        """
        Delete entry at index @index.
        """
        del self.aiFunctionNames[index]
        self.UpdateOffsets()

    def SetValue(self, index: int, value: str):
        """
        Set the function name at index @index to @value.
        """
        self.aiFunctionNames[index].SetString(value)
        self.UpdateOffsets()

    def AddEntriesAuto(self, values: List[bytes]):
        """
        Add entries from @values to the beginning.
        @values - a list of ai function names as bytes.
        """
        idx = 0
        for value in values:
            self.aiFunctionNames.insert(idx, AiFunction(value, 0))
            idx += 1

        self.UpdateOffsets()

    def UpdateOffsets(self):
        """
        Updates offsets.
        """
        self.offsets = []
        baseOffset = len(self.aiFunctionNames) * 4
        for i, item in enumerate(self.aiFunctionNames):
            if (i == len(self.aiFunctionNames) - 1):
                self.offsets.append(0)
            else:
                self.offsets.append(baseOffset)
                baseOffset += len(item.functionName) + 1
