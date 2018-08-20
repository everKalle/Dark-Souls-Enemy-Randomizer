def ReadUInt32(byteArr, loc):
    return int.from_bytes(byteArr[loc: loc+4], byteorder='little', signed=False)

def WriteUInt32(val):
    return (val).to_bytes(4, byteorder='little', signed=False)

def StringFromBytes(byteArr, loca):
    strLen = 0
    if (loca + strLen >= len(byteArr)):
        if (strLen <= 0):
            return (b"", strLen)
        return (byteArr[loca: loca + strLen], strLen)
    while not (byteArr[loca + strLen] == 0):
        strLen += 1
        if (loca + strLen > len(byteArr)):
            if (strLen <= 0):
                return (b"", strLen)
            return (byteArr[loca: loca + strLen], strLen)

    if (strLen <= 0):
        return (b"", strLen)

    return (byteArr[loca: loca + strLen], strLen)

def DecodeString(strBytes):
    return strBytes.decode('shift_jis')

def EncodeString(strToEnc):
    return strToEnc.encode('shift_jis')



class AiFunction():

    def __init__(self, name, ptr):
        self.functionName = name
        self.offset = ptr

    def __str__(self):
        return str(self.offset) + " :: " + DecodeString(self.functionName)

    def DecodedString(self):
        return DecodeString(self.functionName)

    def EncodedString(self):
        return self.functionName

    def SetString(self, newString):
        self.functionName = EncodeString(newString)