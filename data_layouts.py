class Msbdata:

    def __init__(self):
        self.fieldNames = []
        self.fieldTypes = []

        self.nameIdx = -1
        self.pointIndex1 = -1
        self.pointIndex2 = -1
        self.partIndex1 = -1
        self.partIndex2 = -1
        self.partIndex3 = -1

    def add(self, name, fieldType):
        """Add new entry"""
        self.fieldNames.append(name)
        self.fieldTypes.append(fieldType)

    def retrieveName(self, i):
        return self.fieldNames[i]

    def retrieveType(self, i):
        return self.fieldTypes[i]

    def fieldCount(self):
        return len(self.fieldNames)

    def getNameIndex(self):
        return self.nameIdx

    def setNameIndex(self, idx):
        self.nameIdx = idx

    def getFieldIndex(self, name):
        self.fieldNames.index(name)

    def getPointIndices(self):
        result = []
        if (self.pointIndex1 != -1):
            result.append(self.pointIndex1)
            
        if (self.pointIndex2 != -1):
            result.append(self.pointIndex2)

        return result

    def getPartIndices(self):
        result = []

        if (self.partIndex1 != -1):
            result.append(self.partIndex1)
        
        if (self.partIndex2 != -1):
            result.append(self.partIndex2)

        if (self.partIndex3 != -1):
            result.append(self.partIndex3)

        return result

