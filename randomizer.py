from random import randint
import random
import os.path
import os
from tkinter import *
from tkinter.ttk import Progressbar
from tkinter.ttk import Notebook
import tkinter.ttk as ttk
import check_exe
import tkinter.messagebox
import datetime
import threading
import webbrowser
from enum import Enum
from randomizer_rng import Randomizer


def resource_path(relative_path):
    """ Get absolute path to resource, works for dev and for PyInstaller """
    try:
        # PyInstaller creates a temp folder and stores path in _MEIPASS
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")

    return os.path.join(base_path, relative_path)

class randomizationThread (threading.Thread):
    """
    Separate thread for randomizing, so that the UI can be updated.
    """
    def __init__(self, threadID, name, counter, randomizer, rsettings, msgArea, mainw, timeString):
        threading.Thread.__init__(self)
        self.threadID = threadID
        self.name = name
        self.counter = counter
        self.randomizer = randomizer
        self.rsettings = rsettings
        self.msgArea = msgArea
        self.mainw = mainw
        self.timeString = timeString
    def run(self):
        try:
            self.randomizer.randomize(self.rsettings, self.msgArea)
            tkinter.messagebox.showinfo("Randomization complete", "Randomization completed successfully. \nLog saved to 'enemyRandomizerData/logs/rlog" + self.timeString + ".txt'")
        except Exception as e:
            tkinter.messagebox.showerror("Something went very wrong.", "The randomizer has run into an exception:\n'" + str(e) + "'\nTraceback in the console.")
            raise
        finally:
            self.mainw.progressTopLevel.destroy()
            self.mainw.randomize_button.config(state = "normal")
            self.mainw.unrandomize_button.config(state = "normal")

class AutoScrollbar(Scrollbar):
    # a scrollbar that hides itself if it's not needed.  only
    # works if you use the grid geometry manager.
    def set(self, lo, hi):
        if float(lo) <= 0.0 and float(hi) >= 1.0:
            # grid_remove is currently missing from Tkinter!
            self.tk.call("grid", "remove", self)
        else:
            self.grid()
        Scrollbar.set(self, lo, hi)

class ScrollableFrame():
    def __init__(self, container):
        self.vscrollbar = AutoScrollbar(container)
        self.vscrollbar.grid(row=1, column=1, sticky=N+S)

        self.canvas = Canvas(container,
                        yscrollcommand=self.vscrollbar.set)
        self.canvas.grid(row=1, column=0, sticky=N+S+E+W)

        self.vscrollbar.config(command=self.canvas.yview)

        # make the canvas expandable
        container.grid_rowconfigure(1, weight=1)
        container.grid_columnconfigure(0, weight=1)

        #
        # create canvas contents

        self.inner = Frame(self.canvas)

        self.canvas_frame = self.canvas.create_window(0, 0, anchor=NW, window=self.inner)
        self.canvas.bind('<Configure>', self.UpdateFrameWidth)

        #self.canvas.bind_all("<MouseWheel>", self._on_mousewheel)
        container.bind('<Enter>', self._bound_to_mousewheel)
        container.bind('<Leave>', self._unbound_to_mousewheel)

        self.UpdateCanvasSize()

    def UpdateFrameWidth(self, event):
        new_w = event.width
        self.canvas.itemconfig(self.canvas_frame, width = new_w)

    def UpdateCanvasSize(self):
        self.inner.update_idletasks()
        self.canvas.config(scrollregion=self.canvas.bbox("all"))

    def _bound_to_mousewheel(self, event):
        self.canvas.bind_all("<MouseWheel>", self._on_mousewheel)   

    def _unbound_to_mousewheel(self, event):
        self.canvas.unbind_all("<MouseWheel>") 

    def _on_mousewheel(self, event):
        self.canvas.yview_scroll(int(-1*(event.delta/120)), "units")

