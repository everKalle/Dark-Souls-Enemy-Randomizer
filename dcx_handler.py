from byteread import *
import struct
import zlib
import os.path

class DCXHandler:
    """
    Class handling .dcx (de)compression
    """
    
    def __init__(self):
        self.req1 = 0
        self.req2 = 0
        self.req3 = 0
        self.req4 = 0
        self.header_length = 0
        self.uncompressed_size = 0
        self.compressed_size = 0
        self.unknownHeaderPart = b''
        self.compressed_header_length = 0
        self.twobytes = b''
        self.data = 0

    def open_file(self, filename):
        """
        Opens a .dcx compressed file @filename
        """
        mBytes = ''
        with open(filename, 'rb') as f:
            mBytes = f.read()
        self.open_dcx(mBytes)
        

    def open_dcx(self, mBytes):
        """
        Reads the header and decompresses data from @mBytes
        Returns the decompressed data.
        """
        curr_offset = 0

        # Read Header

        header1 = mBytes[0:4]
        if (header1 != b'DCX\x00'):
            raise ValueError('Expected "DCX " at 0 but received "' + str(header1) + '"')

        curr_offset += 0x4
        
        (self.req1,) = struct.unpack_from("<I", mBytes, offset=curr_offset)
        curr_offset += 0x4

        (self.req2, self.req3, self.req4) = struct.unpack_from(">III", mBytes, offset=curr_offset)
        curr_offset += 0xC

        if self.req1 != 0x100:
            raise ValueError("Expected DCX header int 0x100, but received " + hex(self.req1))
        if self.req2 != 0x18:
            raise ValueError("Expected DCX header int 0x18, but received " + hex(self.req2))
        if self.req3 != 0x24:
            raise ValueError("Expected DCX header int 0x24, but received " + hex(self.req3))
        if self.req4 != 0x24:
            raise ValueError("Expected DCX header int 0x24, but received " + hex(self.req4))
        
        (self.header_length,) = struct.unpack_from(">I", mBytes, offset=curr_offset)
        curr_offset += 0x4

        header2 = mBytes[curr_offset:curr_offset + 0x4]
        if (header2 != b'DCS\x00'):
            raise ValueError('Expected "DCS " at ' + str(curr_offset) + ' but received "' + str(header2) + '"')
        
        curr_offset += 0x4
        
        (self.uncompressed_size, self.compressed_size) = struct.unpack_from(">II", mBytes, offset=curr_offset)
        curr_offset += 0x8

        header3 = mBytes[curr_offset:curr_offset + 0x8]
        
        if (header3 != b'DCP\x00DFLT'):
            raise ValueError('Expected "DCP DFLT" at ' + str(curr_offset) + ' but received "' + str(header3) + '"')
        
        curr_offset += 0x8

        self.unknownHeaderPart = mBytes[curr_offset: curr_offset + 0x18]
        curr_offset += 0x18

        header4 = mBytes[curr_offset:curr_offset + 0x4]
        
        if (header4 != b'DCA\x00'):
            raise ValueError('Expected "DCA " at ' + str(curr_offset) + ' but received "' + str(header4) + '"')
        
        curr_offset += 0x4

        (self.compressed_header_length,) = struct.unpack_from(">I", mBytes, offset=curr_offset)
        curr_offset += 0x4
        
        self.twobytes = mBytes[curr_offset:curr_offset + 2]

        curr_offset += 0x2
        self.compressed_size -= 2
        
        # Decompress data
        decomp_obj = zlib.decompressobj(-15)
        a = mBytes[curr_offset:curr_offset + self.compressed_size]
        self.data = decomp_obj.decompress(a, self.uncompressed_size)

        return self.data

    def save_dcx(self, filename, newData, createBackup = True):
        """
        Compress @newData and save as @filename
        """
        # Create backup if it doesn't exist
        if (createBackup):
            if not os.path.isfile(filename + '.bak'):
                with open(filename, 'rb') as origf:
                    with open(filename + '.bak', 'wb') as bakf:
                        bakf.write(origf.read())

        # Recompress and save file
        with open(filename, 'wb') as f:
            compressed_data = zlib.compress(newData, 9)
            f.write(b'DCX\x00')
            f.write(struct.pack("<I", self.req1))
            f.write(struct.pack(">III", self.req2, self.req3, self.req4))
            f.write(struct.pack(">I", self.header_length))
            f.write(b'DCS\x00')
            f.write(struct.pack(">II", len(newData), len(compressed_data)))
            f.write(b'DCP\x00DFLT')
            f.write(self.unknownHeaderPart)
            f.write(b'DCA\x00')
            f.write(struct.pack(">I", self.compressed_header_length))
            f.write(compressed_data)

    def set_emevd_dcx_values(self):
        self.req1 = 256
        self.req2 = 24
        self.req3 = 36
        self.req4 = 36
        self.header_length = 44
        self.compressed_header_length = 8
        self.unknownHeaderPart = b'\x00\x00\x00 \t\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x01\x01\x00'
