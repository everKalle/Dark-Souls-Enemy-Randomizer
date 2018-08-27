import hashlib
import mmap
import os.path
from byteread import *


def sha256_checksum(filename, block_size=65536):
    """
    https://gist.github.com/rji/b38c7238128edf53a181
    """
    sha256 = hashlib.sha256()
    with open(filename, 'rb') as f:
        for block in iter(lambda: f.read(block_size), b''):
            sha256.update(block)
    return sha256.hexdigest()

def check_exe_checksum():
    """
    Check the checksum of DARKSOULS.exe and return the version as a string.
    """

    CHECKSUM_UNPACKED = "903a946273bfe123fe5c85740c3613374e2cf538564bb661db371c6cb5a421ff"
    CHECKSUM_DEBUG_UNPACKED = "473de70f0dd03048ca5dea545508f6776206424494334a9da091fb27c8e5a23f"

    CHECKSUM_NOT_UNPACKED = "67bcab513c8f0ed6164279d85f302e06b1d8a53abff5df7f3d10e1d4dfd81459"
    CHECKSUM_DEBUG_NOT_UNPACKED = "b6958f3f0db5fdb7ce6f56bff14353d8d81da8bae3456795a39dbe217c1897cf"

    CHECKSUM_PATCHED = "5aaea26acfd72976812f00b0bf6dbdc82d78998e161d4e2c17d4a05684f24aad"
    CHECKSUM_PATCHED_DEBUG = "72d231a51635703f3224fdc1f155a148c02b40d2dc4854f27e2da4db64d63eea"

    if (os.path.isfile('DARKSOULS.exe')):
        csum = sha256_checksum('DARKSOULS.exe')
        print(csum)
        if (csum == CHECKSUM_UNPACKED):
            return "Unpacked"
        elif (csum == CHECKSUM_DEBUG_UNPACKED):
            return "Unpacked Debug"
        elif (csum == CHECKSUM_NOT_UNPACKED):
            return "Not Unpacked"
        elif (csum == CHECKSUM_DEBUG_NOT_UNPACKED):
            return "Not Unpacked Debug"
        elif (csum == CHECKSUM_PATCHED):
            return "Patched"
        elif (csum == CHECKSUM_PATCHED_DEBUG):
            return "Patched Debug"
        else:
            return "Unknown"
    else:
        if (os.path.isfile('DarkSoulsRemastered.exe')):
            return "Remaster"
        else:
            return "Not Found"

def patch_exe():
    """
    Modify the exe to allow the game to use more memory so that the game wouldn't crash immediately when all visual effects are loaded at once.
    Thanks to metal-crow for the fix.
    """
    status = check_exe_checksum()
    bakName = 'DARKSOULS.prerandomizer.exe'
    if (status == "Unpacked Debug"):
        bakName = 'DARKSOULS.prerandomizer.debug.exe'

    if not (os.path.isfile(bakName)):
        with open(bakName, 'wb') as bakf:
            with open('DARKSOULS.exe', 'rb') as oldf:
                bakf.write(oldf.read())

    with open('DARKSOULS.exe', mode="rb+") as f:
        fc = mmap.mmap(f.fileno(), 0)

        memAmt = b"h\x00\x00\xa2\x00"
        newMemAmt = b"h\x00\x00\xda\x00"

        count = 0

        next_pos = fc.find(memAmt)
        while next_pos != -1:
            fc.seek(next_pos)
            fc.write(newMemAmt)
            count += 1
            next_pos = fc.find(memAmt)
        
        print("Exe Patched")

        fc.flush()
        fc.close()

def restore_exe():
    """
    Restore the unmodified DARKSOULS.exe
    """
    status = check_exe_checksum()
    bakName = ""
    if (status == 'Patched'):
        if (os.path.isfile('DARKSOULS.prerandomizer.exe')):
            bakName = 'DARKSOULS.prerandomizer.exe'
        else:
            print("Bak file not found [DARKSOULS.prerandomizer.exe]")
    elif (status == 'Patched Debug'):
        if (os.path.isfile('DARKSOULS.prerandomizer.debug.exe')):
            bakName = 'DARKSOULS.prerandomizer.debug.exe'
        else:
            print("Bak file not found [DARKSOULS.prerandomizer.debug.exe]")
    
    if (bakName != ""):
        with open('DARKSOULS.exe', 'wb') as newf:
            with open(bakName, 'rb') as bakf:
                newf.write(bakf.read())

        print("Exe Reverted")