using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ws2Parse.PE;

namespace ws2Parse
{
    public static class CLegacyFunctions
    {
        static readonly Encoding read_encoding = Encoding.GetEncoding("shift_jis");
        static readonly Encoding write_encoding = Encoding.GetEncoding("gbk");
        public class Arg
        {
            public string type;
            ArgTypes etype;
            public object data;

            public Arg(ArgTypes type, object data)
            {
                etype = type;
                this.type = type.ToString();
                this.data = data;
            }

            public Arg(ArgTypes type, int count, object data)
            {
                etype = type;
                this.type = string.Format("{0}[{1}]", type.ToString(), count); ;
                this.data = data;
            }

            public ArgTypes GetArgType()
            {
                return etype;
            }
        }

        public class CommandOffset
        {
            public uint old;
            public uint cur;
        }

        public class Command
        {
            public CommandOffset offset;
            public byte op;
            public string name;
            public List<Arg> args;

            public Command(ref int veip, byte op, byte[] script)
            {
                offset = new CommandOffset();
                offset.old = (uint)veip - 1;
                offset.cur = 0;
                this.op = op;
                name = GetFunctionName(op, veip - 1);
                args = ReadArgs(op, script, ref veip);
            }

            public Command(ref int veip, byte op)
            {
                offset = new CommandOffset();
                offset.old = (uint)veip - 1;
                offset.cur = 0;
                this.op = op;
                args = new List<Arg>();
                name = "End of Script";
            }

            public byte[] GetBytes()
            {
                var ret = new List<byte>();
                ret.Add(op);
                ret.AddRange(WriteArgs(args));
                return ret.ToArray();
            }
        }

        public enum ArgTypes
        {
            ARG_VT_UI1 = 0,
            ARG_VT_I2 = 1,
            ARG_VT_UI2 = 2,
            ARG_VT_INT = 3,
            ARG_VT_UI4 = 4,
            ARG_VT_R4 = 5,
            ARG_STR1 = 6,
            ARG_ARRAY = 7,
            ARG_PERIOD = 8,
            ARG_STR2 = 9,
            ARG_STR3 = 0x0A,
            //ARG_UTF8STR = 0x0B,
            ARG_CALLBACK = 0xFE,
            ARG_END = 0xFF
        }

