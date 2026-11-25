import struct
import re
import sys
import os
from dcx_handler import DCXHandler
from method_names import METHOD_NAMES, GetReversedMethodMapping, IAT


class Event():
    
    def __init__(self, ito: int, pto: int, ao: int):
        self.instruction_t_offs = ito
        self.parameter_t_offs = pto
        self.argument_offset = ao

        self.eventId = 0
        self.instruction_count = 0
        self.instructions_offset = 0
        self.param_count = 0
        self.param_offset = 0
        self.unknown = 0

        self.instructions = []
        self.parameters = []
        self.eventParams = []

    def event_param_string(self) -> str:
        retStr = ""
        for ep in self.eventParams:
            retStr += ep[1]
        return retStr

    def parse_event_init_args(self, eventParamMap: dict, isImport: bool = False):
        for instr in self.instructions:
            if (instr.instruction_class == 2000 and instr.instruction_index == 0):
                if (isImport):
                    instr.parse_event_init_args_import(eventParamMap)
                else:
                    instr.parse_event_init_args(eventParamMap)

    def format_event_params(self) -> str:
        retStr = ""
        for i, ep in enumerate(self.eventParams):
            if (i != 0):
                retStr += ", "
            retStr += ep[1] + ": arg" + str(i)
        return retStr

    def export_dkscript(self) -> str:
        retStr = "def {0}, {1} ({2}) ".format(self.eventId, self.unknown, self.format_event_params()) + "{\n"
        for instr in self.instructions:
            retStr += "\t" + instr.export_dkscript(self.eventParams) + "\n"
        retStr += "}\n\n"

        return retStr

    def read(self, mBytes: bytes, pos: int) -> int:
        offset = pos
        self.eventId, self.instruction_count, self.instructions_offset, self.param_count, self.param_offset, self.unknown, zero1 = struct.unpack_from("<IIIIiII", mBytes, offset)

        offset += struct.calcsize("<IIIIiII")
        prevOffs = offset
        offset = self.instruction_t_offs + self.instructions_offset

        for i in range(self.instruction_count):
            inst = Instruction(self.argument_offset)
            offset = inst.read(mBytes, offset)
            self.instructions.append(inst)

        offset = self.parameter_t_offs + self.param_offset

        for i in range(self.param_count):
            param = Parameter()
            offset = param.read(mBytes, offset)
            param.argType = self.instructions[param.event_instr_index].arg_type_at_offset(param.dest_start_byte, param.length)
            hasParam = False
            for ep in self.eventParams:
                if (ep[0] == param.src_start_byte):
                    hasParam = True
                    break
            if (not hasParam):
                self.eventParams.append((param.src_start_byte, param.argType))
            self.instructions[param.event_instr_index].params.append(param)

        self.eventParams = sorted(self.eventParams, key = lambda x: x[0])

        return prevOffs

    def write(self, eventArr: bytearray, instrArr: bytearray, argArr: bytearray, paramArr: bytearray):
        self.instructions_offset = len(instrArr)
        self.param_offset = len(paramArr)

        self.instruction_count = len(self.instructions)
        self.param_count = 0
        for i, instr in enumerate(self.instructions):
            self.param_count += len(instr.params)
            instr.write(i, instrArr, argArr, paramArr)

        if (self.param_count == 0):
            self.param_offset = -1

        eventArr += struct.pack("<IIIIiII", self.eventId, self.instruction_count, self.instructions_offset, self.param_count, self.param_offset, self.unknown, 0)

    def __str__(self):
        retStr = "Event " + str(self.eventId) + ", " + str(self.unknown) + " " + str(self.eventParams)
        for inst in self.instructions:
            retStr += "\n " + str(inst)

        retStr += "\n"
        return retStr

