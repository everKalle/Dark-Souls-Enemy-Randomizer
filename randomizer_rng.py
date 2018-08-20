from random import randint
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

#logFile = open('log.txt', 'w')
logFile = -1

def printLog(s, f, console = True):
    f.write(s + "\n")
    if (console):
        print(s)

class NewCol(Enum):
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

    MAPCOPY = "enemyRandomizerData/mapStudioCopies/"
    AICOPY = "enemyRandomizerData/mapAiCopies/"

    FFX_DIR = "sfx/FRPG_SfxBnd_{0}.ffxbnd"
    FFX_DIR_REMASTERED = "sfx/FRPG_SfxBnd_{0}.ffxbnd.dcx"
    FFX_COPY_DIR = "enemyRandomizerData/sfxCopies/FRPG_SfxBnd_{0}.ffxbnd"

    inputFiles = ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00", "m12_00_00_01", "m13_00_00_00",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]
    inputFFXFiles = ['CommonEffects', 'm10', 'm10_00', 'm10_01', 'm10_02', 'm11', 'm12', 'm12_00', 'm12_01', 'm13', 'm13_00', 'm13_01', 'm13_02', 'm14', 'm14_00', 'm14_01', 'm15', 'm15_00', 'm15_01', 'm16', 'm17', 'm18', 'm18_00', 'm18_01']
    startIndices = [250, 495, 171, 259, 280, 459, 282, 254, 242, 180, 342, 382, 210, 251, 197, 307, 84, 154]
    names = ["Depths", "Undead Burg/Parish", "Firelink Shrine", "Painted World", "Darkroot Garden", "DLC", "DarkRoot Garden #2", "Catacombs", "Tomb of the Giants", "Great Hollow & Ash Lake", "Blighttown", "Demon Ruins & Lost Izalith", "Sen's Fortress", "Anor Londo", "New Londo Ruins", "Duke's Archives & Crystal Cave", "Kiln of the First Flame", "Northern Undead Asylum"]
    mimicId = "c2780"

    #asylum easy mode is hardcoded for now
    HARDCODED_ASYLUM_NORMAL = [1, 4, 12, 16, 23, 24, 28, 29, 30, 62]
    HARDCODED_ASYLUM_BOSSES = [8, 9, 118]

    #Following is for testing on few files only

    """Blighttown only"""
    #inputFiles = ["m14_00_00_00"]
    #startIndices = [342]
    #names = ["Blighttown"]


    """Firelink Shrine only"""
    #inputFiles = ["m10_02_00_00"]
    #startIndices = [171]
    #names = ["Firelink Shrine"]
    
    

    def __init__(self):
        self.validTargets = []
        self.validNew = []
        self.validNewNormalIndices = []
        self.validNewBossIndices = []
        self.validSizeNew = [[], [], [], [], [], []]    #6 size classes
        self.validSizeNormal = [[], [], [], [], [], []]
        self.validSizeBosses = [[], [], [], [], [], []]
        self.locationIndices = dict()
        self.locationNormalIndices = dict()
        self.locationBossIndices = dict()
        self.aic = None
        self.uniqueIndices = []
        self.uniqueBosses = [[], [], [], [], [], []]
        self.uniqueNormals = [[], [], [], [], [], []]
        self.validDiffNew = [[], [], [], [], [], [], [], []]
        self.validDiffNormal = [[], [], [], [], [], [], [], []]
        self.validDiffBosses = [[], [], [], [], [], [], [], []]
        self.validDiffSizeNew = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.validDiffSizeNormal = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.validDiffSizeBosses = [[[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []], [[], [], [], [], [], []]]
        self.MAX_UNIQUE = 30
        self.msbio = MsbIO()
        self.ffxdata = FFXData()

        self.loadHellkite = True
        self.loadSmallEnemies = True

        self.missingMSB = 0
        self.missingLUABND = 0
        self.missingFFXBND = 0
        self.exeStatus = "None"

        self.missingMSB, self.missingLUABND, self.missingFFXBND, self.exeStatus = self.checkIfRightPlace()

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

        self.writingPermssion = False
        if (self.missingMSB == 0):
            self.writingPermssion = self.checkIfAllowedToModify()

        if (self.missingMSB == 0 and self.missingLUABND == 0 and self.missingFFXBND == 0 and (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug" or self.exeStatus == "Unknown" or self.exeStatus == "Patched" or self.exeStatus == "Patched Debug" or self.exeStatus == "Remaster") and
            self.folderStatus and self.aiRefStatus and self.ffxRefStatus and self.validNewStatus and self.validReplaceStatus and self.writingPermssion and self.originalRefMissing == 0): 
            if (self.exeStatus == "Remaster"):
                self.useDCX = True
                self.startIndices = [250, 495, 171, 259, 280, 464, 282, 254, 242, 181, 342, 382, 210, 251, 197, 307, 84, 154]
                self.MAX_UNIQUE = 70        #can use a much higher unique limit for remaster

                """Firelink shrine only"""
                #self.startIndices = [171]
            self.canRandomize = True
            self.aic = AiDataContainer('enemyRandomizerData/airef.csv')
            if (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug"):
                check_exe.patch_exe()
            self.firstTimeSetup()

            self.retryFileCopy()


    def createBackup(self, filename):
        if (not os.path.isfile(filename + '.bak')) and os.path.isfile(filename):
            with open(filename + '.bak', 'wb') as bakf:
                with open(filename, 'rb') as oldf:
                    bakf.write(oldf.read())
                    print(filename + " backed up")

    def restoreBackup(self, filename):
        if (os.path.isfile(filename + '.bak')):
            with open(filename, 'wb') as oldf:
                with open(filename + '.bak', 'rb') as bakf:
                    oldf.write(bakf.read())
                    print(filename + " reverted")
        else:
            if ((not self.useDCX) and (not "FRPG_SfxBnd" in filename)):
                print("Failed to restore " + filename + ", " + filename + ".bak not found.")

        
    # checks if necessary files exist and exe is in proper status

    def checkIfRightPlace(self):
        print("Checking location")

        notFoundMSB = 0
        notFoundLUABND = 0
        notFoundFFXBND = 0

        exeStatus = check_exe.check_exe_checksum()
        check_for_dcx = False
        if (exeStatus == "Remaster"):
            check_for_dcx = True

        for iFile in self.inputFiles:
            if not (os.path.isfile(self.MAPSTUDIO + iFile + '.msb')):
                notFoundMSB += 1

            if not (iFile == "m12_00_00_01"):
                if (check_for_dcx):
                    if not (os.path.isfile(self.AISCRIPTS + iFile + '.luabnd.dcx')):
                        notFoundLUABND += 1
                else:
                    if not (os.path.isfile(self.AISCRIPTS + iFile + '.luabnd')):
                        notFoundLUABND += 1
        
        for iFile in self.inputFFXFiles:
            if (iFile != "NONE"):
                if (check_for_dcx):
                    if not (os.path.isfile(self.FFX_DIR_REMASTERED.format(iFile))):
                        notFoundFFXBND += 1
                else:
                    if not (os.path.isfile(self.FFX_DIR.format(iFile))):
                        notFoundFFXBND += 1

        return (notFoundMSB, notFoundLUABND, notFoundFFXBND, exeStatus)

    # check if all necessary files exist in enemyRandomizerData
    def checkProperUnpack(self):
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

            """if (os.path.isfile("enemyRandomizerData/ffx-ref.txt")):
                self.ffxRefStatus = True"""

            if (os.path.isfile("enemyRandomizerData/replacement_ref/valid_new.txt")):
                self.validNewStatus = True

            if (os.path.isfile("enemyRandomizerData/replacement_ref/valid_replacements.txt")):
                self.validReplaceStatus = True

            for iFile in ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00.ptde", "m12_01_00_00.remaster", "m12_00_00_01", "m13_00_00_00.remaster",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]:
                if not (os.path.isfile('enemyRandomizerData/original_enemies_ref/' + iFile + '.txt')):
                    self.originalRefMissing += 1
        
    def checkCopiedFiles(self):
        self.missingAiCopies = 0
        self.invalidAiCopies = 0
        self.invalidMapCopies = 0
        self.missingMapCopies = 0

        for iFile in self.inputFiles:
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
        
        """for iFile in self.inputFFXFiles:
            if (iFile != "NONE"):
                if not (os.path.isfile(self.FFX_COPY_DIR.format(iFile))):
                    self.missingSfxCopies += 1
                else:
                    with open(self.FFX_COPY_DIR.format(iFile), 'rb') as testFile:
                        if (len(testFile.read()) < 10):
                            self.invalidSfxCopies += 1"""

        if (self.missingAiCopies > 0 or self.invalidAiCopies > 0 or self.missingMapCopies > 0 or self.invalidMapCopies > 0 or self.missingSfxCopies > 0 or self.invalidSfxCopies > 0):
            return False
        else:
            return True

    def retryFileCopy(self):
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

        # Try writing stuff to the file

        try:
            with open(testFileName, 'wb') as outf:
                outf.write(b'TESTINGIFICANWRITEINTOTHISFILE')
        except:
            return False

        # Because apparently for _some_ reason it doesn throw an error sometimes(?) so we confirm if the file was actually modified

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
        print("Checking Files, Please Wait")
        if not (os.path.isdir("enemyRandomizerData/mapAiCopies")):     #create map ai copy directory
            os.makedirs("enemyRandomizerData/mapAiCopies")

        if not (os.path.isdir("enemyRandomizerData/mapStudioCopies")):     #create map studio copy directory
            os.makedirs("enemyRandomizerData/mapStudioCopies")

        #if not (os.path.isdir("enemyRandomizerData/sfxCopies")):     #create sfx copy directory
        #    os.makedirs("enemyRandomizerData/sfxCopies")

        # yay for hardcoded stuff >.>
        modelsToAdd = ["c1200", "c1201", "c1202", "c1203", "c2060", "c2230", "c2231", "c2232", "c2240", "c2250", "c2260", "c2270", "c2280", "c2300", "c2310", "c2320", "c2330", "c2360", "c2370", "c2380", "c2390", "c2400", "c2410", "c2430", "c2500", "c2510", "c2520", "c2530", "c2540", "c2550", "c2560", "c2570", "c2640", "c2650", "c2660", "c2670", "c2680", "c2690", "c2700", "c2710", "c2711", "c2730", "c2780", "c2790", "c2791", "c2792", "c2793", "c2800", "c2810", "c2811", "c2830", "c2840", "c2860", "c2870", "c2900", "c2910", "c2920", "c2930", "c2940", "c2950", "c2960", "c3090", "c3200", "c3210", "c3220", "c3230", "c3240", "c3250", "c3270", "c3300", "c3320", "c3330", "c3340", "c3341", "c3350", "c3370", "c3380", "c3390", "c3400", "c3410", "c3420", "c3421", "c3430", "c3460", "c3461", "c3471", "c3480", "c3490", "c3491", "c3500", "c3520", "c3530", "c4100", "c4110", "c4120", "c4130", "c4150", "c4160", "c4170", "c4171", "c4172", "c4180", "c4190", "c4500", "c4510", "c5200", "c5201", "c5202", "c5210", "c5220", "c5240", "c5250", "c5260", "c5270", "c5271", "c5280", "c5290", "c5320", "c5350", "c5351", "c5360", "c5370", "c5390"]

        MODEL_TYPE_OFFSET = 1
        MODEL_IDX_OFFSET = 2
        MODEL_NAME_OFFSET = 8
        MODEL_SIBPATH_OFFSET = 9

        SIBPATH_FORMAT = "N:\FRPG\data\Model\chr\{0}\sib\{0}.sib"

        for j in enumerate(self.inputFiles):                                    #backup msb/luabnd
            print("[Check/Preparation] Map and script files " + str(j[0]) + "/" + str(len(self.inputFiles)))
            passed1 = not (os.path.isfile(self.MAPCOPY + j[1] + '.msb'))
            passed2 = False
            if not (passed1):
                with open(self.MAPCOPY + j[1] + '.msb', 'rb') as testf:
                    testData = testf.read()
                    #print(len(testData))
                    if (len(testData) < 10):
                        passed2 = True
            #print(passed1)
            #print(passed2)
            if (passed1 or passed2):
                with open(self.MAPCOPY + j[1] + '.msb', 'wb') as bakf:
                    with open(self.MAPSTUDIO + j[1] + '.msb', 'rb') as oldf:
                        bakf.write(oldf.read())
                
                self.msbio.open(self.MAPCOPY + j[1] + '.msb')


                lastModelIndex = 0
                for model in self.msbio.models_data:
                    if (model[MODEL_TYPE_OFFSET] == 2):     #if it's a character model
                        if (model[MODEL_IDX_OFFSET] > lastModelIndex):
                            lastModelIndex = model[MODEL_IDX_OFFSET]
                
                for i, modelName in enumerate(modelsToAdd):
                    modelRow = [32, 2, lastModelIndex + 1 + i, 38, 1, 0, 0, 0, modelName, SIBPATH_FORMAT.format(modelName)]
                    self.msbio.models_data.append(modelRow)

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

        print("[Check/Preparation] Preparing effect files (Takes a while)")
        self.ffxdata.AddEverythingToCommon(self.useDCX)

        print("[Check/Preparation] Done")

    def check(self):            #check whether or not necessary files are there
        passedCheck = True
        printLog("Checking Files", logFile)
        for j in enumerate(self.inputFiles):
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
            if j[1] == "m12_00_00_01":        #because darkrooot is weird, has a 2 msb files: m12_00_00_00 and m12_00_00_01 that are _nearly_ identical but only the latter seems to be actually used
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
            printLog(j[1] + " - " + self.names[j[0]] + " - offset: " + str(self.startIndices[j[0]]) + s + s2 + s3 + s4, logFile)
        return passedCheck

    def isValid(self, s):                   #is enemy a valid target?
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

    def loadFiles(self, enemyConfigName):                                    # load enemy data
        self.validNew.clear()
        self.validTargets.clear()
        self.validNewNormalIndices.clear()
        self.validNewBossIndices.clear()
        for i in range(0, 6):          #clear size lists
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

        for location in self.inputFiles:
            emList = []
            self.locationIndices[location] = emList
            bsList = []
            self.locationBossIndices[location] = bsList
            nrList = []
            self.locationNormalIndices[location] = nrList

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
                    elif (parts[NewCol.IGNORED.value] == "3" and self.loadSmallEnemies):
                        notIgnored = True
                        print("<Chaos Bug/Vile Maggot> loading enabled")

                    configStringForLog += parts[NewCol.IGNORED.value]
                    
                    if (notIgnored):
                        if (parts[NewCol.TYPE.value] == "0"):
                            self.validNewNormalIndices.append(len(self.validNew) - 1)
                        elif (parts[NewCol.TYPE.value] == "1"):
                            self.validNewBossIndices.append(len(self.validNew) - 1)

                        nwSize = int(parts[NewCol.SIZE.value])
                        nwDiff = int(parts[NewCol.DIFFICULTY.value])
                        for i in range(nwSize, 6):          #populate size lists
                            self.validSizeNew[i].append(len(self.validNew) - 1)
                            self.validDiffSizeNew[nwDiff][i].append(len(self.validNew) - 1)
                            if (parts[NewCol.TYPE.value] == "0"):
                                self.validSizeNormal[i].append(len(self.validNew) - 1)
                                self.validDiffSizeNormal[nwDiff][i].append(len(self.validNew) - 1)
                            elif (parts[NewCol.TYPE.value] == "1"):
                                self.validSizeBosses[i].append(len(self.validNew) - 1)
                                self.validDiffSizeBosses[nwDiff][i].append(len(self.validNew) - 1)
                        
                        #useless
                        self.validDiffNew[nwDiff].append(len(self.validNew) - 1)
                        if (parts[2] == "0"):
                            self.validDiffNormal[nwDiff].append(len(self.validNew) - 1)
                        elif (parts[2] == "1"):
                            self.validDiffBosses[nwDiff].append(len(self.validNew) - 1)
                    
                    for location in self.inputFiles:        # _should_ be obsolete
                        if (parts[NewCol.IGNORED.value] == "0"):
                            if (location in parts[NewCol.LOCATIONS.value]):
                                self.locationIndices[location].append(len(self.validNew) - 1)
                                if (parts[NewCol.TYPE.value] == "0"):
                                    self.locationNormalIndices[location].append(len(self.validNew) - 1)
                                elif (parts[NewCol.TYPE.value] == "1"):
                                    self.locationBossIndices[location].append(len(self.validNew) - 1)

                else:
                    printLog("AI AND PARAM DONT MATCH ON " + line, logFile)      # valid_new.txt is messed up
            else:
                firstLine = False
        f.close()
        printLog("Done, " + str(len(self.validNew)) + " valid new enemies", logFile)
        if (enemyConfigName != 'Default'):
            printLog(configStringForLog, logFile)

    def getRandomFromList(self, l):         #returns a random element from @l
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
            #print("Returning class " + str(returnClass) + " for desired difficulty of " + str(desiredDifficulty))
            if (isBoss):
                return self.validDiffSizeBosses[returnClass][maxSize]
            else:
                return self.validDiffSizeNormal[returnClass][maxSize]

        if (isBoss):
            return self.validDiffSizeBosses[desiredDifficulty][maxSize]
        else:
            return self.validDiffSizeNormal[desiredDifficulty][maxSize]

    def GetEnemyFromListWithRetry(self, enemyList, originalEnemyID):
        retryCount = 10
        newEnemyID = ''
        returnChar = -1
        while (retryCount > 0 and self.isCombinationInvalid(originalEnemyID, newEnemyID)):
            returnChar = self.getRandomFromList(enemyList)
            retryCount -= 1
            newEnemyID = self.validNew[returnChar][NewCol.ID.value]

        if (retryCount <= 0):
            return -4

        return returnChar

    def GetNormalEnemy(self, diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID):                           # returns a normal enemy
        if (diffmode == 0):
            return (self.getRandomFromList(self.locationNormalIndices[mapname]), True)
        else:
            newC = -1
            if (not careAboutLimit or len(self.uniqueIndices) < self.MAX_UNIQUE):
                #newC = self.getRandomFromList(self.validNewNormalIndices)
                if (diffmode == 1):
                    diffList = self.getDifficultyList(desiredDifficulty, diffStrictness, False, maxSize)
                    if (len(diffList) > 0):
                        newC = self.GetEnemyFromListWithRetry(diffList, originalEnemyID)
                    else:
                        newC = -6
                    #print("Getting difficulty scaled normal")
                else:
                    newC = self.GetEnemyFromListWithRetry(self.validSizeNormal[maxSize], originalEnemyID)
            else:
                newC = self.GetEnemyFromListWithRetry(self.uniqueNormals[maxSize], originalEnemyID)

            if (diffmode == 3 and mapname == "m18_01_00_00"):
                newC = self.getRandomFromList(self.HARDCODED_ASYLUM_NORMAL)

            #useless
            modAI = False
            if (mapname in self.validNew[newC][NewCol.LOCATIONS.value]):
                modAI = True
            return (newC, modAI)

    def GetBossEnemy(self, diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID, canBeNormal):               # returns a boss enemy
        if (diffmode == 0):
            return (self.getRandomFromList(self.locationBossIndices[mapname]), True)
        else:
            newC = -1
            if (not careAboutLimit or len(self.uniqueIndices) < self.MAX_UNIQUE):
                if (diffmode == 1):
                    diffList = self.getDifficultyList(desiredDifficulty, diffStrictness, True, maxSize)
                    if (len(diffList) > 0):
                        newC = self.GetEnemyFromListWithRetry(diffList, originalEnemyID)
                    else:
                        if (canBeNormal):
                            #print("Normal:Boss/Normal and failed to find difficulty scaled boss, returning a normal enemy.")
                            return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)
                        else:
                            newC = -6
                    #print("Getting difficulty scaled boss")
                else:
                    newC = self.GetEnemyFromListWithRetry(self.validSizeBosses[maxSize], originalEnemyID)
            else:
                if (len(self.uniqueBosses) == 0):
                    if (canBeNormal):
                        return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)
                else:
                    newC = self.GetEnemyFromListWithRetry(self.uniqueBosses[maxSize], originalEnemyID)

            if (diffmode == 3 and mapname == "m18_01_00_00"):
                newC = self.getRandomFromList(self.HARDCODED_ASYLUM_BOSSES)
            
            #useless
            modAI = False
            if (mapname in self.validNew[newC][NewCol.LOCATIONS.value]):
                modAI = True
            return (newC, modAI)

    def GetNormalOrBossEnemy(self, diffmode, mapname, bosschance, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID):
        if (randint(1, 100) <= bosschance):
            return self.GetBossEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID, True)
        else:
            return self.GetNormalEnemy(diffmode, mapname, careAboutLimit, maxSize, desiredDifficulty, diffStrictness, originalEnemyID)

    #the bakf and oldf variable names make no fucking sense...

    def revertToNormal(self, revertEffectFiles = True):
        for j in enumerate(self.inputFiles):                                    #load backup msb/luabnd
            print("[Unrandomize] Reverting msb and luabnd files " + str(j[0]) + "/" + str(len(self.inputFiles)))
            self.restoreBackup(self.MAPSTUDIO + j[1] + '.msb')
            
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

        if (self.useDCX):
            self.restoreBackup('event/m10_01_00_00.emevd.dcx')
            self.restoreBackup('event/m12_00_00_00.emevd.dcx')
            self.restoreBackup('event/m13_00_00_00.emevd.dcx')
            self.restoreBackup('event/m14_01_00_00.emevd.dcx')
            self.restoreBackup('event/m15_01_00_00.emevd.dcx')
            self.restoreBackup('event/m17_00_00_00.emevd.dcx')
        else:
            self.restoreBackup('event/m10_01_00_00.emevd')
            self.restoreBackup('event/m12_00_00_00.emevd')
            self.restoreBackup('event/m13_00_00_00.emevd')
            self.restoreBackup('event/m14_01_00_00.emevd')
            self.restoreBackup('event/m15_01_00_00.emevd')
            self.restoreBackup('event/m17_00_00_00.emevd')

    def applyEmevd(self, emevdName):
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

    def isCombinationInvalid(self, oldID, newID):       # Is a specific replacement invalid
        if ('c5320' in oldID):          # Gwyndolin
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c2240' in oldID):          # Capra Demon
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c3320_0000' in oldID):     # Pinwheeeeeeeee
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameetc2320
                return True

        if ('c2320' in oldID):          # Iron Golem
            if ('c5290' in newID):      # Seath
                return True
            elif ('c5260' in newID):    # Gaping
                return True
            elif ('c4510' in newID):    # Kalameet
                return True

        if ('c3350' in newID):          # Tree
            if ('c2800' in oldID):      # Undead Crystal Soldiers
                return True
            elif ('c2370' in oldID):    # Channeler
                return True
        
        if (newID == ''):
            return True
        return False

    def randomize(self, settings, msgArea):
        global logFile
        currentTime = datetime.datetime.now()
        timeString = f"{currentTime:%Y-%m-%d-%H-%M-%S}"
        logFile = open('enemyRandomizerData/logs/rlog' + timeString + '.txt', 'w')      #create logfile

        self.firstTimeSetup()

        if (self.check()):
            progressBar, progressLabel, bossMode, enemyMode, npcMode, mimicMode, fitMode, diffMode, replaceChance, bossChance, bossChanceBosses, skeletonMode, gargoyleMode, diffStrictness, tposeCity, hellkiteEn, smallOneEn, seed, textConfig, enemyConfigName = settings

            if (hellkiteEn == 0):
                self.loadHellkite = True
            else:
                self.loadHellkite = False

            if (smallOneEn == 0):
                self.loadSmallEnemies = True
            else:
                self.loadSmallEnemies = False

            if (seed == ""):
                random.seed(datetime.datetime.now())
                seed = str(random.randrange(sys.maxsize))
            
            random.seed(seed)

            self.exeStatus = check_exe.check_exe_checksum()
            if (self.exeStatus == "Unpacked" or self.exeStatus == "Unpacked Debug"):
                check_exe.patch_exe()

            self.ffxdata.AddEverythingToCommon(self.useDCX)


            self.applyEmevd('m10_01_00_00')
            self.applyEmevd('m12_00_00_00')
            self.applyEmevd('m13_00_00_00')
            self.applyEmevd('m14_01_00_00')
            self.applyEmevd('m15_01_00_00')
            self.applyEmevd('m17_00_00_00')

            #msbio = MsbIO()
            luagnl = LuaGnl()
            luainfo = LuaInfo()
            luabnd = BndData()

            MODEL_DATA_COL = 3
            NPCAI_DATA_COL = 38
            PARAM_DATA_COL = 39

            ANIMID_DATA_COL = 50

            EVENTENTITYID_COL = 27

            POS_DATA_COL = 5    # X pos , Y + 1, Z + 2, ROTX + 3, ROTY + 4, ROTZ + 5;
            
            progressBar.step()
            progressLabel.config(text="Loading Files")
            self.loadFiles(enemyConfigName)
            msgArea.config(state = "normal")

            #log settings
            printLog("----\n Starting Randomization \n----", logFile)
            printLog("bossMode=" + str(bossMode) + "; enemyMode=" + str(enemyMode) + "; npcMode=" + str(npcMode) + "; mimicMode=" + str(mimicMode), logFile)
            printLog("fitMode=" + str(fitMode) + "; diffMode=" + str(diffMode) + "; diffStrictness=" + str(diffStrictness) + "; replaceChance=" + str(replaceChance) + "; bossChance(Normal)=" + str(bossChance) + "; bossChance(Boss)=" + str(bossChanceBosses) + "; gargoyleMode=" + str(gargoyleMode), logFile)
            printLog("tpose=" + str(tposeCity) + "; hellkiteIgnored=" + str(hellkiteEn) + "; smallEnIgnored=" + str(smallOneEn), logFile)
            printLog("seed='" + seed + "'", logFile)
            printLog("max_unique=" + str(self.MAX_UNIQUE), logFile)
            printLog("----", logFile)
            printLog("textconfig:", logFile)
            textConfig = textConfig.replace("''''''", "'''" + seed + "'''")
            printLog(textConfig, logFile)
            printLog("----", logFile)

            i = 0
            for inputIndex, inFile in enumerate(self.inputFiles):
                printLog("Randomizing " + inFile + " - " + self.names[i], logFile)
                msgArea.insert(END,  "Randomizing " + inFile + " - " + self.names[i] + "\n")

                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i])

                self.msbio.open(self.MAPCOPY + inFile + ".msb")

                aiFileName = inFile
                if inFile == "m12_00_00_01":        #because darkrooot is weird, has a 2 msb files: m12_00_00_00 and m12_00_00_01 but only the latter seems to change the enemies
                    aiFileName = "m12_00_00_00"     #but the ai is in m12_00_00_00.luabnd file

                gnlBytes, infoBytes = luabnd.open(self.AICOPY + aiFileName + ".luabnd", self.useDCX)
                luagnl.open_bytes(gnlBytes)
                luainfo.open_bytes(infoBytes)

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

                typeMap = dict()
                typeSub = False

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
                            if (self.useDCX):
                                self.restoreBackup('event/m10_01_00_00.emevd.dcx')
                            else:
                                self.restoreBackup('event/m10_01_00_00.emevd')
                        else:
                            changePos = True
                            newPos = (10.69, 48.92, 124.35)
                            newRot = (0.00, 1.84, 0.00)
                    elif ((inFile == "m12_00_00_00" or inFile == "m12_00_00_01") and "c3230_0000" in creatureId):     # Moonlight Butterfly boss 
                        #specialCase = True
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
                    elif ("c5290_0000" in creatureId):                                       #Seath (Scripted death) - needs to be able to kill you in the forced death room 
                        specialCase = True
                    elif (inFile == "m17_00_00_00" and "c2690_0000" in creatureId):     #Key Serpent - need the key (unless you duke skip of course), new enemy either doesnt drop it or drops it inside the wall or something
                        specialCase = True
                    elif ("c5310_0000" in creatureId):                                  #Gwynevere - Can die the moment you enter anor londo and that breaks the game: can't get the lordvessel (the cutscene after gwynevere death doesnt trigger even if you go in the room) and enemies are missing like it's dark anor londo
                        specialCase = True
                    elif ("c3320" in creatureId and inFile == "m13_00_00_00" and not "c3320_0000" in creatureId):          #Pinwheel's clones in the boss fight
                        specialCase = True
                    elif ("c2780_0000" in creatureId and inFile == "m12_01_00_00"):     #Crest key mimic
                        specialCase = True
                    elif ("c4510_0000" in creatureId or "c4510_0002" in creatureId):    #Kalameet flying around versions
                        specialCase = True
                    elif ("c3300" in creatureId and inFile == "m13_02_00_00"):          #Crystal Lizards in Great Hollow, for whatever reason they make the Great Hollow super unstable
                        specialCase = True

                    # SkeletonMode things

                    removeEventEntityID = False

                    #if ("c2900" in creatureId and inFile == "m13_00_00_00"):          #If skeletonMode is 0, don't replace skeletons in catacombs
                    #    removeEventEntityID = True
                    if ("c2900" in creatureId and inFile == "m13_01_00_00"):   # don't replace small skeletons in ToG (Ravelord Nito fight)
                        specialCase = True
                    if (("c2910_0019" in creatureId or "c2910_0020" in creatureId or "c2910_0021" in creatureId) and inFile == "m13_01_00_00"):    # don't replace lg.skeletons in Ravelord Nito fight
                        specialCase = True

                    if (self.isValid(creatureId) and not specialCase):
                        newChar = -1
                        modifyAI = False

                        creatureTypeId = creatureId.split('_')[0]

                        if (typeSub and creatureTypeId in typeMap):
                            newChar = typeMap[creatureTypeId]
                        else:
                            if (randint(1, 100) <= replaceChance):

                                creatureType = self.validTargets[self.validIndex(creatureId)][2]

                                if ("c3320_0000" in creatureId and inFile == "m13_00_00_00"):     #only consider the actual bossfight main pinwheel (the one that actually takes damage) a boss (and not the clones and the ones in ToG)
                                    creatureType = "1"
                                elif (inFile == "m10_01_00_00" and "c2250" in creatureId):          #consider taurus boss a boss
                                    creatureType = "1"
                                elif (inFile == "m14_01_00_00" and "c2240" in creatureId):          #consider capras in D.Ruins normal  enemies
                                    creatureType = "0"
                                elif (inFile == "m15_01_00_00" and "c2860_0000" in creatureId):     #consider blacksmith giant a npc
                                    creatureType = "2"
                                elif (inFile == "m14_00_00_00" and "c3210_0000" in creatureId):     #eingyi
                                    creatureType = "2"

                                maxCreatureSize = 5
                                if (fitMode == 0):
                                    maxCreatureSize = int(creatureSize)
                                
                                expectedDifficulty = int(self.validTargets[self.validIndex(creatureId)][3])

                                if (creatureType == "0" and enemyMode != 0):       #replacing normal
                                    isMimic = self.mimicId in creatureId

                                    if (not isMimic or mimicMode == 1):                                 #mimic replace mode
                                        if (enemyMode == 1):     #replace with bosses only
                                            newChar, modifyAI = self.GetBossEnemy(diffMode, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False)
                                        elif (enemyMode == 2):   #replace with normals only
                                            newChar, modifyAI = self.GetNormalEnemy(diffMode, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                        elif (enemyMode == 3):   #replace with both
                                            newChar, modifyAI = self.GetNormalOrBossEnemy(diffMode, inFile, bossChance, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                    else:
                                        newChar = -3

                                elif (creatureType == "1" and bossMode != 0):     #replacing boss (dont care about enemy limit so bosses _can_ be unique)
                                    if (bossMode == 1):     #replace with bosses only
                                        newChar, modifyAI = self.GetBossEnemy(diffMode, inFile, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False)
                                    elif (bossMode == 2):   #replace with normals only
                                        newChar, modifyAI = self.GetNormalEnemy(diffMode, inFile, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                    elif (bossMode == 3):   #replace with both
                                        newChar, modifyAI = self.GetNormalOrBossEnemy(diffMode, inFile, bossChanceBosses, False, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)

                                elif (creatureType == "2" and npcMode != 0):     #replacing NPC
                                    if (fitMode == 2):
                                        maxCreatureSize = int(creatureSize)
                                    if (npcMode == 1):     #replace with bosses only
                                        newChar, modifyAI = self.GetBossEnemy(2, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId, False)
                                    elif (npcMode == 2):   #replace with normals only
                                        newChar, modifyAI = self.GetNormalEnemy(2, inFile, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)
                                    elif (npcMode == 3):   #replace with both
                                        newChar, modifyAI = self.GetNormalOrBossEnemy(2, inFile, bossChance, True, maxCreatureSize, expectedDifficulty, diffStrictness, creatureId)

                                    if ("c2640" in creatureId):                 # Special Andre -> Gwyndolin Replacement
                                        if (npcMode == 1 or npcMode == 3):
                                            if (randint(1,100) > 60):
                                                newChar = 117
                            
                            else:
                                newChar = -2

                            typeMap[creatureTypeId] = newChar

                        if (newChar >= 0):
                            if (not newChar in self.uniqueIndices and not creatureType == "1"):
                                self.uniqueIndices.append(newChar)
                                if (newChar in self.validNewBossIndices):
                                    for idx in range(int(self.validNew[newChar][NewCol.SIZE.value]), 6):
                                        self.uniqueBosses[idx].append(newChar)
                                else:
                                    for idx in range(int(self.validNew[newChar][NewCol.SIZE.value]), 6):
                                        self.uniqueNormals[idx].append(newChar)
                            
                            oldNPCParam = self.msbio.parts_data[2][rowIndex][PARAM_DATA_COL]
                            oldNPCAI = self.msbio.parts_data[2][rowIndex][NPCAI_DATA_COL]
                            oldModel = self.msbio.parts_data[2][rowIndex][MODEL_DATA_COL]
                            
                            newAI = ""
                            newParam = ""
                            if (len(self.validNew[newChar][NewCol.AI.value]) == 1):
                                newAI = self.validNew[newChar][NewCol.AI.value][0]
                                newParam = self.validNew[newChar][NewCol.PARAM.value][0]
                            else:
                                newAiParamIndex = randint(0, len(self.validNew[newChar][NewCol.AI.value]) - 1)
                                newAI = self.validNew[newChar][NewCol.AI.value][newAiParamIndex]
                                newParam = self.validNew[newChar][NewCol.PARAM.value][newAiParamIndex]


                            self.msbio.parts_data[2][rowIndex][PARAM_DATA_COL] = int(newParam)
                            aiStr = "  ai = <original>; param = " + newParam
                            if(not self.validTargets[self.validIndex(creatureId)][2] == "2"):    # lets not mod npc ai for now
                                self.msbio.parts_data[2][rowIndex][NPCAI_DATA_COL] = int(newAI)
                                aiStr = " ai = " + newAI + "; param = " + newParam
                            self.msbio.parts_data[2][rowIndex][MODEL_DATA_COL] = self.startIndices[i] + newChar

                            aiEntry = self.aic.GetEntryByAI(newAI)

                            exists = luainfo.AddEntryAuto(aiEntry.info)
                            if not (exists):
                                luagnl.AddEntriesAuto(aiEntry.aiFuncsGnl)
                                luabnd.addAuto(aiEntry.battle_script)
                                luabnd.addAuto(aiEntry.logic_script)

                            animLine = ""
                            if (tposeCity == 1):
                                if (creatureType != "2"):
                                    currentAnim = self.msbio.parts_data[2][rowIndex][ANIMID_DATA_COL]
                                    if (currentAnim != -1):
                                        newAnim = self.getRandomFromList(self.validNew[newChar][NewCol.ANIMIDS.value])
                                        self.msbio.parts_data[2][rowIndex][ANIMID_DATA_COL] = int(newAnim)
                                        animLine = " >> changing idle anim from " + str(currentAnim) + " to " + newAnim + ";"

                            if ("c5350_0001" in creatureId and "c5350" in self.validNew[newChar][NewCol.ID.value]):    # restore original event script for 10_01 if gargoyle#2 is replaced by gargoyle, so that jumping down and stuff works properly
                                self.restoreBackup('event/m10_01_00_00.emevd')

                            # update position if necessary:
                            posLine = ""
                            if (changePos):
                                posLine = " changed position"
                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL] = newPos[0]
                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL + 1] = newPos[1]
                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL + 2] = newPos[2]

                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL + 3] = newRot[0]
                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL + 4] = newRot[1]
                                self.msbio.parts_data[2][rowIndex][POS_DATA_COL + 5] = newRot[2]

                            #if (removeEventEntityID):
                            #    self.msbio.parts_data[2][rowIndex][EVENTENTITYID_COL] = -1

                            
                            
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

                            if ((inFile == "m12_00_00_00" or inFile == "m12_00_00_01") and "c3230_0000" in creatureId):         # restore 12_00_00_00 if MLB is not replaced, so that flying down works properly
                                if (self.useDCX):
                                    self.restoreBackup('event/m12_00_00_00.emevd.dcx')
                                else:
                                    self.restoreBackup('event/m12_00_00_00.emevd')
                                printLog("MLB Boss not replaced, reverting m12_00_00_00.emevd(.dcx)", logFile, False)

                    rowIndex += 1
                f.close()
                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i] + " - saving .luabnd")
                luabnd.save(self.AISCRIPTS + aiFileName + ".luabnd", luagnl.save_bytes(), luainfo.save_bytes())
                progressBar.step()
                progressLabel.config(text="Randomizing " + self.names[i] + " - saving .msb")
                self.msbio.save(self.MAPSTUDIO + inFile + ".msb")
                """if (self.inputFFXFiles[inputIndex] != "NONE"):
                    self.ffxdata.Save(self.FFX_DIR.format(self.inputFFXFiles[inputIndex]))"""
                printLog("---------------------", logFile)
                i += 1

            msgArea.insert(END,  "Randomization complete\n")
            msgArea.config(state = "disabled")

        else:
            tkinter.messagebox.showerror("Randomization error", "Required files not found. \nCheck log rlog" + timeString + ".txt for details")

        logFile.close()