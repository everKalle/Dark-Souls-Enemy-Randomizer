dataLayoutMap = {
    "eventTemplate": "Name Offset:i|Name Offset:i|Event Index:i|Type:i|Index:i|Base Data Offset:i|Type Data Offset:i|x18:i|Name:string:[NAME]|PartIndex1:i:[PRT1]|PointIndex1:i:[PNT1]|EventEntityID:i|p+0x0c:i",
    "models": "Name Offset:i|Type:i|Index:i|Sib Offset:i|x10:i|x14:i|x18:i|x1C:i|Name:string:[NAME]|Sibpath:string",
    "events0": "[EVENT]|p+0x10:i",
    "events1": "[EVENT]|p+0x10:i|p+0x14:i",
    "events2": "[EVENT]|ParticleEffectId:i",
    "events3": "[EVENT]|p+0x10:i|p+0x14:i|p+0x18:i|p+0x1C:i|p+0x20:i|p+0x24:i|p+0x28:i|p+0x2C:i|p+0x30:i|p+0x34:i|p+0x38:i|p+0x3C:i|p+0x40:i|p+0x44:i|p+0x48:i|p+0x4C:i",
    "events4": "[EVENT]|p+0x10:i|PartIndex2:i:[PRT2]|p+0x18:i|p+0x1C:i|p+0x20:i|p+0x24:i|p+0x28:i|p+0x2C:i|p+0x30:i|p+0x34:i|p+0x38:i|p+0x3C:i|p+0x40:i",
    "events5": "[EVENT]|p+0x10:i|p+0x14:i|p+0x18:i|p+0x1C:i|p+0x20:i|p+0x24:i|p+0x28:i|p+0x2C:i|p+0x30:i|p+0x34:i|p+0x38:i|p+0x3C:i|PointIndex2:i:[PNT2]|p+0x44:i|p+0x48:i|p+0x4C:i|PartIndex2:i:[PRT2]|PartIndex3:i:[PRT3]|p+0x58:i|p+0x5C:i|p+0x60:i|p+0x64:i|p+0x68:i|p+0x6C:i|p+0x70:i|p+0x74:i|p+0x78:i|p+0x7C:i|p+0x80:i|p+0x84:i|p+0x88:i|p+0x8C:i|p+0x90:i|p+0x94:i|p+0x98:i|p+0x9C:i|p+0xA0:i|p+0xA4:i|p+0xA8:i|p+0xAC:i|p+0xB0:i|p+0xB4:i|p+0xB8:i|p+0xBC:i|p+0xC0:i|p+0xC4:i|p+0xC8:i|p+0xCC:i|p+0xD0:i|p+0xD4:i|p+0xD8:i|p+0xDC:i|p+0xE0:i|p+0xE4:i|p+0xE8:i|p+0xEC:i|p+0xF0:i|p+0xF4:i|p+0xF8:i|p+0xFC:i|p+0x100:i|p+0x104:i|p+0x108:i|p+0x10C:i",
    "events6": "[EVENT]|p+0x10:i|p+0x14:i",
    "events7": "[EVENT]|p+0x10:i|PartIndex2:i|p+0x18:i|p+0x1C:i",
    "events8": "[EVENT]|PartIndex2:i:[PRT2]|p+0x14:i|p+0x18:i|p+0x1C:i",
    "events9": "[EVENT]|p+0x10:i|p+0x14:i|p+0x18:i|p+0x1C:i",
    "events10": "[EVENT]|PointIndex2:i:[PNT2]|p+0x14:i|p+0x18:i|p+0x1C:i",
    "events11": "[EVENT]|p+0x10:i|p+0x14:i|p+0x18:i|p+0x1C:i|p+0x20:i|p+0x24:i|p+0x28:i|p+0x2C:i",
    "events12": "[EVENT]|p+0x10:i|p+0x14:i|PointIndex2:i|p+0x1C:i",
    "points0": "Name Offset:i|x04:i|index:i|Type:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|x28:i|x2c:i|x30:i|x34:i|Name:string:[NAME]|p+0x00:i|p+0x04:i|EventEntityID:i",
    "points2": "Name Offset:i|x04:i|index:i|Type:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|x28:i|x2c:i|x30:i|x34:i|Name:string:[NAME]|p+0x00:i|p+0x04:i|p+0x08:f|EventEntityID:i",
    "points3": "Name Offset:i|x04:i|index:i|Type:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|x28:i|x2c:i|x30:i|x34:i|Name:string:[NAME]|p+0x00:i|p+0x04:i|p+0x08:f|p+0x0C:f|EventEntityID:i",
    "points5": "Name Offset:i|x04:i|index:i|Type:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|x28:i|x2c:i|x30:i|x34:i|Name:string:[NAME]|p+0x00:i|p+0x04:i|p+0x08:f|p+0x0C:f|p+0x10:f|EventEntityID:i",
    "mapPieces0": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|DrawGroup1:i|DrawGroup2:i|DrawGroup3:i|DrawGroup4:i|x4c:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:b|p+x09:b|p+x0A:b|p+x0B:b|p+x0C:i|p+x10:i|p+x14:i|p+x18:i|p+x1C:i",
    "objects1": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:i|p+x0C:b|p+x0D:b|p+x0E:b|p+x0F:b|p+x10:b|p+x11:b|p+x12:b|p+x13:b|p+x14:i|p+x18:i|PartIndex:i:[PRT1]|p+x20:i|p+x24:i|p+x28:i|p+x2C:i",
    "creatures2": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:i|p+x0C:i|p+x10:i|p+x14:i|p+x18:i|p+x1C:i|AI:i|NPCParam:i|TalkID:i|p+x2C:i|ChrInitParam:i|PartIndex:i:[PRT1]|p+x38:i|p+x3C:i|p+x40:i|p+x44:i|p+x48:i|p+x4C:i|AnimID:i|p+x54:i",
    "creatures4": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:i|p+x0C:i|p+x10:i|p+x14:i|p+x18:i|p+x1C:i|p+x20:i|p+x24:i",
    "collision5": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|p+x04:b|p+x05:b|p+x06:b|p+x07:b|p+x08:b|p+x09:b|p+x0A:b|p+x0B:b|p+x0C:b|p+x0D:b|p+x0E:b|p+x0F:b|p+x10:b|p+x11:b|p+x12:b|p+x13:b|p+x14:b|p+x15:b|p+x16:b|p+x17:b|p+x18:b|p+x19:b|p+x1A:b|p+x1B:b|p+x1C:b|p+x1D:b|p+x1E:b|p+x1F:b|p+x20:i|p+x24:i|p+x28:i|p+x2C:i|p+x30:i|p+x34:i|p+x38:i|p+x3C:h|p+x3E:h|p+x40:i|p+x44:i|p+x48:i|p+x4C:i|p+x50:i|p+x54:h|p+x56:h|p+x58:i|p+x5C:i|p+x60:i|p+x64:i",
    "navimesh8": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|DrawGroup1:i|DrawGroup2:i|DrawGroup3:i|DrawGroup4:i|x4c:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:b|p+x09:b|p+x0A:b|p+x0B:b|p+x0C:i|p+x10:i|p+x14:i|p+x18:i|p+x1C:i|p+x20:i|p+x24:i|p+x28:i|p+x2C:i|p+x30:i|p+x34:i",
    "objects9": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|p+x04:b|p+x05:b|p+x06:b|p+x07:b|p+x08:i|p+x0C:b|p+x0D:b|p+x0E:b|p+x0F:b|p+x10:b|p+x11:b|p+x12:b|p+x13:b|p+x14:i|p+x18:i|PartIndex:i:[PRT1]|p+x20:i|p+x24:i|p+x28:i|p+x2C:i",
    "creatures10": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|LightId:b|FogId:b|ScatId:b|p+x07:b|p+x08:i|p+x0C:i|p+x10:i|p+x14:i|p+x18:i|p+x1C:i|p+x20:i|NPC ID:i|p+x28:i|p+x2C:i|p+x30:i|PartIndex:i:[PRT1]|p+x38:i|p+x3C:i|p+x40:i|p+x44:i|p+x48:i|p+x4C:i|AnimID:i|p+x54:i",
    "collision11": "Name Offset:i|Type:i|Index:i|Model:i|Sib Offset:i|X pos:f|Y pos:f|Z pos:f|Rot X:f|Rot Y:f|Rot Z:f|Scale X:f|Scale Y:f|Scale Z:f|x38:i|x3C:i|x40:i|x44:i|x48:i|x4C:i|x50:i|x54:i|x58:i|x5C:i|x60:i|Name:string:[NAME]|Sibpath:string|EventEntityID:i|p+x04:b|p+x05:b|p+x06:b|p+x07:b|p+x08:b|p+x09:b|p+x0A:b|p+x0B:b|p+x0C:b|p+x0D:b|p+x0E:b|p+x0F:b|p+x10:b|p+x11:b|p+x12:b|p+x13:b|p+x14:b|p+x15:b|p+x16:b|p+x17:b|p+x18:b|p+x19:b|p+x1A:b|p+x1B:b|p+x1C:h|p+x1E:h|p+x20:i|p+x24:i"
}