class Instruction:

    def __init__(self, ao: int):
        self.argument_offset = ao

        self.instruction_class = 0
        self.instruction_index = 0
        self.bytes_in_instr_arg = 0
        self.instr_arg_offset = 0
        self.signed_int = -1

        self.argBytes = b''
        self.argTypes = ''
        self.args = []
        self.params = []

        self.import_args = []
        self.import_event_params = []

    def arg_type_at_offset(self, offset, expectSize):
        argIdx = self.arg_index_at_offset(offset, expectSize)
        retType = "<invalid>"
        if (argIdx != -1):
            retType = self.argTypes[argIdx + 1]
            if (struct.calcsize("@" + retType) != expectSize):
                raise ValueError("Invalid retType for [{0}, {1}]: {2} at offset {3}. Expected type of size {4}.".format(self.instruction_class, self.instruction_index, retType, offset, expectSize))
        else:
            raise ValueError("Invalid offset for [{0}, {1}]: {2}, {3}. Self argTypes are: {4}".format(self.instruction_class, self.instruction_index, offset, expectSize, self.argTypes))
        return retType

    def arg_index_at_offset(self, offset, expectSize):
        argTypeCount = len(self.argTypes)
        retIdx = 0
        for i in range(argTypeCount):
            currSize = struct.calcsize(self.argTypes[:1 + i]) - expectSize
            if currSize == offset:
                retIdx = i
                break
        return retIdx - 1

    def export_dkscript(self, currentEventParams) -> str:
        retStr = ""

        try:
            retStr += METHOD_NAMES[self.instruction_class][self.instruction_index] + "("
        except:
            raise ValueError("Method Name [{0}, {1}] is undefined.".format(self.instruction_class, self.instruction_index))

        for i in range(len(self.args)):
            if (i > 0):
                retStr += ", "
            paramIdx = -1
            for idx, param in enumerate(self.params):
                if (self.arg_index_at_offset(param.dest_start_byte, param.length) == i):
                    paramIdx = idx

            if (paramIdx == -1):
                retStr += str(self.args[i])
            else:
                for epIdx, ep in enumerate(currentEventParams):
                    if (ep[0] == self.params[paramIdx].src_start_byte):
                        retStr += "arg" + str(epIdx)
                        break

        retStr += ")"

        return retStr

    def parse_event_init_args(self, eventParamMap: dict):
        tempArgs = struct.unpack_from(self.argTypes, self.argBytes, 0)
        event_to_init = tempArgs[1]
        if (event_to_init in eventParamMap):
            event_params = eventParamMap[event_to_init]
            self.argTypes = "@iI"
            if (len(event_params) == 0):
                self.argTypes += "I"
            else:
                self.argTypes += event_params
        else:
            self.argTypes = "@iII"
        
        self.args = struct.unpack_from(self.argTypes, self.argBytes, 0)

    def parse_event_init_args_import(self, eventParamMap: dict):
        event_to_init = int(self.import_args[1])
        if (event_to_init in eventParamMap):
            event_params = eventParamMap[event_to_init]
            if (len(event_params) > 0):
                self.argTypes = "@iI" + event_params
        else:
            self.argTypes = "@iII"

        for i in range(len(self.import_args)):
            argType = self.argTypes[i + 1]
            if (argType == 'f'):
                self.args.append(float(self.import_args[i]))
            else:
                self.args.append(int(self.import_args[i]))

        self.import_args.clear()

    def read(self, mBytes: bytes, pos: int) -> int:
        offset = pos
        self.instruction_class, self.instruction_index, self.bytes_in_instr_arg, self.instr_arg_offset, self.signed_int, zero1 = struct.unpack_from("<IIIIiI", mBytes, offset)
        offset += struct.calcsize("<IIIIiI")

        self.argBytes = mBytes[self.argument_offset + self.instr_arg_offset:self.argument_offset + self.instr_arg_offset + self.bytes_in_instr_arg]
        try:
            self.argTypes = IAT[self.instruction_class][self.instruction_index]
        except:
            raise ValueError("Trying to get IAT [{0}, {1}]".format(self.instruction_class, self.instruction_index))
        if not (self.instruction_class == 2000 and self.instruction_index == 0):
            if (struct.calcsize(self.argTypes) > len(self.argBytes)):
                raise ValueError("Format '{0}' is unable to unpack values: expected buffer size of at least {1} bytes, current buffer size is {2} bytes.".format(self.argTypes, struct.calcsize(self.argTypes), len(self.argBytes)))
            self.args = list(struct.unpack_from(self.argTypes, self.argBytes, 0))

        return offset

    def write(self, instrIdx: int, instrArr: bytearray, argArr: bytearray, paramArr: bytearray):
        self.instr_arg_offset = len(argArr)
        self.argBytes = struct.pack(self.argTypes, *self.args)
        padding = len(self.argBytes) % 4
        if (padding > 0):
            self.argBytes += b'\x00' * (4 - padding)
        argArr += self.argBytes

        self.bytes_in_instr_arg = len(self.argBytes)

        for param in self.params:
            param.write(instrIdx, paramArr)
        
        instrArr += struct.pack("<IIIIiI", self.instruction_class, self.instruction_index, self.bytes_in_instr_arg, self.instr_arg_offset, self.signed_int, 0)

    def new(self, instr_class, instr_index, arg_types, arg_values):
        self.instruction_class = instr_class
        self.instruction_index = instr_index
        self.argTypes = arg_types
        self.args = arg_values

    def __str__(self):
        retStr = str(self.instruction_class) + "[" + str(self.instruction_index) + "] (" + str(self.argTypes[1:]) + ")" + str(self.args)
        for param in self.params:
            retStr += "\n    " + str(param)

        return retStr

