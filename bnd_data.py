import os.path
import bnd_rebuilder
import byteread
import re
from dcx_handler import DCXHandler

class BndData():
    """
    Class to handle .luabnd files and adding the .lua files to them.
    """

    def __init__(self):
        self.luabnd_content = []
        self.luabnd_maxIndex = 1
        self.basePath = b'N:\\FRPG\\data\\INTERROOT_win32\\script\\ai\\out\\bin\\'
        self.basePathRemaster = b'N:\\FRPG\\data\\INTERROOT_x64\\script\\ai\\out\\bin\\'
        self.dcxhandler = None
        self.useDCXCompression = False

    def open(self, filename, dcx = True):
        """
        Opens .luabnd file with filename @filename.
        """
        
        self.luabnd_content = []
        self.luabnd_maxIndex = 0
        self.useDCX = dcx

        fullname = filename
        if (self.useDCX):
            fullname += '.dcx'

        with open(fullname, 'rb') as f:
            content = f.read()
            bndcontent = content
            if (self.useDCX):
                self.dcxhandler = DCXHandler()
                bndcontent = self.dcxhandler.open_dcx(content)
            self.luabnd_content = bnd_rebuilder.unpack_bnd(bndcontent)

        luagnlBytes = b''
        luainfoBytes = b''

        self.luabnd_maxIndex = 0

        for item in self.luabnd_content:
            if (item[0] == 1000000):
                luagnlBytes = item[2]
            elif (item[0] == 1000001):
                luainfoBytes = item[2]
            else:
                if (item[0] > self.luabnd_maxIndex):
                    self.luabnd_maxIndex = item[0]

        return (luagnlBytes, luainfoBytes)

    def save(self, filename, luagnlBytes, luainfoBytes):
        """
        Save the luabnd file as @filename.
        """
        if (not self.useDCX):
            if (not os.path.isfile(filename + '.bak')):
                with open(filename + '.bak', 'wb') as bakf:
                    with open(filename, 'rb') as oldf:
                        bakf.write(oldf.read())

        new_content = []
        for i in range(len(self.luabnd_content)):
            if (self.luabnd_content[i][0] == 1000000):
                new_content.append((self.luabnd_content[i][0], self.luabnd_content[i][1], luagnlBytes))
            elif (self.luabnd_content[i][0] == 1000001):
                new_content.append((self.luabnd_content[i][0], self.luabnd_content[i][1], luainfoBytes))
            else:
                new_content.append(self.luabnd_content[i])

        if (self.useDCX):
            self.dcxhandler.save_dcx(filename + '.dcx', bnd_rebuilder.repack_bnd(new_content))
        else:
            with open(filename, 'wb') as f:
                f.write(bnd_rebuilder.repack_bnd(new_content))

    def add(self, scriptName, newBytes):
        """
        Add a .lua file with the name @scriptName and contents @newBytes to the currently opened .luabnd file
        """
        newPath = self.basePath + byteread.EncodeString(scriptName)
        if (self.useDCX):
            self.basePathRemaster + byteread.EncodeString(scriptName)
        dta = (self.luabnd_maxIndex + 1, newPath, newBytes)
        self.luabnd_content.insert(-2, dta)
        self.luabnd_maxIndex += 1

    def addAuto(self, scriptName):
        """
        Adds <scriptName>.lua to currently opened .luabnd file.
        """
        if not (scriptName == ""):
            with open('enemyRandomizerData\\aiscripts\\' + scriptName, 'rb') as aif:
                data = aif.read()
                self.add(scriptName, data)

    def delete(self, index):
        del self.luabnd_content[index]

    def generateAiScripts(self, baseFolder, targetFolder, dcx = True):
        """
        Extract all unique .lua files from .laubnd files so they can be easily accessed.
        """
        SN_RGX = r"[1-9][0-9][0-9][0-9][0-9][0-9]_(battle|logic).lua"
        inputFiles = ["m10_00_00_00", "m10_01_00_00", "m10_02_00_00", "m11_00_00_00", "m12_00_00_00", "m12_01_00_00", "m13_00_00_00",  "m13_01_00_00", "m13_02_00_00", "m14_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m16_00_00_00", "m17_00_00_00", "m18_00_00_00", "m18_01_00_00"]
        tempList = []
        for iFile in inputFiles:
            fullName = baseFolder + iFile + '.luabnd'
            if (dcx):
                fullName += '.dcx'
            with open(fullName, 'rb') as f:
                content = f.read()
                bndcontent = content
                if (dcx):
                    dcxHandler = DCXHandler()
                    bndcontent = dcxHandler.open_dcx(content)
                data = bnd_rebuilder.unpack_bnd(bndcontent)
                for item in data:
                    strName = item[1].decode('shift_jis')
                    match = re.search(SN_RGX, strName)
                    if not (match == None):
                        if not strName in tempList:
                            tempList.append(strName)
                            fileName = match.group(0)
                            if not (os.path.isfile(targetFolder + fileName)):
                                with open(targetFolder + fileName, 'wb') as wf:
                                    wf.write(item[2])
