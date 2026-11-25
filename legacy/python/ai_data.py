import bnd_rebuilder
import byteread

class AiDataContainer():
    
    def __init__(self, file):
        with open(file, 'r') as fm:
            lines = fm.readlines()

        self.aidatas = []
        
        for line in lines:
            parts = line.strip().split('\t')
            nid = int(parts[0])
            name1 = parts[1]
            name2 = parts[2]
            battle = parts[3]
            logic = parts[4]
            gnlScriptNames = []
            for e in parts[5].split(';'):
                gnlScriptNames.append(byteread.EncodeString(e))

            iEntriesList = []

            infoEntries = parts[6].split('|')
            
            for ientry in infoEntries:
                parts2 = ientry.split(';')
                infoEntry = (int(parts2[0]), byteread.EncodeString(parts2[1]), byteread.EncodeString(parts2[2]), int(parts2[3]), int(parts2[4]), int(parts2[5]), int(parts2[6]))
                iEntriesList.append(infoEntry)
            
            iEntriesList.reverse()

            self.aidatas.append(AiData(nid, name1, name2, battle, logic, gnlScriptNames, iEntriesList))

    def GetListEntries(self):
        retList = []
        for ai in self.aidatas:
            retList.append(str(ai.npcai) + " - " + ai.descName + " (" + ai.filesName + ")")

        return retList

    def GetEntryByAI(self, npcai_id):
        intid = int(npcai_id)
        for ai in self.aidatas:
            if (ai.npcai == intid):
                return ai
        return None



class AiData():

    def __init__(self, nid, name1, name2, battle, logic, gnlScriptNames, infoEntry):
        self.npcai = nid
        self.descName = name1
        self.filesName = name2
        self.battle_script = battle
        self.logic_script = logic
        self.aiFuncsGnl = gnlScriptNames
        self.info = infoEntry