class Parameter:

    def __init__(self):
        self.event_instr_index = 0
        self.dest_start_byte = 0
        self.src_start_byte = 0
        self.length = 0
        self.argType = "<invalid>"

    def read(self, mBytes: bytes, pos: int) -> int:
        offset = pos
        self.event_instr_index, self.dest_start_byte, self.src_start_byte, self.length, zero1 = struct.unpack_from("<IIIII", mBytes, offset)
        offset += struct.calcsize("<IIIII")
        return offset

    def write(self,instrIdx: int, paramArr: bytearray):
        self.event_instr_index = instrIdx
        paramArr += struct.pack("<IIIII", self.event_instr_index, self.dest_start_byte, self.src_start_byte, self.length, 0)

    def __str__(self):
        return str(self.event_instr_index) + ": " + str(self.dest_start_byte) + "<-" + str(self.src_start_byte) + "; " + str(self.length) + " [" + self.argType + "]"

class EmevdHandler():

    def __init__(self):
        self.magic = b''
        self.magic_const = 0xCC
        self.file_size = 0
        self.event_count = 0
        self.event_t_offs = 0
        self.instruction_count = 0
        self.instruction_t_offs = 0
        self.parameter_count = 0
        self.parameter_t_offs = 0
        self.argument_size = 0
        self.argument_offs = 0
        self.events = []

    def export_dkscript(self):
        retStr = ""
        for event in self.events:
            retStr += event.export_dkscript()
        return retStr

    def import_dkscript(self, filename: str):
        VAR_DEF_RE = r"( |\t)*(int|float) +(([A-Z]|[a-z]|\.|_|[0-9])+) * = *(.*)"
        REVERSE_DEF_RE = r"def ([0-9]+), *(0|1|2) *\((.*)\) *{"
        REVERSE_ARG_RE = r" *(b|B|i|I|f|h): *(.*) *"
        REVERSE_METHOD_DEF_RE = r"( |\t)*(([A-Z]|[a-z]|\.|_|[0-9])+)\((.*)\)"
        REVERSE_EVENT_END_RE = r" *} *"

        reverseMap = GetReversedMethodMapping(METHOD_NAMES)

        ce = None
        argMap = dict()
        eventParamMap = dict()
        varMap = dict()

        with open(filename) as f:
            for line in f:
                event_def = re.match(REVERSE_DEF_RE, line)
                if (event_def != None):
                    argMap.clear()

                    # Create New Event
                    ce = Event(0, 0, 0)
                    ce.eventId = int(event_def.group(1))
                    ce.unknown = int(event_def.group(2))

                    # Event params
                    argStr = "@"
                    for paramIdx, event_arg in enumerate(event_def.group(3).split(",")):
                        event_arg_def = re.match(REVERSE_ARG_RE, event_arg)
                        if (event_arg_def != None):
                            argName = event_arg_def.group(2).strip()
                            argType = event_arg_def.group(1).strip()
                            argStr += argType
                            ce.eventParams.append((struct.calcsize(argStr) - struct.calcsize("@" + argType), argType))
                            argMap[argName] = paramIdx

                    eventParamMap[ce.eventId] = ce.event_param_string()
                else:
                    var_match = re.match(VAR_DEF_RE, line)
                    if (var_match != None):
                        var_type = var_match.group(2)
                        var_name = var_match.group(3)
                        var_value = var_match.group(5)
                        if (var_type == "int"):
                            varMap[var_name] = int(var_value)
                        else:
                            varMap[var_name] = float(var_value)
                    else:
                        method_call = re.match(REVERSE_METHOD_DEF_RE, line)
                        if (method_call != None):
                            method_name = method_call.group(2)
                            method_args = method_call.group(4)

                            ci = Instruction(0)
                            ci.instruction_class, ci.instruction_index = reverseMap[method_name]
                            ci.argTypes = IAT[ci.instruction_class][ci.instruction_index]

                            if not (ci.instruction_class == 2000 and ci.instruction_index == 0):
                                for argIdx, arg in enumerate(method_args.split(",")):
                                    argValue = arg.strip()
                                    if (argValue != ""):
                                        argType = ci.argTypes[argIdx + 1]
                                        if (argValue in argMap):
                                            structStr = ci.argTypes[:argIdx + 2]

                                            if (argType == 'f'):
                                                ci.args.append(0.0)
                                            else:
                                                ci.args.append(0)

                                            cparam = Parameter()
                                            cparam.dest_start_byte = struct.calcsize(structStr) - struct.calcsize("@" + argType)
                                            cparam.src_start_byte = ce.eventParams[argMap[argValue]][0]
                                            cparam.length = struct.calcsize("@" + argType)
                                            cparam.argType = argType
                                            ci.params.append(cparam)
                                        else:
                                            if (argValue in varMap):
                                                ci.args.append(varMap[argValue])
                                            else:
                                                try:
                                                    if (argType == 'f'):
                                                        ci.args.append(float(argValue))
                                                    else:
                                                        ci.args.append(int(argValue))
                                                except:
                                                    raise ValueError("Undefined variable: " + argValue)
                            
                            else:
                                ci.import_args = method_args.split(",")
                                #print(line)
                                #print(ci.import_args)

                            ci.params = sorted(ci.params, key = lambda x : x.src_start_byte)

                            ce.instructions.append(ci)
                        else:
                            event_end = re.match(REVERSE_EVENT_END_RE, line)
                            if (event_end != None):
                                self.events.append(ce)
                                ce = None

        for event in self.events:
            event.parse_event_init_args(eventParamMap, True)

    def read(self, mBytes):
        offset = 0
        self.magic = mBytes[0:4]
        if (self.magic != b'EVD\x00'):
            raise ValueError("Expected b'EVD\x00', instead got " + str(self.magic))

        offset += 0x8
        self.magic_const = mBytes[offset]

        if (self.magic_const != 0xCC):
            raise ValueError("Expected magic const 0xCC, instead got " + str(hex(self.magic_const)))
        
        offset += 0x4

        self.file_size, self.event_count, self.event_t_offs, self.instruction_count, self.instruction_t_offs, zero1, dummy1, zero2, dummy2, self.parameter_count, self.parameter_t_offs, zero3, dummy3, self.argument_size, self.argument_offs, zero4, dummy4, zero5 = struct.unpack_from("<IIIIIIIIIIIIIIIIII", mBytes, offset)
        offset += struct.calcsize("<IIIIIIIIIIIIIIIIII")

        for i in range(self.event_count):
            ev = Event(self.instruction_t_offs, self.parameter_t_offs, self.argument_offs)
            self.events.append(ev)
            offset = ev.read(mBytes, offset)

        eventParamMap = dict()
        for event in self.events:
            eventParamMap[event.eventId] = event.event_param_string()

        for event in self.events:
            event.parse_event_init_args(eventParamMap)

    def write(self):
        # Header size = 84

        headerArr = bytearray()
        eventArr = bytearray()
        instrArr = bytearray()
        argArr = bytearray()
        paramArr = bytearray()

        self.event_count = len(self.events)
        self.instruction_count = 0
        self.parameter_count = 0

        for ev in self.events:
            ev.write(eventArr, instrArr, argArr, paramArr)
            self.instruction_count += ev.instruction_count
            self.parameter_count += ev.param_count

        self.event_t_offs = 84
        self.instruction_t_offs = self.event_t_offs + len(eventArr)
        self.argument_offs = self.instruction_t_offs + len(instrArr)
        self.argument_size = len(argArr)
        self.parameter_t_offs = self.argument_offs + self.argument_size
        self.file_size = self.parameter_t_offs + len(paramArr)

        headerArr += b'EVD\x00'
        headerArr += b'\x00\x00\x00\x00'
        headerArr += b'\xCC\x00\x00\x00'
        headerArr += struct.pack("<IIIIIIIIIIIIIIIIII", self.file_size, self.event_count, self.event_t_offs, self.instruction_count, self.instruction_t_offs, 0, self.argument_offs, 0, self.argument_offs, self.parameter_count, self.parameter_t_offs, 0, self.file_size, self.argument_size, self.argument_offs, 0, self.file_size, 0)

        return bytes(headerArr) + bytes(eventArr) + bytes(instrArr) + bytes(argArr) + bytes(paramArr)

    def __str__(self):
        retStr = "Header:"
        retStr += "\nFile Size = " + str(self.file_size)
        retStr += "\nEvent Count = " + str(self.event_count)
        retStr += "\nEvent Table Offset = " + str(self.event_t_offs)
        retStr += "\nInstruction Count = " + str(self.instruction_count)
        retStr += "\nInstruction Table Offset = " + str(self.instruction_t_offs)
        retStr += "\nParameter Count = " + str(self.parameter_count)
        retStr += "\nParameter Table Offset = " + str(self.parameter_t_offs)
        retStr += "\nArgument Size = " + str(self.argument_size)
        retStr += "\nArgument Offset = " + str(self.argument_offs)
        retStr += "\n---\nEvents:\n---"
        for ev in self.events:
            retStr += "\n" + str(ev)
        
        return retStr