class Msbdata:

    def __init__(self, name):
        self.fieldNames = []
        self.structLayoutBeforeName = ""
        self.structLayoutAfterName = ""

        self.nameIndex = -1
        self.pointIndex1 = -1
        self.pointIndex2 = -1
        self.partIndex1 = -1
        self.partIndex2 = -1
        self.partIndex3 = -1

        self.name = name

        self.rows = []

        self.create(self.name)

    def add(self, fieldName, fieldType):
        """
            Add a field with name @fieldName and type @fieldType
        """
        if (len(fieldType) == 1):
            self.fieldNames.append(fieldName)
            if (self.nameIndex == -1):
                self.structLayoutBeforeName += fieldType
            else:
                self.structLayoutAfterName += fieldType
        else:
            if (fieldType == "string"):
                self.fieldNames.append(fieldName)
            else:
                raise ValueError("Invalid field type: '" + fieldType + "'")

    def hasSib(self):
        return self.fieldNames[self.nameIndex + 1] == "Sibpath"

    def create(self, name):
        if (name in dataLayoutMap):
            layoutString = dataLayoutMap[name]
            for pt in layoutString.split('|'):
                parts = pt.split(':')
                if (len(parts) == 1):
                    if (parts[0] == "[EVENT]"):
                        self.create("eventTemplate")
                    else:
                        raise NameError("Invalid single tag: " + parts[0])
                elif (len(parts) == 3):
                    if (parts[2] == "[NAME]"):
                        self.nameIndex = len(self.fieldNames)
                    elif (parts[2] == "[PRT1]"):
                        self.partIndex1 = len(self.fieldNames)
                    elif (parts[2] == "[PRT2]"):
                        self.partIndex2 = len(self.fieldNames)
                    elif (parts[2] == "[PRT3]"):
                        self.partIndex3 = len(self.fieldNames)
                    elif (parts[2] == "[PNT1]"):
                        self.pointIndex1 = len(self.fieldNames)
                    elif (parts[2] == "[PNT2]"):
                        self.pointIndex2 = len(self.fieldNames)
                    else:
                        raise NameError("Invalid index tag: " + parts[2])

                if (len(parts) > 1):
                    self.add(parts[0], parts[1])
        else:
            raise NameError('Invalid layout name: ' + name)
