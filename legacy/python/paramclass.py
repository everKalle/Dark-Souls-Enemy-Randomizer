from param import Param, RowStruct
from random import uniform, randint
import struct


def bin(s):
    return str(s) if s<=1 else bin(s>>1) + str(s&1)

def binlen(s, expected = 8):
    binStr = bin(s)
    preStr = ""
    for i in range(expected - len(binStr)):
        preStr += "0"
    return (preStr + binStr)[::-1]

class ParamClass:

    ParamTypes = []

    ParamEndTypes = []

    def __init__(self):
        self.param = Param()
        self.data = []
        self.dataStruct = "<"
        for name, value in self.ParamTypes:
            self.dataStruct += value

    def read(self, mBytes):
        self.param.read(mBytes)
        #print(self.param)
        for row in self.param.Rows:
            rowDict = dict()
            normalValues = dict()
            
            values = struct.unpack_from(self.dataStruct, row.data, 0)

            for i, entry in enumerate(self.ParamTypes):
                entryName = entry[0]
                normalValues[entryName] = values[i]

            rowDict['normal'] = normalValues
            rowDict['end'] = self.readEnd(row.data[struct.calcsize(self.dataStruct):])
            
            self.data.append(rowDict)

    def readEnd(self, mBytes):
        bString = ""
        for b in mBytes:
            bString += binlen(b)
        
        offset = 0
        entries = dict()

        sm = 0

        for i, wpet in enumerate(self.ParamEndTypes):
            if (wpet[1] == '?'):
                entries[wpet[0]] = bString[offset] == '1'
                offset += 1
            elif (wpet[1] == 'B'):
                entries[wpet[0]] = int(bString[offset:offset + wpet[2]], 2)
                offset += wpet[2]
            elif (wpet[1] == 'r'):
                entries[wpet[0]] = bString[offset:offset + wpet[2]]
                offset += wpet[2]
            else:
                raise ValueError("Unhandled WPET type: '" + wpet[1] + "'")
            sm += wpet[2]

        if (len(bString) != offset):
            raise ValueError("Ended end read at offset " + str(offset) + ", but data length is " + str(len(bString)))

        return entries

    def saveEnd(self, idx):
        bString = ""

        for i, entry in enumerate(self.data[idx]['end']):
            wpetType = self.ParamEndTypes[i][1]
            wpetLen = self.ParamEndTypes[i][2]
            entryVal = self.data[idx]['end'][entry]

            if (wpetType == '?'):
                if (entryVal):
                    bString += "1"
                else:
                    bString += "0"
            elif (wpetType == 'B'):
                bString += binlen(entryVal, wpetLen)
            elif (wpetType == 'r'):
                bString += entryVal

        bArr = bytearray()

        for i in range(len(bString) // 8):
            intVal = int(bString[i * 8: i * 8 + 8][::-1], 2)
            bArr.append(intVal)
        
        return bytes(bArr)

    def write(self):
        for rowIdx, row in enumerate(self.data):
            bArr = bytearray()
            for i, entry in enumerate(row['normal']):
                bArr += struct.pack("<" + self.ParamTypes[i][1], row['normal'][entry])
            if (len(self.ParamEndTypes) > 0):
                bArr += self.saveEnd(rowIdx)


            self.param.Rows[rowIdx].data = bytes(bArr)

        return self.param.write()

    def addEntry(self, rowId, rowName, rowData):
        rs = RowStruct()
        rs.new(rowId, rowName)
        self.param.Rows.append(rs)
        self.data.append(rowData)
        if (len(self.param.Rows) != len(self.data)):
            raise ValueError("Param.Rows and self.data lengths do not match after adding a new entry.")