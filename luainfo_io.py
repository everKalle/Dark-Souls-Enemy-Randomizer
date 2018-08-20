from byteread import *
import os.path

# Make the new offsets depend on primaryRef/secondaryRef

class LuaInfo:

    def __init__(self):
        self.mbytes = b'\x00\x00'

        self.header = "LUAI".encode('ASCII')
        self.unknownValue = 0
        self.entryCount = 0
        self.separator = 0
        self.entries = []
        self.aiFunctionNames = []

    def open(self, filename):

        self.mbytes = b'\x00\x00'

        self.header = "LUAI".encode('ASCII')
        self.unknownValue = 0
        self.entryCount = 0
        self.separator = 0
        self.entries = []
        self.aiFunctionNames = []

        #print('opening ' + filename)
        with open(filename, "rb") as f:
            self.mbytes = f.read()

        self.header =  self.mbytes[0:4].decode('ASCII')

        self.unknownValue = ReadUInt32(self.mbytes, 4)
        self.entryCount = ReadUInt32(self.mbytes, 8)
        self.separator = ReadUInt32(self.mbytes, 12)

        ptr = 16

        while True:
            NPCAI_value = ReadUInt32(self.mbytes, ptr)
            primaryFunctionNameOffset = ReadUInt32(self.mbytes, ptr + 4)
            secondaryFunctionNameOffset = ReadUInt32(self.mbytes, ptr + 8)
            unknownBoolA = self.mbytes[ptr + 12]
            unknownBoolB = self.mbytes[ptr + 13]
            unknownBoolC = self.mbytes[ptr + 14]
            unknownBoolD = self.mbytes[ptr + 15]

            self.entries.append(LuaInfoEntry(NPCAI_value, primaryFunctionNameOffset, secondaryFunctionNameOffset, unknownBoolA, unknownBoolB, unknownBoolC, unknownBoolD))

            ptr += 16

            if (len(self.entries) == self.entryCount):
                break

        while True:
            name, strLen = StringFromBytes(self.mbytes, ptr)
            self.aiFunctionNames.append(AiFunction(name, ptr))
            ptr += (strLen + 1)
            if (ptr >= len(self.mbytes)):
                break

        for entry in self.entries:
            for i, ai in enumerate(self.aiFunctionNames):
                if (entry.pFuncNameOffset == ai.offset):
                    entry.SetPrimaryRef(i)
                if (entry.sFuncNameOffset == ai.offset):
                    entry.SetSecondaryRef(i)

        self.UpdateOffsets()

    def open_bytes(self, luainfoBytes):

        self.mbytes = b'\x00\x00'

        self.header = "LUAI".encode('ASCII')
        self.unknownValue = 0
        self.entryCount = 0
        self.separator = 0
        self.entries = []
        self.aiFunctionNames = []

        self.mbytes = luainfoBytes

        self.header =  self.mbytes[0:4].decode('ASCII')

        self.unknownValue = ReadUInt32(self.mbytes, 4)
        self.entryCount = ReadUInt32(self.mbytes, 8)
        self.separator = ReadUInt32(self.mbytes, 12)

        ptr = 16

        while True:
            NPCAI_value = ReadUInt32(self.mbytes, ptr)
            primaryFunctionNameOffset = ReadUInt32(self.mbytes, ptr + 4)
            secondaryFunctionNameOffset = ReadUInt32(self.mbytes, ptr + 8)
            unknownBoolA = self.mbytes[ptr + 12]
            unknownBoolB = self.mbytes[ptr + 13]
            unknownBoolC = self.mbytes[ptr + 14]
            unknownBoolD = self.mbytes[ptr + 15]

            self.entries.append(LuaInfoEntry(NPCAI_value, primaryFunctionNameOffset, secondaryFunctionNameOffset, unknownBoolA, unknownBoolB, unknownBoolC, unknownBoolD))

            ptr += 16

            if (len(self.entries) == self.entryCount):
                break

        while True:
            name, strLen = StringFromBytes(self.mbytes, ptr)
            self.aiFunctionNames.append(AiFunction(name, ptr))
            ptr += (strLen + 1)
            if (ptr >= len(self.mbytes)):
                break

        for entry in self.entries:
            for i, ai in enumerate(self.aiFunctionNames):
                if (entry.pFuncNameOffset == ai.offset):
                    entry.SetPrimaryRef(i)
                if (entry.sFuncNameOffset == ai.offset):
                    entry.SetSecondaryRef(i)

        self.UpdateOffsets()

    def save(self, filename):
        if (not os.path.isfile(filename + '.bak')):
            with open(filename + '.bak', 'wb') as bakf:
                with open(filename, 'rb') as oldf:
                    bakf.write(oldf.read())

        with open(filename, 'wb') as f:
            f.write(self.header.encode('ASCII'))
            f.write(WriteUInt32(self.unknownValue))
            f.write(WriteUInt32(self.entryCount))
            f.write(WriteUInt32(self.separator))
            for entry in self.entries:
                f.write(WriteUInt32(entry.NPCAI_value))
                pOff = 0
                if (entry.primaryRef > -1):
                    pOff = self.aiFunctionNames[entry.primaryRef].offset
                f.write(WriteUInt32(pOff))
                sOff = 0
                if (entry.primaryRef > -1):
                    sOff = self.aiFunctionNames[entry.secondaryRef].offset
                f.write(WriteUInt32(sOff))
                f.write(entry.unknownBoolA.to_bytes(1, 'little', signed=False))
                f.write(entry.unknownBoolB.to_bytes(1, 'little', signed=False))
                f.write(entry.unknownBoolC.to_bytes(1, 'little', signed=False))
                f.write(entry.unknownBoolD.to_bytes(1, 'little', signed=False))
            for ai in self.aiFunctionNames:
                f.write(ai.functionName)
                f.write(b'\x00')

    def save_bytes(self):

        retBytes = b''

        retBytes += self.header.encode('ASCII')
        retBytes += WriteUInt32(self.unknownValue)
        retBytes += WriteUInt32(self.entryCount)
        retBytes += WriteUInt32(self.separator)
        for entry in self.entries:
            retBytes += WriteUInt32(entry.NPCAI_value)
            pOff = 0
            if (entry.primaryRef > -1):
                pOff = self.aiFunctionNames[entry.primaryRef].offset
            retBytes += WriteUInt32(pOff)
            sOff = 0
            if (entry.secondaryRef > -1):
                sOff = self.aiFunctionNames[entry.secondaryRef].offset
            retBytes += WriteUInt32(sOff)
            retBytes += entry.unknownBoolA.to_bytes(1, 'little', signed=False)
            retBytes += entry.unknownBoolB.to_bytes(1, 'little', signed=False)
            retBytes += entry.unknownBoolC.to_bytes(1, 'little', signed=False)
            retBytes += entry.unknownBoolD.to_bytes(1, 'little', signed=False)
        for ai in self.aiFunctionNames:
            retBytes += ai.functionName
            retBytes += b'\x00'

        return retBytes

    def SetAiFuncNameValue(self, index, value):
        self.aiFunctionNames[index].SetString(value)
        self.UpdateOffsets()

    def AddAiFuncEntry(self, index):
        self.aiFunctionNames.insert(index + 1, AiFunction(b'', 0))
        for entry in self.entries:
            entry.UpdateAddRefs(index)
        self.UpdateOffsets()

    def DeleteAiFuncEntry(self, index):
        del self.aiFunctionNames[index]
        for entry in self.entries:
            entry.UpdateDeleteRefs(index)
        self.UpdateOffsets()

    def AddInfoEntry(self, index):
        self.entries.insert(index + 1, LuaInfoEntry(0, 0, 0, 0, 0, 0, 0))
        self.entryCount += 1
        self.UpdateOffsets()

    def DeleteInfoEntry(self, index):
        del self.entries[index]
        self.entryCount -= 1
        self.UpdateOffsets()

    def UpdateOffsets(self):
        baseOffset = 16 + len(self.entries) * 16
        for i, item in enumerate(self.aiFunctionNames):
            self.aiFunctionNames[i].offset = baseOffset
            baseOffset += len(item.functionName) + 1

    def UpdateEntryAI(self, index, value):
        self.entries[index].NPCAI_value = int(value)

    def UpdateEntryPrimaryRef(self, index, value):
        self.entries[index].primaryRef = value

    def UpdateEntrySecondaryRef(self, index, value):
        self.entries[index].secondaryRef = value

    def UpdateEntryUBoolA(self, index, value):
        self.entries[index].unknownBoolA = int(value)

    def UpdateEntryUBoolB(self, index, value):
        self.entries[index].unknownBoolB = int(value)

    def UpdateEntryUBoolC(self, index, value):
        self.entries[index].unknownBoolC = int(value)

    def UpdateEntryUBoolD(self, index, value):
        self.entries[index].unknownBoolD = int(value)

    def AddEntryAuto(self, values):  #struct: {(int)120100;b'prim_name';b'sec_name';(int)1;(int)0;(int)0;(int)0}
        #print(values)

        exists = False

        for entry in self.entries:
            if (entry.NPCAI_value == values[0][0]):
                exists = True
                break

        if (exists):
            return True
        else:
            for value in values:
                #print(value)
                if not (value[1] == b''):
                    self.aiFunctionNames.insert(0, AiFunction(value[1], 0))
                    for entry in self.entries:
                        entry.UpdateAddRefs(-1)
                if not (value[2] == b''):
                    self.aiFunctionNames.insert(1, AiFunction(value[2], 0))
                    for entry in self.entries:
                        entry.UpdateAddRefs(0)

                self.entries.insert(0, LuaInfoEntry(value[0], 0, 0, value[3], value[4], value[5], value[6]))
                if not (value[1] == b''):
                    self.entries[0].primaryRef = 0

                if not (value[2] == b''):
                    self.entries[0].secondaryRef = 1

                self.entryCount += 1

                self.UpdateOffsets()
                    #return False
            return False
        
            

