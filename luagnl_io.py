from byteread import *
import os.path

class LuaGnl:
    def __init__(self):
        self.mbytes = b'\x00\x00'
        self.offsets = []
        self.aiFunctionNames = []

    def open(self, filename):
        
        self.mbytes = b'\x00\x00'
        self.offsets = []
        self.aiFunctionNames = []

        with open(filename, "rb") as f:
            self.mbytes = f.read()

        ptr = 0

        while True:
            val = ReadUInt32(self.mbytes, ptr)
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

    def open_bytes(self, luagnlBytes):
        
        self.mbytes = b'\x00\x00'
        self.offsets = []
        self.aiFunctionNames = []

        self.mbytes = luagnlBytes

        ptr = 0

        while True:
            val = ReadUInt32(self.mbytes, ptr)
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

    def save(self, filename):
        offs = 0

        if (not os.path.isfile(filename + '.bak')):
            with open(filename + '.bak', 'wb') as bakf:
                with open(filename, 'rb') as oldf:
                    bakf.write(oldf.read())

        with open(filename, 'wb') as f:
            for offset in self.offsets:
                f.write(WriteUInt32(offset))
                offs += 4
            for i, ai in enumerate(self.aiFunctionNames):
                #print(str(offs) + " : " + str(self.offsets[i]))
                f.write(ai.functionName)
                offs += len(ai.functionName) + 1
                f.write(b'\x00')

    def save_bytes(self):
        retBytes = b''
        offs = 0
        for offset in self.offsets:
            retBytes += WriteUInt32(offset)
            offs += 4
        for i, ai in enumerate(self.aiFunctionNames):
            #print(str(offs) + " : " + str(self.offsets[i]))
            retBytes += ai.functionName
            offs += len(ai.functionName) + 1
            retBytes += b'\x00'

        return retBytes

    def AddEntry(self, index):
        self.aiFunctionNames.insert(index + 1, AiFunction(b'', 0))
        self.UpdateOffsets()

    def DeleteEntry(self, index):
        del self.aiFunctionNames[index]
        self.UpdateOffsets()

    def SetValue(self, index, value):
        self.aiFunctionNames[index].SetString(value)
        self.UpdateOffsets()

    def AddEntriesAuto(self, values):   # values = array of bytestrings
        idx = 0
        for value in values:
            self.aiFunctionNames.insert(idx, AiFunction(value, 0))
            idx += 1

        self.UpdateOffsets()

    def UpdateOffsets(self):
        self.offsets = []
        baseOffset = len(self.aiFunctionNames) * 4
        for i, item in enumerate(self.aiFunctionNames):
            if (i == len(self.aiFunctionNames) - 1):
                self.offsets.append(0)
            else:
                self.offsets.append(baseOffset)
                baseOffset += len(item.functionName) + 1