class MainWindow():

    messages = [["* Bosses will not be replaced", "* Bosses will be replaced with bosses", "* Bosses will be replaced with normal enemies", "* Bosses will be replaced both with bosses and normal enemies"],
        ["* Normal will not be replaced", "* Normal will be replaced with bosses\n  Would recommend you to set Replacement Chance to < 15%\n  otherwise it can become pretty difficult to beat the game", "* Normal enemies will be replaced with normal enemies", "* Normal enemies will be replaced both with bosses and normal enemies"],
        ["* Enemies will only be placed where they fit", "* Enemies can be placed anywhere, regardless of their size\n  May cause some NPC-s to become too big to be able to talk to them if\n  NPC replacement is enabled.", "* Enemies can be placed anywhere, regardless of their size\n  Except when replacing NPC-s (so size limit is enforced on them)"],
        ["* Removed mode.", "* Mode: Enemies will be replaced with enemies from other areas\n  while still trying to maintain some sort of a difficulty curve\n  NOTICE: not really compatible with 'Replace normals only with bosses' and\n  'Replace bosses only with normals', you'll be seeing lots of unmodified\n  enemies in those modes.", "* Mode: Any enemy/boss can be placed ANYWHERE in the world\n  So yes, you could find things like Manus or Nito in the Asylum", "* Mode: Any enemy/boss can be placed ANYWHERE in the world\n  with asylum being the only exception.\n  Keeps asylum easy so you don't have to fight someone like\n  Manus with starting stats and equipment."],
        ["* Mimics are not replaced", "* Mimics are replaced; in this version of the randomizer\n  they will properly drop their items."],
        ["* NPC-s will not be replaced", "* Certain NPC-s will be replaced with bosses\n  Has no impact on talking with them", "* Certain NPC-s will be replaced with normal enemies\n  Has no impact on talking with them", "* Certain NPC-s can be replaced both with bosses and normal enemies\n  Has no impact on talking with them"],
        ["* -", "* -", "* -"],
        ["* The second gargoyle in the boss fight is not replaced", "* The second gargoyle in the boss fight IS replaced\n  Can potentially make the fight extremely difficult\n  eg. if you get Kalameet+Manus there"],
        ["* Difficulty curve is followed quite strictly: Harder enemies can still appear\n  in early-game but it's somewhat rare.\n  Least variety, but most consistent difficulty\n  (Only has effect if mode is set to 'Random with difficulty curve')", "* Following the difficulty curve is a bit looser: a bit higher chance to get\n  harder enemies in early game.\n  A bit more variety, while maintaining relatively reasonable difficulty.\n  (Only has effect if mode is set to 'Random with difficulty curve')", "* Difficulty curve following is rather loose: Enemy difficulty can vary much\n  more and harder enemies in early game are more common.\n  It's possible to, in rare cases, to even see Manus in Asylum in this mode.\n  (Only has effect if mode is set to 'Random with difficulty curve')"],
        ["* T-Posing Enabled: Enemies replacing enemies with special idle state retain\n  the original enemys idle animation resulting in T-Posing most of the time.", "* T-Posing Disabled: Enemies replacing enemies with special idle state (like\n  hollows in New Londo) have a proper animation assigned to them.\n  Do note that most of these enemies will now be immediately hostile.\n  NPC-s are still in T-Pose mode for that reason."],
        ["* Type replacement enabled: If within one area there were originally multiple\n  of the same enemy, then all of those enemies will be replaced with one type\n  of enemy. For example: all Silver Knights in Anor Londo would be replaced\n  with Darkwraiths, instead of individual Silver Knights being replaced by\n  different enemies.", "* Type replacement disabled, each enemy is randomized separately."],
        ["* The main Pinwheel in the boss is not replaced, but it's clones are.\n  Replaced clones have normal HP instead of being 1 hit kill.\n  (A quite ridiculous and unfair mode; also can cause the game to lag severely\n  during the boss fight with certain enemies)", "* Pinwheel Boss will be replaced as normal, a single enemy replacing the main\n  Pinwheel."],
        ["* When an enemy is chosen to be replaced by Gwyn, there is a 85% chance that a\n  new enemy will be chosen instead.", "* When an enemy is chosen to be replaced by Gwyn, there is a 60% chance that a\n  new enemy will be chosen instead.", "* Gwyn spawn rate is not nerfed."],
        ["* Enemies are not allowed to be replaced with the same enemy (so a Hollow can't\n  be replaced with another Hollow). Can result in less boss variety with the\n  difficulty curve option in early game.", "* Enemies can be replaced with the same enemy. (eg. a Hollow can be replaced\n  with a Hollow)"]]

    def __init__(self):
        self.root = Tk()
        self.randomizerVersion = "v0.4"
        self.root.title("Dark Souls - Enemy randomizer " + self.randomizerVersion + " by rycheNhavalys")

        self.root.iconbitmap(default=resource_path('favicon.ico'))

        self.randomizer = Randomizer()

        self.settingsTabs = Notebook(self.root)
        self.settingsTabs.grid(row=2, column=2, columnspan=2, rowspan=4, sticky='NEWS')
        self.settingsTabs.bind('<<NotebookTabChanged>>', self.SettingsPageChanged)

        self.settingsPage1 = ttk.Frame(self.settingsTabs)
        self.settingsTabs.add(self.settingsPage1, text='Main Replacement Options')

        self.settingsPage2 = ttk.Frame(self.settingsTabs)
        self.settingsTabs.add(self.settingsPage2, text='Other Options')

        self.buttons_frame = LabelFrame(self.root, text="Randomization")
        self.buttons_frame.grid(row=4, column=4, sticky='NWES', padx=2)
        self.buttons_frame.columnconfigure(0, weight=1)
        self.buttons_frame.rowconfigure(0, weight=1)
        self.buttons_frame.rowconfigure(1, weight=1)

        self.randomize_button = Button(self.buttons_frame, text="Randomize", width=24, command=self.RandomizeEnemies)
        self.randomize_button.grid(row=0, column=0, sticky='NWSE', padx=2, pady=4)

        self.unrandomize_button = Button(self.buttons_frame, text="Revert to normal", command=self.Unrandomize)
        self.unrandomize_button.grid(row=1, column=0, sticky='NWSE', padx=2, pady=4)

        # Create message area
        self.msg_area = Text(self.root, width=80, height=38, state="disabled", background="gray84", relief=GROOVE, wrap="word")
        self.msg_area.grid(row=2, column=0, columnspan=2, rowspan=7, padx=2, pady=2)

        self.msg_area.tag_config("uf", foreground="gray50")                         # hovering over a different setting
        self.msg_area.tag_config("f", foreground="gray10", background="gray88")     # hovering over the selected value of a setting
        self.msg_area.tag_config("c", foreground="DeepPink3", background="gray88")  # hovering over a different value of a setting

        self.msg_area.tag_config("sellout_text", foreground="gray10")
        self.msg_area.tag_config("sellout_link", foreground="DeepPink2", underline=1)   # link to streamlabs sellout page
        self.msg_area.tag_config("major_error", foreground="red")                       # error messages

        self.msg_area.tag_bind("sellout_link", "<Enter>", lambda _: self.msg_area.config(cursor="hand2"))
        self.msg_area.tag_bind("sellout_link", "<Leave>", lambda _: self.msg_area.config(cursor=""))
        self.msg_area.tag_bind("sellout_link", "<Button-1>", lambda _: webbrowser.open_new_tab("https://streamlabs.com/rychenhavalys"))

        self.sellout_button = Button(self.root, text="$", foreground="DeepPink3", command=self.OpenSelloutPage)
        self.sellout_button.grid(row=1, column=0, sticky="NWS", padx=6, pady=4)

        self.sellout_close_button = Button(self.root, text="Back", command=self.CloseSelloutPage, width=10)
        self.sellout_close_button.grid(row=1, column=0, sticky="NWS", padx=6, pady=4)
        self.sellout_close_button.grid_remove() # Hide the sellout page closing button by default

        self.tags=["f", "f", "f", "f", "f", "f", "f", "f", "f", "f", "f", "f", "f", "f"]

        self.hoverO = -1
        self.hoverL = -1

        # Init setting variables

        self.bossReplaceMode = IntVar()
        self.bossReplaceMode.set(1)

        self.enemyReplaceMode = IntVar()
        self.enemyReplaceMode.set(2)

        self.fitMode = IntVar()
        self.fitMode.set(0)

        self.difficultyMode = IntVar()
        self.difficultyMode.set(3)

        self.mimicMode = IntVar()
        self.mimicMode.set(0)

        self.npcMode = IntVar()
        self.npcMode.set(0)

        self.gargoyleMode = IntVar()
        self.gargoyleMode.set(1)

        self.diffStrictness = IntVar()
        self.diffStrictness.set(1)

        self.tposeCity = IntVar()
        self.tposeCity.set(0)

        self.typeReplacement = IntVar()
        self.typeReplacement.set(1)

        self.pinwheelChaos = IntVar()
        self.pinwheelChaos.set(1)

        self.gwynNerf = IntVar()
        self.gwynNerf.set(1)

        self.preventSame = IntVar()
        self.preventSame.set(1)

        self.seedValue = StringVar()
        self.seedValue.set("")

        self.configValue = StringVar()
        self.configValue.set("")

        self.configString = ""

        self.root.columnconfigure(4, weight=1)
        self.root.columnconfigure(3, weight=1)
        self.root.columnconfigure(2, weight=1)

        self.settingsPage1.columnconfigure(4, weight=1)
        self.settingsPage1.columnconfigure(3, weight=1)
        self.settingsPage1.columnconfigure(2, weight=1)

        self.settingsPage2.columnconfigure(4, weight=1)
        self.settingsPage2.columnconfigure(3, weight=1)
        self.settingsPage2.columnconfigure(2, weight=1)

        # Seed entry

        self.seedLabel = Label(self.root, text="Seed (leave blank for random): ")
        self.seedLabel.grid(row = 1, column = 2, sticky="E", padx=2)

        self.seedEntry = Entry(self.root, textvariable=self.seedValue)
        self.seedEntry.grid(row = 1, column = 3, columnspan=1, sticky="NEWS", padx=8, pady=4)

        # Settings

        self.bosses_frame = LabelFrame(self.settingsPage1, text="Replace Bosses:")
        self.bosses_frame.grid(row=2, column=2, sticky='NWES', padx=2)

        self.bossBtn1 = Radiobutton(self.bosses_frame, text="Don't replace", variable=self.bossReplaceMode, value=0, command=self.UpdateMessageArea)
        self.bossBtn1.pack(anchor=W)
        self.bossBtn2 = Radiobutton(self.bosses_frame, text="Only with bosses", variable=self.bossReplaceMode, value=1, command=self.UpdateMessageArea)
        self.bossBtn2.pack(anchor=W)
        self.bossBtn3 = Radiobutton(self.bosses_frame, text="Only with normal enemies", variable=self.bossReplaceMode, value=2, command=self.UpdateMessageArea)
        self.bossBtn3.pack(anchor=W)
        self.bossBtn4 = Radiobutton(self.bosses_frame, text="With bosses or normal enemies", variable=self.bossReplaceMode, value=3, command=self.UpdateMessageArea)
        self.bossBtn4.pack(anchor=W)

        self.BindTags(self.bossBtn1, 0, 0)
        self.BindTags(self.bossBtn2, 0, 1)
        self.BindTags(self.bossBtn3, 0, 2)
        self.BindTags(self.bossBtn4, 0, 3)


        self.normal_frame = LabelFrame(self.settingsPage1, text="Replace Normal Enemies:")
        self.normal_frame.grid(row=3, column=2, sticky='NWES', padx=2)

        self.normBtn1 = Radiobutton(self.normal_frame, text="Don't replace", variable=self.enemyReplaceMode, value=0, command=self.UpdateMessageArea)
        self.normBtn1.pack(anchor=W)
        self.normBtn2 = Radiobutton(self.normal_frame, text="Only with bosses", variable=self.enemyReplaceMode, value=1, command=self.UpdateMessageArea)
        self.normBtn2.pack(anchor=W)
        self.normBtn3 = Radiobutton(self.normal_frame, text="Only with normal enemies", variable=self.enemyReplaceMode, value=2, command=self.UpdateMessageArea)
        self.normBtn3.pack(anchor=W)
        self.normBtn4 = Radiobutton(self.normal_frame, text="With bosses or normal enemies", variable=self.enemyReplaceMode, value=3, command=self.UpdateMessageArea)
        self.normBtn4.pack(anchor=W)

        self.BindTags(self.normBtn1, 1, 0)
        self.BindTags(self.normBtn2, 1, 1)
        self.BindTags(self.normBtn3, 1, 2)
        self.BindTags(self.normBtn4, 1, 3)


        self.fit_frame = LabelFrame(self.settingsPage1, text="Enemy placement:")
        self.fit_frame.grid(row=4, column=3, sticky='NWES', padx=2)

        self.fitBtn1 = Radiobutton(self.fit_frame, text="Only where they fit", variable=self.fitMode, value=0, command=self.UpdateMessageArea)
        self.fitBtn1.pack(anchor=W)
        self.fitBtn2 = Radiobutton(self.fit_frame, text="Anywhere", variable=self.fitMode, value=1, command=self.UpdateMessageArea)
        self.fitBtn2.pack(anchor=W)
        self.fitBtn3 = Radiobutton(self.fit_frame, text="Anywhere, except when replacing NPC-s", variable=self.fitMode, value=2, command=self.UpdateMessageArea)
        self.fitBtn3.pack(anchor=W)

        self.BindTags(self.fitBtn1, 2, 0)
        self.BindTags(self.fitBtn2, 2, 1)
        self.BindTags(self.fitBtn3, 2, 2)


        self.dif_frame = LabelFrame(self.settingsPage1, text="Mode:")
        self.dif_frame.grid(row=2, column=3, sticky='NWES', padx=2)

        self.difBtn2 = Radiobutton(self.dif_frame, text="Random with difficulty curve", variable=self.difficultyMode, value=1, command=self.UpdateMessageArea)
        self.difBtn2.pack(anchor=W)
        self.difBtn4 = Radiobutton(self.dif_frame, text="Fully random with easy asylum", variable=self.difficultyMode, value=3, command=self.UpdateMessageArea)
        self.difBtn4.pack(anchor=W)
        self.difBtn3 = Radiobutton(self.dif_frame, text="Fully random", variable=self.difficultyMode, value=2, command=self.UpdateMessageArea)
        self.difBtn3.pack(anchor=W)
        
        self.BindTags(self.difBtn2, 3, 1)
        self.BindTags(self.difBtn3, 3, 2)
        self.BindTags(self.difBtn4, 3, 3)


        self.mimics_frame = LabelFrame(self.settingsPage2, text="Mimics:")
        self.mimics_frame.grid(row=3, column=2, sticky='NWES', padx=2)

        self.mimBtn1 = Radiobutton(self.mimics_frame, text="Do not replace", variable=self.mimicMode, value=0, command=self.UpdateMessageArea)
        self.mimBtn1.pack(anchor=W)
        self.mimBtn2 = Radiobutton(self.mimics_frame, text="Replace", variable=self.mimicMode, value=1, command=self.UpdateMessageArea)
        self.mimBtn2.pack(anchor=W)

        self.BindTags(self.mimBtn1, 4, 0)
        self.BindTags(self.mimBtn2, 4, 1)


        self.npc_frame = LabelFrame(self.settingsPage1, text="Replace NPC-s:", width=256)
        self.npc_frame.grid(row=4, column=2, sticky='NWES', padx=2)

        self.npcBtn1 = Radiobutton(self.npc_frame, text="Do not replace", variable=self.npcMode, value=0, command=self.UpdateMessageArea)
        self.npcBtn1.pack(anchor=W)
        self.npcBtn2 = Radiobutton(self.npc_frame, text="Only with bosses", variable=self.npcMode, value=1, command=self.UpdateMessageArea)
        self.npcBtn2.pack(anchor=W)
        self.npcBtn3 = Radiobutton(self.npc_frame, text="Only with normal enemies", variable=self.npcMode, value=2, command=self.UpdateMessageArea)
        self.npcBtn3.pack(anchor=W)
        self.npcBtn4 = Radiobutton(self.npc_frame, text="With bosses or normal enemies", variable=self.npcMode, value=3, command=self.UpdateMessageArea)
        self.npcBtn4.pack(anchor=W)

        self.BindTags(self.npcBtn1, 5, 0)
        self.BindTags(self.npcBtn2, 5, 1)
        self.BindTags(self.npcBtn3, 5, 2)
        self.BindTags(self.npcBtn4, 5, 3)


        self.replace_chance_frame = LabelFrame(self.settingsPage1, text="Replacement chance (%):")
        self.replace_chance_frame.grid(row=5, column=2, sticky='NWES', padx=2)

        self.replace_chance_slider = Scale(self.replace_chance_frame, from_=0, to=100, orient=HORIZONTAL)
        self.replace_chance_slider.grid(row=0, column=0, sticky='NWES', padx=2)
        self.replace_chance_slider.set(100)

        Label(self.replace_chance_frame, text="Chance that an enemy/boss\n will be replaced at all.").grid(row=2, column=0, sticky='NWES', padx=2, pady=2)


        self.boss_chance_frame = LabelFrame(self.settingsPage1, text="Boss chance [Normal Enemies](%):")
        self.boss_chance_frame.grid(row=5, column=3, sticky='NWES', padx=2)

        self.boss_chance_slider = Scale(self.boss_chance_frame, from_=0, to=100, orient=HORIZONTAL)
        self.boss_chance_slider.grid(row=0, column=0, sticky='NWES', padx=2)
        self.boss_chance_slider.set(10)

        Label(self.boss_chance_frame, text="Chance that a normal enemy or NPC will be replaced\nwith a boss instead of an normal enemy.\nOnly has effect when normal enemy or NPC\nmode is set to 'With bosses or normal enemies'.").grid(row=2, column=0, sticky='NWES', padx=2, pady=2)


        self.boss_chance_frame_bosses = LabelFrame(self.settingsPage1, text="Boss chance [Bosses](%):")
        self.boss_chance_frame_bosses.grid(row=6, column=3, sticky='NWES', padx=2)

        self.boss_chance_slider_bosses = Scale(self.boss_chance_frame_bosses, from_=0, to=100, orient=HORIZONTAL)
        self.boss_chance_slider_bosses.grid(row=0, column=0, sticky='NWES', padx=2)
        self.boss_chance_slider_bosses.set(90)

        Label(self.boss_chance_frame_bosses, text="Chance that a boss will be replaced with a boss instead\nof an normal enemy.\nOnly has effect when boss mode\n is set to 'With bosses or normal enemies'").grid(row=1, column=0, sticky='NWES', padx=2, pady=2)


        self.gargoyle_frame = LabelFrame(self.settingsPage2, text="Gargoyle #2:", width=256)
        self.gargoyle_frame.grid(row=4, column=2, sticky='NWES', padx=2)

        self.gargBtn1 = Radiobutton(self.gargoyle_frame, text="Do not replace         ", variable=self.gargoyleMode, value=0, command=self.UpdateMessageArea)
        self.gargBtn1.pack(anchor=W)
        self.gargBtn2 = Radiobutton(self.gargoyle_frame, text="Replace", variable=self.gargoyleMode, value=1, command=self.UpdateMessageArea)
        self.gargBtn2.pack(anchor=W)

        self.BindTags(self.gargBtn1, 7, 0)
        self.BindTags(self.gargBtn2, 7, 1)


        self.diff_strict_frame = LabelFrame(self.settingsPage1, text="Difficulty strictness:")
        self.diff_strict_frame.grid(row=3, column=3, sticky='NWES', padx=2)

        self.strictBtn1 = Radiobutton(self.diff_strict_frame, text="Strict", variable=self.diffStrictness, value=0, command=self.UpdateMessageArea)
        self.strictBtn1.pack(anchor=W)
        self.strictBtn2 = Radiobutton(self.diff_strict_frame, text="A bit loose", variable=self.diffStrictness, value=1, command=self.UpdateMessageArea)
        self.strictBtn2.pack(anchor=W)
        self.strictBtn3 = Radiobutton(self.diff_strict_frame, text="Very loose", variable=self.diffStrictness, value=2, command=self.UpdateMessageArea)
        self.strictBtn3.pack(anchor=W)

        self.BindTags(self.strictBtn1, 8, 0)
        self.BindTags(self.strictBtn2, 8, 1)
        self.BindTags(self.strictBtn3, 8, 2)


        self.tpose_frame = LabelFrame(self.settingsPage2, text="T-Posing enemies:")
        self.tpose_frame.grid(row=5, column=2, sticky='NWES', padx=2)
        
        self.tposeBtn1 = Radiobutton(self.tpose_frame, text="Enabled", variable=self.tposeCity, value=0, command=self.UpdateMessageArea)
        self.tposeBtn1.pack(anchor=W)
        self.tposeBtn2 = Radiobutton(self.tpose_frame, text="Disabled", variable=self.tposeCity, value=1, command=self.UpdateMessageArea)
        self.tposeBtn2.pack(anchor=W)

        self.BindTags(self.tposeBtn1, 9, 0)
        self.BindTags(self.tposeBtn2, 9, 1)


        self.openConfTopLevel = Button(self.root, text="Open config input", command=self.OpenTextConfigTopLevel)
        self.openConfTopLevel.grid(row=1, column=4, sticky='NEWS', padx=4, pady=4)

        self.textConfigTopLevel = None

        # Enemy Config

        self.customEnemyConfigs = ["Default"]
        self.BuildCustomEnemyConfigList()

        self.enemy_config_frame = LabelFrame(self.root, text="Enemy Config:")
        self.enemy_config_frame.grid(row=5, column=4, sticky='NWES', padx=2)

        self.enemyConfigForRandomization = StringVar()

        self.enemyConfigForRandomization.set(self.customEnemyConfigs[0])

        self.enemyConfigSelectForRandomization = OptionMenu(self.enemy_config_frame, self.enemyConfigForRandomization, *self.customEnemyConfigs)
        self.enemyConfigSelectForRandomization.grid(row=1, column=0, padx=2, sticky='NWSE')

        self.enemyConfigTopLevel = None

        self.open_enemy_config_button = Button(self.enemy_config_frame, text="Open Enemy Config Editor", width=24, command=self.TryOpenEnemyConfigTopLevel)
        self.open_enemy_config_button.grid(row=2, column=0, sticky='NWSE', padx=2, pady=4)

        self.progressTopLevel = None

        # Boss Souls

        self.boss_souls_frame = LabelFrame(self.settingsPage2, text="Roaming boss soul drops (%):")
        self.boss_souls_frame.grid(row=6, column=3, sticky='NWES', padx=2)

        self.boss_souls_slider = Scale(self.boss_souls_frame, from_=0, to=100, orient=HORIZONTAL)
        self.boss_souls_slider.grid(row=0, column=0, sticky='NWES', padx=2)
        self.boss_souls_slider.set(50)

        Label(self.boss_souls_frame, text="Controls the amount of souls bosses that\nreplace normal enemies drop.").grid(row=2, column=0, sticky='NWES', padx=2, pady=2)

        # Type replacement

        self.type_replace_frame = LabelFrame(self.settingsPage2, text="Type Replacement:")
        self.type_replace_frame.grid(row=3, column=3, sticky='NWES', padx=2)
        
        self.typeReplaceBtn1 = Radiobutton(self.type_replace_frame, text="Enabled", variable=self.typeReplacement, value=0, command=self.UpdateMessageArea)
        self.typeReplaceBtn1.pack(anchor=W)
        self.typeReplaceBtn2 = Radiobutton(self.type_replace_frame, text="Disabled", variable=self.typeReplacement, value=1, command=self.UpdateMessageArea)
        self.typeReplaceBtn2.pack(anchor=W)

        self.BindTags(self.typeReplaceBtn1, 10, 0)
        self.BindTags(self.typeReplaceBtn2, 10, 1)

        # Pinwheel Chaos

        self.pinwheel_frame = LabelFrame(self.settingsPage2, text="Pinwheel Chaos:")
        self.pinwheel_frame.grid(row=4, column=3, sticky='NWES', padx=2)
        
        self.pinwheelBtn1 = Radiobutton(self.pinwheel_frame, text="Enabled", variable=self.pinwheelChaos, value=0, command=self.UpdateMessageArea)
        self.pinwheelBtn1.pack(anchor=W)
        self.pinwheelBtn2 = Radiobutton(self.pinwheel_frame, text="Disabled", variable=self.pinwheelChaos, value=1, command=self.UpdateMessageArea)
        self.pinwheelBtn2.pack(anchor=W)

        self.BindTags(self.pinwheelBtn1, 11, 0)
        self.BindTags(self.pinwheelBtn2, 11, 1)

        # Gwyn Nerf

        self.gwynrate_frame = LabelFrame(self.settingsPage2, text="Gwyn Spawn-Rate Nerf:")
        self.gwynrate_frame.grid(row=5, column=3, sticky='NWES', padx=2)
        
        self.gwynrateBtn1 = Radiobutton(self.gwynrate_frame, text="Strong", variable=self.gwynNerf, value=0, command=self.UpdateMessageArea)
        self.gwynrateBtn1.pack(anchor=W)
        self.gwynrateBtn2 = Radiobutton(self.gwynrate_frame, text="Medium", variable=self.gwynNerf, value=1, command=self.UpdateMessageArea)
        self.gwynrateBtn2.pack(anchor=W)
        self.gwynrateBtn3 = Radiobutton(self.gwynrate_frame, text="None", variable=self.gwynNerf, value=2, command=self.UpdateMessageArea)
        self.gwynrateBtn3.pack(anchor=W)

        self.BindTags(self.gwynrateBtn1, 12, 0)
        self.BindTags(self.gwynrateBtn2, 12, 1)
        self.BindTags(self.gwynrateBtn3, 12, 2)

        # Same enemy replacement prevention

        self.replace_same_frame = LabelFrame(self.settingsPage2, text="Prevent replacement with same enemy: ")
        self.replace_same_frame.grid(row=6, column=2, sticky='NWES', padx=2)
        
        self.sameReplaceBtn1 = Radiobutton(self.replace_same_frame, text="Enabled", variable=self.preventSame, value=0, command=self.UpdateMessageArea)
        self.sameReplaceBtn1.pack(anchor=W)
        self.sameReplaceBtn2 = Radiobutton(self.replace_same_frame, text="Disabled", variable=self.preventSame, value=1, command=self.UpdateMessageArea)
        self.sameReplaceBtn2.pack(anchor=W)

        self.BindTags(self.sameReplaceBtn1, 13, 0)
        self.BindTags(self.sameReplaceBtn2, 13, 1)

        if (self.randomizer.canRandomize):
            if (self.randomizer.exeStatus == "Unknown"):
                # Show a warning if the .exe checksum is unknown.
                tkinter.messagebox.showwarning("Unknown DARKSOULS.exe", "The checksum of DARKSOULS.exe is unknown, if you know what you're doing then proceed, otherwise it might be a unpacking issue maybe?")
            
            if not (self.randomizer.areCopiesValid):
                retryCopy = True
                while (retryCopy):
                    retryCopy = tkinter.messagebox.askretrycancel("Copies failed", "Something went wrong with file copies.\nRetry copying?")
                    if (retryCopy):
                        self.randomizer.retryFileCopy()

                if (not self.randomizer.areCopiesValid and not retryCopy):
                    self.randomize_button.config(state = "disabled")
                    self.unrandomize_button.config(state = "disabled")
                    self.randomizer.canRandomize = False
        else:
            self.randomize_button.config(state = "disabled")
            self.unrandomize_button.config(state = "disabled")

        # Update the title of the window depending on the game version used.
        if (self.randomizer.exeStatus == "Remaster"):
            self.root.title("Dark Souls - Enemy randomizer " + self.randomizerVersion + " by rycheNhavalys    [Current Mode: REMASTERED]")
        else:
            self.root.title("Dark Souls - Enemy randomizer " + self.randomizerVersion + " by rycheNhavalys    [Current Mode: PTDE]")

        self.isSelloutActive = False
            
        self.UpdateMessageArea()

    def OpenSelloutPage(self):
        """
        Open the sellout page and show the back button, hide $ button.
        """

        self.isSelloutActive = True
        self.sellout_close_button.grid()
        self.sellout_button.grid_remove()
        self.UpdateMessageArea()

    def CloseSelloutPage(self):
        """
        Hide the sellout page and the back button, show $ button.
        """

        self.isSelloutActive = False
        self.sellout_close_button.grid_remove()
        self.sellout_button.grid()
        self.UpdateMessageArea()

    def UpdateMessageArea(self):
        """
        Handles the message area contents.
        """

        if (self.isSelloutActive):
            # Sellout section
            self.msg_area.config(state = "normal")
            self.msg_area.delete(1.0, END)
            self.msg_area.insert(END,  "\n\n If you wish to support the mod author via a donation, you can do so here:\n\n ", "sellout_text")
            self.msg_area.insert(END,  "https://streamlabs.com/rychenhavalys", "sellout_link")
            self.msg_area.insert(END,  "\n\n Donations are not required, but they are appreciated.\n\n\n\n", "sellout_text")
            self.msg_area.insert(END,  " \n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n \"That wasn't necessary of you, but you have my thanks\" - Eileen the Crow\n\n", "uf")
            self.msg_area.config(state = "disabled")
        else:
            # Normal message area
            if (self.randomizer.canRandomize):
                # If there are no errors to display show the descriptions of selected settings and toggle the enabled state of certain options depending on whether or not they have an effect or not.

                if (self.bossReplaceMode.get() == 3):   # If boss mode is 'With normals and bosses' then enable the boss chance[bosses] slider, otherwise disable it.
                    self.boss_chance_slider_bosses.config(state = 'normal', fg='gray5')
                    self.boss_chance_frame_bosses.config(fg = 'gray5', text="Boss chance [Bosses](%):")
                else:
                    self.boss_chance_slider_bosses.config(state = 'disabled', fg='gray50')
                    self.boss_chance_frame_bosses.config(fg = 'gray50', text="[No effect] Boss chance [Bosses](%):")
                
                if (self.enemyReplaceMode.get() == 3 or self.npcMode.get() == 3):   # If normal enemy or npc mode is 'With normals and bosses' then enable the boss chance[normal enemies] slider, otherwise disable it.
                    self.boss_chance_slider.config(state = 'normal', fg='gray5')
                    self.boss_chance_frame.config(fg = 'gray5', text="Boss chance [Normal Enemies](%):")
                else:
                    self.boss_chance_slider.config(state = 'disabled', fg='gray50')
                    self.boss_chance_frame.config(fg = 'gray50', text="[No effect] Boss chance [Normal Enemies](%):")

                if (self.difficultyMode.get() == 1):    # If difficulty setting is 'Random with difficuly curve' then enable the difficulty strictness option, otherwise disable it.
                    self.strictBtn1.config(state = 'normal')
                    self.strictBtn2.config(state = 'normal')
                    self.strictBtn3.config(state = 'normal')
                    self.diff_strict_frame.config(fg = 'gray5', text="Difficulty strictness:")
                else:
                    self.strictBtn1.config(state = 'disabled')
                    self.strictBtn2.config(state = 'disabled')
                    self.strictBtn3.config(state = 'disabled')
                    self.diff_strict_frame.config(fg = 'gray50', text="[No Effect] Difficulty strictness:")


                for i in range(0, 14):
                    if (self.hoverO == -1):
                        self.tags[i] = "f"
                    else:
                        if (i == self.hoverO):
                            if (i == 0):
                                if (self.bossReplaceMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 1):
                                if (self.enemyReplaceMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 2):
                                if (self.fitMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 3):
                                if (self.difficultyMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 4):
                                if (self.mimicMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 5):
                                if (self.npcMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 6):
                                self.tags[i] = "f"
                            elif (i == 7):
                                if (self.gargoyleMode.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 8):
                                if (self.diffStrictness.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 9):
                                if (self.tposeCity.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 10):
                                if (self.typeReplacement.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 11):
                                if (self.pinwheelChaos.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 12):
                                if (self.gwynNerf.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                            elif (i == 13):
                                if (self.preventSame.get() == self.hoverL):
                                    self.tags[i] = "f"
                                else:
                                    self.tags[i] = "c"
                        else:
                            self.tags[i] = "uf"

                self.msg_area.config(state = "normal")

                self.msg_area.delete(1.0, END)

                self.msg_area.insert(END,  "Only the descriptions for the settings on the currently selected tab are displayed here, to see other descriptions, select the other settings tab." + "\n\n", "uf")

                currentPage = self.settingsTabs.index(self.settingsTabs.select())
                if (currentPage == 0):
                    self.AddDescription(0, self.bossReplaceMode.get())
                    self.AddDescription(1, self.enemyReplaceMode.get())
                    self.AddDescription(5, self.npcMode.get())
                    self.AddDescription(3, self.difficultyMode.get())
                    self.AddDescription(8, self.diffStrictness.get())
                    self.AddDescription(2, self.fitMode.get())
                elif (currentPage == 1):
                    self.AddDescription(4, self.mimicMode.get())
                    self.AddDescription(7, self.gargoyleMode.get())
                    self.AddDescription(9, self.tposeCity.get())
                    self.AddDescription(10, self.typeReplacement.get())
                    self.AddDescription(11, self.pinwheelChaos.get())
                    self.AddDescription(12, self.gwynNerf.get())
                    self.AddDescription(13, self.preventSame.get())


                self.msg_area.config(state = "disabled")
            
            else:   # Display errors:
                self.msg_area.config(state = "normal")
                self.msg_area.delete(1.0, END)
                self.msg_area.insert(END,  "The randomizer encountered following errors that must be resolved before you can use the randomizer (restart randomizer when the problems are resolved): " + "\n\n", "major_error")

                exeString = ""

                if (self.randomizer.exeStatus == "Not Unpacked"):
                    exeString = "* The exe has not been modified by UDSFM, are you sure you unpacked the game properly?\n\n"
                elif (self.randomizer.exeStatus == "Not Unpacked Debug"):
                    exeString = "* The debug exe has not been modified by UDSFM, are you sure you unpacked the game properly?\n\n"
                elif (self.randomizer.exeStatus == "Not Found"):
                    exeString = "* DARKSOULS.exe/DarkSoulsRemastered.exe not found, are you sure you placed the randomizer in the\n  correct directory?\n\n"

                self.msg_area.insert(END, exeString, "f")

                gameFileString = ""

                if (self.randomizer.missingMSB == len(self.randomizer.inputFiles)):
                    gameFileString = "* No files found in DATA/map/MapStudio, are you sure the game is properly unpacked (PTDE only) and the randomizer is placed in the correct directory?\n\n"
                elif (self.randomizer.missingMSB != 0):
                    gameFileString = "* " + str(self.randomizer.missingMSB) + "/" + str(len(self.randomizer.inputFiles)) + " .msb files missing from DATA/map/MapStudio\n\n"
                
                self.msg_area.insert(END, gameFileString, "f")

                gameFileString = ""

                if (self.randomizer.missingLUABND == len(self.randomizer.inputFiles) - 1):
                    gameFileString = "* No required files found in DATA/script/, are you sure the game is properly unpacked (PTDE only) and the randomizer is placed in the correct directory?\n\n"
                elif (self.randomizer.missingLUABND != 0):
                    if (self.randomizer.useDCX):
                        gameFileString = "* " + str(self.randomizer.missingLUABND) + "/" + str(len(self.randomizer.inputFiles) - 1) + " .luabnd.dcx files missing from DATA/script/\n\n"
                    else:
                        gameFileString = "* " + str(self.randomizer.missingLUABND) + "/" + str(len(self.randomizer.inputFiles) - 1) + " .luabnd files missing from DATA/script/\n\n"
                
                self.msg_area.insert(END, gameFileString, "f")

                gameFileString = ""

                if (self.randomizer.missingFFXBND == len(self.randomizer.inputFiles) - 1):
                    gameFileString = "* No required files found in DATA/sfx/, are you sure the game is properly unpacked (PTDE only) and the randomizer is placed in the correct directory?\n\n"
                elif (self.randomizer.missingFFXBND != 0):
                    if (self.randomizer.useDCX):
                        gameFileString = "* " + str(self.randomizer.missingFFXBND) + "/" + str(len(self.randomizer.inputFFXFiles)) + " .ffxbnd.dcx files missing from DATA/sfx/\n\n"
                    else:
                        gameFileString = "* " + str(self.randomizer.missingFFXBND) + "/" + str(len(self.randomizer.inputFFXFiles)) + " .ffxbnd files missing from DATA/sfx/\n\n"
                
                self.msg_area.insert(END, gameFileString, "f")

                if (self.randomizer.folderStatus == False):
                    self.msg_area.insert(END, "* enemyRandomizerData folder not found, are you sure you unpacked the .zip file properly?\n\n", "f")
                else:
                    if (self.randomizer.aiRefStatus == False):
                        self.msg_area.insert(END, "* enemyRandomizerData/airef.csv not found.\n\n", "f")

                    if (self.randomizer.ffxRefStatus == False):
                        self.msg_area.insert(END, "* enemyRandomizerData/ffx-ref.txt not found.\n\n", "f")
                    
                    if (self.randomizer.validNewStatus == False):
                        self.msg_area.insert(END, "* enemyRandomizerData/replacement_ref/valid_new.txt not found.\n\n", "f")

                    if (self.randomizer.validReplaceStatus == False):
                        self.msg_area.insert(END, "* enemyRandomizerData/replacement_ref/valid_replacements.txt not found.\n\n", "f")

                    if (self.randomizer.originalRefMissing == len(self.randomizer.inputFiles)):
                        self.msg_area.insert(END, "* reference files in enemyRandomizerData/original_enemies_ref/ not found.\n\n", "f")
                    elif (self.randomizer.originalRefMissing != 0):
                        self.msg_area.insert(END, "* " + str(self.randomizer.originalRefMissing) + "/" + str(len(self.randomizer.inputFiles)) + " reference files in enemyRandomizerData/original_enemies_ref/ not found.\n\n", "f")

                if (not self.randomizer.writingPermssion):
                    self.msg_area.insert(END, "* Randomizer doesn't seem to have writing permissions here. You might need to\n  either install the game in a different location or run the randomizer as\n  administrator.\n\n", "f")

                if (self.randomizer.missingAiCopies > 0):
                    if (self.randomizer.useDCX):
                        self.msg_area.insert(END, "* " + str(self.randomizer.missingAiCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .luabnd.dcx copies are missing from enemyRandomizerData/mapAiCopies.\n\n", "f")
                    else:
                        self.msg_area.insert(END, "* " + str(self.randomizer.missingAiCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .luabnd copies are missing from enemyRandomizerData/mapAiCopies.\n\n", "f")

                if (self.randomizer.invalidAiCopies > 0):
                    if (self.randomizer.useDCX):
                        self.msg_area.insert(END, "* " + str(self.randomizer.invalidAiCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .luabnd.dcx copies are invalid in enemyRandomizerData/mapAiCopies.\n\n", "f")
                    else:
                        self.msg_area.insert(END, "* " + str(self.randomizer.invalidAiCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .luabnd copies are invalid in enemyRandomizerData/mapAiCopies.\n\n", "f")

                if (self.randomizer.missingMapCopies > 0):
                    self.msg_area.insert(END, "* " + str(self.randomizer.missingMapCopies) + "/" + str(len(self.randomizer.inputFiles)) + " of .msb copies are missing from enemyRandomizerData/mapAiCopies.\n\n", "f")

                if (self.randomizer.invalidMapCopies > 0):
                    self.msg_area.insert(END, "* " + str(self.randomizer.invalidMapCopies) + "/" + str(len(self.randomizer.inputFiles)) + " of .msb copies are invalid in enemyRandomizerData/mapStudioCopies.\n\n", "f")

                if (self.randomizer.missingSfxCopies > 0):
                    self.msg_area.insert(END, "* " + str(self.randomizer.missingSfxCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .ffxbnd copies are missing from enemyRandomizerData/sfxCopies.\n\n", "f")

                if (self.randomizer.invalidSfxCopies > 0):
                    self.msg_area.insert(END, "* " + str(self.randomizer.invalidSfxCopies) + "/" + str(len(self.randomizer.inputFiles) - 1) + " of .ffxbnd copies are invalid in enemyRandomizerData/sfxCopies.\n\n", "f")

                self.msg_area.config(state = "disabled")

    def UpdateTags(self, event, arg1, arg2):
        self.hoverO = arg1
        self.hoverL = arg2
        self.UpdateMessageArea()

    def BindTags(self, btn, oIndex, lIndex):
        """
        Change the description box entry tags as options are hovered.
        """

        btn.bind("<Enter>", lambda _: self.UpdateTags(_,oIndex,lIndex))
        btn.bind("<Leave>", lambda _: self.UpdateTags(_,-1,-1))

    def SettingsPageChanged(self, e):
        self.UpdateMessageArea()

    def AddDescription(self, messageIndex, variableVal):
        if (self.hoverO == messageIndex):
            self.msg_area.insert(END,  self.messages[messageIndex][self.hoverL] + "\n\n", self.tags[messageIndex])
        else:
            self.msg_area.insert(END,  self.messages[messageIndex][variableVal] + "\n\n", self.tags[messageIndex])

    def RandomizeEnemies(self):
        """
        Open the progress bar window and execute the randomization on a separate thread
        """

        currentTime = datetime.datetime.now()
        timeString = f"{currentTime:%Y-%m-%d-%H-%M-%S}"

        self.progressTopLevel = Toplevel(self.root)
        self.progressTopLevel.title("Randomizing, please wait")

        progLen = 3 + len(self.randomizer.inputFiles) * 3

        self.progressBar = Progressbar(self.progressTopLevel, maximum=progLen, length=512)
        self.progressBar.grid(row = 0, column = 0, sticky="NEWS", padx=8, pady=4)

        self.progressLabel = Label(self.progressTopLevel, text="Checking and preparing effect files [can take a while]")
        self.progressLabel.grid(row = 1, column = 0, sticky = "NEWS")

        self.randomize_button.config(state = "disabled")
        self.unrandomize_button.config(state = "disabled")

        self.BuildConfigString()
        
        randomSettings = (self.progressBar, self.progressLabel, self.bossReplaceMode.get(), self.enemyReplaceMode.get(), self.npcMode.get(), self.mimicMode.get(), self.fitMode.get(), self.difficultyMode.get(), self.replace_chance_slider.get(), self.boss_chance_slider.get(), self.boss_chance_slider_bosses.get(), self.gargoyleMode.get(), self.diffStrictness.get(), self.tposeCity.get(), self.boss_souls_slider.get(), self.pinwheelChaos.get(), self.typeReplacement.get(), self.gwynNerf.get(), self.preventSame.get(), self.seedValue.get(), self.configString, self.enemyConfigForRandomization.get())

        self.randThread = randomizationThread(1, "Random-Thread", 1, self.randomizer, randomSettings, self.msg_area, self, timeString)
        self.randThread.start()

    def OpenTextConfigTopLevel(self):
        """
        Open the text config toplevel if it's not already open.
        """

        if (self.textConfigTopLevel == None):
            self.textConfigTopLevel = Toplevel(self.root)
            self.textConfigTopLevel.title("Text config")

            self.copyBTN = Button(self.textConfigTopLevel, text="Generate current settings as text:", command=self.BuildConfigString)
            self.copyBTN.grid(row=1, column=1, sticky='NEWS', padx=4, pady=4)

            self.configEntry = Entry(self.textConfigTopLevel, textvariable=self.configValue)
            self.configEntry.grid(row = 1, column = 2, columnspan=2, sticky="NEWS", padx=8, pady=8)

            self.configApplyBTN = Button(self.textConfigTopLevel, text="Apply settings from text", command=self.ApplyConfigString)
            self.configApplyBTN.grid(row=1, column=4, sticky='NEWS', padx=4, pady=4)

            self.configLabel = Label(self.textConfigTopLevel, text="* This field does NOT update automatically, press the 'Generate' button to update.")
            self.configLabel.grid(row = 2, column = 3, sticky="W", padx=2)

            self.configDexcriptionLabel = Label(self.textConfigTopLevel, text="This window allows you to generate a text representation of the currently selected settings and seed.\nAfter pressing 'Generate current settings as text' the text version of current settings will be put into the text field.\nYou can also input a config into the text field and press 'Apply settings from text' to apply the settings from the text to the randomizer.\n\nThis is useful if you want to for example get the same enemy configuration with a friend.\nOne of you can configure the randomizer to desired settings, then generate the text version of the setup and send that to the other person,\nwho can then paste it here and apply the settings+seed from that.")
            self.configDexcriptionLabel.grid(row = 0, column = 3, sticky="W", padx=2)
        else:
            try:
                self.textConfigTopLevel.state()
            except:
                self.textConfigTopLevel = None
                self.OpenTextConfigTopLevel()

    def BuildCustomEnemyConfigList(self):
        """
        Populates the customEnemyConfigs list with available configs.
        """

        self.customEnemyConfigs.clear()
        self.customEnemyConfigs.append("Default")
        if (os.path.isdir('enemyRandomizerData/customConfigs/')):
            for fName in os.listdir('enemyRandomizerData/customConfigs/'):
                if (".txt" in fName):
                    self.customEnemyConfigs.append(fName.replace(".txt", ""))

        else:
            os.makedirs('enemyRandomizerData/customConfigs')

    def TryOpenEnemyConfigTopLevel(self):
        """
        Open the toplevel for enemy config editing if it's not already open.
        """

        if (self.enemyConfigTopLevel == None):
            self.OpenEnemyConfigTopLevel()
        else:
            try:
                self.enemyConfigTopLevel.state()
            except:
                self.enemyConfigTopLevel = None
                self.TryOpenEnemyConfigTopLevel()

    def OpenEnemyConfigTopLevel(self):
        """
        Create the enemy config editing toplevel and populate the list.
        """

        self.enemyConfigTopLevel = Toplevel(self.root)
        self.enemyConfigTopLevel.title("Enemy config editor")
        self.enemyConfigTopLevel.wm_geometry("1000x512")

        self.enemyConfigControlFrame = Frame(self.enemyConfigTopLevel)
        self.enemyConfigControlFrame.grid(row=0, column=0, sticky="NEWS")

        self.enemyConfigUnsavedChanges = False

        self.enemyConfigSaveButton = Button(self.enemyConfigControlFrame, text="Save", state="disabled", command=self.EnemyConfigSave)
        self.enemyConfigSaveButton.grid(row=0, column=2, padx=4)

        self.enemyConfigSaveAsButton = Button(self.enemyConfigControlFrame, text="Save As", state="disabled", command=self.EnemyConfigSaveAs)
        self.enemyConfigSaveAsButton.grid(row=0, column=3, padx=4)

        self.enemyConfigSaveAsNameInputLabel = Label(self.enemyConfigControlFrame, text="Name the config: ")
        self.enemyConfigSaveAsNameInputLabel.grid(row=0, column=0, sticky="NSW")
        self.enemyConfigSaveAsNameInputLabel.grid_remove()

        self.enemyConfigSaveAsNameInput = Entry(self.enemyConfigControlFrame, width=20)
        self.enemyConfigSaveAsNameInput.grid(row=0, column=1, padx=4)
        self.enemyConfigSaveAsNameInput.grid_remove()

        self.enemyConfigSaveAsNameInput.bind('<KeyRelease>', self.EnemyConfigSaveAsInputUpdate)

        self.enemyConfigSaveAsSaveButton = Button(self.enemyConfigControlFrame, text="Save", state="disabled", command=self.EnemyConfigSaveAsStart)
        self.enemyConfigSaveAsSaveButton.grid(row=0, column=2, padx=4)
        self.enemyConfigSaveAsSaveButton.grid_remove()

        self.enemyConfigSaveAsCancelButton = Button(self.enemyConfigControlFrame, text="Cancel", state="normal", command=self.EnemyConfigCancel)
        self.enemyConfigSaveAsCancelButton.grid(row=0, column=3, padx=4, pady=2)
        self.enemyConfigSaveAsCancelButton.grid_remove()

        self.enemyConfigOpenedName = StringVar()

        self.BuildCustomEnemyConfigList()

        self.enemyConfigOpenedName.set(self.customEnemyConfigs[0])
        self.enemyConfigPrevName = self.customEnemyConfigs[0]

        self.enemyConfigSelect = OptionMenu(self.enemyConfigControlFrame, self.enemyConfigOpenedName, *self.customEnemyConfigs, command=self.EnemyConfigOpenedNameChange)
        self.enemyConfigSelect.grid(row=0, column=1, padx=4)

        self.enemyConfigSelectLabel = Label(self.enemyConfigControlFrame, text="Current config: ")
        self.enemyConfigSelectLabel.grid(row=0, column=0, sticky="NSW")

        self.enemyConfigFrame = Frame(self.enemyConfigTopLevel)
        self.enemyConfigFrame.grid(row=1, column=0, sticky="NEWS")

        self.enemyConfigTopLevel.columnconfigure(0, weight=1)
        self.enemyConfigTopLevel.rowconfigure(1, weight=1)

        self.enemyconfig_scroll = ScrollableFrame(self.enemyConfigFrame)
        self.enemyconfig_scroll.inner.columnconfigure(3, weight=1)

        
        self.enemyConfigSaveAsNameInputLabel = Label(self.enemyConfigControlFrame, text="                              ")
        self.enemyConfigSaveAsNameInputLabel.grid(row=0, column=4, sticky="NSW")

        self.enemyConfigDisableAllButton = Button(self.enemyConfigControlFrame, text="Disable All", state="normal", command=self.EnemyConfigDisableAll)
        self.enemyConfigDisableAllButton.grid(row=0, column=5, padx=4, pady=2)

        self.enemyConfigEnableAllButton = Button(self.enemyConfigControlFrame, text="Enable All", state="normal", command=self.EnemyConfigEnableAll)
        self.enemyConfigEnableAllButton.grid(row=0, column=6, padx=4, pady=2)

        l = Label(self.enemyconfig_scroll.inner, text="ID", fg="DeepPink3")
        l.config()
        l.grid(row=0, column=0, sticky="W")

        l = Label(self.enemyconfig_scroll.inner, text="Enemy", fg="DeepPink3")
        l.grid(row=0, column=1, sticky="W")

        l = Label(self.enemyconfig_scroll.inner, text="Is Enabled?", fg="DeepPink3")
        l.grid(row=0, column=2, sticky="W")

        l = Label(self.enemyconfig_scroll.inner, text="--Comment", fg="DeepPink3")
        l.grid(row=0, column=3, sticky="W")

        i = 0

        self.enabledVars = []

        with open('enemyRandomizerData/replacement_ref/valid_new.txt') as f:
            for line in f:
                if (i > 0):
                    parts = line.strip().split('\t')

                    bgCol = "gray90"
                    if (i % 2 == 0):
                        bgCol = "gray85"

                    titlelabel = Label(self.enemyconfig_scroll.inner, text=" ", bg = bgCol)
                    titlelabel.grid(row=i, column=0, sticky='NSEW', columnspan=5)

                    l = Label(self.enemyconfig_scroll.inner, text=parts[0], fg="gray40", bg = bgCol)
                    l.grid(row=i, column=0, sticky="NSW")

                    l = Label(self.enemyconfig_scroll.inner, text=parts[1], bg = bgCol)
                    l.grid(row=i, column=1, sticky="NSW")

                    enabledVar = IntVar()

                    if (parts[3] == "1"):
                        enabledVar.set(0)
                    elif (parts[3] == "2"):     # artorias special value
                        if (self.randomizer.exeStatus == "Remaster"):
                            enabledVar.set(1)
                        else:
                            enabledVar.set(0)
                    else:
                        enabledVar.set(1)
                    
                    enemyToggle = Checkbutton(self.enemyconfig_scroll.inner, text="", variable=enabledVar, bg = bgCol)
                    enemyToggle.grid(row=i, column=2, sticky=W)
                    enemyToggle.bind("<ButtonRelease-1>", self.UpdateEnemyConfigSaveStatus)

                    self.enabledVars.append(enabledVar)

                    if (len(parts) == 11):
                        l = Label(self.enemyconfig_scroll.inner, text=parts[10], fg="DeepPink1", bg = bgCol)
                        l.grid(row=i, column=3, sticky="W")

                i += 1

        self.enemyconfig_scroll.UpdateCanvasSize()

    def EnemyConfigOpenedNameChange(self, e):
        """
        Check if the user has selected a different config from the dropdown menu.
        """

        if (self.enemyConfigPrevName != self.enemyConfigOpenedName.get()):
            changeConfig = False
            if (self.enemyConfigUnsavedChanges):
                changeConfig = tkinter.messagebox.askyesno("You have unsaved changes.", "You have unsaved changes, do you really wish to discard your changes and open a different enemy config?")
            else:
                changeConfig = True
            
            if (changeConfig):
                self.enemyConfigUnsavedChanges = False
                self.enemyConfigSaveButton.config(state="disabled")
                self.enemyConfigSaveAsButton.config(state="disabled")

                self.EnemyConfigOpen()

                self.enemyConfigPrevName = self.enemyConfigOpenedName.get()
            else:
                self.enemyConfigOpenedName.set(self.enemyConfigPrevName)

    def EnemyConfigOpen(self):
        """
        Open a config for editing.
        """

        fileName = self.enemyConfigOpenedName.get()
        filePath = ""
        if (fileName == "Default"):
            filePath = 'enemyRandomizerData/replacement_ref/valid_new.txt'
        else:
            filePath = 'enemyRandomizerData/customConfigs/' + fileName + '.txt'

        i = 0

        with open(filePath) as f:
            for line in f:
                if (i > 0):
                    parts = line.strip().split('\t')

                    enabledValue = 1

                    if (parts[3] == "1"):
                        enabledValue = 0

                    self.enabledVars[i - 1].set(enabledValue)

                i += 1

    def EnemyConfigEnableAll(self):
        """
        Enable all* enemies in the list
        """

        for i in range(len(self.enabledVars)):
            if (i != 106 and i != 107):
                self.enabledVars[i].set(1)

        self.UpdateEnemyConfigSaveStatus(None)

    def EnemyConfigDisableAll(self):
        """
        Disable all enemies in the list
        """

        for i in range(len(self.enabledVars)):
            self.enabledVars[i].set(0)

        self.UpdateEnemyConfigSaveStatus(None)

    def UpdateEnemyConfigSaveStatus(self, e):
        """
        If any value is changed, set the unsaved changes flag to true and enable save button(s).
        """

        self.enemyConfigUnsavedChanges = True
        if not (self.enemyConfigOpenedName.get() == "Default"):
            self.enemyConfigSaveButton.config(state='normal')
        self.enemyConfigSaveAsButton.config(state='normal')

    def EnemyConfigSave(self):
        """
        Don't ask why this method exists...
        """

        self.EnemyConfigSaveStart()

    def EnemyConfigSaveAs(self):
        """
        Save As button has been pressed, show config name input.
        """

        self.enemyConfigSaveButton.grid_remove()
        self.enemyConfigSaveAsButton.grid_remove()
        self.enemyConfigSelect.grid_remove()
        self.enemyConfigSelectLabel.grid_remove()

        self.enemyConfigSaveAsNameInput.delete(0, END)
        self.enemyConfigSaveAsSaveButton.config(state="disabled")

        self.enemyConfigSaveAsNameInputLabel.grid()
        self.enemyConfigSaveAsNameInput.grid()
        self.enemyConfigSaveAsSaveButton.grid()
        self.enemyConfigSaveAsCancelButton.grid()

    def EnemyConfigCancel(self):
        """
        Save As has been concluded, hide the name input and show the original controls.
        """

        self.enemyConfigSaveButton.grid()
        self.enemyConfigSaveAsButton.grid()
        self.enemyConfigSelect.grid()
        self.enemyConfigSelectLabel.grid()

        self.enemyConfigSaveAsNameInputLabel.grid_remove()
        self.enemyConfigSaveAsNameInput.grid_remove()
        self.enemyConfigSaveAsSaveButton.grid_remove()
        self.enemyConfigSaveAsCancelButton.grid_remove()

    def EnemyConfigSaveAsInputUpdate(self, e):
        """
        Only allow the save button be pressed when a non-empty name has been given.
        """

        if (self.enemyConfigSaveAsNameInput.get() == ""):
            self.enemyConfigSaveAsSaveButton.config(state="disabled")
        else:
            self.enemyConfigSaveAsSaveButton.config(state="normal")

    def UpdateEnemyConfigListAfterSave(self, newName):
        """
        Update the dropdown menus to contain all available configs.
        """

        oldSelectedConfig = self.enemyConfigForRandomization.get()

        self.BuildCustomEnemyConfigList()
        self.enemyConfigOpenedName.set(newName)
        self.enemyConfigPrevName = newName
        self.enemyConfigSelect.destroy()
        self.enemyConfigSelect = OptionMenu(self.enemyConfigControlFrame, self.enemyConfigOpenedName, *self.customEnemyConfigs, command=self.EnemyConfigOpenedNameChange)
        self.enemyConfigSelect.grid(row=0, column=1, padx=4)

        self.enemyConfigForRandomization.set(oldSelectedConfig)

        self.enemyConfigSelectForRandomization.destroy()
        self.enemyConfigSelectForRandomization = OptionMenu(self.enemy_config_frame, self.enemyConfigForRandomization, *self.customEnemyConfigs)
        self.enemyConfigSelectForRandomization.grid(row=1, column=0, padx=2, sticky='NWSE')

    def EnemyConfigSaveAsStart(self):
        """
        Save the config as a new file.
        """

        fileName = self.enemyConfigSaveAsNameInput.get()
        canSave = False

        if (os.path.isfile('enemyRandomizerData/customConfigs/' + fileName + '.txt')):
            canSave = tkinter.messagebox.askyesno('Config already exists.', 'A custom enemy config named "' + fileName + '" already exists.\nDo you wish to overwrite that config?')
        else:
            canSave = True

        if (canSave):
            if (self.EnemyConfigSaveConfig(fileName)):
                self.EnemyConfigCancel()
                self.enemyConfigUnsavedChanges = False
                self.enemyConfigSaveButton.config(state="disabled")
                self.enemyConfigSaveAsButton.config(state="disabled")
                self.UpdateEnemyConfigListAfterSave(fileName)
                tkinter.messagebox.showinfo("Config saved.", "Custom enemy config successfully saved as\n'enemyRandomizerData/customConfigs/"  + fileName + ".txt'")
            else:
                tkinter.messagebox.showerror("Failed to save config.", "Failed to save the custom config.\nPlease check if you've entered a valid filename.")

    def EnemyConfigSaveStart(self):
        """
        Save the config, overwriting the current config.
        """

        fileName = self.enemyConfigOpenedName.get()
        if (self.EnemyConfigSaveConfig(fileName)):
            self.EnemyConfigCancel()
            self.enemyConfigUnsavedChanges = False
            self.enemyConfigSaveButton.config(state="disabled")
            self.enemyConfigSaveAsButton.config(state="disabled")
            self.UpdateEnemyConfigListAfterSave(fileName)
            tkinter.messagebox.showinfo("Config saved.", "Custom enemy config\n'enemyRandomizerData/customConfigs/"  + fileName + ".txt'\nsuccessfully saved.")
        else:
            tkinter.messagebox.showerror("Failed to save config.", "Something went wrong when saving the custom config :/")
        
    def EnemyConfigSaveConfig(self, fileName):
        """
        Try saving the config with the name @filename.
        @return: Success status (bool)
        """
        
        try:
            lineIdx = 0
            firstLine = True
            with open('enemyRandomizerData/customConfigs/' + fileName + '.txt', 'w') as newf:
                with open('enemyRandomizerData/replacement_ref/valid_new.txt') as oldf:
                    for line in oldf:
                        if (firstLine):
                            newf.write(line.strip())
                            firstLine = False
                        else:
                            parts = line.strip().split('\t')
                            newLine = '\n' + parts[0] + '\t' + parts[1] + '\t' + parts[2] + '\t'
                            if (self.enabledVars[lineIdx].get() == 1):
                                newLine += "0"
                            else:
                                newLine += "1"
                            
                            newLine += '\t' + parts[4] + '\t' + parts[5] + '\t' + parts[6] + '\t' + parts[7] + '\t' + parts[8] + '\t' + parts[9] + '\t'

                            if (len(parts) == 11):
                                newLine += parts[10]

                            newf.write(newLine)

                            lineIdx += 1
            return True


        except:
            return False

    """ The next two methods are going to be a P A I N to keep up to date... """

    def BuildConfigString(self):
        """
        Build the compact config string.
        """

        self.configString = str(self.bossReplaceMode.get()) + "/-/" + str(self.enemyReplaceMode.get()) + "/-/" + str(self.npcMode.get()) + "/-/" + str(self.mimicMode.get()) + "/-/" + str(self.fitMode.get()) + "/-/" + str(self.difficultyMode.get()) + "/-/" + str(self.replace_chance_slider.get()) + "/-/" + str(self.boss_chance_slider.get()) + "/-/"  + str(self.boss_chance_slider_bosses.get()) + "/-/" + str(self.gargoyleMode.get()) + "/-/" + str(self.diffStrictness.get()) + "/-/" + str(self.tposeCity.get()) + "/-/" + str(self.boss_souls_slider.get()) + "/-/" + str(self.pinwheelChaos.get()) + "/-/" + str(self.typeReplacement.get()) + "/-/" + str(self.gwynNerf.get()) + "/-/" + str(self.preventSame.get()) + "/-/'''" + self.seedValue.get() + "'''"
        self.configValue.set(self.configString)

    def ApplyConfigString(self):
        """
        Apply the settings from the config string, if the settings are valid, otherwise complain.
        """

        confStr = self.configValue.get()
        parts = confStr.split("/-/")
        isValidConfig = True
        inpBossMode = 0
        inpNormMode = 0
        inpNPCMode = 0
        inpMimicMode = 0
        inpFitMode = 0
        inpDiffMode = 0
        inpRepChance = 0
        inpBossChanceN = 0
        inpBossChanceB = 0
        inpGargMode = 0
        inpDiffStrict = 0
        inpTpose = 0
        inpSoulDrop = 0
        inpPinwheel = 0
        inpTypeReplacement = 0
        inpGwynRate = 0
        inpPreventSame = 0
        inpSeed = ""
        if (len(parts) == 18):
            try:
                inpBossMode = int(parts[0])
                if (inpBossMode < 0 or inpBossMode > 3):
                    isValidConfig = False

                inpNormMode = int(parts[1])
                if (inpNormMode < 0 or inpNormMode > 3):
                    isValidConfig = False

                inpNPCMode = int(parts[2])
                if (inpNPCMode < 0 or inpNormMode > 3):
                    isValidConfig = False

                inpMimicMode = int(parts[3])
                if (inpMimicMode < 0 or inpMimicMode > 1):
                    isValidConfig = False
                
                inpFitMode = int(parts[4])
                if (inpFitMode < 0 or inpFitMode > 2):
                    isValidConfig = False
                
                inpDiffMode = int(parts[5])
                if (inpDiffMode < 1 or inpDiffMode > 3):
                    isValidConfig = False

                inpRepChance = int(parts[6])
                if (inpRepChance < 0 or inpRepChance > 100):
                    isValidConfig = False
                
                inpBossChanceN = int(parts[7])
                if (inpBossChanceN < 0 or inpBossChanceN > 100):
                    isValidConfig = False
                
                inpBossChanceB = int(parts[8])
                if (inpBossChanceB < 0 or inpBossChanceB > 100):
                    isValidConfig = False
                
                inpGargMode = int(parts[9])
                if (inpGargMode < 0 or inpGargMode > 1):
                    isValidConfig = False
                
                inpDiffStrict = int(parts[10])
                if (inpDiffStrict < 0 or inpDiffStrict > 100):
                    isValidConfig = False
                
                inpTpose = int(parts[11])
                if (inpTpose < 0 or inpTpose > 1):
                    isValidConfig = False
                
                inpSoulDrop = int(parts[12])
                if (inpSoulDrop < 0 or inpSoulDrop > 100):
                    isValidConfig = False
                
                inpPinwheel = int(parts[13])
                if (inpPinwheel < 0 or inpPinwheel > 1):
                    isValidConfig = False
                
                inpTypeReplacement = int(parts[14])
                if (inpTypeReplacement < 0 or inpTypeReplacement > 1):
                    isValidConfig = False
                
                inpGwynRate = int(parts[15])
                if (inpGwynRate < 0 or inpGwynRate > 2):
                    isValidConfig = False
                
                inpPreventSame = int(parts[16])
                if (inpPreventSame < 0 or inpPreventSame > 2):
                    isValidConfig = False
                
                inpSeed = parts[17].replace("'''", "")
            except:
                isValidConfig = False
        else:
            isValidConfig = False
        
        if (isValidConfig):
            # Set state of anything that can be disabled to normal

            self.strictBtn1.config(state = 'normal')
            self.strictBtn2.config(state = 'normal')
            self.strictBtn3.config(state = 'normal')

            self.boss_chance_slider_bosses.config(state = 'normal', fg='gray5')
            self.boss_chance_slider.config(state = 'normal', fg='gray5')

            # Apply values

            self.bossReplaceMode.set(inpBossMode)
            self.enemyReplaceMode.set(inpNormMode)
            self.npcMode.set(inpNPCMode)
            self.mimicMode.set(inpMimicMode)
            self.fitMode.set(inpFitMode)
            self.difficultyMode.set(inpDiffMode)
            self.replace_chance_slider.set(inpRepChance)
            self.boss_chance_slider.set(inpBossChanceN)
            self.boss_chance_slider_bosses.set(inpBossChanceB)
            self.gargoyleMode.set(inpGargMode)
            self.diffStrictness.set(inpDiffStrict)
            self.tposeCity.set(inpTpose)
            self.boss_souls_slider.set(inpSoulDrop)
            self.pinwheelChaos.set(inpPinwheel)
            self.typeReplacement.set(inpTypeReplacement)
            self.gwynNerf.set(inpGwynRate)
            self.preventSame.set(inpPreventSame)
            self.seedValue.set(inpSeed)
            self.UpdateMessageArea()
            tkinter.messagebox.showinfo("Config Applied", "The config has been applied successfully")
        else:
            tkinter.messagebox.showerror("Invalid Config", "The input text config is invalid, please try again.")
        
    def Unrandomize(self):
        """
        Ask the user whether or not to revert the effect files, then revert enemies to normal.
        """

        revertEffects = tkinter.messagebox.askyesno("Restore effect files", "Restore effect files?\nIf you plan to leave the enemies to normal, select YES\nIf you plan to immediately re-randomize, select NO")
        self.randomizer.revertToNormal(revertEffects)
        tkinter.messagebox.showinfo("De-Randomization complete", "Enemies reverted to normal")

        

mw = MainWindow()
mw.root.mainloop()