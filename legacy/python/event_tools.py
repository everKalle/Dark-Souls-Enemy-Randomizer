from emevd_handler import EmevdHandler, Instruction
from dcx_handler import DCXHandler
import os

idxDisableRespawn = 0
idxTailCut = 1
idxItemLotAwardOnKill = 2
idxGargoyleFix1 = 3
idxGargoyleFix2 = 4
idxMoonlightButterflyAnimation = 5
idxAsylumDemonFix = 6

class EventTools:

    disableRespawnEventIDs = {
        'm10_00_00_00': 11009000,
        'm10_01_00_00': 11019000,
        'm10_02_00_00': 11029000,
        'm11_00_00_00': 11109000,
        'm12_00_00_00': 11209000,
        'm12_01_00_00': 11219000,
        'm13_00_00_00': 11309000,
        'm13_01_00_00': 11319000,
        'm13_02_00_00': 11329000,
        'm14_00_00_00': 11409000,
        'm14_01_00_00': 11419000,
        'm15_00_00_00': 11509000,
        'm15_01_00_00': 11519000,
        'm16_00_00_00': 11609000,
        'm17_00_00_00': 11709000,
        'm18_00_00_00': 11809000,
        'm18_01_00_00': 11819000
    }

    # (partNPCType, itemLotID)
    tailValues = {
        'c2730': (2730, 27310000),  # Crossbreed Priscilla
        'c3430': (3430, 34310000),  # Hellkite
        'c3471': (3471, 34720000),  # Sanctuary Guardian
        'c4510': (4510, 45110000),  # Kalameet
        'c5260': (5260, 52610000),  # Gaping Dragon
        'c5290': (5290, 52910000),  # Seath
        'c5350': (5350, 53520000),  # Bell Gargoyle
        'c5351': (5351, 53530000)   # Anor Londo Gargoyle
    }

    def __init__(self, dcx: bool):
        self.dcx = dcx
        self.eventHandler = None

        eh = EmevdHandler()
        eh.import_dkscript('enemyRandomizerData/eventTemplates.dkscript')

        self.templates = eh.events

        self.currentDisableRespawnEventID = 0
        self.disableRespawnEventIndex = 0

        self.currentTailCutEventID = 0
        self.tailCutEventIndex = 0

        self.currentItemLotAwardEventID = 0
        self.itemlotAwardEventIndex = 0
        
    def open(self, mapName):
        filename = "event/{0}.emevd{1}".format(mapName, ".dcx" if self.dcx else "")

        # Create a backup
        if not os.path.isfile(filename + ".bak"):
            with open(filename, 'rb') as origf:
                with open(filename + '.bak', 'wb') as bakf:
                    bakf.write(origf.read())

        self.disableRespawnEventIndex = 0
        self.tailCutEventIndex = 0

        self.eventHandler = EmevdHandler()

        with open(filename, 'rb') as f:
            content = f.read()
            if (self.dcx):
                dcxh = DCXHandler()
                content = dcxh.open_dcx(content)
            self.eventHandler.read(content)

        self.currentDisableRespawnEventID = self.disableRespawnEventIDs[mapName]
        self.currentTailCutEventID = self.currentDisableRespawnEventID + 200
        self.currentItemLotAwardEventID = self.currentDisableRespawnEventID - 200

        self.templates[idxDisableRespawn].eventId = self.currentDisableRespawnEventID
        self.eventHandler.events.append(self.templates[idxDisableRespawn])

        self.templates[idxTailCut].eventId = self.currentTailCutEventID
        self.eventHandler.events.append(self.templates[idxTailCut])

        self.templates[idxItemLotAwardOnKill].eventId = self.currentItemLotAwardEventID
        self.eventHandler.events.append(self.templates[idxItemLotAwardOnKill])

        self.EnableUndeadDragonsGravity(mapName)

    def save(self, mapName):
        filename = "event/{0}.emevd{1}".format(mapName, ".dcx" if self.dcx else "")

        if (self.eventHandler != None):
            if (self.dcx):
                dcxh = DCXHandler()
                dcxh.set_emevd_dcx_values()
                dcxh.save_dcx(filename, self.eventHandler.write())
            else:
                with open(filename, 'wb') as f:
                    f.write(self.eventHandler.write())

            self.eventHandler = None
        else:
            print("Trying to use EventTools.save() while no .emevd file is open.")

    def EnableUndeadDragonsGravity(self, mapName):
        if (mapName == 'm11_00_00_00'):
            for i in range(len(self.eventHandler.events)):
                if (self.eventHandler.events[i].eventId == 11100400):
                    if (self.eventHandler.events[i].instructions[7].instruction_class == 2004 and self.eventHandler.events[i].instructions[7].instruction_index == 10):
                        self.eventHandler.events[i].instructions[7].args[1] = 1     # Reenable gravity
        elif (mapName == 'm16_00_00_00'):
            for i in range(len(self.eventHandler.events)):
                if (self.eventHandler.events[i].eventId == 11600400):
                    if (self.eventHandler.events[i].instructions[0].instruction_class == 2004 and self.eventHandler.events[i].instructions[0].instruction_index == 31):
                        self.eventHandler.events[i].instructions[0].args[0] = 0     # Reenable collision
                    if (self.eventHandler.events[i].instructions[1].instruction_class == 2004 and self.eventHandler.events[i].instructions[1].instruction_index == 10):
                        self.eventHandler.events[i].instructions[1].args[1] = 1     # Reenable gravity


    def AddRespawnEventInit(self, eventEntityID):
        disableRespawnInstruction = Instruction(0)
        """disableRespawnInstruction.instruction_class = 2000
        disableRespawnInstruction.instruction_index = 0
        disableRespawnInstruction.argTypes = "@iII"
        disableRespawnInstruction.args = [self.disableRespawnEventIndex, self.currentDisableRespawnEventID, eventEntityID]"""
        disableRespawnInstruction.new(2000, 0, "@iII", [self.disableRespawnEventIndex, self.currentDisableRespawnEventID, eventEntityID])

        self.eventHandler.events[0].instructions.append(disableRespawnInstruction)
        self.disableRespawnEventIndex += 1

    def AddTailCutEventInit(self, enemyEntityID, tailEntityID, enemyID):
        tailCutInstruction = Instruction(0)
        tailCutInstruction.new(2000, 0, "@iIIIIII", [self.tailCutEventIndex, self.currentTailCutEventID, enemyEntityID, self.tailValues[enemyID][0], self.tailValues[enemyID][0], tailEntityID, self.tailValues[enemyID][1]])

        self.eventHandler.events[0].instructions.append(tailCutInstruction)
        self.tailCutEventIndex += 1

    def AddItemLotAwardOnDeath(self, enemyEntityID, itemLotID):
        itemLotAwardInstruction = Instruction(0)
        itemLotAwardInstruction.new(2000, 0, "@iIII", [self.itemlotAwardEventIndex, self.currentItemLotAwardEventID, enemyEntityID, itemLotID])

        self.eventHandler.events[0].instructions.append(itemLotAwardInstruction)
        self.itemlotAwardEventIndex += 1

    def SetCharacterAllegiance(self, enemyEntityID, teamTypeID = 6):
        allegianceSetInstruction = Instruction(0)
        allegianceSetInstruction.new(2004, 2, "@iB", [enemyEntityID, teamTypeID])

        self.eventHandler.events[0].instructions.append(allegianceSetInstruction)

    def ApplyGargoyle2Fix(self):
        for i in range(len(self.eventHandler.events)):
            if (self.eventHandler.events[i].eventId == 11015396):
                self.eventHandler.events[i] = self.templates[idxGargoyleFix1]
            elif (self.eventHandler.events[i].eventId == 11015382):
                self.eventHandler.events[i] = self.templates[idxGargoyleFix2]

    def ApplyMoonlightButterflyAnimFix(self):
        for i in range(len(self.eventHandler.events)):
            if (self.eventHandler.events[i].eventId == 11205382):
                self.eventHandler.events[i] = self.templates[idxMoonlightButterflyAnimation]

    def RemoveAsylumDemonWarping(self):
        for i in range(len(self.eventHandler.events)):
            if (self.eventHandler.events[i].eventId == 11810310):
                self.eventHandler.events[i] = self.templates[idxAsylumDemonFix]
                print("Removed Asylum Demon Warping")
