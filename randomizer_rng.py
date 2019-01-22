from random import randint, uniform
import random
import os.path
import os
from msb_io import MsbIO
from luagnl_io import LuaGnl
from luainfo_io import LuaInfo
from bnd_data import BndData
from ffx_data import FFXData
import bnd_rebuilder as bndr
from ai_data import AiDataContainer
from tkinter import *
from tkinter.ttk import Progressbar
import check_exe
import tkinter.messagebox
import datetime
from enum import Enum
from NpcParam import NpcParam
from dcx_handler import DCXHandler
from event_tools import EventTools

#logFile = open('log.txt', 'w')
logFile = -1

def printLog(s, f, console = True):
    """
    Write line @s to file @f, also print it in the console if @console is True
    """
    f.write(s + "\n")
    if (console):
        print(s)

class NewCol(Enum):
    """
    Columns in valid_new.txt
    """
    ID = 0
    NAME = 1
    TYPE = 2
    IGNORED = 3
    SIZE = 4
    DIFFICULTY = 5
    LOCATIONS = 6
    AI = 7
    PARAM = 8
    ANIMIDS = 9

class Randomizer:

    MAPSTUDIO = "map/MapStudio/"
    AISCRIPTS = "script/"
    EMEVDS = "event/"

    MAPCOPY = "enemyRandomizerData/mapStudioCopies/"
    AICOPY = "enemyRandomizerData/mapAiCopies/"

    FFX_DIR = "sfx/FRPG_SfxBnd_{0}.ffxbnd"
    FFX_DIR_REMASTERED = "sfx/FRPG_SfxBnd_{0}.ffxbnd.dcx"
    FFX_COPY_DIR = "enemyRandomizerData/sfxCopies/FRPG_SfxBnd_{0}.ffxbnd"

    GAMEPARAM_PATH = "param/GameParam/GameParam.parambnd"
    GAMEPARAM_PATH_REMASTERED = "param/GameParam/GameParam.parambnd.dcx"
    NPCPARAM_INDEX = 12

    inputFilesAll = ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00", "m12_00_00_01", "m13_00_00_00",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]
    inputFFXFiles = ['CommonEffects', 'm10', 'm10_00', 'm10_01', 'm10_02', 'm11', 'm12', 'm12_00', 'm12_01', 'm13', 'm13_00', 'm13_01', 'm13_02', 'm14', 'm14_00', 'm14_01', 'm15', 'm15_00', 'm15_01', 'm16', 'm17', 'm18', 'm18_00', 'm18_01']
    startIndicesAll = [250, 495, 171, 259, 280, 459, 282, 254, 242, 180, 342, 382, 210, 251, 197, 307, 84, 154]
    namesAll = ["Depths", "Undead Burg/Parish", "Firelink Shrine", "Painted World", "Darkroot Garden", "DLC", "DarkRoot Garden #2", "Catacombs", "Tomb of the Giants", "Great Hollow & Ash Lake", "Blighttown", "Demon Ruins & Lost Izalith", "Sen's Fortress", "Anor Londo", "New Londo Ruins", "Duke's Archives & Crystal Cave", "Kiln of the First Flame", "Northern Undead Asylum"]
    mimicId = "c2780"

    inputFiles = ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00", "m13_00_00_00",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]
    startIndices = [250, 495, 171, 259, 280, 459, 254, 242, 180, 342, 382, 210, 251, 197, 307, 84, 154]
    names = ["Depths", "Undead Burg/Parish", "Firelink Shrine", "Painted World", "Darkroot Garden", "DLC", "Catacombs", "Tomb of the Giants", "Great Hollow & Ash Lake", "Blighttown", "Demon Ruins & Lost Izalith", "Sen's Fortress", "Anor Londo", "New Londo Ruins", "Duke's Archives & Crystal Cave", "Kiln of the First Flame", "Northern Undead Asylum"]

    startEventEntityIDs = [1009400, 1019400, 1029400, 1119400, 1209400, 1219400, 1309400, 1319400, 1329400, 1409400, 1419400, 1509400, 1519400, 1609400, 1709400, 1809400, 1819400]
    # Asylum easy mode is hardcoded for now
    HARDCODED_ASYLUM_NORMAL = [1, 4, 12, 16, 23, 24, 28, 29, 30, 62]
    HARDCODED_ASYLUM_BOSSES = [8, 9, 118]

    # Targets for Easy Asylum
    EASYASYLUM_TARGETS = ['c2232_0000', 'c2500_0000', 'c2500_0001', 'c2500_0002', 'c2500_0003', 'c2500_0005', 'c2500_0006', 'c2500_0007', 'c2500_0009', 'c2500_0010', 'c2500_0011', 'c2550_0000']

    # Tail values
    # (Relative Model Index, NPCparam value)
    TAIL_VALUES = {
        'c2730': (123, 273100),  # Crossbreed Priscilla
        'c3430': (124, 343100),  # Hellkite
        'c3471': (125, 347200),  # Sanctuary Guardian
        'c4510': (126, 451100),  # Kalameet
        'c5260': (127, 526100),  # Gaping Dragon
        'c5290': (128, 529100),  # Seath
        'c5350': (129, 535200),  # Bell Gargoyle
        'c5351': (130, 535300)   # Anor Londo Gargoyle (the second one has NPCParam 535301)
    }

    newCharacterAllegiances = {
        'c2510': 6,
        'c2640': 6,
        'c2920': 6,
        'c4110': 6
    }

    #Following is for testing on few files only

    """Blighttown only"""
    #inputFiles = ["m14_00_00_00"]
    #startIndices = [342]
    #names = ["Blighttown"]


    """Firelink Shrine only"""
    #inputFiles = ["m10_02_00_00"]
    #startIndices = [171]
    #names = ["Firelink Shrine"]

    newAmbushPositions = {
        'm10_01_00_00': {
            'c2500_0001': ([-53.502, -29.666, -36.589], None),          # Hollows that hang from ledges
            'c2500_0008': ([-51.08, -29.69, -40.893], None),
            'c2500_0021': ([-65.055, -31.109, -34.112], None),
            'c2500_0025': ([-67.493, -31.749, -31.279], None),
            'c2500_0026': ([-69.256, -31.844, -29.678], None),
            'c2500_0027': ([-64.886, -31.094, -36.741], None)
        },
        'm16_00_00_00': {
            'c2670_0001': ([73.41113, -143.279, 62.07529], None),       # Ghosts
            'c2670_0003': ([102.7754, -143.208, 47.02634], None),
            'c2670_0004': ([90.55, -148.159, 40.9], None),
            'c2670_0006': ([53.811, -147.03, 1.325704], None),
            'c2670_0007': ([57.76, -146.988, -3.42], None),
            'c2670_0008': ([61.37214, -146.958, -7.72154], None),
            'c2670_0010': ([56.419, -146.96, -9.909], None),
            'c2670_0012': ([63.34, -142.4, 35.43], None),
            'c2670_0013': ([69.561, -142.4, 41.554], None),
            'c2670_0014': ([69.69, -142.4, 51.4], None),
            'c2670_0015': ([51.07, -151.989, 4.251727], None),
            'c2670_0016': ([104.783, -143.202, 50.314], None),
            'c2670_0018': ([19.92, -157.36, 74.24], None),
            'c2670_0019': ([24.623, -157.36, 72.851], None),
            'c2670_0021': ([98.845, -143.237, 49.019], None),
            'c2670_0022': ([98.275, -148.199, 31.818], None),
            'c2670_0023': ([68.064, -147.977, 10.831], None),
            'c2670_0024': ([79.2, -154.158, -12.804], None),
            'c2670_0025': ([47.96, -138.998, -13.26], None),
            'c2670_0026': ([54.03, -147, -13.779], None),
            'c2670_0027': ([65, -146.996, -9.42], None),
            'c2670_0028': ([80.061, -163.157, -1.964], None),
            'c2670_0029': ([34.97, -157.295, 70.12], None),
            'c2670_0030': ([78.7, -143.23, 65.06], None),
            'c2670_0031': ([97.34, -143.196, 53.92], None)
        }
    }

    newOtherPositions = {
        'm16_00_00_00': {
            'c3420_0000': ([22.96, -155.141, -66.44], None)     # Undead Dragon in valley of the drakes
        }
    }

    itemLotsToAward = {
        'm10_01_00_00': {
            'c2300_0000': 23000100,
            'c2570_0000': 25700100      # Berenike Knight guaranteed 1x Titanite Shard
        },
        'm13_00_00_00': {
            'c2300_0000': 23000000
        },
        'm14_01_00_00': {
            'c2300_0000': 23000500,
            'c3480_0000': 34800100      # Sunlight Maggot
        },
        'm15_00_00_00': {
            'c2300_0000': 23000200,
            'c2300_0001': 23000200,
            'c2300_0002': 23000100,
            'c2300_0003': 23000100
        },
        'm15_01_00_00': {
            'c2300_0000': 23000400
        }
    }

    def __init__(self):
        self.validTargets = []
        self.validNew = []
        self.validNewNormalIndices = []
        self.validNewBossIndices = []
        self.validSizeNew = [[], [], [], [], [], []]    #6 size classes
        self.validSizeNormal = [[], [], [], [], [], []]
        self.validSizeBosses = [[], [], [], [], [], []]
        self.aic = None
        self.uniqueIndices = []
        self.uniqueBosses = [[], [], [], [], [], []]
        self.uniqueNormals = [[], [], [], [], [], []]
        self.validDiffNew = [[], [], [], [], [], [], [], []]
        self.validDiffNormal = [[], [], [], [], [], [], [], []]
        self.validDiffBosses = [[], [], [], [], [], [], [], []]

        # Look at this mess >.<
        self.validDiffSizeNew = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.validDiffSizeNormal = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.validDiffSizeBosses = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.MAX_UNIQUE = 30
        self.msbio = MsbIO()
        self.ffxdata = FFXData()

        self.gwynNerfMode = 2
        self.typeSub = False
        self.typeReplaceMap = dict()
        self.disallowSameReplacement = False
        self.attemptUniqueBosses = False
        self.currentBosses = []
        self.spawnNPCS = False

        self.missingMSB = 0
        self.missingLUABND = 0
        self.missingFFXBND = 0
        self.missingEmevd = 0
        self.exeStatus = "None"
        self.hasGameParam = True

        self.missingMSB, self.missingLUABND, self.missingFFXBND, self.missingEmevd, self.exeStatus, self.hasGameParam = self.checkIfRightPlace()

        self.folderStatus = False
        self.aiRefStatus = False
        self.ffxRefStatus = False
        self.validNewStatus = False
        self.validReplaceStatus = False
        self.originalRefMissing = 0
        self.rngState = None

        self.checkProperUnpack()

        self.missingAiCopies = 0
        self.invalidAiCopies = 0
        self.invalidMapCopies = 0
        self.missingMapCopies = 0
        self.invalidSfxCopies = 0
        self.missingSfxCopies = 0

        self.areCopiesValid = False

        self.canRandomize = False
        self.useDCX = False

        self.exeModificationSuccessful = True

        self.writingPermssion = True
        if (self.missingMSB == 0):
            self.writingPermssion = self.checkIfAllowedToModify()

        if (self.missingMSB == 0 and self.missingLUABND == 0 and self.missingFFXBND == 0 and self.missingEmevd == 0 and (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug" or self.exeStatus == "Unknown" or self.exeStatus == "Patched" or self.exeStatus == "Patched Debug" or self.exeStatus == "Remaster") and
            self.folderStatus and self.aiRefStatus and self.ffxRefStatus and self.validNewStatus and self.validReplaceStatus and self.writingPermssion and self.originalRefMissing == 0): 
            if (self.exeStatus == "Remaster"):
                self.useDCX = True
                self.startIndicesAll = [250, 495, 171, 259, 280, 464, 282, 254, 242, 181, 342, 382, 210, 251, 197, 307, 84, 154]
                self.startIndices = [250, 495, 171, 259, 280, 464, 254, 242, 181, 342, 382, 210, 251, 197, 307, 84, 154]
                self.MAX_UNIQUE = 60        #can use a much higher unique limit for remaster
                #self.MAX_UNIQUE = 30

                """Firelink shrine only"""
                #self.startIndices = [171]
            self.canRandomize = True
            self.aic = AiDataContainer('enemyRandomizerData/airef.csv')

            if (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug"):
                self.exeModificationSuccessful = check_exe.patch_exe()
            
            if (self.exeModificationSuccessful):
                self.firstTimeSetup()
                self.cleanupV032backup()

                self.retryFileCopy()
            else:
                self.canRandomize = False

    def cleanupV032backup(self):
        """
        Fix the invalid backups created by v0.3.2
        """
        doCleanup = False
        if (os.path.isfile(self.MAPSTUDIO + 'm10_00_00_00.msb.bak')):
            self.msbio.open(self.MAPSTUDIO + 'm10_00_00_00.msb.bak')
            if (len(self.msbio.models.rows) >= 373):
                doCleanup = True

        if (doCleanup):
            print("Detected invalid .msb file backups from v0.3.2, attempting to fix.")
            try:
                for i, iFile in enumerate(self.inputFiles):
                    if (os.path.isfile(self.MAPCOPY + iFile + '.msb')):
                        self.msbio.open(self.MAPCOPY + iFile + '.msb')
                        self.msbio.models.rows = self.msbio.models.rows[:self.startIndices[i]]
                        self.msbio.save(self.MAPSTUDIO + iFile + '.msb.bak', False)
                        print("Fixed " + self.MAPSTUDIO + iFile + '.msb.bak')
                    else:
                        print("Failed to fix backup: " + self.MAPSTUDIO + iFile + '.msb.bak, ' + self.MAPCOPY + iFile + '.msb from previous use of the randomizer does not exist.')
            except:
                print("[ERROR] Failed to fix the backups.")



    def createBackup(self, filename):
        """
        Creates a backup of file @filename if it doesn't exist.
        """
        if (not os.path.isfile(filename + '.bak')) and os.path.isfile(filename):
            with open(filename + '.bak', 'wb') as bakf:
                with open(filename, 'rb') as oldf:
                    bakf.write(oldf.read())
                    print(filename + " backed up")

    def restoreBackup(self, filename, warnMissing = True):
        """
        Restores the backup of @filename.
        """
        if (os.path.isfile(filename + '.bak')):
            with open(filename, 'wb') as oldf:
                with open(filename + '.bak', 'rb') as bakf:
                    oldf.write(bakf.read())
                    print(filename + " reverted")
        else:
            if ((not self.useDCX) and (not "FRPG_SfxBnd" in filename) and warnMissing):
                print("Failed to restore " + filename + ", " + filename + ".bak not found.")

        
    def checkIfRightPlace(self):
        """
        Check if Randomizer is placed into the correct location and all the necessary files exist.
        """
        print("Checking location")

        notFoundMSB = 0
        notFoundLUABND = 0
        notFoundFFXBND = 0
        notFoundEmevd = 0
        gameParamExists = False

        exeStatus = check_exe.check_exe_checksum()
        check_for_dcx = False
        if (exeStatus == "Remaster"):
            check_for_dcx = True

        for iFile in self.inputFilesAll:
            if not (os.path.isfile(self.MAPSTUDIO + iFile + '.msb')):
                notFoundMSB += 1

            if not (iFile == "m12_00_00_01"):
                if (check_for_dcx):
                    if not (os.path.isfile(self.AISCRIPTS + iFile + '.luabnd.dcx')):
                        notFoundLUABND += 1
                else:
                    if not (os.path.isfile(self.AISCRIPTS + iFile + '.luabnd')):
                        notFoundLUABND += 1

                if (check_for_dcx):
                    if not (os.path.isfile(self.EMEVDS + iFile + '.emevd.dcx')):
                        notFoundEmevd += 1
                else:
                    if not (os.path.isfile(self.EMEVDS + iFile + '.emevd')):
                        notFoundEmevd += 1
        
        for iFile in self.inputFFXFiles:
            if (iFile != "NONE"):
                if (check_for_dcx):
                    if not (os.path.isfile(self.FFX_DIR_REMASTERED.format(iFile))):
                        notFoundFFXBND += 1
                else:
                    if not (os.path.isfile(self.FFX_DIR.format(iFile))):
                        notFoundFFXBND += 1

        if (check_for_dcx):
            gameParamExists = os.path.isfile(self.GAMEPARAM_PATH_REMASTERED)
        else:
            gameParamExists = os.path.isfile(self.GAMEPARAM_PATH)

        return (notFoundMSB, notFoundLUABND, notFoundFFXBND, notFoundEmevd, exeStatus, gameParamExists)

    def checkProperUnpack(self):
        """
        Check if the randomizer was properly unpacked (all files in enemyRandomizerData folder exist)
        """
        print("Checking Randomizer files")

        self.folderStatus = False
        self.aiRefStatus = False
        self.ffxRefStatus = True
        self.validNewStatus = False
        self.validReplaceStatus = False
        self.originalRefMissing = 0

        if (os.path.isdir("enemyRandomizerData/")):
            self.folderStatus = True

            if (os.path.isfile("enemyRandomizerData/airef.csv")):
                self.aiRefStatus = True

            if (os.path.isfile("enemyRandomizerData/replacement_ref/valid_new.txt")):
                self.validNewStatus = True

            if (os.path.isfile("enemyRandomizerData/replacement_ref/valid_replacements.txt")):
                self.validReplaceStatus = True

            for iFile in ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00.ptde", "m12_01_00_00.remaster", "m12_00_00_01", "m13_00_00_00.remaster",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]:
                if not (os.path.isfile('enemyRandomizerData/original_enemies_ref/' + iFile + '.txt')):
                    self.originalRefMissing += 1
        
    def checkCopiedFiles(self):
        """
        Check if copied files exist and appear to be valid.
        """
        self.missingAiCopies = 0
        self.invalidAiCopies = 0
        self.invalidMapCopies = 0
        self.missingMapCopies = 0

        for iFile in self.inputFilesAll:
            if not (os.path.isfile(self.MAPCOPY + iFile + '.msb')):
                self.missingMapCopies += 1
            else:
                with open(self.MAPCOPY + iFile + '.msb', 'rb') as testFile:
                    if (len(testFile.read()) < 10):
                        self.invalidMapCopies += 1

            if not (iFile == "m12_00_00_01"):
                if (self.useDCX):
                    if not (os.path.isfile(self.AICOPY + iFile + '.luabnd.dcx')):
                        self.missingAiCopies += 1
                    else:
                        with open(self.AICOPY + iFile + '.luabnd.dcx', 'rb') as testFile:
                            if (len(testFile.read()) < 10):
                                self.invalidAiCopies += 1
                else:
                    if not (os.path.isfile(self.AICOPY + iFile + '.luabnd')):
                        self.missingAiCopies += 1
                    else:
                        with open(self.AICOPY + iFile + '.luabnd', 'rb') as testFile:
                            if (len(testFile.read()) < 10):
                                self.invalidAiCopies += 1

        if (self.missingAiCopies > 0 or self.invalidAiCopies > 0 or self.missingMapCopies > 0 or self.invalidMapCopies > 0 or self.missingSfxCopies > 0 or self.invalidSfxCopies > 0):
            return False
        else:
            return True

    def retryFileCopy(self):
        """
        Try copying files a few times if something goes wrong.
        """
        self.areCopiesValid = self.checkCopiedFiles()
        copyRetryCount = 5
        while(copyRetryCount > 1 and not self.areCopiesValid):
            print("Something failed in copy, retrying " + str(copyRetryCount))
            self.firstTimeSetup()
            self.areCopiesValid = self.checkCopiedFiles()
            copyRetryCount -= 1

    def checkIfAllowedToModify(self):
        """
        Janky check to see if we have writing permission (for at least one file).
        """

        oldBytes = b''
        testFileName = self.MAPSTUDIO + self.inputFiles[0] + '.msb'

        with open(testFileName, 'rb') as oldf:
            oldBytes = oldf.read()

        # Try writing something to the file

        try:
            with open(testFileName, 'wb') as outf:
                outf.write(b'TESTINGIFICANWRITEINTOTHISFILE')
        except:
            return False

        # Because apparently for _some_ reason it doesn't throw an error sometimes(?) so we confirm if the file was actually modified

        newBytes = b''
        with open(testFileName, 'rb') as oldf:
            newBytes = oldf.read()

        if (oldBytes == newBytes):
            return False

        # Restore the file to normal

        with open(testFileName, 'wb') as outf:
            outf.write(oldBytes)

        oldBytes = b''
        newBytes = b''

        return True

    def firstTimeSetup(self):
        """
        Perform first time setup if necessary.
        """
        print("Checking Files, Please Wait")
        if not (os.path.isdir("enemyRandomizerData/mapAiCopies")):     #create map ai copy directory
            os.makedirs("enemyRandomizerData/mapAiCopies")

        if not (os.path.isdir("enemyRandomizerData/mapStudioCopies")):     #create map studio copy directory
            os.makedirs("enemyRandomizerData/mapStudioCopies")

        modelsToAdd = ["c1200", "c1201", "c1202", "c1203", "c2060", "c2230", "c2231", "c2232", "c2240", "c2250", "c2260", "c2270", "c2280", "c2300", "c2310", "c2320", "c2330", "c2360", "c2370", "c2380", "c2390", "c2400", "c2410", "c2430", "c2500", "c2510", "c2520", "c2530", "c2540", "c2550", "c2560", "c2570", "c2640", "c2650", "c2660", "c2670", "c2680", "c2690", "c2700", "c2710", "c2711", "c2730", "c2780", "c2790", "c2791", "c2792", "c2793", "c2800", "c2810", "c2811", "c2830", "c2840", "c2860", "c2870", "c2900", "c2910", "c2920", "c2930", "c2940", "c2950", "c2960", "c3090", "c3200", "c3210", "c3220", "c3230", "c3240", "c3250", "c3270", "c3300", "c3320", "c3330", "c3340", "c3341", "c3350", "c3370", "c3380", "c3390", "c3400", "c3410", "c3420", "c3421", "c3430", "c3460", "c3461", "c3471", "c3480", "c3490", "c3491", "c3500", "c3520", "c3530", "c4100", "c4110", "c4120", "c4130", "c4150", "c4160", "c4170", "c4171", "c4172", "c4180", "c4190", "c4500", "c4510", "c5200", "c5201", "c5202", "c5210", "c5220", "c5240", "c5250", "c5260", "c5270", "c5271", "c5280", "c5290", "c5320", "c5350", "c5351", "c5360", "c5370", "c5390"]
        
        tailModels = ['c2731', 'c3431', 'c3472', 'c4511', 'c5261', 'c5291', 'c5352', 'c5353']
        modelsToAdd += tailModels

        MODEL_TYPE_OFFSET = 1
        MODEL_IDX_OFFSET = 2
        MODEL_NAME_OFFSET = 8
        MODEL_SIBPATH_OFFSET = 9

        SIBPATH_FORMAT = "N:\FRPG\data\Model\chr\{0}\sib\{0}.sib"

        for j in enumerate(self.inputFilesAll):                                    #backup msb/luabnd
            print("[Check/Preparation] Map and script files " + str(j[0]) + "/" + str(len(self.inputFiles)))
            copyMissing = not (os.path.isfile(self.MAPCOPY + j[1] + '.msb'))
            invalidCopy = False
            needsModelsListUpdate = False
            if not (copyMissing):
                with open(self.MAPCOPY + j[1] + '.msb', 'rb') as testf:
                    testData = testf.read()
                    if (len(testData) < 10):
                        invalidCopy = True

            if not (invalidCopy):
                self.msbio.open(self.MAPCOPY + j[1] + '.msb')
                if (len(self.msbio.models.rows) < self.startIndicesAll[j[0]] + len(modelsToAdd)):
                    needsModelsListUpdate = True
                    print("Models list requires update. Current model count: {0}, expected model count: {1}.".format(len(self.msbio.models.rows), self.startIndicesAll[j[0]] + len(modelsToAdd)))
                self.msbio.clean()

            if (copyMissing or invalidCopy or needsModelsListUpdate):
                
                self.msbio.open(self.MAPSTUDIO + j[1] + '.msb')

                lastModelIndex = 0
                for model in self.msbio.models.rows:
                    if (model[MODEL_TYPE_OFFSET] == 2):     #if it's a character model
                        if (model[MODEL_IDX_OFFSET] > lastModelIndex):
                            lastModelIndex = model[MODEL_IDX_OFFSET]
                
                for i, modelName in enumerate(modelsToAdd):
                    modelRow = [32, 2, lastModelIndex + 1 + i, 38, 1, 0, 0, 0, modelName, SIBPATH_FORMAT.format(modelName)]
                    self.msbio.models.rows.append(modelRow)

                self.msbio.save(self.MAPCOPY + j[1] + '.msb', False)
                self.msbio.clean()
                print(" > Map File copied and prepared")

            if not (j[1] == "m12_00_00_01"):
                if (not self.useDCX):
                    if not (os.path.isfile(self.AICOPY + j[1] + '.luabnd')):
                        with open(self.AICOPY + j[1] + '.luabnd', 'wb') as bakf:
                            with open(self.AISCRIPTS + j[1] + '.luabnd', 'rb') as oldf:
                                bakf.write(oldf.read())
                        print(" > AI File copied")
                    else:
                        passed = False
                        with open(self.AICOPY + j[1] + '.luabnd', 'rb') as testf:
                            r = testf.read()
                            if (len(r) < 10):
                                passed = True
                        if (passed):
                            with open(self.AICOPY + j[1] + '.luabnd', 'wb') as bakf:
                                with open(self.AISCRIPTS + j[1] + '.luabnd', 'rb') as oldf:
                                    bakf.write(oldf.read())
                            print(" > AI File backed up")
                else:
                    if not (os.path.isfile(self.AICOPY + j[1] + '.luabnd.dcx')):
                        with open(self.AICOPY + j[1] + '.luabnd.dcx', 'wb') as bakf:
                            with open(self.AISCRIPTS + j[1] + '.luabnd.dcx', 'rb') as oldf:
                                bakf.write(oldf.read())
                        print(" > AI File (DCX) copied")
                    
                    else:
                        passed = False
                        with open(self.AICOPY + j[1] + '.luabnd.dcx', 'rb') as testf:
                            r = testf.read()
                            if (len(r) < 10):
                                passed = True
                        if (passed):
                            with open(self.AICOPY + j[1] + '.luabnd.dcx', 'wb') as bakf:
                                with open(self.AISCRIPTS + j[1] + '.luabnd.dcx', 'rb') as oldf:
                                    bakf.write(oldf.read())
                            print(" > AI File backed up")
            else:
                print(" > AI Copy ignored m12_00_00_01, doesn't have one (This is supposed to happen)")
                    

        if not (os.path.isfile('enemyRandomizerData/aiscripts/' + '120000_battle.lua') and os.path.isfile('enemyRandomizerData/aiscripts/' + '540000_battle.lua')):    #extract ai scripts
            print("[Check/Preparation] Extracting ai scripts")
            if not (os.path.isdir("enemyRandomizerData/aiscripts")):
                os.makedirs("enemyRandomizerData/aiscripts")
            luabnd = BndData()
            luabnd.generateAiScripts('script/', 'enemyRandomizerData/aiscripts/', self.useDCX)

        if not (os.path.isdir("enemyRandomizerData/logs")):     #create log directory
            print("[Check/Preparation] Created log directory")
            os.makedirs("enemyRandomizerData/logs")

        if not (os.path.isdir("enemyRandomizerData/param")):
            print("[Check/Preparation] Created param directory")
            os.makedirs("enemyRandomizerData/param")

        paramPath = 'param/GameParam/GameParam.parambnd'
        copyParamPath = 'enemyRandomizerData/param/GameParam.parambnd'
        if (self.useDCX):
            paramPath += '.dcx'
            copyParamPath += '.dcx'

        if (not os.path.isfile(copyParamPath)):
            with open(paramPath, 'rb') as origf:
                with open(copyParamPath, 'wb') as bakf:
                    bakf.write(origf.read())
                    print("[Check/Preparation] Backed up GameParam.param")

        print("[Check/Preparation] Preparing effect files (Takes a while)")
        self.ffxdata.AddEverythingToCommon(self.useDCX)

        print("[Check/Preparation] Done")

    def check(self):            #check whether or not necessary files are there
        passedCheck = True
        printLog("Checking Files", logFile)
        for j in enumerate(self.inputFilesAll):
            s = " - ref file exists"
            s2 = " - msb file exists"
            s3 = " - luabnd file exists"
            s4 = " - sfx file exists"
            refFileName = j[1]
            if (j[1] == 'm12_01_00_00'):
                if (self.useDCX):
                    refFileName = 'm12_01_00_00.remaster'
                else:
                    refFileName = 'm12_01_00_00.ptde'
            elif (j[1] == 'm13_00_00_00'):
                if (self.useDCX):
                    refFileName = 'm13_00_00_00.remaster'
                else:
                    refFileName = 'm13_00_00_00.ptde'
            if not (os.path.isfile('enemyRandomizerData/original_enemies_ref/' + refFileName + '.txt')):
                s = " !!! REF FILE NOT FOUND"
                passedCheck = False
            if not (os.path.isfile(self.MAPSTUDIO + j[1] + '.msb')):
                s2 = " !!! MSB FILE NOT FOUND"
                passedCheck = False

            aiFileName = j[1]
            if j[1] == "m12_00_00_01":
                aiFileName = "m12_00_00_00"
            if (self.useDCX):
                if not (os.path.isfile(self.AISCRIPTS + aiFileName + '.luabnd.dcx')):
                    s3 = " !!! LUABND FILE NOT FOUND"
                    passedCheck = False
            else:
                if not (os.path.isfile(self.AISCRIPTS + aiFileName + '.luabnd')):
                    s3 = " !!! LUABND FILE NOT FOUND"
                    passedCheck = False
            if (j[1] != "m12_00_00_01"):
                if (self.useDCX):
                    if not (os.path.isfile(self.FFX_DIR_REMASTERED.format(self.inputFFXFiles[j[0]]))):
                        s4 = " !!! SFX FILE NOT FOUND"
                else:
                    if not (os.path.isfile(self.FFX_DIR.format(self.inputFFXFiles[j[0]]))):
                        s4 = " !!! SFX FILE NOT FOUND"
            printLog(j[1] + " - " + self.namesAll[j[0]] + " - offset: " + str(self.startIndicesAll[j[0]]) + s + s2 + s3 + s4, logFile)
        return passedCheck

    def isValid(self, s):
        """
        Is enemy a valid replacement target.
        """
        for valid in self.validTargets:
            if (valid[0] in s):
                return True
        return False

    def getValidDiff(self, s):
        for valid in self.validTargets:
            if (valid[0] in s):
                return valid[3]
        return -1

    def validIndex(self, s):
        for (i, valid) in enumerate(self.validTargets):
            if (valid[0] in s):
                return i
        return -1

    def loadFiles(self, enemyConfigName):
        """
        Load enemy data.
        """
        self.validNew.clear()
        self.validTargets.clear()
        self.validNewNormalIndices.clear()
        self.validNewBossIndices.clear()
        for i in range(0, 6):
            self.validSizeNew[i].clear()
            self.validSizeNormal[i].clear()
            self.validSizeBosses[i].clear()

        for i in range(0, 8):
            for j in range(0, 6):
                self.validDiffSizeNew[i][j].clear()
                self.validDiffSizeNormal[i][j].clear()
                self.validDiffSizeBosses[i][j].clear()
            self.validDiffNew[i].clear()
            self.validDiffNormal[i].clear()
            self.validDiffBosses[i].clear()

        # Load valid replacement targets
        printLog("Loading valid targets", logFile)
        f = open('enemyRandomizerData/replacement_ref/valid_replacements.txt', 'r')
        firstLine = True
        for line in f:
            if (not firstLine):
                parts = line.strip().split("\t")
                self.validTargets.append(parts)
            else:
                firstLine = False
        f.close()
        printLog("Done, " + str(len(self.validTargets)) + " valid target enemies", logFile)

        printLog("Loading valid new enemies", logFile)
        printLog("Enemy Config: " + enemyConfigName, logFile)
        configPath = 'enemyRandomizerData/replacement_ref/valid_new.txt'
        if (enemyConfigName != 'Default'):
            configPath = 'enemyRandomizerData/customConfigs/' + enemyConfigName + '.txt'
        
        f = open(configPath, 'r')
        firstLine = True

        configStringForLog = ""

        for line in f:
            if (not firstLine):
                parts = line.strip().split("\t")
                parts_ai = parts[NewCol.AI.value].split(";")
                parts_param = parts[NewCol.PARAM.value].split(";")
                validAnimIDS = parts[NewCol.ANIMIDS.value].split(";")
                if (len(parts_ai) == len(parts_param)):
                    newEntry = parts[NewCol.ID.value:NewCol.AI.value]
                    newEntry.append(parts_ai)
                    newEntry.append(parts_param)
                    newEntry.append(validAnimIDS)
                    self.validNew.append(newEntry)

                    notIgnored = False
                    if (parts[NewCol.IGNORED.value] == "0"):
                        notIgnored = True
                    elif (parts[NewCol.IGNORED.value] == "2" and self.useDCX):
                        notIgnored = True
                        print("[Remaster] Loading Artorias")

                    configStringForLog += parts[NewCol.IGNORED.value]
                    
                    if (notIgnored):
                        if (parts[NewCol.TYPE.value] == "0" or (parts[NewCol.TYPE.value] == "2" and self.spawnNPCS)):
                            self.validNewNormalIndices.append(len(self.validNew) - 1)
                        elif (parts[NewCol.TYPE.value] == "1"):
                            self.validNewBossIndices.append(len(self.validNew) - 1)

                        nwSize = int(parts[NewCol.SIZE.value])
                        nwDiff = int(parts[NewCol.DIFFICULTY.value])
                        for i in range(nwSize, 6):          # Populate size lists
                            self.validSizeNew[i].append(len(self.validNew) - 1)
                            self.validDiffSizeNew[nwDiff][i].append(len(self.validNew) - 1)
                            if (parts[NewCol.TYPE.value] == "0" or (parts[NewCol.TYPE.value] == "2" and self.spawnNPCS)):
                                self.validSizeNormal[i].append(len(self.validNew) - 1)
                                self.validDiffSizeNormal[nwDiff][i].append(len(self.validNew) - 1)
                            elif (parts[NewCol.TYPE.value] == "1"):
                                self.validSizeBosses[i].append(len(self.validNew) - 1)
                                self.validDiffSizeBosses[nwDiff][i].append(len(self.validNew) - 1)
                        
                        # 
                        self.validDiffNew[nwDiff].append(len(self.validNew) - 1)
                        if (parts[NewCol.TYPE.value] == "0" or (parts[NewCol.TYPE.value] == "2" and self.spawnNPCS)):
                            self.validDiffNormal[nwDiff].append(len(self.validNew) - 1)
                        elif (parts[NewCol.TYPE.value] == "1"):
                            self.validDiffBosses[nwDiff].append(len(self.validNew) - 1)

                else:
                    printLog("AI AND PARAM DONT MATCH ON " + line, logFile)      # valid_new.txt is messed up
            else:
                firstLine = False
        f.close()
        printLog("Done, " + str(len(self.validNew)) + " valid new enemies", logFile)
        if (enemyConfigName != 'Default'):
            printLog(configStringForLog, logFile)

    def getRandomFromList(self, l):
        """
        Returns a random element from list @l.
        """
        if (len(l) == 0):
            return -1
        return l[randint(0, len(l) - 1)]

    def getDifficultyList(self, desiredDifficulty, diffStrictness, isBoss, maxSize):
        classDiffs = [7, 6, 5, 4, 3, 2, 1, -1, -2, -3, -4, -5, -6, -7, 0]
        chances = [0, 0, 0, 0, 0, 2, 12, 5, 0, 0, 0, 0, 0, 0, 81]
        if (diffStrictness == 1):
            chances = [0, 0, 2, 3, 4, 8, 15, 5, 2, 1, 0, 0, 0, 0, 60]
        elif (diffStrictness == 2):
            chances = [1, 3, 4, 5, 7, 10, 20, 5, 3, 1, 1, 0, 0, 0, 40]

        if (sum(chances) != 100):
            print("[WARNING] Difficulty chances don't add up properly")
        if (len(classDiffs) != len(chances)):
            print("[WARNING] length of classDiffs and chances do not match")

        chanceSum = 0
        currentClass = desiredDifficulty
        desiredRNG = random.randint(0, 100)
        returnClass = -1

        for i, chance in enumerate(chances):
            currentClass = desiredDifficulty + classDiffs[i]
            if (currentClass >= 0 and currentClass <= 7):
                if (chance > 0):
                    chanceSum += chance
                    if (chanceSum > desiredRNG):
                        returnClass = currentClass
                        break
        
        returnDefault = False

        if (returnClass == -1):
            returnDefault = True
        else:
            if (isBoss):
                if (len(self.validDiffSizeBosses[returnClass][maxSize]) == 0):
                    returnDefault = True
            else:
                if (len(self.validDiffSizeNormal[returnClass][maxSize]) == 0):
                    returnDefault = True
        
        if not returnDefault:
            if (isBoss):
                return self.validDiffSizeBosses[returnClass][maxSize]
            else:
                return self.validDiffSizeNormal[returnClass][maxSize]

        if (isBoss):
            return self.validDiffSizeBosses[desiredDifficulty][maxSize]
        else:
            return self.validDiffSizeNormal[desiredDifficulty][maxSize]

    def GetEnemyFromListWithRetry(self, enemyList, originalEnemyID, isBoss = False):
        """
        Try getting a new enemy from list @enemyList until a valid replacement is found.
        """

        l = enemyList
        newEnemyID = ''
        returnChar = -1
        idx = -1
        foundValid = False

        while(len(l) > 0):
            idx = randint(0, len(l) - 1)
            returnChar = l[idx]
            newEnemyID = self.validNew[returnChar][NewCol.ID.value]
            if (not self.isCombinationInvalid(originalEnemyID, newEnemyID)):
                if (isBoss):
                    if (self.attemptUniqueBosses):
                        if (not returnChar in self.currentBosses or len(self.currentBosses) == 22 or len(l) == 1):
                            if (len(self.currentBosses) == 22):
                                self.currentBosses.clear()
                            foundValid = True
                        else:
                            l = l[:idx] + l[idx + 1:]
                    else:
                        foundValid = True
                else:
                    foundValid = True

                if (foundValid):
                    break
            else:
                l = l[:idx] + l[idx + 1:]

        if (not foundValid):
            return -4

        return returnChar

    def GetNormalEnemy(self, diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID):
        """
        @diffmode           selected difficulty mode
        @mapname            the map we are currently randomizing
        @careAboutLimit     whether or not we should comply with the unique enemy limit
        @maxSize            maximum size of the enemy
        @desiredDifficulty  the difficulty class we should aim for
        @diffStrictness     how strictly the difficulty curve should be followed
        @originalEnemyID    the enemy we are replacing

        Returns the index a normal enemy.
        """
        newC = -1
        if (not careAboutLimit or len(self.uniqueIndices) < self.MAX_UNIQUE):
            if (diffmode == 1):
                diffList = self.getDifficultyList(desiredDifficulty, diffStrictness, False, maxSize)
                if (len(diffList) > 0):
                    newC = self.GetEnemyFromListWithRetry(diffList, originalEnemyID)
                else:
                    newC = -6
            else:
                newC = self.GetEnemyFromListWithRetry(self.validSizeNormal[maxSize], originalEnemyID)
        else:
            newC = self.GetEnemyFromListWithRetry(self.uniqueNormals[maxSize], originalEnemyID)

        if (diffmode >= 3 and mapname == "m18_01_00_00" and originalEnemyID in self.EASYASYLUM_TARGETS):
            newC = self.getRandomFromList(self.HARDCODED_ASYLUM_NORMAL)

        return newC

    def GetBossEnemy(self, diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID, canBeNormal, replacingBoss = False):
        """
        @diffmode           selected difficulty mode
        @mapname            the map we are currently randomizing
        @careAboutLimit     whether or not we should comply with the unique enemy limit
        @maxSize            maximum size of the enemy
        @desiredDifficulty  the difficulty class we should aim for
        @diffStrictness     how strictly the difficulty curve should be followed
        @originalEnemyID    the enemy we are replacing
        @canBeNormal        can we also return a normal enemy in case we fail to find an appropriate boss enemy

        Returns the index a boss enemy.
        """
        newC = -1
        if (not careAboutLimit or len(self.uniqueIndices) < self.MAX_UNIQUE):
            if (diffmode == 1):
                diffList = self.getDifficultyList(desiredDifficulty, diffStrictness, True, maxSize)
                if (len(diffList) > 0):
                    newC = self.GetEnemyFromListWithRetry(diffList, originalEnemyID, replacingBoss)
                else:
                    if (canBeNormal):
                        return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)
                    else:
                        newC = -6
            else:
                newC = self.GetEnemyFromListWithRetry(self.validSizeBosses[maxSize], originalEnemyID, replacingBoss)
        else:
            if (len(self.uniqueBosses) == 0):
                if (canBeNormal):
                    return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)
            else:
                newC = self.GetEnemyFromListWithRetry(self.uniqueBosses[maxSize], originalEnemyID)

        if (diffmode >= 3 and mapname == "m18_01_00_00" and originalEnemyID in self.EASYASYLUM_TARGETS):
            newC = self.getRandomFromList(self.HARDCODED_ASYLUM_BOSSES)

        return newC

    def GetNormalOrBossEnemy(self, diffmode, mapname, bosschance, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID, replacingBoss = False):
        """
        @diffmode           selected difficulty mode
        @mapname            the map we are currently randomizing
        @bosschance         the chance of this enemy being a boss instead of a normal enemy
        @careAboutLimit     whether or not we should comply with the unique enemy limit
        @maxSize            maximum size of the enemy
        @desiredDifficulty  the difficulty class we should aim for
        @diffStrictness     how strictly the difficulty curve should be followed
        @originalEnemyID    the enemy we are replacing
        @canBeNormal        can we also return a normal enemy in case we fail to find an appropriate boss enemy

        Returns the index a normal or a boss enemy.
        """
        if (randint(1, 100) <= bosschance):
            return self.GetBossEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID, True, replacingBoss)
        else:
            return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)

    def revertToNormal(self, revertEffectFiles = True):
        """
        Restore the backups of all modified files.
        """
        for j in enumerate(self.inputFiles):
            # Load the backups of msb/luabnd files
            print("[Unrandomize] Reverting msb and luabnd files " + str(j[0]) + "/" + str(len(self.inputFiles)))
            self.restoreBackup(self.MAPSTUDIO + j[1] + '.msb')
            self.restoreBackup('event/{0}.emevd{1}'.format(j[1], '.dcx' if self.useDCX else ''))
            
            if not (j[1] == "m12_00_00_01"):
                if (self.useDCX):
                    self.restoreBackup(self.AISCRIPTS + j[1] + '.luabnd.dcx')
                else:
                    self.restoreBackup(self.AISCRIPTS + j[1] + '.luabnd')

        if (revertEffectFiles):
            for iFile in self.inputFFXFiles:
                if (iFile != "NONE"):
                    if (self.useDCX):
                        self.restoreBackup(self.FFX_DIR_REMASTERED.format(iFile))
                    else:
                        self.restoreBackup(self.FFX_DIR.format(iFile))

            check_exe.restore_exe()

        self.revertParam()

    def revertEmevds(self):
        for inFile in self.inputFiles:
            self.restoreBackup('event/{0}.emevd{1}'.format(inFile, '.dcx' if self.useDCX else ''), False)

    def applyEmevd(self, emevdName):
        """
        Replaces an emevd file with a custom one.
        """
        emevdFileName = emevdName + '.emevd'
        emevdPathName = 'PTDE/'
        if (self.useDCX):
            emevdFileName = emevdName + '.emevd.dcx'
            emevdPathName = 'REMASTER/'

        self.createBackup('event/' + emevdFileName)
            
        if os.path.isfile('enemyRandomizerData/emevd/' + emevdPathName + emevdFileName):
            with open('event/' + emevdFileName, 'wb') as oldf:
                with open('enemyRandomizerData/emevd/' + emevdPathName + emevdFileName, 'rb') as modf:
                    oldf.write(modf.read())
                    print('copied new ' + emevdFileName)
    
    def applyBossSouls(self, soulPercentage:int, disableRespawn:bool):
        """
        Adds new NpcParam entries for bosses, so there's a separate version that drops souls when killed (@soulPercentage % of the souls dropped from the original boss fight).
        Also changes the respawn flags of those versions, so they will not respawn upon a reload and optionally stay permanently dead.
        """
        paramPath = self.GAMEPARAM_PATH

        if (self.useDCX):
            paramPath = self.GAMEPARAM_PATH_REMASTERED

        paramData = []
        content = b''
        dcxh = DCXHandler()

        with open(paramPath, 'rb') as f:
            content = f.read()
        
        if (self.useDCX):
            content = dcxh.open_dcx(content)
        
        paramData = bndr.unpack_bnd(content)

        np = NpcParam()
        np.read(paramData[self.NPCPARAM_INDEX][2])
        np.AddNewBossParams()

        nData = np.write()
        np = NpcParam()
        np.read(nData)
        np.ApplyBossSoulCount(soulPercentage)
        np.SetRespawnFlags(disableRespawn, False)
        np.RemoveItemLots()

        paramData[self.NPCPARAM_INDEX] = (paramData[self.NPCPARAM_INDEX][0], paramData[self.NPCPARAM_INDEX][1], np.write())

        content = bndr.repack_bnd(paramData)
        if (self.useDCX):
            dcxh.save_dcx(paramPath, content)
        else:
            with open(paramPath, 'wb') as f:
                f.write(content)

    def revertParam(self):
        """
        Revert NpcParam.param in param/GameParam/GameParam.parambnd
        """
        paramPath = self.GAMEPARAM_PATH
        copyPath = 'enemyRandomizerData/param/GameParam.parambnd'

        if (self.useDCX):
            paramPath = self.GAMEPARAM_PATH_REMASTERED
            copyPath += '.dcx'

        paramDataBak = []
        content = b''
        dcxh = DCXHandler()
        with open(copyPath, 'rb') as f:
            content = f.read()
        
        if (self.useDCX):
            content = dcxh.open_dcx(content)

        paramDataBak = bndr.unpack_bnd(content)
        paramData = []
        
        with open(paramPath, 'rb') as f:
            content = f.read()
        
        if (self.useDCX):
            content = dcxh.open_dcx(content)

        paramData = bndr.unpack_bnd(content)
        paramData[self.NPCPARAM_INDEX] = paramDataBak[self.NPCPARAM_INDEX]

        if (self.useDCX):
            dcxh.save_dcx(paramPath, bndr.repack_bnd(paramData))
        else:
            with open(paramPath, 'wb') as f:
                f.write(bndr.repack_bnd(paramData))
    

    def isCombinationInvalid(self, oldID, newID):
        """
        Returns True if an enemy with @oldID is not allowed to be replaced with an enemy with @newID
        """
        if ('c5320' in oldID):          # Gwyndolin (To avoid the bosses clipping out of the world and dying, which causes the player to get teleported to the arena)
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c2240' in oldID):          # Capra Demon (These replacements can get stuck floating above the arena)
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c3320_0000' in oldID):     # Pinwheeeeeeeee (Can clip above the arena, being unkillable)
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c2320' in oldID):          # Iron Golem (Can get stuck floating)
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c3350' in newID):          # Tree (they are so tall that they can block a walkway above them)
            if ('c2800' in oldID):      # Undead Crystal Soldiers
                return True
            elif ('c2370' in oldID):    # Channeler
                return True

        # When type replacement is enabled, avoid replacing multiple enemy types in one area with the same enemy
        if (self.typeSub):
            for key in self.typeReplaceMap:
                tVal = self.typeReplaceMap[key]
                if (self.validNew[tVal][NewCol.ID.value] in newID):
                    return True

        # Nerfing gwyn spawn rate.
        if (self.gwynNerfMode < 2):
            if ('c5370' in newID):
                rngThreshhold = 85
                if (self.gwynNerfMode == 1):
                    rngThreshhold = 60
                if (uniform(0, 100) < rngThreshhold):
                    return True

        # Replacement with the same enemy
        if (self.disallowSameReplacement):
            if (newID in oldID):
                return True
        
        if (newID == ''):
            return True
        return False

    def copyDarkrootGarden(self):
        self.msbio.open(self.MAPCOPY + "m12_00_00_01.msb")
        refMsb = MsbIO()
        refMsb.open(self.MAPSTUDIO + "m12_00_00_00.msb")

        MODEL_DATA_COL = 3
        NPCAI_DATA_COL = 38
        PARAM_DATA_COL = 39

        EVENT_ENTITY_ID_DATA_COL = 27
        ANIMID_DATA_COL = 50

        POS_DATA_COL = 5    # X pos , Y + 1, Z + 2, ROTX + 3, ROTY + 4, ROTZ + 5;

        MODEL_INDEX_DIFF = 2

        rowCount = len(self.msbio.parts[2].rows)

        for i in range(len(refMsb.parts[2].rows)):
            if (i < rowCount):
                self.msbio.parts[2].rows[i][MODEL_DATA_COL] = refMsb.parts[2].rows[i][MODEL_DATA_COL] + MODEL_INDEX_DIFF
                self.msbio.parts[2].rows[i][NPCAI_DATA_COL] = refMsb.parts[2].rows[i][NPCAI_DATA_COL]
                self.msbio.parts[2].rows[i][PARAM_DATA_COL] = refMsb.parts[2].rows[i][PARAM_DATA_COL]
                self.msbio.parts[2].rows[i][EVENT_ENTITY_ID_DATA_COL] = refMsb.parts[2].rows[i][EVENT_ENTITY_ID_DATA_COL]
                self.msbio.parts[2].rows[i][ANIMID_DATA_COL] = refMsb.parts[2].rows[i][ANIMID_DATA_COL]

                self.msbio.parts[2].rows[i][POS_DATA_COL] = refMsb.parts[2].rows[i][POS_DATA_COL]
                self.msbio.parts[2].rows[i][POS_DATA_COL + 1] = refMsb.parts[2].rows[i][POS_DATA_COL + 1]
                self.msbio.parts[2].rows[i][POS_DATA_COL + 2] = refMsb.parts[2].rows[i][POS_DATA_COL + 2]
                self.msbio.parts[2].rows[i][POS_DATA_COL + 3] = refMsb.parts[2].rows[i][POS_DATA_COL + 3]
                self.msbio.parts[2].rows[i][POS_DATA_COL + 4] = refMsb.parts[2].rows[i][POS_DATA_COL + 4]
                self.msbio.parts[2].rows[i][POS_DATA_COL + 5] = refMsb.parts[2].rows[i][POS_DATA_COL + 5]
            else:
                self.msbio.AddCreatureRow(refMsb.parts[2].rows[i])

        self.msbio.save(self.MAPSTUDIO + "m12_00_00_01.msb")

    def randomize(self, settings, msgArea):
        """
        Perform the randomization
        """
        global logFile
        currentTime = datetime.datetime.now()
        timeString = f"{currentTime:%Y-%m-%d-%H-%M-%S}"
        logFile = open('enemyRandomizerData/logs/rlog' + timeString + '.txt', 'w')      # Create logfile

        self.firstTimeSetup()

        if (self.check()):
            # Get settings
            progressBar, progressLabel, bossMode, enemyMode, npcMode, mimicMode, fitMode, diffMode, replaceChance, bossChance, bossChanceBosses, gargoyleMode, diffStrictness, tposeCity, bossSoulDrops, chaosPinwheel, typeReplacement, gwynNerf, preventSame, uniqueBosses, respawningBosses, hostileNPC, seed, textConfig, enemyConfigName = settings

            self.gwynNerfMode = gwynNerf
            self.disallowSameReplacement = (preventSame == 0)
            self.attemptUniqueBosses = (uniqueBosses == 1)
            disableRoamingBossRespawning = (respawningBosses == 1)
            self.spawnNPCS = (hostileNPC == 1)

            # Generate a seed if none is provided.
            if (seed == ""):
                random.seed(datetime.datetime.now())
                seed = str(random.randrange(sys.maxsize))
            
            random.seed(seed)

            self.exeStatus = check_exe.check_exe_checksum()

            #Patch the exe if necessary
            if (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug"):
                check_exe.patch_exe()

            self.ffxdata.AddEverythingToCommon(self.useDCX)

            self.revertEmevds()             # Restore original .emevd files so modifications are not made multiple times

            # Replace original event scripts with custom ones.
            self.applyEmevd('m12_01_00_00') # Mimic drops
            self.applyEmevd('m13_00_00_00') # Skeleton immortality removed
            self.applyEmevd('m14_01_00_00') # Remove BoC parts AI activation, remove immortality of actual boss immediately, remove immortality of branches
            self.applyEmevd('m15_00_00_00') # Mimic drops
            self.applyEmevd('m15_01_00_00') # Make the statue disappear if Gwyndolin dies, mimic drops
            self.applyEmevd('m17_00_00_00') # Remove Seath's immortality immediately when the crystal is broken instad of waiting for a flag from the animation. Mimic drops, passive Pisaca drops

            #msbio = MsbIO()
            luagnl = LuaGnl()
            luainfo = LuaInfo()
            luabnd = BndData()
            eventTools = EventTools(self.useDCX)

            MODEL_DATA_COL = 3
            NPCAI_DATA_COL = 38
            PARAM_DATA_COL = 39

            EVENT_ENTITY_ID_DATA_COL = 27
            ANIMID_DATA_COL = 50

            POS_DATA_COL = 5    # X pos , Y + 1, Z + 2, ROTX + 3, ROTY + 4, ROTZ + 5;

            originalUniqueLimit = self.MAX_UNIQUE
            
            progressBar.step()
            progressLabel.config(text="Loading Files")
            self.loadFiles(enemyConfigName)
            msgArea.config(state = "normal")

            # Log settings to the log file
            printLog("----\n Starting Randomization \n----", logFile)
            printLog("Boss Replacement: {0}; Normal Replacement: {1}; NPC Replacement: {2}".format(bossMode, enemyMode, npcMode), logFile)
            printLog("Replace Chance: {0}%; Boss Chance (Normal): {1}%; Boss Chance (Bosses): {2}%".format(replaceChance, bossChance, bossChanceBosses), logFile)
            printLog("Fit Mode: {0}; Difficulty Mode: {1}; Difficulty Strictness: {2}".format(fitMode, diffMode, diffStrictness), logFile)
            printLog("T-Pose: {0}; Type Replacement: {1}; Same Enemy Prevention: {2}".format(tposeCity, typeReplacement, preventSame), logFile)
            printLog("Mimic Replacement: {0}; Gargoyle #2 Replacement: {1}; Pinwheel Chaos: {2}".format(mimicMode, gargoyleMode, chaosPinwheel), logFile)
            printLog("Roaming Boss Soul Drops: {0}%; Gwyn Spawn Rate Nerf: {1}; Attempt Unique Bosses: {2}".format(bossSoulDrops, gwynNerf, uniqueBosses), logFile)
            printLog("Disable Roaming Boss Respawning {0}; Spawn Hostile NPCs: {1}".format(respawningBosses, hostileNPC), logFile)
            printLog("Seed: '{0}'".format(seed), logFile)
            printLog("Max Unique: {0}".format(self.MAX_UNIQUE), logFile)

            printLog("----", logFile)
            printLog("Textconfig:", logFile)
            textConfig = textConfig.replace("''''''", "'''" + seed + "'''")
            printLog(textConfig, logFile)
            printLog("----", logFile)

            printLog("Applying " + str(bossSoulDrops) + "% roaming boss soul drops.", logFile)
            self.applyBossSouls(bossSoulDrops, False)
            printLog("----", logFile)

            i = 0
            for inputIndex, inFile in enumerate(self.inputFiles):
                if (inFile == "m14_00_00_00"):
                    if (self.useDCX):
                        self.MAX_UNIQUE = 50
                    else:
                        self.MAX_UNIQUE = 24
                else:
                    self.MAX_UNIQUE = originalUniqueLimit

                printLog("Randomizing " + inFile + " - " + self.names[i] + " (" + str(self.MAX_UNIQUE) + ")", logFile)
                msgArea.insert(END,  "Randomizing " + inFile + " - " + self.names[i] + "\n")

                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i])

                self.createBackup(self.MAPSTUDIO + inFile + ".msb")
                self.msbio.open(self.MAPCOPY + inFile + ".msb")

                aiFileName = inFile
                if inFile == "m12_00_00_01":
                    aiFileName = "m12_00_00_00"

                gnlBytes, infoBytes = luabnd.open(self.AICOPY + aiFileName + ".luabnd", self.useDCX)
                luagnl.open_bytes(gnlBytes)
                luainfo.open_bytes(infoBytes)

                eventTools.open(inFile)
                currentEventEntityID = self.startEventEntityIDs[i]

                self.uniqueIndices = []
                self.uniqueBosses = [[], [], [], [], [], []]
                self.uniqueNormals = [[], [], [], [], [], []]

                refFileName = inFile
                if (inFile == 'm12_01_00_00'):
                    if (self.useDCX):
                        refFileName = 'm12_01_00_00.remaster'
                    else:
                        refFileName = 'm12_01_00_00.ptde'
                elif (inFile == 'm13_00_00_00'):
                    if (self.useDCX):
                        refFileName = 'm13_00_00_00.remaster'
                    else:
                        refFileName = 'm13_00_00_00.ptde'

                f = open('enemyRandomizerData/original_enemies_ref/' + refFileName + '.txt', 'r')

                rowIndex = 0

                self.typeReplaceMap = dict()
                self.typeSub = typeReplacement != 1
                self.typeExceptBosses = typeReplacement == 2

                for line in f:
                    parts = line.split("\t")
                    creatureId = parts[0]
                    creatureSize = parts[1]

                    changePos = False
                    newPos = (0.00, 0.00, 0.00)
                    newRot = (0.00, 0.00, 0.00)

                    # In addition to 'globaly' ignored enemies, certain specific enemies must be ignored or have things changed about them as well:
                    specialCase = False
                    if (inFile == "m10_01_00_00" and "c2250" in creatureId):            #Taurus demon (Boss in burg) - special animation for jumping down
                        changePos = True
                        newPos = (1.16, 15.82, -114.34)
                        newRot = (0.00, -73.54, 0.00)
                    elif (inFile == "m10_01_00_00" and "c5350_0001" in creatureId):     #Second gargoyle in the boss fight
                        if (gargoyleMode == 0):
                            specialCase = True
                        else:
                            changePos = True
                            newPos = (10.69, 48.92, 124.35)
                            newRot = (0.00, 1.84, 0.00)
                    elif ((inFile == "m12_00_00_00" or inFile == "m12_00_00_01") and "c3230_0000" in creatureId):     # Moonlight Butterfly boss
                        changePos = True
                        newPos = (196.12, 8.09, 62.25)
                        newRot = (0.00, 27.37, 0.00)
                    elif (inFile == "m14_01_00_00" and "c5250_0000" in creatureId):     # Cheesless Discharge
                        changePos = True
                        newPos = (396.14, -278.14, 74.56)
                        newRot = (0.00, 130.84, 0.00)
                    elif (inFile == "m12_00_00_00" and "c3530_0000" in creatureId):     # Hydra (Basin)
                        changePos = True
                        newPos = (140.30, -72.31, -194.80)
                        newRot = (0.00, 141.00, 0.00)
                    elif (inFile == "m12_00_00_01" and "c3530_0000" in creatureId):     # Hydra (Basin)
                        changePos = True
                        newPos = (140.30, -72.31, -194.80)
                        newRot = (0.00, 141.00, 0.00)
                    elif (inFile == "m13_02_00_00" and "c3530_0000" in creatureId):     # Hydra (Ash Lake)
                        changePos = True
                        newPos = (-440.52, -411.72, 15.16)
                        newRot = (0.00, -144.53, 0.00)
                    elif ("c5401_0000" in creatureId):          # BoC Parasite
                        changePos = True
                        newPos = (548.65, -437.23, 416.95)
                        newRot = (0.00, 53.00, 0.00)
                    elif ("c5400_0000" in creatureId):          # BoC Large
                        changePos = True
                        newPos = (548, -340.23, 416.95)
                        newRot = (0.00, 53.00, 0.00)
                    elif ("c2232" in creatureId):
                        changePos = True
                        newPos = (3.41, 197.61, -23.10)
                        newRot = (0.00, 180.0, 0.00)
                    elif ("c5290_0000" in creatureId):                                       #Seath (Scripted death) - needs to be able to kill you in the forced death room 
                        specialCase = True
                    elif (inFile == "m17_00_00_00" and "c2690_0000" in creatureId):     #Key Serpent - need the key (unless you dukeskip of course), new enemy either doesnt drop it
                        specialCase = True
                    elif ("c5310_0000" in creatureId):                                  #Gwynevere - Can die the moment you enter anor londo and that breaks the game: can't get the lordvessel (the cutscene after gwynevere death doesnt trigger even if you go in the room) and enemies are missing like it's dark anor londo
                        specialCase = True
                    elif ("c3320" in creatureId and inFile == "m13_00_00_00"):          #Pinwheel boss fight
                        if (chaosPinwheel == 1):
                            if (not "c3320_0000" in creatureId):
                                specialCase = True
                        else:
                            if ("c3320_0000" in creatureId):
                                specialCase = True
                    elif ("c4510_0000" in creatureId or "c4510_0002" in creatureId):    #Kalameet flying versions
                        specialCase = True
                    elif ("c3300" in creatureId and inFile == "m13_02_00_00"):          #Crystal Lizards in Great Hollow, for whatever reason they make the Great Hollow super unstable
                        specialCase = True

                    if ("c2900" in creatureId and inFile == "m13_01_00_00"):            # don't replace small skeletons in ToG (Ravelord Nito fight)
                        specialCase = True
                    if (("c2910_0019" in creatureId or "c2910_0020" in creatureId or "c2910_0021" in creatureId) and inFile == "m13_01_00_00"):    # don't replace large skeletons in Ravelord Nito fight
                        specialCase = True

                    if (self.isValid(creatureId) and not specialCase):
                        newChar = -1

                        creatureTypeId = creatureId.split('_')[0]

                        creatureType = self.validTargets[self.validIndex(creatureId)][2]

                        if (inFile == "m13_00_00_00"):       # Only consider the actual bossfight main pinwheel (the one that actually takes damage) a boss (and not the clones and the ones in ToG)
                            if (chaosPinwheel == 0):
                                if ("c3320_0000" in creatureId):
                                    creatureType = "0"
                                elif ("c3320" in creatureId):
                                    creatureType = "1"
                            else:
                                if ("c3320_0000" in creatureId):
                                    creatureType = "1"
                        elif (inFile == "m10_01_00_00" and "c2250" in creatureId):          # Consider taurus boss a boss
                            creatureType = "1"
                        elif (inFile == "m14_01_00_00" and "c2240" in creatureId):          # Consider capras in Demon Ruins normal enemies
                            creatureType = "0"
                        elif (inFile == "m15_01_00_00" and "c2860_0000" in creatureId):     # Consider blacksmith giant a npc
                            creatureType = "2"
                        elif (inFile == "m14_00_00_00" and "c3210_0000" in creatureId):     # Eingyi
                            creatureType = "2"

                        
                        if (randint(1, 100) <= replaceChance):

                            maxCreatureSize = 5
                            if (fitMode == 0):
                                maxCreatureSize = int(creatureSize)
                            
                            expectedDifficulty = int(self.validTargets[self.validIndex(creatureId)][3])

                            if (creatureType == "0" and enemyMode != 0):       #replacing normal
                                isMimic = self.mimicId in creatureId

                                if (not isMimic or mimicMode == 1):                                 #mimic replace mode
                                    if (enemyMode == 1):     #replace with bosses only
                                        newChar = self.GetBossEnemy(diffMode, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False)
                                    elif (enemyMode == 2):   #replace with normals only
                                        newChar = self.GetNormalEnemy(diffMode, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                    elif (enemyMode == 3):   #replace with both
                                        newChar = self.GetNormalOrBossEnemy(diffMode, inFile, bossChance, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                else:
                                    newChar = -3

                            elif (creatureType == "1" and bossMode != 0):     #replacing boss (dont care about enemy limit so bosses _can_ be unique)
                                if (bossMode == 1):     #replace with bosses only
                                    newChar = self.GetBossEnemy(diffMode, inFile, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False, True)

                                    if (not newChar in self.currentBosses):
                                        self.currentBosses.append(newChar)

                                elif (bossMode == 2):   #replace with normals only
                                    newChar = self.GetNormalEnemy(diffMode, inFile, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                elif (bossMode == 3):   #replace with both
                                    newChar = self.GetNormalOrBossEnemy(diffMode, inFile, bossChanceBosses, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, True)

                                    if (self.validNew[newChar][NewCol.TYPE.value] == "1" and not newChar in self.currentBosses):
                                        self.currentBosses.append(newChar)

                            elif (creatureType == "2" and npcMode != 0):     #replacing NPC
                                if (fitMode == 2):
                                    maxCreatureSize = int(creatureSize)
                                if (npcMode == 1):     #replace with bosses only
                                    newChar = self.GetBossEnemy(2, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False)
                                elif (npcMode == 2):   #replace with normals only
                                    newChar = self.GetNormalEnemy(2, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                elif (npcMode == 3):   #replace with both
                                    newChar = self.GetNormalOrBossEnemy(2, inFile, bossChance, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)

                                if ("c2640" in creatureId):                 # Special Andre -> Gwyndolin Replacement
                                    if (npcMode == 1 or npcMode == 3):
                                        if (randint(1,100) > 60):
                                            newChar = 117
                        
                        else:
                            newChar = -2

                        if (self.typeSub and creatureTypeId in self.typeReplaceMap and creatureType != "1"):
                            if (self.typeExceptBosses):
                                if (self.validNew[newChar][NewCol.TYPE.value] != "1"):
                                    newChar = self.typeReplaceMap[creatureTypeId]
                            else:
                                newChar = self.typeReplaceMap[creatureTypeId]
                        else:
                            if (self.typeExceptBosses):
                                if (self.validNew[newChar][NewCol.TYPE.value] != "1"):
                                    self.typeReplaceMap[creatureTypeId] = newChar
                            else:
                                self.typeReplaceMap[creatureTypeId] = newChar

                        if (newChar >= 0):
                            if (not newChar in self.uniqueIndices and not creatureType == "1"):
                                self.uniqueIndices.append(newChar)
                                if (newChar in self.validNewBossIndices):
                                    for idx in range(int(self.validNew[newChar][NewCol.SIZE.value]), 6):
                                        self.uniqueBosses[idx].append(newChar)
                                else:
                                    for idx in range(int(self.validNew[newChar][NewCol.SIZE.value]), 6):
                                        self.uniqueNormals[idx].append(newChar)
                            
                            newAI = ""
                            newParam = ""
                            if (len(self.validNew[newChar][NewCol.AI.value]) == 1):
                                newAI = self.validNew[newChar][NewCol.AI.value][0]
                                newParam = self.validNew[newChar][NewCol.PARAM.value][0]
                            else:
                                newAiParamIndex = randint(0, len(self.validNew[newChar][NewCol.AI.value]) - 1)
                                newAI = self.validNew[newChar][NewCol.AI.value][newAiParamIndex]
                                newParam = self.validNew[newChar][NewCol.PARAM.value][newAiParamIndex]

                            paramValue = int(newParam)
                            if (creatureType == "0" and newChar in self.validNewBossIndices and (self.validNew[newChar][NewCol.ID.value] != 'c5351')):
                                paramValue += 50

                            self.msbio.parts[2].rows[rowIndex][PARAM_DATA_COL] = paramValue
                            aiStr = "  ai = <original>; param = " + newParam
                            if(not self.validTargets[self.validIndex(creatureId)][2] == "2"):    # lets not mod npc ai for now
                                self.msbio.parts[2].rows[rowIndex][NPCAI_DATA_COL] = int(newAI)
                                aiStr = " ai = " + newAI + "; param = " + str(paramValue)
                            self.msbio.parts[2].rows[rowIndex][MODEL_DATA_COL] = self.startIndices[i] + newChar

                            aiEntry = self.aic.GetEntryByAI(newAI)

                            exists = luainfo.AddEntryAuto(aiEntry.info)
                            if not (exists):
                                luagnl.AddEntriesAuto(aiEntry.aiFuncsGnl)
                                luabnd.addAuto(aiEntry.battle_script)
                                luabnd.addAuto(aiEntry.logic_script)

                            # Change assigned animation if T-Posing is off.
                            animLine = ""
                            if (tposeCity == 1):
                                if (creatureType != "2"):
                                    currentAnim = self.msbio.parts[2].rows[rowIndex][ANIMID_DATA_COL]
                                    if (currentAnim != -1):
                                        newAnim = self.getRandomFromList(self.validNew[newChar][NewCol.ANIMIDS.value])
                                        self.msbio.parts[2].rows[rowIndex][ANIMID_DATA_COL] = int(newAnim)
                                        animLine = " >> changing idle anim from " + str(currentAnim) + " to " + newAnim + ";"

                            if ("c2232" in creatureId and "c2232" in self.validNew[newChar][NewCol.ID.value]):
                                changePos = False

                            # Update position if necessary:
                            posLine = ""
                            if (changePos):
                                posLine = " changed position"
                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL] = newPos[0]
                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 1] = newPos[1]
                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 2] = newPos[2]

                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 3] = newRot[0]
                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 4] = newRot[1]
                                self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 5] = newRot[2]

                            # Ambush position change
                            if (inFile in self.newAmbushPositions):
                                if (creatureId in self.newAmbushPositions[inFile]):
                                    posLine = " changed position"
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL] = self.newAmbushPositions[inFile][creatureId][0][0]
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 1] = self.newAmbushPositions[inFile][creatureId][0][1]
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 2] = self.newAmbushPositions[inFile][creatureId][0][2]

                                    if (self.newAmbushPositions[inFile][creatureId][1] != None):
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 3] = self.newAmbushPositions[inFile][creatureId][1][0]
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 4] = self.newAmbushPositions[inFile][creatureId][1][1]
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 5] = self.newAmbushPositions[inFile][creatureId][1][2]

                            # Other position changes
                            if (inFile in self.newOtherPositions):
                                if (creatureId in self.newOtherPositions[inFile]):
                                    posLine = " changed position"
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL] = self.newOtherPositions[inFile][creatureId][0][0]
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 1] = self.newOtherPositions[inFile][creatureId][0][1]
                                    self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 2] = self.newOtherPositions[inFile][creatureId][0][2]

                                    if (self.newOtherPositions[inFile][creatureId][1] != None):
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 3] = self.newOtherPositions[inFile][creatureId][1][0]
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 4] = self.newOtherPositions[inFile][creatureId][1][1]
                                        self.msbio.parts[2].rows[rowIndex][POS_DATA_COL + 5] = self.newOtherPositions[inFile][creatureId][1][2]

                            # Add scripted item drops
                            if (inFile in self.itemLotsToAward):
                                if (creatureId in self.itemLotsToAward[inFile]):
                                    if (self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] == -1):
                                        self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] = currentEventEntityID
                                        currentEventEntityID += 1

                                    eventTools.AddItemLotAwardOnDeath(self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL], self.itemLotsToAward[inFile][creatureId])

                            # Change Event Scripts
                            if (disableRoamingBossRespawning):
                                if (self.validNew[newChar][NewCol.TYPE.value] == "1" and creatureType == "0"):
                                    if (self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] == -1):
                                        self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] = currentEventEntityID
                                        currentEventEntityID += 1
                                    
                                    eventTools.AddRespawnEventInit(self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL])
                            
                            # Tail Cuts
                            if (self.validNew[newChar][NewCol.ID.value] in self.TAIL_VALUES):
                                tailRow = self.msbio.parts[2].rows[rowIndex][:]
                                tailRow[25] = tailRow[25][:6] + 'tail'

                                tailRow[MODEL_DATA_COL] = self.startIndices[i] + self.TAIL_VALUES[self.validNew[newChar][NewCol.ID.value]][0]

                                tailRow[EVENT_ENTITY_ID_DATA_COL] = currentEventEntityID
                                currentEventEntityID += 1

                                tailRow[PARAM_DATA_COL] = self.TAIL_VALUES[self.validNew[newChar][NewCol.ID.value]][1]
                                tailRow[NPCAI_DATA_COL] = 1

                                self.msbio.AddCreatureRow(tailRow)

                                if (self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] == -1):
                                    self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] = currentEventEntityID
                                    currentEventEntityID += 1

                                eventTools.AddTailCutEventInit(self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL], tailRow[EVENT_ENTITY_ID_DATA_COL], self.validNew[newChar][NewCol.ID.value])
                                #print('Added Tail Cut For {0}'.format(self.validNew[newChar][NewCol.ID.value]))

                            if (self.validNew[newChar][NewCol.ID.value] in self.newCharacterAllegiances):
                                if (self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] == -1):
                                    self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL] = currentEventEntityID
                                    currentEventEntityID += 1

                                eventTools.SetCharacterAllegiance(self.msbio.parts[2].rows[rowIndex][EVENT_ENTITY_ID_DATA_COL], self.newCharacterAllegiances[self.validNew[newChar][NewCol.ID.value]])
                                #print("Set {0} allegiance to {1}".format(self.validNew[newChar][NewCol.ID.value], self.newCharacterAllegiances[self.validNew[newChar][NewCol.ID.value]]))
                            
                            # Gargoyle#2 Changes
                            if ("c5350_0001" in creatureId and not "c5350" in self.validNew[newChar][NewCol.ID.value]):
                                eventTools.ApplyGargoyle2Fix()

                            # Remove the forced animation playing at the start of MLB boss fight
                            if (inFile == "m12_00_00_00" and "c3230_0000" in creatureId and not "c3230" in self.validNew[newChar][NewCol.ID.value]):
                                eventTools.ApplyMoonlightButterflyAnimFix()
                            
                            printLog("Replacing (" + creatureId + ") " + self.validTargets[self.validIndex(creatureId)][1] + " with (" + self.validNew[newChar][NewCol.ID.value] + ") " + self.validNew[newChar][NewCol.NAME.value] + "[" + str(newChar) + "]" + aiStr + posLine + animLine, logFile, False)
                        else:
                            if (newChar == -2):
                                printLog("Did not replace (" + creatureId + ") " + self.validTargets[self.validIndex(creatureId)][1] + " - random chance", logFile, False)
                            elif (newChar == -3):
                                printLog("Did not replace (" + creatureId + ") " + self.validTargets[self.validIndex(creatureId)][1] + " - mimic mode is 0", logFile, False)
                            elif (newChar == -6):
                                printLog("Did not replace (" + creatureId + ") " + self.validTargets[self.validIndex(creatureId)][1] + " - failed to find fitting enemy with appropriate difficulty", logFile, False)
                            else:
                                printLog("Did not replace (" + creatureId + ") " + self.validTargets[self.validIndex(creatureId)][1] + " - c=" + str(newChar), logFile, False)

                    rowIndex += 1
                f.close()
                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i] + " - saving .luabnd")
                luabnd.save(self.AISCRIPTS + aiFileName + ".luabnd", luagnl.save_bytes(), luainfo.save_bytes())

                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i] + " - saving .emevd")
                eventTools.save(inFile)

                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i] + " - saving .msb")
                self.msbio.save(self.MAPSTUDIO + inFile + ".msb")

                printLog("---------------------", logFile)
                i += 1

            self.copyDarkrootGarden()

            msgArea.insert(END,  "Randomization complete\n")
            msgArea.config(state = "disabled")

        else:
            tkinter.messagebox.showerror("Randomization error", "Required files not found. \nCheck log rlog" + timeString + ".txt for details")

        logFile.close()