if __name__ == "__main__":
    if (len(sys.argv) == 1):
        print("No arguments given, use emevd_handler.py -h to see help.")
    if len(sys.argv) == 2:
        if (sys.argv[1] == "-h"):
            print("""
-h show this help menu
-e convert to emevd (if not given, convert emevd to dkscript)
-r remastered
-d directory conversion
-i <name> input file, or directory if -d flag is used
-o <name> output file, or directory if -d flag is used
""")
    else:
        filename = sys.argv[1]
        inFile = ""
        outFile = ""
        lastTag = ""
        dirConv = False
        toDkscript = True
        useDCX = False
        for i, flag in enumerate(sys.argv):
            if (i > 0):
                if (flag == "-e"):
                    toDkscript = False
                elif (flag == "-d"):
                    dirConv = True
                elif (flag == "-r"):
                    useDCX = True
                else:
                    if (lastTag == "-i"):
                        inFile = flag
                    elif (lastTag == "-o"):
                        outFile = flag
                lastTag = flag

        if (dirConv):
            if (os.path.isdir(inFile)):
                inFiles = [f for f in os.listdir(inFile) if os.path.isfile(os.path.join(inFile, f))]
                if not os.path.isdir(outFile):
                    os.makedirs(outFile)
                if (toDkscript):
                    for inF in inFiles:
                        if ".emevd" in inF:
                            try:
                                eh = EmevdHandler()
                                with open(inFile + inF, 'rb') as f:
                                    if (useDCX):
                                        dcxh = DCXHandler()
                                        eh.read(dcxh.open_dcx(f.read()))
                                    else:
                                        eh.read(f.read())

                                outF = inF.replace('.emevd', '.dkscript')
                                if (useDCX):
                                    outF = inF.replace('.emevd.dcx', '.dkscript')
                                with open(outFile + outF, 'w') as f:
                                    f.write(eh.export_dkscript())

                                print("Converted '" + inF + "' to .dkscript")
                            except:
                                print("Failed to convert '" + inF + "' to .dkscript")
                                raise ValueError("---")
                else:
                    for inF in inFiles:
                        if ".dkscript" in inF:
                            eh = EmevdHandler()
                            eh.import_dkscript(inFile + inF)

                            if (useDCX):
                                dcxh = DCXHandler()
                                dcxh.set_emevd_dcx_values()
                                dcxh.save_dcx(outFile + inF.replace('.dkscript', '.emevd.dcx'), eh.write(), False)
                            else:
                                with open(outFile + inF.replace('.dkscript', '.emevd'), 'wb') as f:
                                    f.write(eh.write())

                            print("Converted '" + inF + "' to .emevd")

            else:
                print("Input directiory '" + inFile + "' not found.")
        else:
            if (os.path.isfile(inFile)):
                if (toDkscript):
                    eh = EmevdHandler()
                    with open(inFile, 'rb') as f:
                        if (useDCX):
                            dcxh = DCXHandler()
                            eh.read(dcxh.open_dcx(f.read()))
                        else:
                            eh.read(f.read())
                    
                    with open(outFile, 'w') as f:
                        f.write(eh.export_dkscript())
                else:
                    eh = EmevdHandler()
                    eh.import_dkscript(inFile)

                    if (useDCX):
                        dcxh = DCXHandler()
                        dcxh.set_emevd_dcx_values()
                        dcxh.save_dcx(outFile, eh.write(), False)
                    else:
                        with open(outFile, 'wb') as f:
                            f.write(eh.write())
            else:
                print("Input file '" + inFile + "' not found.")