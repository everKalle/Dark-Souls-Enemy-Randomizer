import data_layouts
import struct
from data_layouts import Msbdata
import os.path

class MsbIO:
    
    models = Msbdata()
    events0 = Msbdata()
    events1 = Msbdata()
    events2 = Msbdata()
    events3 = Msbdata()
    events4 = Msbdata()
    events5 = Msbdata()
    events6 = Msbdata()
    events7 = Msbdata()
    events8 = Msbdata()
    events9 = Msbdata()
    events10 = Msbdata()
    events11 = Msbdata()
    events12 = Msbdata()
    points0 = Msbdata()
    points2 = Msbdata()
    points3 = Msbdata()
    points5 = Msbdata()
    mapPieces0 = Msbdata()
    objects1 = Msbdata()
    creatures2 = Msbdata()
    creatures4 = Msbdata()
    collision5 = Msbdata()
    navimesh8 = Msbdata()
    objects9 = Msbdata()
    creatures10 = Msbdata()
    collision11 = Msbdata()

    layouts = [] #list of msbdata, maybe need dict ???
    models_data = []

    events = [] #ditto
    events_data = []

    points = []
    points_data = []

    parts = []
    parts_data = []

    m_Bytes = [] #list of bytes ??


    bigEndian = True

    def WriteBytes(self, fs, byt):
        for i in range(0, len(byt)):
            fs.write(byt[i])

    def RawStrFromBytes(self, loca):

        m_len = 0

        while True:
            if (self.m_Bytes[loca + m_len] == 0):
                break
            else:
                #print(self.m_Bytes[loca + m_len])
                m_len += 1

        if (m_len <= 0):
            return b""

        strBytesJIS = self.m_Bytes[loca: loca + m_len]

        return strBytesJIS

    def Str2Bytes(self, s):
        return s.encode('shift_jis')

    def RawStrToStr(self, rawStr):
        return rawStr.decode('shift_jis')

    def Int8ToOneByte(self, val):
        return (val).to_bytes(1, byteorder='big')

    def Int16ToTwoByte(self, val):
        if (self.bigEndian):
            return (val).to_bytes(2, byteorder='big', signed=True)
        else:
            return (val).to_bytes(2, byteorder='little', signed=True)

    def Int32ToFourByte(self, val):
        if (self.bigEndian):
            return (val).to_bytes(4, byteorder='big', signed=True)
        else:
            return (val).to_bytes(4, byteorder='little', signed=True)

    def Int16ToTwoByteI(self, val):
        if (self.bigEndian):
            return (val).to_bytes(2, byteorder='little', signed=True)
        else:
            return (val).to_bytes(2, byteorder='big', signed=True)

    def Int32ToFourByteI(self, val):
        if (self.bigEndian):
            return (val).to_bytes(4, byteorder='little', signed=True)
        else:
            return (val).to_bytes(4, byteorder='big', signed=True)

    def UInt8ToOneByte(self, val):
        return (val).to_bytes(1, byteorder='big', signed=False)

    def UInt16ToTwoByte(self, val):
        if (self.bigEndian):
            return (val).to_bytes(2, byteorder='big', signed=False)
        else:
            return (val).to_bytes(2, byteorder='little', signed=False)

    def UInt32ToFourByte(self, val):
        if (self.bigEndian):
            return (val).to_bytes(4, byteorder='big', signed=False)
        else:
            return (val).to_bytes(4, byteorder='little', signed=False)

    def SingleToFourByte(self, val):
        
        if (self.bigEndian):
            return struct.pack('f', val)[::-1]
        else:
            return struct.pack('f', val)
        """if (val.isdigit()):
            if (self.bigEndian):
                return int(val).to_bytes(4, byteorder='big')
            else:
                return int(val).to_bytes(4, byteorder='little')
        else:
            return b'\x00\x00\x00\x00'"""

    """def SingleToFourByte(self, val):
        if (self.bigEndian):
            return int(val).to_bytes(4, byteorder='big')
        else:
            return int(val).to_bytes(4, byteorder='little')"""

    def InsBytes(self, loc, byt):
        for i in range(0, len(byt)):
            self.m_Bytes[loc + i] = byt[i]

    def SIntFromOne(self, loc):
        return int.from_bytes(self.m_Bytes[loc: loc+1], byteorder='big')

    def SIntFromTwo(self, loc):
        bArray = self.m_Bytes[loc:loc+2]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='big', signed=True)
        else:
            return int.from_bytes(bArray, byteorder='little', signed=True)

    def SIntFromFour(self, loc):
        bArray = self.m_Bytes[loc: loc+4]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='big', signed=True)
        else:
            return int.from_bytes(bArray, byteorder='little', signed=True)

    def SIntFromTwoI(self, loc):
        bArray = self.m_Bytes[loc:loc+2]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='little', signed=True)
        else:
            return int.from_bytes(bArray, byteorder='big', signed=True)

    def SIntFromFourI(self, loc):
        bArray = self.m_Bytes[loc: loc+4]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='little', signed=True)
        else:
            return int.from_bytes(bArray, byteorder='big', signed=True)

    def UIntFromTwo(self, loc):
        bArray = self.m_Bytes[loc:loc+2]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='big', signed=False)
        else:
            return int.from_bytes(bArray, byteorder='little', signed=False)

    def UIntFromFour(self, loc):
        bArray = self.m_Bytes[loc: loc+4]
        #bArray.reverse()
        if (self.bigEndian):
            return int.from_bytes(bArray, byteorder='big', signed=False)
        else:
            return int.from_bytes(bArray, byteorder='little', signed=False)

    def SingleFromFour(self, loc):
        bArray = self.m_Bytes[loc: loc+4]
        #bArray.reverse()
        #return bArray
        if (self.bigEndian):
            return struct.unpack('f', bArray[::-1])[0]
        else:
            return struct.unpack('f', bArray)[0]

    def readRow(self, dgv, layout, ptr):
        currOffset = 0
        partRow = []
        for k in range(0, layout.fieldCount()):
            partRow.append(0)
        partName = []
        sibpath = []
        textboost = 0
        hasSib = True
        Padding = 0

        nameoffset = self.SIntFromFour(ptr)
        partName = self.RawStrFromBytes(ptr + nameoffset)
        partRow[layout.getNameIndex()] = self.RawStrToStr(partName)
        Padding = len(partName) + 1

        hasSib = layout.retrieveName(layout.getNameIndex() + 1) == "Sibpath"

        if (hasSib):
            siboffset = self.SIntFromFour(ptr + 0x10)
            sibpath = self.RawStrFromBytes(ptr + siboffset)
            partRow[layout.getNameIndex() + 1] = self.RawStrToStr(sibpath)

            Padding += len(sibpath) + 1
            if (len(sibpath) == 0):
                 Padding += 5

        Padding = (Padding + 3) & -0x4

        for j in range(0, layout.fieldCount()):
            if (j < layout.getNameIndex()):
                textboost = 0
            else:
                textboost = 0
                textboost = nameoffset + Padding

            if (j == layout.getNameIndex()):
                currOffset = 0

            rt = layout.retrieveType(j)
            
            if (rt == "i8"):
                partRow[j] = self.SIntFromOne(ptr + textboost + currOffset)
                currOffset += 1
            elif (rt == "i16"):
                partRow[j] = self.SIntFromTwo(ptr + textboost + currOffset)
                currOffset += 2
            elif (rt == "i32"):
                partRow[j] = self.SIntFromFour(ptr + textboost + currOffset)
                currOffset += 4
            elif (rt == "f32"):
                partRow[j] = self.SingleFromFour(ptr + textboost + currOffset)
                currOffset += 4

            #print(ptr + textboost + currOffset)

        dgv.append(partRow)

    def open(self, filename):
        self.clean()

        f = open(filename, 'rb')
        self.m_Bytes = f.read()
        f.close()

        #print("loading " + filename + " <")
        #print(self.m_Bytes)

        ptr = 0
        nameoffset = 0

        name = []
        sibpath = []

        row = []

        modelPtr = 0
        modelCnt = 0

        eventPtr = 0
        eventCnt = 0

        pointPtr = 0
        pointCnt = 0

        partsPtr = 0
        partsCnt = 0

        mapstudioPtr = 0
        mapstudioCnt = 0

        self.bigEndian = True
        if (self.UIntFromFour(0x8) > 0x10000):
            self.bigEndian = False

        #print(self.bigEndian)

        modelPtr = 0
        modelCnt = self.UIntFromFour(0x8)

        eventPtr = self.UIntFromFour((modelCnt * 0x4) + 0x8)
        eventCnt = self.UIntFromFour(eventPtr + 0x8)

        pointPtr = self.UIntFromFour((eventCnt * 0x4) + 0x8 + eventPtr)
        pointCnt = self.UIntFromFour(pointPtr + 0x8)

        partsPtr = self.UIntFromFour((pointCnt * 0x4) + 0x8 + pointPtr)
        partsCnt = self.UIntFromFour(partsPtr + 0x8)

        #print("loading models")
        for i in range(0, modelCnt - 1):
            currOffset = 0
            mdlRow = []
            for k in range(0, self.models.fieldCount()):
                mdlRow.append(0)

            ptr = self.UIntFromFour(modelPtr + 0xC + i * 0x4)

            nameoffset = self.UIntFromFour(ptr)
            name = self.RawStrFromBytes(ptr + nameoffset)
            sibpath = self.RawStrFromBytes(ptr + nameoffset + len(name) + 1)

            mdlRow[self.models.getNameIndex()] = self.RawStrToStr(name)
            mdlRow[self.models.getNameIndex() + 1] = self.RawStrToStr(sibpath)

            for j in range(0, self.models.fieldCount()):
                if (self.models.retrieveType(j) == "i32"):
                    mdlRow[j] = self.SIntFromFour(ptr + currOffset)
                    currOffset += 4

            self.models_data.append(mdlRow)

        idx = 0

        eventtype = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]

        for i in range(0, eventCnt - 1):
            ptr = self.UIntFromFour(eventPtr + 0xC + i * 0x4)

            idx = eventtype.index(self.SIntFromFour(ptr + 0x8))
            self.readRow(self.events_data[idx], self.events[idx], ptr)

        idx = 0
        pointtype = [0, 2, 3, 5]

        for i in range(0, pointCnt - 1):
            ptr = self.UIntFromFour(pointPtr + 0xC + i * 0x4)

            idx = pointtype.index(self.SIntFromFour(ptr + 0xC))
            self.readRow(self.points_data[idx], self.points[idx], ptr)


        idx = 0
        parttype = [0, 1, 2, 4, 5, 8, 9, 0xA, 0xB]
        for i in range(0, partsCnt - 1):
            ptr = self.UIntFromFour(partsPtr + 0xC + i * 0x4)

            idx = parttype.index(self.SIntFromFour(ptr + 0x4))
            #print("parts-" + str(idx))
            self.readRow(self.parts_data[idx], self.parts[idx], ptr)

        mapstudioPtr = self.UIntFromFour((partsCnt * 0x4) + 0x8 + partsPtr)
        mapstudioCnt = self.UIntFromFour(mapstudioPtr + 0x8)

        #print(filename + " loaded")

    def writeToPos(self, byteStream, value, pos):
        tempStream = byteStream[:pos] + value + byteStream[pos + len(value):]
        return (byteStream, pos + len(value))

    def writeToPosReal(self, byteStream, value, pos):
        tempStream = byteStream[:pos] + value + byteStream[pos + len(value):]
        return (tempStream, pos + len(value))

    def saveRow(self, byteStream, row, data, pos, ptr, partsidx):
        """ByRef MSBStream As FileStream, ByRef row As DataGridViewRow, ByRef data As msbdata, ByRef ptr As Integer, ByRef partsidx As Integer"""
        #stream = byteStream
        curroffset = pos
        pos = ptr + 0xC + partsidx * 0x4
        byteStream, pos = self.WriteBytesAt(byteStream, pos, self.UInt32ToFourByte(curroffset))
        pos = curroffset

        nameoffset = row[0]
        Name = self.Str2Bytes(row[data.getNameIndex()])

        Padding = len(Name) + 1

        hasSib = data.retrieveName(data.getNameIndex() + 1) == "Sibpath"
        if (hasSib):
            sibpath = self.Str2Bytes(row[data.getNameIndex() + 1])
            Padding += len(sibpath) + 1
            if (len(sibpath) == 0):
                Padding += 5

        
        Padding = (Padding + 3) & -0x4

        #print(row[2])
        #print(pos)

        for j in range(0, data.fieldCount()):
            if (j == data.getNameIndex()):
                pos = curroffset + nameoffset

            if (hasSib):
                if (j == data.getNameIndex() + 2):
                    pos = curroffset + nameoffset + Padding
            else:
                if (j == data.getNameIndex() + 1):
                    pos = curroffset + nameoffset + Padding

            rt = data.retrieveType(j)

            #print(pos)
            if (rt == "i8"):
                byteStream, pos = self.WriteBytesAt(byteStream, pos, self.Int8ToOneByte(row[j]))
                #print("i8 : " + str(row[j]) + " - " + str(self.Int8ToOneByte(row[j])))
            elif (rt == "i16"):
                byteStream, pos = self.WriteBytesAt(byteStream, pos, self.Int16ToTwoByte(row[j]))
                #print("i16 : " + str(row[j]) + " - " + str(self.Int16ToTwoByte(row[j])))
            elif (rt == "i32"):
                byteStream, pos = self.WriteBytesAt(byteStream, pos, self.Int32ToFourByte(row[j]))
                #print("i32 : " + str(row[j]) + " - " + str(self.Int32ToFourByte(row[j])))
            elif (rt == "f32"):
                #stream, pos = self.writeToPos(stream, row[j], pos)
                byteStream, pos = self.WriteBytesAt(byteStream, pos, self.SingleToFourByte(row[j]))
                #print("f32 : " + str(row[j]) + " - " + str(self.SingleToFourByte(row[j])))
            elif (rt == "string"):
                byteStream, pos = self.WriteBytesAt(byteStream, pos, self.Str2Bytes(row[j]))
                byteStream, pos = self.WriteBytesAt(byteStream, pos, b'\x00')
                #print("str : " + str(self.Str2Bytes(row[j])))

            #print(pos)

            

        partsidx += 1
        #print("saved row: " + str(pos) + ", " + str(partsidx))
        return (byteStream, pos, partsidx)

    def save(self, filename, createBackup = True):
        f = open(filename, 'rb')
        toWriteBytes = self.m_Bytes
        self.m_Bytes = f.read()
        f.close()

        if (createBackup):
            if (not os.path.isfile(filename + '.bak')):
                f = open(filename + '.bak', 'wb')
                f.write(self.m_Bytes)
                f.close()

        modelPtr = self.UIntFromFour(0x4)
        modelCnt = self.UIntFromFour(0x8)

        eventPtr = self.UIntFromFour((modelCnt * 0x4) + 0x8)
        eventCnt = self.UIntFromFour(eventPtr + 0x8)

        pointPtr = self.UIntFromFour((eventCnt * 0x4) + 0x8 + eventPtr)
        pointCnt = self.UIntFromFour(pointPtr + 0x8)

        partsPtr = self.UIntFromFour((pointCnt * 0x4) + 0x8 + pointPtr)
        partsCnt = self.UIntFromFour(partsPtr + 0x8)

        self.bigEndian = True
        if (self.UIntFromFour(0x8) > 0x10000):
            self.bigEndian = False

        self.effinLimit = 40

        curroffset = 0
        nameoffset = 0
        name = b''
        sibpath = b''
        padding = 0

        pos = 0

        MSBStream = bytearray()

        MSBStream, pos = self.WriteBytesAt(MSBStream, 0, self.UInt32ToFourByte(0))


        modelPtr = 0
        modelCnt = len(self.models_data) + 1
        curroffset = modelPtr + 0xC + modelCnt * 0x4

        pos = 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(curroffset))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(modelCnt))

        pos = curroffset
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.Str2Bytes("MODEL_PARAM_ST"))
        pos = (len(MSBStream) & -0x4) + 0x4
        #print(MSBStream)

        for i in range(modelCnt - 1):
            curroffset = pos
            pos = modelPtr + 0xC + i * 0x4
            MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(curroffset))
            pos = curroffset

            nameoffset = self.models_data[i][0]
            name = self.Str2Bytes(self.models_data[i][self.models.getNameIndex()])
            sibpath = self.Str2Bytes(self.models_data[i][self.models.getNameIndex() + 1])

            padding = ((len(sibpath) + len(name) + 5) & -0x4)

            if (padding <= 0x10):
                padding = 0x10
                if not self.bigEndian:
                    padding += 0x4

            #print(padding)
            
            for j in range(self.models.fieldCount()):
                if (j == self.models.getNameIndex()):
                    pos = curroffset + nameoffset
                
                #print(pos)

                rt = self.models.retrieveType(j)
                if (rt == "i32"):
                    MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(self.models_data[i][j]))
                elif (rt == "string"):
                    MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.Str2Bytes(self.models_data[i][j]))
                    MSBStream, pos = self.WriteBytesAt(MSBStream, pos, b'\x00')

                

            pos = curroffset + nameoffset + padding
            #print(len(MSBStream))

        """ EVENTS """

        eventPtr = (len(MSBStream) & -0x4) + 0x4
        pos = modelPtr + 0xC + (modelCnt - 1) * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(eventPtr))
        pos = eventPtr

        eventCnt = 1
        for event in self.events_data:
            eventCnt += len(event)

        curroffset = eventPtr + 0xC + eventCnt * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(0))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(curroffset))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(eventCnt))

        pos = curroffset
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.Str2Bytes("EVENT_PARAM_ST"))
        pos = (len(MSBStream) & -0x4) + 0x4

        toSortEvents = []
        for i, ev in enumerate(self.events_data):
            for row in ev:
                toSortEvents.append((row[3], row, self.events[i]))
        
        sortedEvents = sorted(toSortEvents, key=lambda x: x[0])

        eventidx = 0


        for eventRow in toSortEvents:
            MSBStream, pos, eventidx = self.saveRow(MSBStream, eventRow[1], eventRow[2], pos, eventPtr, eventidx)

        #(byteStream, pos, partsidx)

        """ POINTS """

        pointPtr = len(MSBStream)
        pos = eventPtr + 0xC + (eventCnt - 1) * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(pointPtr))
        pos = pointPtr

        pointCnt = len(self.points_data[0]) + len(self.points_data[1]) + len(self.points_data[2]) + len(self.points_data[3]) + 1
        curroffset = pointPtr + 0xC + pointCnt * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(0))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(curroffset))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(pointCnt))

        pos = curroffset
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.Str2Bytes("POINT_PARAM_ST"))
        pos = (len(MSBStream) & -0x4) + 0x4

        toSortPoints = []
        for i, pt in enumerate(self.points_data):
            for row in pt:
                toSortPoints.append((row[2], row, self.points[i]))
        
        sortedPoints = sorted(toSortPoints, key=lambda x: x[0])

        pointidx = 0


        for pointRow in sortedPoints:
            MSBStream, pos, pointidx = self.saveRow(MSBStream, pointRow[1], pointRow[2], pos, pointPtr, pointidx)

        

        """ Parts """

        partsPtr = len(MSBStream)
        pos = pointPtr + 0xC + (pointCnt - 1) * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(partsPtr))
        pos = partsPtr

        partsCnt = 1
        for partsPage in self.parts_data:
            partsCnt += len(partsPage)
        curroffset = partsPtr + 0xC + partsCnt * 0x4
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(0))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(curroffset))
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.UInt32ToFourByte(partsCnt))

        pos = curroffset
        MSBStream, pos = self.WriteBytesAt(MSBStream, pos, self.Str2Bytes("PARTS_PARAM_ST"))
        pos = (len(MSBStream) & -0x4) + 0x4

        partidx = 0

        for i, partsPage in enumerate(self.parts_data):
            for partsRow in partsPage:
                MSBStream, pos, partidx = self.saveRow(MSBStream, partsRow, self.parts[i], pos, partsPtr, partidx)


        with open(filename, 'wb') as f:
            f.write(bytes(MSBStream))

    """
        @return (byteStream, newPos)
    """
    def WriteBytesAt(self, byteStream, pos, data):
        if (pos > len(byteStream)):
            for i in range(pos - len(byteStream)):
                byteStream.append(0)
        
        #print(pos)
        if (self.effinLimit > 0):
            #print("Adding : " + str(data))
            #print("[LEN] : current(" + str(len(byteStream)) + "), preceeding(" + str(len(byteStream[:pos])) + ") data(" + str(len(data)) + ")")
            #print(byteStream[:pos])
            self.effinLimit = self.effinLimit - 1
        return (byteStream[:pos] + data + byteStream[pos + len(data):], pos + len(data))
        

    def __init__(self):
        #print("new msbio")
        data_layouts.generate(self.models,"models")
        data_layouts.generate(self.events0,"events0")
        data_layouts.generate(self.events1, "events1")
        data_layouts.generate(self.events2, "events2")
        data_layouts.generate(self.events3, "events3")
        data_layouts.generate(self.events4, "events4")
        data_layouts.generate(self.events5, "events5")
        data_layouts.generate(self.events6, "events6")
        data_layouts.generate(self.events7, "events7")
        data_layouts.generate(self.events8, "events8")
        data_layouts.generate(self.events9, "events9")
        data_layouts.generate(self.events10, "events10")
        data_layouts.generate(self.events11, "events11")
        data_layouts.generate(self.events12, "events12")
        data_layouts.generate(self.points0, "points0")
        data_layouts.generate(self.points2, "points2")
        data_layouts.generate(self.points3, "points3")
        data_layouts.generate(self.points5, "points5")
        data_layouts.generate(self.mapPieces0, "mapPieces0")
        data_layouts.generate(self.objects1, "objects1")
        data_layouts.generate(self.creatures2, "creatures2")
        data_layouts.generate(self.creatures4, "creatures4")
        data_layouts.generate(self.collision5, "collision5")
        data_layouts.generate(self.navimesh8, "navimesh8")
        data_layouts.generate(self.objects9, "objects9")
        data_layouts.generate(self.creatures10, "creatures10")
        data_layouts.generate(self.collision11, "collision11")

        self.events = [self.events0, self.events1, self.events2, self.events3, self.events4, self.events5, self.events6, self.events7, self.events8, self.events9, self.events10, self.events11, self.events12]
        
        for i in range(0,len(self.events)):
            self.events_data.append([])

        self.points = [self.points0, self.points2, self.points3, self.points5]

        for i in range(0,len(self.points)):
            self.points_data.append([])

        self.parts = [self.mapPieces0, self.objects1, self.creatures2, self.creatures4, self.collision5, self.navimesh8, self.objects9, self.creatures10, self.collision11]

        for i in range (0, len(self.parts)):
            self.parts_data.append([])

        self.clean()

    def clean(self):

        #print("MSB_IO cleaned")

        self.models_data.clear()
        
        self.events_data.clear()
        for i in range(0,len(self.events)):
            self.events_data.append([])

        self.points_data.clear()
        for i in range(0,len(self.points)):
            self.points_data.append([])

        self.parts_data.clear()
        for i in range (0, len(self.parts)):
            self.parts_data.append([])