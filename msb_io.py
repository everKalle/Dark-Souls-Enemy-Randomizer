from byteread import *
import struct
from msbdata import Msbdata

class MsbIO:
    
    def __init__(self):
        self.models = Msbdata("models")

        # Events
        self.events0 = Msbdata("events0")
        self.events1 = Msbdata("events1")
        self.events2 = Msbdata("events2")
        self.events3 = Msbdata("events3")
        self.events4 = Msbdata("events4")
        self.events5 = Msbdata("events5")
        self.events6 = Msbdata("events6")
        self.events7 = Msbdata("events7")
        self.events8 = Msbdata("events8")
        self.events9 = Msbdata("events9")
        self.events10 = Msbdata("events10")
        self.events11 = Msbdata("events11")
        self.events12 = Msbdata("events12")

        self.events = [self.events0, self.events1, self.events2, self.events3, self.events4, self.events5, self.events6, self.events7, self.events8, self.events9, self.events10, self.events11, self.events12]

        # Points
        self.points0 = Msbdata("points0")
        self.points2 = Msbdata("points2")
        self.points3 = Msbdata("points3")
        self.points5 = Msbdata("points5")

        self.points = [self.points0, self.points2, self.points3, self.points5]

        # Parts
        self.mapPieces0 = Msbdata("mapPieces0")
        self.objects1 = Msbdata("objects1")
        self.creatures2 = Msbdata("creatures2")
        self.creatures4 = Msbdata("creatures4")
        self.collision5 = Msbdata("collision5")
        self.navimesh8 = Msbdata("navimesh8")
        self.objects9 = Msbdata("objects9")
        self.creatures10 = Msbdata("creatures10")
        self.collision11 = Msbdata("collision11")

        self.parts = [self.mapPieces0, self.objects1, self.creatures2, self.creatures4, self.collision5, self.navimesh8, self.objects9, self.creatures10, self.collision11]

    def open(self, filename):
        """
            Opens msb file @filename
        """
        with open(filename, 'rb') as f:
            self.open_bytes(f.read())

    def open_bytes(self, mBytes):
        """
            Opens a msb file from bytes @mBytes.
        """
        self.clean()

        modelPointer = 0
        modelCount = struct.unpack_from("<I", mBytes, 0x8)[0]

        eventPointer = struct.unpack_from("<I", mBytes, modelCount * 0x4 + 0x8)[0]
        eventCount = struct.unpack_from("<I", mBytes, eventPointer + 0x8)[0]

        pointPointer = struct.unpack_from("<I", mBytes, eventCount * 0x4 + 0x8 + eventPointer)[0]
        pointCount = struct.unpack_from("<I", mBytes, pointPointer + 0x8)[0]

        partsPointer = struct.unpack_from("<I", mBytes, pointCount * 0x4 + 0x8 + pointPointer)[0]
        partsCount = struct.unpack_from("<I", mBytes, partsPointer + 0x8)[0]


        # Models
        for i in range(0, modelCount - 1):
            offset = struct.unpack_from("<I", mBytes, modelPointer + 0xC + i * 0x4)[0]
            self.read_row(mBytes, offset, self.models)

        # Events
        for i in range(0, eventCount - 1):
            offset = struct.unpack_from("<I", mBytes, eventPointer + 0xC + i * 0x4)[0]

            eventType = struct.unpack_from("<I", mBytes, offset + 0x8)[0]
            self.read_row(mBytes, offset, self.events[eventType])

        # Points
        pointTypes = [0, 2, 3, 5]
        for i in range(0, pointCount - 1):
            offset = struct.unpack_from("<I", mBytes, pointPointer + 0xC + i * 0x4)[0]

            pointType = pointTypes.index(struct.unpack_from("<I", mBytes, offset + 0xC)[0])
            self.read_row(mBytes, offset, self.points[pointType])

        # Parts
        partTypes = [0, 1, 2, 4, 5, 8, 9, 0xA, 0xB]
        for i in range(0, partsCount - 1):
            offset = struct.unpack_from("<I", mBytes, partsPointer + 0xC + i * 0x4)[0]

            partType = partTypes.index(struct.unpack_from("<I", mBytes, offset + 0x4)[0])
            self.read_row(mBytes, offset, self.parts[partType])


    def save(self, filename, createBackup = True):
        """
            Saves the msb file as @filename.
        """
        if (createBackup):
            with open(filename, 'rb') as origFile:
                with open(filename + '.bak', 'wb') as bakFile:
                    bakFile.write(origFile.read())
        with open(filename, 'wb') as f:
            f.write(self.save_bytes())
    
    def save_bytes(self):
        """
            Write the msb data as bytes and return it.
        """
        offset = 0
        msbBytes = bytearray()

        # Models

        modelCount = len(self.models.rows) + 1
        modelParamStOffset = 0xC + modelCount * 0x4

        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<III", 0, modelParamStOffset, modelCount))
        offset = modelParamStOffset
        offset = self.WriteBytesAt(msbBytes, offset, EncodeString("MODEL_PARAM_ST"))
        offset = (len(msbBytes) & -0x4) + 0x4

        for i in range(modelCount - 1):
            curroffset = offset
            offset = 0xC + i * 0x4
            offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<I", curroffset))
            offset = curroffset

            nameOffset = self.models.rows[i][0]
            name = EncodeString(self.models.rows[i][self.models.nameIndex])
            sibPath = EncodeString(self.models.rows[i][self.models.nameIndex + 1])

            padding = ((len(sibPath) + len(name) + 5) & -0x4)

            if (padding <= 0x10):
                padding = 0x10
                padding += 0x4

            offset = self.WriteBytesAt(msbBytes, offset, struct.pack(self.models.structLayoutBeforeName, *self.models.rows[i][0:self.models.nameIndex]))
            offset = curroffset + nameOffset
            offset = self.WriteBytesAt(msbBytes, offset, name + b'\x00' + sibPath + b'\x00')

            offset = curroffset + nameOffset + padding

        # Events

        eventPointer = (len(msbBytes) & -0x4) + 0x4
        offset = 0xC + (modelCount - 1) * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<I", eventPointer))
        offset = eventPointer

        eventCount = 1
        for eventLayouts in self.events:
            eventCount += len(eventLayouts.rows)

        curroffset = eventPointer + 0xC + eventCount * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<III", 0, curroffset, eventCount))
        offset = curroffset
        offset = self.WriteBytesAt(msbBytes, offset, EncodeString("EVENT_PARAM_ST"))
        offset = (len(msbBytes) & -0x4) + 0x4

        eventIndex = 0
        for eventLayout in self.events:
            for eventRow in eventLayout.rows:
                offset = self.save_row(msbBytes, offset, eventPointer, eventLayout, eventRow, eventIndex)
                eventIndex += 1

        # Points

        pointPointer = len(msbBytes)
        offset = eventPointer + 0xC + (eventCount - 1) * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<I", pointPointer))
        offset = pointPointer

        pointCount = 1
        for pointLayouts in self.points:
            pointCount += len(pointLayouts.rows)

        curroffset = pointPointer + 0xC + pointCount * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<III", 0, curroffset, pointCount))
        offset = curroffset
        offset = self.WriteBytesAt(msbBytes, offset, EncodeString("POINT_PARAM_ST"))
        offset = (len(msbBytes) & -0x4) + 0x4


        allPoints = []
        for layoutIndex, pointLayout in enumerate(self.points):
            for pointRow in pointLayout.rows:
                allPoints.append((pointRow[2], pointRow, layoutIndex))

        sortedPoints = sorted(allPoints, key = lambda x: x[0])
        
        for pointIndex, pointEntry in enumerate(sortedPoints):
            offset = self.save_row(msbBytes, offset, pointPointer, self.points[pointEntry[2]], pointEntry[1], pointIndex)

        # Parts
        
        partsPointer = len(msbBytes)
        offset = pointPointer + 0xC + (pointCount - 1) * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<I", partsPointer))
        offset = partsPointer

        partsCount = 1
        for partsLayouts in self.parts:
            partsCount += len(partsLayouts.rows)
        
        curroffset = partsPointer + 0xC + partsCount * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<III", 0, curroffset, partsCount))

        offset = curroffset
        offset = self.WriteBytesAt(msbBytes, offset, EncodeString("PARTS_PARAM_ST"))
        offset = (len(msbBytes) & -0x4) + 0x4

        partIndex = 0
        for partLayout in self.parts:
            for partRow in partLayout.rows:
                offset = self.save_row(msbBytes, offset, partsPointer, partLayout, partRow, partIndex)
                partIndex += 1

        
        return msbBytes


    def read_row(self, mBytes, pointer: int, msbData: Msbdata):
        """
            Read a row of data with layout @msbData from offset @pointer from bytes @mBytes
        """
        offset = pointer
        row = []

        for i  in range(0, len(msbData.fieldNames)):
            row.append(0)

        strOffset = 0

        nameOffset = struct.unpack_from("<i", mBytes, offset)[0]
        nameBytes = StringFromBytes(mBytes, offset + nameOffset)[0]
        row[msbData.nameIndex] = DecodeString(nameBytes)

        strOffset = len(nameBytes) + 1

        if (msbData.hasSib()):
            sibOffset = struct.unpack_from("<i", mBytes, offset + 0x10)[0]
            if (msbData.name == "models"):
                sibOffset = nameOffset + len(nameBytes) + 1
            sibPathBytes = StringFromBytes(mBytes, offset + sibOffset)[0]
            row[msbData.nameIndex + 1] = DecodeString(sibPathBytes)

            strOffset += len(sibPathBytes) + 1
            if (len(sibPathBytes) == 0):
                strOffset += 0x5

        strOffset = (strOffset + 0x3) & -0x4

        values = struct.unpack_from(msbData.structLayoutBeforeName, mBytes, offset)
        row[0 : len(values)] = values

        if (len(msbData.structLayoutAfterName) > 0):
            offset += nameOffset + strOffset
            values = struct.unpack_from(msbData.structLayoutAfterName, mBytes, offset)

            indexOffset = msbData.nameIndex + 1
            if (msbData.hasSib()):
                indexOffset += 1

            row[indexOffset : indexOffset + len(values)] = values

        msbData.rows.append(row)

    def save_row(self, msbBytes: bytearray, pos, pointer, layout: Msbdata, row, idx):
        """
            Saves a row of data (@row) with layout @layout to @msbBytes
        """
        offset = pointer + 0xC + idx * 0x4
        offset = self.WriteBytesAt(msbBytes, offset, struct.pack("<I", pos))
        offset = pos

        nameOffset = row[0]
        name = EncodeString(row[layout.nameIndex])

        strOffset = len(name) + 1
        if (layout.hasSib()):
            sibPath = EncodeString(row[layout.nameIndex + 1])
            strOffset += len(sibPath) + 1
            if (len(sibPath) == 0):
                strOffset += 0x5

        strOffset = (strOffset + 0x3) & -0x4

        offset = self.WriteBytesAt(msbBytes, offset, struct.pack(layout.structLayoutBeforeName, *row[0:len(layout.structLayoutBeforeName)]))
        offset = self.WriteBytesAt(msbBytes, pos + nameOffset, EncodeString(row[layout.nameIndex]) + b'\x00')

        afterIndex = layout.nameIndex + 1
        if (layout.hasSib()):
            offset = self.WriteBytesAt(msbBytes, offset, EncodeString(row[layout.nameIndex + 1]) + b'\x00')
            afterIndex += 1
        offset = pos + nameOffset + strOffset
        
        if (len(layout.structLayoutAfterName) > 0):
            offset = self.WriteBytesAt(msbBytes, offset, struct.pack(layout.structLayoutAfterName, *row[afterIndex:afterIndex + len(layout.structLayoutAfterName)]))

        return offset


    def WriteBytesAt(self, byteStream: bytearray, pos: int, data: bytes):
        """
            Write bytes @data into bytearray @byteStream at position @pos.
            Extends the bytearray with \x00 if @pos is larger than the length of the bytearray.
        """
        if (pos > len(byteStream)):
            for i in range(pos - len(byteStream)):
                byteStream.append(0)

        byteStream[pos : pos + len(data)] = data
        
        #return (byteStream[:pos] + data + byteStream[pos + len(data):], pos + len(data))
        return pos + len(data)

    def clean(self):
        """
            Clears all the row data.
        """
        self.models.rows.clear()
        for event in self.events:
            event.rows.clear()
        for point in self.points:
            point.rows.clear()
        for part in self.parts:
            part.rows.clear()