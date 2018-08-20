from byteread import *
import struct
import zlib
import os.path

class DCXHandler:
    
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
        mBytes = ''
        with open(filename, 'rb') as f:
            mBytes = f.read()
        self.open_dcx(mBytes)
        

    def open_dcx(self, mBytes):
        curr_offset = 0

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
        #curr_offset = consume_byte(mBytes, curr_offset, '0x78', 1)
        #curr_offset = consume_byte(mBytes, curr_offset, '0xDA', 1)
        curr_offset += 0x2
        self.compressed_size -= 2  # The previous two bytes are included in the compressed data, for some reason.
        
        decomp_obj = zlib.decompressobj(-15)
        a = mBytes[curr_offset:curr_offset + self.compressed_size]
        self.data = decomp_obj.decompress(a, self.uncompressed_size)
        #self.data = zlib.decompress(mBytes[curr_offset:curr_offset + self.compressed_size], -15)
        return self.data

    def save_dcx(self, filename, newData):
        if not os.path.isfile(filename + '.bak'):
            with open(filename, 'rb') as origf:
                with open(filename + '.bak', 'wb') as bakf:
                    bakf.write(origf.read())

        with open(filename, 'wb') as f:
            #comp_obj = zlib.compressobj(-1, zlib.DEFLATED, -15)
            compressed_data = zlib.compress(newData, 9)#comp_obj.compress(newData)
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
            #f.write(self.twobytes)
            f.write(compressed_data)
            print("[DCX] Compressed")