        static ArgTypes[][] func_args = new ArgTypes[256][]
        {
            //For AdvHD ver 1.2.1.0
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI4, ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END },//JX
            new ArgTypes[]{ ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END },//JMP
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END },//JMP
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_ARRAY, ArgTypes.ARG_STR1, ArgTypes.ARG_END },//string arr
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1,ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
#region unused
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
#endregion unused
            new ArgTypes[] { ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>()//ScriptEnd
        };

        public static void UpdateVM1993()
        {
            //AdvHD ver 1.9.9.3 updated VM Functions
            func_args[17] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[20] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[21] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[22] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[30] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[40] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[95] = new ArgTypes[] { ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[96] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[97] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[98] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[99] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[105] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[106] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[107] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[108] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[110] = new ArgTypes[] { ArgTypes.ARG_STR2, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[120] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[127] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[128] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[129] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[130] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[131] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[132] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[133] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[134] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[135] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[136] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[140] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[141] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[142] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[143] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[144] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[150] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[151] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[152] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[153] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[154] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[155] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[156] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[157] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[158] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[159] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[200] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[201] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[202] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[203] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[204] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[205] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[206] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[207] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[208] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[209] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[210] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[211] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_PERIOD, ArgTypes.ARG_END };
            func_args[212] = new ArgTypes[] { ArgTypes.ARG_STR3, ArgTypes.ARG_PERIOD, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[230] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END };
            func_args[231] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[232] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[240] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
        }

        public static bool SetArgTypes(ArgTypes[][] input)
        {
            if(input.Length != 256)
            {
                Console.Error.WriteLine("SetArgTypes：传入的参数类型数组大小不正确。");
                return false;
            }
            func_args = input;
            return true;
        }

        public static void SaveArgTypes(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                for(int i = 0; i < 256; i++)
                {
                    if (func_args[i].Length != 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach(var t in func_args[i])
                        {
                            sb.Append(t.ToString() + ", ");
                        }
                        sw.WriteLine($"{i:X2} {sb.ToString()[..(sb.Length - 2)]}");
                    }
                    else
                    {
                        sw.WriteLine($"{i:X2} Empty");
                    }
                }
                sw.Flush();
                sw.Close();
            }
        }

        static readonly Dictionary<int, Tuple<string, string>> func_name = new Dictionary<int, Tuple<string, string>>()
        {
            { 0x01, new("//JX( byte flag, short unk1, float unk2, uint dest1, uint dest2 );", "JX") },
            { 0x02, new("//JMP( uint dest );","JMP") },
            { 0x04, new("//CallFunc( string script_name );","CallFunc") },
            { 0x05, new("","Return") },
            { 0x06, new("//JMP( uint dest );","JMP") },
            { 0x07, new("//JumpTarget( string target_script_name );","JumpTarget") },
            { 0x0F, new("//Selection( byte count );","Selection") },
            { 0x14, new("//Message( uint index, string type? string text );","Message") },
            { 0x15, new("//SetName( string name );","SetName") },
            { 0x33, new("//SetLayer( string layerName, string graphName );","SetLayer") }
        };

        public class Argstruc
        {
            public string? closure;
            public string? arrdef;
        }

        static string GetFunctionName(int op, int addr)
        {
            if (func_name.ContainsKey(op))
                return $"{func_name[op].Item1}\n{addr:d08}: {func_name[op].Item2}";
            else
                return $"{addr:d08}: Function_{op:X2}";
        }

        public static Argstruc ParseArgs(byte command, ref int arrc, List<Arg> args)
        {
            StringBuilder sb = new StringBuilder();
            Argstruc struc = new Argstruc();
            sb.Append("( ");
            var limit = command == 0x0F ? 1 : args.Count;
            var i = 0;
            for (; i < limit; i++)
            {
                var arg = args[i];
                if (arg.GetArgType() == ArgTypes.ARG_PERIOD)
                {
                    continue;
                }
                else if (arg.GetArgType() != ArgTypes.ARG_ARRAY)
                {
                    if (arg.GetArgType() == ArgTypes.ARG_STR1 || arg.GetArgType() == ArgTypes.ARG_STR2 || arg.GetArgType() == ArgTypes.ARG_STR3)
                    {
                        sb.Append(string.Format("\"{0}\", ", arg.data));
                    }
                    else
                    {
                        sb.Append(string.Format("{0}, ", arg.data));
                    }
                }
                else
                {
                    StringBuilder sb2 = new StringBuilder();

                    if (arg.data is IList list)
                    {
                        string typ = arg.data switch
                        {
                            List<byte> => "byte",
                            List<short> => "short",
                            List<ushort> => "unsigned short",
                            List<int> => "int",
                            List<uint> => "unsigned int",
                            List<float> => "float",
                            List<string> => "string",
                            _ => throw new Exception("type of arg.data is not valid"),
                        };
                        sb2.Append(string.Format("{0} a{1}[{2}] = {{ ", typ, arrc, list.Count));

                        if (typ == "string")
                        {
                            foreach (var elm in list)
                            {
                                sb2.Append(string.Format("{0}, ", elm));
                            }
                        }
                        else
                        {
                            foreach (var elm in list)
                            {
                                sb2.Append(string.Format("\"{0}\", ", elm));
                            }
                        }
                    }

                    sb.Append(string.Format("a{0}, ", arrc));
                    arrc++;
                    if (struc.arrdef != null)
                    {
                        struc.arrdef += sb2.ToString().TrimEnd(new char[] { ' ', ',' }) + " };\n";
                    }
                    else
                    {
                        struc.arrdef = sb2.ToString().TrimEnd(new char[] { ' ', ',' }) + " };\n";
                    }
                }
            }
            if (i < args.Count - 1)//This means there are some extra params, and that is (at least now) the selection command.
            {
                struc.closure = sb.ToString().TrimEnd(new char[] { ' ', ',' }) + " );\n";
                var c = 0;
                while (i < args.Count)
                {
                    if (args[i + 4].data is Command callback_cmd)
                    {
                        struc.closure += string.Format("\tSelection[{0}].set( {1}, \"{2}\", {3}, {4}, \"{5}\" );\n", c, args[i].data, args[i + 1].data, args[i + 2].data, args[i + 3].data, callback_cmd.args[0].data);
                        c++;
                        i += 5;
                    }
                    else
                    {
                        throw new Exception("Selection command is not valid");
                    }
                }
                return struc;
            }
            struc.closure = sb.ToString().TrimEnd(new char[] { ' ', ',' }) + " );\n";
            return struc;
        }

        public static List<Arg> ReadArgs(byte command, byte[] script, ref int veip)
        {
            var args = func_args[command];
            List<Arg> ret = new List<Arg>();
            for (var argi = 0; argi < args.Length; ++argi)
            {
                var arg = args[argi];
                if (arg == ArgTypes.ARG_END)
                {
                    break;
                }
                switch (arg)
                {
                    case ArgTypes.ARG_VT_UI1:
                        ret.Add(new Arg(arg, script[veip++]));
                        break;
                    case ArgTypes.ARG_PERIOD:     //for null-terminated str's '\0'
                        veip++;
                        break;
                    case ArgTypes.ARG_VT_I2:
                        ret.Add(new Arg(arg, BitConverter.ToInt16(script, veip)));
                        veip += 2;
                        break;
                    case ArgTypes.ARG_VT_UI2:
                        ret.Add(new Arg(arg, BitConverter.ToUInt16(script, veip)));
                        veip += 2;
                        break;
                    case ArgTypes.ARG_VT_INT:
                        ret.Add(new Arg(arg, BitConverter.ToInt32(script, veip)));
                        veip += 4;
                        break;
                    case ArgTypes.ARG_VT_UI4:
                        ret.Add(new Arg(arg, BitConverter.ToUInt32(script, veip)));
                        veip += 4;
                        break;
                    case ArgTypes.ARG_VT_R4:
                        ret.Add(new Arg(arg, BitConverter.ToSingle(script, veip)));
                        veip += 4;
                        break;
                    case ArgTypes.ARG_STR1:
                    case ArgTypes.ARG_STR2:
                    case ArgTypes.ARG_STR3:
                    {
                        var length = strlen(script, veip);
                        var bin_arg = new byte[length - 1];
                        Array.Copy(script, veip, bin_arg, 0, bin_arg.Length);
                        ret.Add(new Arg(arg, read_encoding.GetString(bin_arg)));
                        veip += bin_arg.Length;
                        break;
                    }
                    case ArgTypes.ARG_ARRAY:
                    {
                        var array_length = script[veip++];
                        argi++;
                        var elem_type = args[argi];
                        switch (elem_type)
                        {
                            case ArgTypes.ARG_VT_UI1:
                            {
                                List<byte> array_val = new List<byte>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(script[veip++]);
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_VT_I2:
                            {
                                List<short> array_val = new List<short>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(BitConverter.ToInt16(script, veip));
                                    veip += 2;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_VT_UI2:
                            {
                                List<ushort> array_val = new List<ushort>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(BitConverter.ToUInt16(script, veip));
                                    veip += 2;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_VT_R4:
                            {
                                List<float> array_val = new List<float>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(BitConverter.ToSingle(script, veip));
                                    veip += 4;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_VT_UI4:
                            {
                                List<uint> array_val = new List<uint>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(BitConverter.ToUInt32(script, veip));
                                    veip += 4;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_VT_INT:
                            {
                                List<int> array_val = new List<int>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    array_val.Add(BitConverter.ToInt32(script, veip));
                                    veip += 4;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            case ArgTypes.ARG_STR1:
                            case ArgTypes.ARG_STR2:
                            case ArgTypes.ARG_STR3:
                            {
                                List<string> array_val = new List<string>();
                                for (var i = 0; i < array_length; i++)
                                {
                                    var length = strlen(script, veip);
                                    var bin_arg = new byte[length - 1];
                                    Array.Copy(script, veip, bin_arg, 0, bin_arg.Length);
                                    array_val.Add(read_encoding.GetString(bin_arg));
                                    veip += bin_arg.Length + 1;
                                }
                                ret.Add(new Arg(arg, array_length, array_val));
                                break;
                            }
                            default:
                                throw new Exception(string.Format("Invalid Array Type:{0}", elem_type));
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
            if (command == 0x0F)//This is a choice command
            {
                if (ret[0].data is byte num)
                {
                    for (var i = 0; i < num; i++)
                    {
                        ret.Add(new Arg(ArgTypes.ARG_VT_UI2, BitConverter.ToUInt16(script, veip)));
                        veip += 2;
                        var length = strlen(script, veip);
                        var bin_arg = new byte[length - 1];
                        Array.Copy(script, veip, bin_arg, 0, bin_arg.Length);
                        ret.Add(new Arg(ArgTypes.ARG_STR1, read_encoding.GetString(bin_arg)));
                        veip += bin_arg.Length + 1;
                        ret.Add(new Arg(ArgTypes.ARG_VT_UI1, script[veip++]));
                        ret.Add(new Arg(ArgTypes.ARG_VT_I2, BitConverter.ToInt16(script, veip)));
                        veip += 2;
                        var ncommand = script[veip++];
                        ret.Add(new Arg(ArgTypes.ARG_CALLBACK, new Command(ref veip, ncommand, script)));
                    }
                }
                else
                {
                    throw new Exception("Selection count invalid");
                }
            }
            return ret;
        }

        public static byte[] WriteArgs(List<Arg> args)
        {
            var ret = new List<byte>();
            foreach (var o in args)
            {
                switch (o.GetArgType())
                {
                    case ArgTypes.ARG_VT_UI1:
                        if (o.data is byte b) ret.Add(b);
                        break;
                    case ArgTypes.ARG_VT_I2:
                        if (o.data is short s) ret.AddRange(BitConverter.GetBytes(s));
                        break;
                    case ArgTypes.ARG_VT_UI2:
                        if (o.data is ushort us) ret.AddRange(BitConverter.GetBytes(us));
                        break;
                    case ArgTypes.ARG_VT_INT:
                        if (o.data is int i) ret.AddRange(BitConverter.GetBytes(i));
                        break;
                    case ArgTypes.ARG_VT_UI4:
                        if (o.data is uint ui) ret.AddRange(BitConverter.GetBytes(ui));
                        break;
                    case ArgTypes.ARG_VT_R4:
                        if (o.data is float f) ret.AddRange(BitConverter.GetBytes(f));
                        break;
                    case ArgTypes.ARG_STR1:
                    case ArgTypes.ARG_STR2:
                    case ArgTypes.ARG_STR3:
                        if (o.data is string str) ret.AddRange(write_encoding.GetBytes(str));
                        ret.Add(0);
                        break;
                    case ArgTypes.ARG_ARRAY:
                    {
                        if (o.data is IList list)
                        {
                            //1byte数组长度，要保证长度不会超过byte所能表示的大小
                            ret.Add((byte)list.Count);
                            foreach (var elm in list)
                            {
                                switch (elm)
                                {
                                    case byte elmb:
                                        ret.Add(elmb);
                                        break;
                                    case short elms:
                                        ret.AddRange(BitConverter.GetBytes(elms));
                                        break;
                                    case ushort elmus:
                                        ret.AddRange(BitConverter.GetBytes(elmus));
                                        break;
                                    case int elmi:
                                        ret.AddRange(BitConverter.GetBytes(elmi));
                                        break;
                                    case uint elmui:
                                        ret.AddRange(BitConverter.GetBytes(elmui));
                                        break;
                                    case float elmf:
                                        ret.AddRange(BitConverter.GetBytes(elmf));
                                        break;
                                    case string elmstr:
                                        ret.AddRange(write_encoding.GetBytes(elmstr));
                                        ret.Add(0);
                                        break;
                                    default:
                                        throw new Exception("Unexpected array type.");
                                }
                            }
                        }
                        break;
                    }
                    case ArgTypes.ARG_CALLBACK:
                        if (o.data is Command cmd)
                        {
                            ret.AddRange(cmd.GetBytes());
                        }
                        break;
                    default:
                        break;
                }
            }
            return ret.ToArray();
        }

        static int strlen(byte[] input, int offset)
        {
            var i = 0;
            while (input[offset + i++] != '\0') ;
            return i;
        }
    }
}
