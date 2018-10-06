from paramclass import ParamClass

class NpcParam(ParamClass):

    ParamTypes = [
        ('behaviorVariationId','i'),
        ('aiThinkId','i'),
        ('nameId','i'),
        ('turnVellocity','f'),
        ('hitHeight','f'),
        ('hitRadius','f'),
        ('weight','I'),
        ('hitYOffset','f'),
        ('hp','I'),
        ('mp','I'),
        ('getSoul','I'),
        ('itemLotId_1','i'),
        ('itemLotId_2','i'),
        ('itemLotId_3','i'),
        ('itemLotId_4','i'),
        ('itemLotId_5','i'),
        ('itemLotId_6','i'),
        ('humanityLotId','i'),
        ('spEffectID0','i'),
        ('spEffectID1','i'),
        ('spEffectID2','i'),
        ('spEffectID3','i'),
        ('spEffectID4','i'),
        ('spEffectID5','i'),
        ('spEffectID6','i'),
        ('spEffectID7','i'),
        ('GameClearSpEffectID','i'),
        ('physGuardCutRate','f'),
        ('magGuardCutRate','f'),
        ('fireGuardCutRate','f'),
        ('thunGuardCutRate','f'),
        ('animIdOffset','i'),
        ('moveAnimId','i'),
        ('spMoveAnimId1','i'),
        ('spMoveAnimId2','i'),
        ('networkWarpDist','f'),
        ('dbgBehaviorR1','i'),
        ('dbgBehaviorL1','i'),
        ('dbgBehaviorR2','i'),
        ('dbgBehaviorL2','i'),
        ('dbgBehaviorRL','i'),
        ('dbgBehaviorRR','i'),
        ('dbgBehaviorRD','i'),
        ('dbgBehaviorRU','i'),
        ('dbgBehaviorLL','i'),
        ('dbgBehaviorLR','i'),
        ('dbgBehaviorLD','i'),
        ('dbgBehaviorLU','i'),
        ('animIdOffset2','i'),
        ('partsDamageRate1','f'),
        ('partsDamageRate2','f'),
        ('partsDamageRate3','f'),
        ('partsDamageRate4','f'),
        ('partsDamageRate5','f'),
        ('partsDamageRate6','f'),
        ('partsDamageRate7','f'),
        ('partsDamageRate8','f'),
        ('weakPartsDamageRate','f'),
        ('superArmorRecoverCorrection','f'),
        ('superArmorBrakeKnockbackDist','f'),
        ('stamina','H'),
        ('staminaRecoverBaseVel','H'),
        ('def_phys','H'),
        ('def_slash','h'),
        ('def_blow','h'),
        ('def_thrust','h'),
        ('def_mag','H'),
        ('def_fire','H'),
        ('def_thunder','H'),
        ('defFlickPower','H'),
        ('resist_poison','H'),
        ('resist_desease','H'),
        ('resist_blood','H'),
        ('resist_curse','H'),
        ('ghostModelId','h'),
        ('normalChangeResouceId','h'),
        ('guardAngle','h'),
        ('slashGuardCutRate','h'),
        ('blowGuardCutRate','h'),
        ('thrustGuardCutRate','h'),
        ('superArmorDurability','h'),
        ('normalChangeTexChrId','h'),
        ('dropType','H'),
        ('knockbackRate','B'),
        ('knockbackParamId','B'),
        ('fallDamageDump','B'),
        ('staminaGuardDef','B'),
        ('pcAttrB','B'),
        ('pcAttrW','B'),
        ('pcAttrL','B'),
        ('pcAttrR','B'),
        ('areaAttrB','B'),
        ('areaAttrW','B'),
        ('areaAttrL','B'),
        ('areaAttrR','B'),
        ('mpRecoverBaseVel','B'),
        ('flickDamageCutRate','B'),
        ('defaultLodParamId','b'),
        ('drawType','B'),
        ('npcType','B'),
        ('teamType','B'),
        ('moveType','B'),
        ('lockDist','B'),
        ('material','B'),
        ('materialSfx','B'),
        ('material_Weak','B'),
        ('materialSfx_Weak','B'),
        ('partsDamageType','B'),
        ('maxUndurationAng','B'),
        ('guardLevel','b'),
        ('burnSfxType','B'),
        ('poisonGuardResist','b'),
        ('diseaseGuardResist','b'),
        ('bloodGuardResist','b'),
        ('curseGuardResist','b'),
        ('parryAttack','B'),
        ('parryDefence','B'),
        ('sfxSize','B'),
        ('pushOutCamRegionRadius','B'),
        ('hitStopType','B'),
        ('ladderEndChkOffsetTop','B'),
        ('ladderEndChkOffsetLow','B')
    ]

    ParamEndTypes = [
        ('useRagdollCamHit:1','?',1),
        ('disableClothRigidHit:1','?',1),
        ('useRagdoll:1','?',1),
        ('isDemon:1','?',1),
        ('isGhost:1','?',1),
        ('isNoDamageMotion:1','?',1),
        ('isUnduration:1','?',1),
        ('isChangeWanderGhost:1','?',1),
        ('modelDispMask0:1','?',1),
        ('modelDispMask1:1','?',1),
        ('modelDispMask2:1','?',1),
        ('modelDispMask3:1','?',1),
        ('modelDispMask4:1','?',1),
        ('modelDispMask5:1','?',1),
        ('modelDispMask6:1','?',1),
        ('modelDispMask7:1','?',1),
        ('modelDispMask8:1','?',1),
        ('modelDispMask9:1','?',1),
        ('modelDispMask10:1','?',1),
        ('modelDispMask11:1','?',1),
        ('modelDispMask12:1','?',1),
        ('modelDispMask13:1','?',1),
        ('modelDispMask14:1','?',1),
        ('modelDispMask15:1','?',1),
        ('isEnableNeckTurn:1','?',1),
        ('disableRespawn:1','?',1),
        ('isMoveAnimWait:1','?',1),
        ('isCrowd:1','?',1),
        ('isWeakSaint:1','?',1),
        ('isWeakA:1','?',1),
        ('isWeakB:1','?',1),
        ('pad1:1','B',1),
        ('vowType:3','B',3),
        ('disableInitializeDead:1','?',1),
        ('pad3:4','B',4),
        ('pad2[6]','B',48)
    ]

    BossSouls = [
        (223000, 20000, "Stray Demon"),
        (223100, 20000, "Demon Firesage"),
        (223200, 2000, "Asylum Demon"),
        (224000, 6000, "Capra Demon"),
        (225000, 3000, "Taurus Demon"),
        (232000, 40000, "Iron Golem"),
        (236000, 10000, "Small Smough"),
        (236001, 25000, "Big Smough"),
        (273000, 30000, "Priscilla"),
        (323000, 10000, "MLB"),
        (332000, 15000, "Pinwheel"),
        (347100, 30000, "Sanctuary Guardian"),
        (410000, 50000, "Artorias"),
        (450000, 60000, "Manus"),
        (451000, 60000, "Kalameet"),
        (520000, 40000, "Centipede"),
        (521000, 40000, "Sif"),
        (522000, 60000, "Nito"),
        (525000, 20000, "Ceaseless"),
        (526000, 25000, "Gaping"),
        (527000, 10000, "Ornstein"),
        (527100, 25000, "Super Ornstein"),
        (528000, 20000, "Quelaag"),
        (529000, 60000, "Seath"),
        (532000, 40000, "Gwyndolin"),
        (535000, 5000, "Bell Gargs"),
        (537000, 70000, "GWYN"),
        (539001, 60000, "Four Kings")
    ]

    def ApplyBossSoulCount(self, soulPercentage:int):
        entryCount = len(self.data)
        expectedIndex = 0;
        for i in range(entryCount):
            if (expectedIndex < len(self.BossSouls)):
                if (self.param.Rows[i].id == self.BossSouls[expectedIndex][0] + 50):
                    newSouls = (int)(self.BossSouls[expectedIndex][1] * (soulPercentage / 100.0))
                    self.data[i]['normal']['getSoul'] = newSouls
                    expectedIndex += 1
        
    def AddNewBossParams(self):
        entryCount = len(self.data)
        hasEntries = False
        for row in self.param.Rows:
            if (row.id == 223050):
                hasEntries = True
                break

        if (not hasEntries):
            print("Custom NpcParam entries not found, adding.")
            toAddList = []
            expectedIndex = 0;
            for i in range(entryCount):
                if (expectedIndex < len(self.BossSouls)):
                    if (self.param.Rows[i].id == self.BossSouls[expectedIndex][0]):
                        entry = self.data[i]
                        toAddList.append((self.param.Rows[i].id, entry, self.BossSouls[expectedIndex][2]))
                        expectedIndex += 1;
                else:
                    break
            for rowIdx, rowData, bossName in toAddList:
                self.addEntry(rowIdx + 50, bossName + " with souls", rowData)

