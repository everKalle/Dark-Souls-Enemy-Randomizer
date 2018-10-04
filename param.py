import struct
from byteread import DecodeString, EncodeString, StringFromBytes

class RowStruct():

    def __init__(self):
        self.id = -1
        self.dataOffset = -1
        self.nameOffset = -1
        self.data = b''
        self.name = b''

    def new(self, nId, nName):
        self.id = nId
        self.name = nName

    def read(self, mBytes, pos, dataSize):
        offset = pos
        self.id, self.dataOffset = struct.unpack_from("<II", mBytes, offset)
        offset += struct.calcsize("<II")

        if (self.dataOffset == 0):
            raise ValueError("Null pointer for DataOffset!")
        else:
            oldPos = offset
            offset = self.dataOffset
            self.data = mBytes[offset:offset + dataSize]
            offset = oldPos

        self.nameOffset = struct.unpack_from("<I", mBytes, offset)[0]
        offset += struct.calcsize("<I")

        if (self.nameOffset != 0):
            oldPos = offset
            offset = self.nameOffset
            self.name = DecodeString(StringFromBytes(mBytes, offset)[0])
            offset = oldPos

        return offset

    def write(self, bArr: bytes, strArr: bytes, pos: int, rowIndex: int, dataSize: int, firstRowDataOffset: int, stringsOffset: int):
        self.dataOffset = firstRowDataOffset + rowIndex * dataSize
        self.nameOffset = stringsOffset + len(strArr)

        offset = pos
        header = struct.pack("<III", self.id, self.dataOffset, self.nameOffset)

        expectedSize = offset + len(header)
        while (len(bArr) < expectedSize):
            bArr += b'\x00'

        bArr[offset:offset + len(header)] = header

        offset += struct.calcsize("<III")

        while (len(bArr) < self.dataOffset + dataSize):
            bArr += b'\x00'

        bArr[self.dataOffset:self.dataOffset + dataSize] = self.data

        nm = EncodeString(self.name) + b'\x00'

        strArr += nm

        return offset
        


    def __str__(self):
        retStr = str(self.id) + ": [" + str(self.dataOffset) + "," + str(self.nameOffset) + "]" + str(self.name) + " len(" + str(len(self.data)) + ")"
        return retStr

class Param():
    
    def __init__(self):
        self.StringsOffset = -1
        self.FirstRowDataOffset = -1
        self.Unknown1 = -1
        self.RowCount = -1
        self.Name = ""  # [0x20]
        self.Unknown2 = -1
        self.DataSize = -1
        self.Rows = []

    def read(self, mBytes, pos = 0):
        offset = pos

        self.StringsOffset = struct.unpack_from("<I", mBytes, offset)[0]
        offset += struct.calcsize("<I")

        self.FirstRowDataOffset = int.from_bytes(mBytes[offset:offset + 2], 'little', signed=False)
        offset += 0x2

        self.Unknown1 = int.from_bytes(mBytes[offset:offset + 4], 'little', signed=True)
        offset += 0x4

        self.RowCount = int.from_bytes(mBytes[offset:offset + 2], 'little', signed=False)
        offset += 0x2

        self.Name = mBytes[offset:offset + 0x20]
        offset += 0x20

        self.Unknown2 = struct.unpack_from("<i", mBytes, offset)[0]
        offset += struct.calcsize("<i")

        self.DataSize = (self.StringsOffset - self.FirstRowDataOffset)//self.RowCount

        self.Rows.clear()

        for i in range(self.RowCount):
            rs = RowStruct()
            offset = rs.read(mBytes, offset, self.DataSize)
            self.Rows.append(rs)

    def write(self):
        self.FirstRowDataOffset = len(self.Rows) * 0xC + 0x30
        self.StringsOffset = len(self.Rows) * self.DataSize + self.FirstRowDataOffset
        self.RowCount = len(self.Rows)

        bArr = bytearray()
        strArr = bytearray()
        offset = 0
        bArr += struct.pack("<IHih", self.StringsOffset, self.FirstRowDataOffset, self.Unknown1, self.RowCount)
        offset += struct.calcsize("<IHih")
        bArr += self.Name
        offset += 0x20
        bArr += struct.pack("<i", self.Unknown2)
        offset += struct.calcsize("<i")

        for i, row in enumerate(self.Rows):
            offset = row.write(bArr, strArr, offset, i, self.DataSize, self.FirstRowDataOffset, self.StringsOffset)

        #bArr += b'\x00\x00\x00\x00'
        return bArr + strArr

    def __str__(self):
        retStr = "StringsOffset = " + str(self.StringsOffset)
        retStr += "\nFirstRowDataOffset = " + str(self.FirstRowDataOffset)
        retStr += "\nUnknown1 = " + str(self.Unknown1) 
        retStr += "\nRowCount = " + str(self.RowCount)  
        retStr += "\nName = " + str(self.Name) 
        retStr += "\nUnknown2 = " + str(self.Unknown2) 
        retStr += "\nDataSize = " + str(self.DataSize)
        retStr += "\nRows:" 
        """for row in self.Rows:
            retStr += "\n  " + str(row)"""
        return retStr

"""
short FirstRowDataOffset <format=hex>;
short Unknown1[2];
"""