class LuaInfoEntry():

    def __init__(self, ai_val, pfno, sfno, b1, b2, b3, b4):
        self.NPCAI_value = ai_val
        self.pFuncNameOffset = pfno
        self.sFuncNameOffset = sfno
        self.unknownBoolA = b1
        self.unknownBoolB = b2
        self.unknownBoolC = b3
        self.unknownBoolD = b4
        self.primaryRef = -1
        self.secondaryRef = -1

    def SetPrimaryRef(self, index):
        self.primaryRef = index
    
    def SetSecondaryRef(self, index):
        self.secondaryRef = index

    def UpdateAddRefs(self, index):
        if (index < self.primaryRef):
            self.primaryRef += 1

        if (index < self.secondaryRef):
            self.secondaryRef += 1

    def UpdateDeleteRefs(self, index):
        if (index < self.primaryRef):
            self.primaryRef -= 1
        elif (index == self.primaryRef):
            self.primaryRef = -1

        if (index < self.secondaryRef):
            self.secondaryRef -= 1
        elif (index == self.secondaryRef):
            self.secondaryRef = -1

    def __str__(self):
        return str(self.NPCAI_value) + " primary: " + str(self.pFuncNameOffset) + " (" + str(self.primaryRef) + ")" + "; secondary: " + str(self.sFuncNameOffset) + " (" + str(self.secondaryRef) + ")" + "; (" + str(self.unknownBoolA) + ", " + str(self.unknownBoolB) + ", " + str(self.unknownBoolC) + ", " + str(self.unknownBoolD) + ")"