# Format: <Field Name>:<Field Type>|...
dataLayoutMap = {
    "eventTemplate": "Name Offset:i32|Name Offset:i32|Event Index:i32|Type:i32|Index:i32|Base Data Offset:i32|Type Data Offset:i32|x18:i32|Name:string:[NAME]|PartIndex1:i32:[PRT1]|PointIndex1:i32:[PNT1]|EventEntityID:i32|p+0x0c:i32",
    "models": "Name Offset:i32|Type:i32|Index:i32|Sib Offset:i32|x10:i32|x14:i32|x18:i32|x1C:i32|Name:string:[NAME]|Sibpath:string",
    "events0": "[EVENT]|p+0x10:i32",
    "events1": "[EVENT]|p+0x10:i32|p+0x14:i32",
    "events2": "[EVENT]|ParticleEffectId:i32",
    "events3": "[EVENT]|p+0x10:i32|p+0x14:i32|p+0x18:i32|p+0x1C:i32|p+0x20:i32|p+0x24:i32|p+0x28:i32|p+0x2C:i32|p+0x30:i32|p+0x34:i32|p+0x38:i32|p+0x3C:i32|p+0x40:i32|p+0x44:i32|p+0x48:i32|p+0x4C:i32",
    "events4": "[EVENT]|p+0x10:i32|PartIndex2:i32:[PRT2]|p+0x18:i32|p+0x1C:i32|p+0x20:i32|p+0x24:i32|p+0x28:i32|p+0x2C:i32|p+0x30:i32|p+0x34:i32|p+0x38:i32|p+0x3C:i32|p+0x40:i32",
    "events5": "[EVENT]|p+0x10:i32|p+0x14:i32|p+0x18:i32|p+0x1C:i32|p+0x20:i32|p+0x24:i32|p+0x28:i32|p+0x2C:i32|p+0x30:i32|p+0x34:i32|p+0x38:i32|p+0x3C:i32|PointIndex2:i32:[PNT2]|p+0x44:i32|p+0x48:i32|p+0x4C:i32|PartIndex2:i32:[PRT2]|PartIndex3:i32:[PRT3]|p+0x58:i32|p+0x5C:i32|p+0x60:i32|p+0x64:i32|p+0x68:i32|p+0x6C:i32|p+0x70:i32|p+0x74:i32|p+0x78:i32|p+0x7C:i32|p+0x80:i32|p+0x84:i32|p+0x88:i32|p+0x8C:i32|p+0x90:i32|p+0x94:i32|p+0x98:i32|p+0x9C:i32|p+0xA0:i32|p+0xA4:i32|p+0xA8:i32|p+0xAC:i32|p+0xB0:i32|p+0xB4:i32|p+0xB8:i32|p+0xBC:i32|p+0xC0:i32|p+0xC4:i32|p+0xC8:i32|p+0xCC:i32|p+0xD0:i32|p+0xD4:i32|p+0xD8:i32|p+0xDC:i32|p+0xE0:i32|p+0xE4:i32|p+0xE8:i32|p+0xEC:i32|p+0xF0:i32|p+0xF4:i32|p+0xF8:i32|p+0xFC:i32|p+0x100:i32|p+0x104:i32|p+0x108:i32|p+0x10C:i32",
    "events6": "[EVENT]|p+0x10:i32|p+0x14:i32",
    "events7": "[EVENT]|p+0x10:i32|PartIndex2:i32|p+0x18:i32|p+0x1C:i32",
    "events8": "[EVENT]|PartIndex2:i32:[PRT2]|p+0x14:i32|p+0x18:i32|p+0x1C:i32",
    "events9": "[EVENT]|p+0x10:i32|p+0x14:i32|p+0x18:i32|p+0x1C:i32",
    "events10": "[EVENT]|PointIndex2:i32:[PNT2]|p+0x14:i32|p+0x18:i32|p+0x1C:i32",
    "events11": "[EVENT]|p+0x10:i32|p+0x14:i32|p+0x18:i32|p+0x1C:i32|p+0x20:i32|p+0x24:i32|p+0x28:i32|p+0x2C:i32",
    "events12": "[EVENT]|p+0x10:i32|p+0x14:i32|PointIndex2:i32|p+0x1C:i32",
    "points0": "Name Offset:i32|x04:i32|index:i32|Type:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|x28:i32|x2c:i32|x30:i32|x34:i32|Name:string:[NAME]|p+0x00:i32|p+0x04:i32|EventEntityID:i32",
    "points2": "Name Offset:i32|x04:i32|index:i32|Type:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|x28:i32|x2c:i32|x30:i32|x34:i32|Name:string:[NAME]|p+0x00:i32|p+0x04:i32|p+0x08:f32|EventEntityID:i32",
    "points3": "Name Offset:i32|x04:i32|index:i32|Type:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|x28:i32|x2c:i32|x30:i32|x34:i32|Name:string:[NAME]|p+0x00:i32|p+0x04:i32|p+0x08:f32|p+0x0C:f32|EventEntityID:i32",
    "points5": "Name Offset:i32|x04:i32|index:i32|Type:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|x28:i32|x2c:i32|x30:i32|x34:i32|Name:string:[NAME]|p+0x00:i32|p+0x04:i32|p+0x08:f32|p+0x0C:f32|p+0x10:f32|EventEntityID:i32",
    "mapPieces0": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|DrawGroup1:i32|DrawGroup2:i32|DrawGroup3:i32|DrawGroup4:i32|x4c:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i8|p+x09:i8|p+x0A:i8|p+x0B:i8|p+x0C:i32|p+x10:i32|p+x14:i32|p+x18:i32|p+x1C:i32",
    "objects1": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i32|p+x0C:i8|p+x0D:i8|p+x0E:i8|p+x0F:i8|p+x10:i8|p+x11:i8|p+x12:i8|p+x13:i8|p+x14:i32|p+x18:i32|PartIndex:i32:[PRT1]|p+x20:i32|p+x24:i32|p+x28:i32|p+x2C:i32",
    "creatures2": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i32|p+x0C:i32|p+x10:i32|p+x14:i32|p+x18:i32|p+x1C:i32|AI:i32|NPCParam:i32|TalkID:i32|p+x2C:i32|ChrInitParam:i32|PartIndex:i32:[PRT1]|p+x38:i32|p+x3C:i32|p+x40:i32|p+x44:i32|p+x48:i32|p+x4C:i32|AnimID:i32|p+x54:i32",
    "creatures4": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i32|p+x0C:i32|p+x10:i32|p+x14:i32|p+x18:i32|p+x1C:i32|p+x20:i32|p+x24:i32",
    "collision5": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|p+x04:i8|p+x05:i8|p+x06:i8|p+x07:i8|p+x08:i8|p+x09:i8|p+x0A:i8|p+x0B:i8|p+x0C:i8|p+x0D:i8|p+x0E:i8|p+x0F:i8|p+x10:i8|p+x11:i8|p+x12:i8|p+x13:i8|p+x14:i8|p+x15:i8|p+x16:i8|p+x17:i8|p+x18:i8|p+x19:i8|p+x1A:i8|p+x1B:i8|p+x1C:i8|p+x1D:i8|p+x1E:i8|p+x1F:i8|p+x20:i32|p+x24:i32|p+x28:i32|p+x2C:i32|p+x30:i32|p+x34:i32|p+x38:i32|p+x3C:i16|p+x3E:i16|p+x40:i32|p+x44:i32|p+x48:i32|p+x4C:i32|p+x50:i32|p+x54:i16|p+x56:i16|p+x58:i32|p+x5C:i32|p+x60:i32|p+x64:i32",
    "navimesh8": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|DrawGroup1:i32|DrawGroup2:i32|DrawGroup3:i32|DrawGroup4:i32|x4c:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i8|p+x09:i8|p+x0A:i8|p+x0B:i8|p+x0C:i32|p+x10:i32|p+x14:i32|p+x18:i32|p+x1C:i32|p+x20:i32|p+x24:i32|p+x28:i32|p+x2C:i32|p+x30:i32|p+x34:i32",
    "objects9": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|p+x04:i8|p+x05:i8|p+x06:i8|p+x07:i8|p+x08:i32|p+x0C:i8|p+x0D:i8|p+x0E:i8|p+x0F:i8|p+x10:i8|p+x11:i8|p+x12:i8|p+x13:i8|p+x14:i32|p+x18:i32|PartIndex:i32:[PRT1]|p+x20:i32|p+x24:i32|p+x28:i32|p+x2C:i32",
    "creatures10": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|LightId:i8|FogId:i8|ScatId:i8|p+x07:i8|p+x08:i32|p+x0C:i32|p+x10:i32|p+x14:i32|p+x18:i32|p+x1C:i32|p+x20:i32|NPC ID:i32|p+x28:i32|p+x2C:i32|p+x30:i32|PartIndex:i32:[PRT1]|p+x38:i32|p+x3C:i32|p+x40:i32|p+x44:i32|p+x48:i32|p+x4C:i32|AnimID:i32|p+x54:i32",
    "collision11": "Name Offset:i32|Type:i32|Index:i32|Model:i32|Sib Offset:i32|X pos:f32|Y pos:f32|Z pos:f32|Rot X:f32|Rot Y:f32|Rot Z:f32|Scale X:f32|Scale Y:f32|Scale Z:f32|x38:i32|x3C:i32|x40:i32|x44:i32|x48:i32|x4C:i32|x50:i32|x54:i32|x58:i32|x5C:i32|x60:i32|Name:string:[NAME]|Sibpath:string|EventEntityID:i32|p+x04:i8|p+x05:i8|p+x06:i8|p+x07:i8|p+x08:i8|p+x09:i8|p+x0A:i8|p+x0B:i8|p+x0C:i8|p+x0D:i8|p+x0E:i8|p+x0F:i8|p+x10:i8|p+x11:i8|p+x12:i8|p+x13:i8|p+x14:i8|p+x15:i8|p+x16:i8|p+x17:i8|p+x18:i8|p+x19:i8|p+x1A:i8|p+x1B:i8|p+x1C:i16|p+x1E:i16|p+x20:i32|p+x24:i32"
}

def generate(layout, name):
    
    if (name in dataLayoutMap):
        layoutString = dataLayoutMap[name]
        for pt in layoutString.split('|'):
            parts = pt.split(':')
            if (len(parts) == 1):
                if (parts[0] == "[EVENT]"):
                    generate(layout, "eventTemplate")
                else:
                    raise NameError("Invalid single tag: " + parts[0])
            elif (len(parts) == 3):
                if (parts[2] == "[NAME]"):
                    layout.setNameIndex(layout.fieldCount())
                elif (parts[2] == "[PRT1]"):
                    layout.partIndex1 = layout.fieldCount()
                elif (parts[2] == "[PRT2]"):
                    layout.partIndex2 = layout.fieldCount()
                elif (parts[2] == "[PRT3]"):
                    layout.partIndex3 = layout.fieldCount()
                elif (parts[2] == "[PNT1]"):
                    layout.pointIndex1 = layout.fieldCount()
                elif (parts[2] == "[PNT2]"):
                    layout.pointIndex2 = layout.fieldCount()
                else:
                    raise NameError("Invalid index tag: " + parts[2])

            if (len(parts) > 1):
                layout.add(parts[0], parts[1])
    else:
        raise NameError('Invalid layout name: ' + name)
        
    
