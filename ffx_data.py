import os.path
import os
import bnd_rebuilder
import byteread
import re
from collections import defaultdict
from dcx_handler import DCXHandler
from check_exe import sha256_checksum

class FFXData():

    modelString = "N:\\FRPG\\data\\INTERROOT_win32\\sfx\\model\\s{0}.flver"
    tpfString = "N:\\FRPG\\data\\INTERROOT_win32\\sfx\\tex\\s{0}.tpf"
    ffxString = "N:\\FRPG\\data\\Sfx\\OutputData\\Main\\Effect_win32\\f00{0}.ffx"

    ffxPath = "enemyRandomizerData/ffxExtract/effect/f00{0}.ffx"
    modelPath = "enemyRandomizerData/ffxExtract/model/s{0}.flver"
    tpfPath = "enemyRandomizerData/ffxExtract/tex/s{0}.tpf"

    def __init__(self):
        self.ffx_files = []
        self.basePath = b'N:\\FRPG\\data\\INTERROOT_win32\\script\\ai\\out\\bin\\'
        self.ffx_ref = defaultdict(list)
        self.currentContent = []
        self.lastFFX = 0
        self.lastFFXIdx = 0
        self.lastMDL = 0
        self.lastMDLIdx = 0
        self.lastTPF = 0
        self.lastTPFIdx = 0
        self.existingFiles = []
        self.fileOpen = False

    def CheckDirs(self):    #Unused
        if not (os.path.isdir("enemyRandomizerData/ffxExtract")):     #create map ai copy directory
            os.makedirs("enemyRandomizerData/ffxExtract")
            os.makedirs("enemyRandomizerData/ffxExtract/model")
            os.makedirs("enemyRandomizerData/ffxExtract/tex")
            os.makedirs("enemyRandomizerData/ffxExtract/effect")

    def LoadFfxRef(self, path):    #Unused
        with open(path, mode="r", encoding="utf-8") as f:
            for line in f:
                enemyId, effectIdsStr = line.strip().split(":")
                effects = effectIdsStr.split(",")
                self.ffx_ref[enemyId] = effects

    def CheckFiles(self):    #Unused
        self.CheckDirs()
        if (os.path.isfile(self.ffxPath.format('13520')) and os.path.isfile(self.modelPath.format('15391')) and os.path.isfile(self.tpfPath.format('15395'))):
            return True
        else:
            self.ExtractEffects()

    def ExtractEffects(self):    #Unused
        self.CheckDirs()

        inputFiles = ['m10', 'm10_00', 'm10_01', 'm10_02', 'm11', 'm12', 'm12_00', 'm12_01', 'm13', 'm13_00', 'm13_01', 'm13_02', 'm14', 'm14_00', 'm14_01', 'm15', 'm15_00', 'm15_01', 'm16', 'm17', 'm18', 'm18_00', 'm18_01']
        
        tempList = []

        toExtract = []

        for key in self.ffx_ref:
            toExtract += self.ffx_ref[key]

        #print(toExtract)

        for iFile in inputFiles:
            print(iFile)
            
            with open('sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd', 'rb') as f:
                content = f.read()
                data = bnd_rebuilder.unpack_bnd(content)
                for item in data:
                    strName = item[1].decode('shift_jis')
                    #print(strName)
                    toRemove = -1
                    for i, ffxId in enumerate(toExtract):
                        if (strName == self.ffxString.format(ffxId)):
                            #print("ffx : " + ffxId)
                            with open(self.ffxPath.format(ffxId), "wb") as ffxFile:
                                ffxFile.write(item[2])
                        elif (strName == self.tpfString.format(ffxId)):
                            #print("tex : " + ffxId)
                            with open(self.tpfPath.format(ffxId), "wb") as tpfFile:
                                tpfFile.write(item[2])
                        elif (strName == self.modelString.format(ffxId)):
                            #print("modle : " + ffxId)
                            with open(self.modelPath.format(ffxId), "wb") as flverFile:
                                flverFile.write(item[2])
        #print(toExtract)

    def Open(self, fname):    #Unused
        print("ffx opened: " + fname)
        self.currentContent = []
        self.lastFFX = 0
        self.lastMDL = 0
        self.lastTPF = 0
        self.existingFiles = []

        with open(fname, 'rb') as f:
            content = f.read()
            self.currentContent = bnd_rebuilder.unpack_bnd(content)
            for i, item in enumerate(self.currentContent):
                strName = item[1].decode('shift_jis')
                if not strName in self.existingFiles:
                    self.existingFiles.append(strName)
                if (item[0] < 100000):
                    if (item[0] > self.lastFFX):
                        self.lastFFX = item[0]
                        self.lastFFXIdx = i + 1
                elif (item[0] < 200000):
                    if (item[0] > self.lastTPF):
                        self.lastTPF = item[0]
                        self.lastTPFIdx = i + 1
                else:
                    if (item[0] > self.lastMDL):
                        self.lastMDL = item[0]
                        self.lastMDLIdx = i + 1
            self.fileOpen = True

    def Save(self, fname):    #Unused
        print("saving: " + fname)
        if (not os.path.isfile(fname + ".bak")):
            with open(fname, 'rb') as f:
                with open(fname + ".bak", 'wb') as f2:
                    f2.write(f.read())
        
        with open(fname, 'wb') as f:
            f.write(bnd_rebuilder.repack_bnd(self.currentContent))
        self.fileOpen = False

    def AddEffectData(self, enemyId):    #Unused
        if (self.fileOpen):
            cleanedId = enemyId.replace("c", "")
            if (cleanedId in self.ffx_ref):
                for ffxName in self.ffx_ref[cleanedId]:
                    if (os.path.isfile(self.ffxPath.format(ffxName))):
                        properPath = self.ffxString.format(ffxName)
                        if not properPath in self.existingFiles:
                            with open(self.ffxPath.format(ffxName), 'rb') as f:
                                self.currentContent = self.currentContent[:self.lastFFXIdx] + [(self.lastFFX + 1, self.ffxString.format(ffxName).encode('shift_jis'), f.read())] + self.currentContent[self.lastFFXIdx:]
                            self.lastFFX += 1
                            self.lastFFXIdx += 1
                            self.lastTPFIdx += 1
                            self.lastMDLIdx += 1
                        
                    if (os.path.isfile(self.tpfPath.format(ffxName))):
                        properPath = self.tpfString.format(ffxName)
                        if not properPath in self.existingFiles:
                            with open(self.tpfPath.format(ffxName), 'rb') as f:
                                self.currentContent = self.currentContent[:self.lastTPFIdx] + [(self.lastTPF + 1, self.tpfString.format(ffxName).encode('shift_jis'), f.read())] + self.currentContent[self.lastTPFIdx:]
                            self.lastTPF += 1
                            self.lastTPFIdx += 1
                            self.lastMDLIdx += 1


                    if (os.path.isfile(self.modelPath.format(ffxName))):
                        properPath = self.modelString.format(ffxName)
                        if properPath in self.existingFiles:
                            with open(self.modelPath.format(ffxName), 'rb') as f:
                                self.currentContent = self.currentContent[:self.lastMDLIdx] + [(self.lastTPF + 1, self.modelString.format(ffxName).encode('shift_jis'), f.read())] + self.currentContent[self.lastMDLIdx:]
                            self.lastMDL += 1
                            self.lastMDLIdx += 1
                return True
        else:
            print("! Trying to add ffx data, but no file open !")
        return False

    
    def AddEverythingToCommon(self, useDCX):
        """
        Collects all effects from individual map effect files and adds them to CommonEffects
        """

        inputFiles = ['m10', 'm10_00', 'm10_01', 'm10_02', 'm11', 'm12', 'm12_00', 'm12_01', 'm13', 'm13_00', 'm13_01', 'm13_02', 'm14', 'm14_00', 'm14_01', 'm15', 'm15_00', 'm15_01', 'm16', 'm17', 'm18', 'm18_00', 'm18_01']
        
        MODEL_PATTERN = r'.*s1([0-9][0-9][0-9])[0-9].*'
        TPF_PATTERN = r'.*s1([0-9][0-9][0-9])[0-9].*'
        ENEMY_EFFECT_ID_PATTERN = r'.*00([1-9][0-9][0-9][0-9][0-9]).ffx'
        ENEMY_MODEL_ID_PATTERN = r'.*s([1-9][0-9][0-9][0-9][0-9]).flver'
        ENEMY_TPF_ID_PATTERN = r'.*s([1-9][0-9][0-9][0-9][0-9]).tpf'

        tempList = []
        self.ffx_files.clear()
        for iFile in inputFiles:
            openFileName = 'sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd'
            if (useDCX):
                openFileName += '.dcx'
            with open(openFileName, 'rb') as f:
                upcontent = f.read()
                content = upcontent
                if (useDCX):
                    dcxh = DCXHandler()
                    content = dcxh.open_dcx(upcontent)
                data = bnd_rebuilder.unpack_bnd(content)
                for item in data:
                    strName = item[1].decode('shift_jis')
                    if not strName in tempList:
                        if (useDCX):
                            enFXMatch = re.match(ENEMY_EFFECT_ID_PATTERN, strName)
                            if (enFXMatch != None):
                                fid = int(enFXMatch.group(1))
                                if (fid < 20001):                   # prev = 60001 below as well in 2 locations
                                    tempList.append(strName)
                                    self.ffx_files.append(item)
                            else:
                                enMDLMatch = re.match(ENEMY_MODEL_ID_PATTERN, strName)
                                if (enMDLMatch != None):
                                    fid = int(enMDLMatch.group(1))
                                    if (fid < 20001):
                                        tempList.append(strName)
                                        self.ffx_files.append(item)
                                else:
                                    enTPFMatch = re.match(ENEMY_TPF_ID_PATTERN, strName)
                                    if (enTPFMatch != None):
                                        fid = int(enTPFMatch.group(1))
                                        if (fid < 20001):
                                            tempList.append(strName)
                                            self.ffx_files.append(item)
                        else:
                            tempList.append(strName)
                            self.ffx_files.append(item)


        existingEffects = []
        lastIndex = 0
        lastIndexTpf = 0
        lastIndexMdl = 0
        ffxEntries = []
        tpfEntries = []
        mdlEntries = []
        openFileName = 'sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd'
        if (useDCX):
            openFileName += '.dcx'

        oldCheckSum = sha256_checksum(openFileName)
        writeSuccessful = True

        with open(openFileName, 'rb') as f:
            upcontent = f.read()
            content = upcontent
            if (useDCX):
                dcxh = DCXHandler()
                content = dcxh.open_dcx(upcontent)
            data = bnd_rebuilder.unpack_bnd(content)
            for item in data:
                strName = item[1].decode('shift_jis')
                if not strName in existingEffects:
                    existingEffects.append(strName)
                if (item[0] < 100000):
                    ffxEntries.append(item)
                    if (item[0] > lastIndex):
                        lastIndex = item[0]
                elif (item[0] < 200000):
                    tpfEntries.append(item)
                    if (item[0] > lastIndexTpf):
                        lastIndexTpf = item[0]
                else:
                    mdlEntries.append(item)
                    if (item[0] > lastIndexMdl):
                        lastIndexMdl = item[0]
            if (not os.path.isfile(openFileName + '.bak')):
                with open(openFileName + '.bak', 'wb') as f2:
                    f2.write(upcontent)

        
        oldLen = len(ffxEntries) + len(tpfEntries) + len(mdlEntries)

        lastIndex += 1
        lastIndexTpf += 1
        lastIndexMdl += 1

        for i, ffx in enumerate(self.ffx_files):
            strName = ffx[1].decode('shift_jis')
            if not strName in existingEffects:
                if (ffx[0] < 100000):
                    newEntry = (lastIndex, ffx[1], ffx[2])
                    lastIndex += 1
                    ffxEntries.append(newEntry)
                elif (ffx[0] < 200000):
                    newEntry = (lastIndexTpf, ffx[1], ffx[2])
                    lastIndexTpf += 1
                    tpfEntries.append(newEntry)
                else:
                    newEntry = (lastIndexMdl, ffx[1], ffx[2])
                    lastIndexMdl += 1
                    mdlEntries.append(newEntry)

        newContent = []
        newContent += ffxEntries
        newContent += tpfEntries
        newContent += mdlEntries
        print("[FFX] Effect data gathered: " + str(len(newContent)))

        if not len(newContent) == oldLen:
            if (useDCX):
                print("[FFX] Saving and recompressing sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd.dcx. This takes quite a while with the Remaster.")
                dcxh = DCXHandler()
                temp = dcxh.open_file('sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd.dcx')
                dcxh.save_dcx('sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd.dcx', bnd_rebuilder.repack_bnd(newContent))
            else:
                print("[FFX] Saving sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd.")
                with open('sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd', 'wb') as f:
                    f.write(bnd_rebuilder.repack_bnd(newContent))

            newCheckSum = sha256_checksum(openFileName)

            if (oldCheckSum == newCheckSum):
                writeSuccessful = False
        
            print('[FFX] sfx\\FRPG_SfxBnd_CommonEffects.ffxbnd saved')

        if not useDCX:
            for iFile in inputFiles:
                data = []
                with open('sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd', 'rb') as f:
                    content = f.read()
                    data = bnd_rebuilder.unpack_bnd(content)
                    if not (os.path.isfile('sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd.bak')):
                        with open('sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd.bak', 'wb') as bakf:
                            bakf.write(content)
                with open('sfx\\FRPG_SfxBnd_' + iFile + '.ffxbnd', 'wb') as sf:
                    sf.write(bnd_rebuilder.repack_bnd(data[:1]))

            print('[FFX] Clean-up complete')
        else:
            print('[FFX] Ignored cleanup (REMASTERED Version being used)')

        print("[FFX] Done")
        return writeSuccessful


