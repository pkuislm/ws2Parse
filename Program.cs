using System.Text;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;

namespace ws2Parse
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            CLegacyFunctions.UpdateVM1993();

            foreach (var item in Directory.EnumerateFiles(args[0], "*.ws2"))
            {
                Dis(item);

            }
            //WS2Script.Encrypt(ref script);
            //File.WriteAllBytes(BaseBame + ".ws2", script);
        }

        static void Dis(string BaseName)
        {
            var script = File.ReadAllBytes(BaseName);

            List<CLegacyFunctions.Command> script_commands = new List<CLegacyFunctions.Command>();

            var veip = 0;
            while (veip < script.Length)
            {
                //var offset = veip;
                var command = script[veip++];
                if (command == 0xFF)
                {
                    Console.WriteLine("Code ends.");
                    break;
                }
                //var arg_list = CLegacyFunctions.GetArgs(command, script, ref veip);
                script_commands.Add(new CLegacyFunctions.Command(ref veip, command, script));
            }
            CLegacyFunctions.ExportStrings(script_commands, BaseName);
/*            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(BaseName, "json")))
            {
                sw.WriteLine(JsonConvert.SerializeObject(script_commands, Formatting.Indented));
                sw.Flush();
                sw.Close();
            }*/
/*            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(BaseName, "txt")))
            {
                sw.WriteLine(CLegacyFunctions.Decompile(script_commands));
                sw.Flush();
                sw.Close();
            }*/
        }

    }
    public static class WS2Script
    {
        public static void Encrypt(ref byte[] script)
        {
            for (var i = 0; i < script.Length; ++i)
            {
                script[i] = (byte)((byte)(script[i] >> 6) | (byte)(script[i] << 2));
            }
        }
        public static void Decrypt(ref byte[] script)
        {
            for (var i = 0; i < script.Length; ++i)
            {
                script[i] = (byte)((byte)(script[i] >> 2) | (byte)(script[i] << 6));
            }
        }
    }

    public static class CLegacyFunctions
    {
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
            public int old;
            public int cur;
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
                offset.old = veip;
                offset.cur = 0;
                this.op = op;
                args = GetArgs(op, script, ref veip);
                name = GetFunctionName(op);
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
            ARG_UNK8 = 8,
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
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END },//JMP
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_ARRAY, ArgTypes.ARG_STR1, ArgTypes.ARG_END },//string arr
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_ARRAY, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1,ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            Array.Empty<ArgTypes>(),
            Array.Empty<ArgTypes>(),
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END },
            new ArgTypes[]{ ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
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
            new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_END },
            new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END }, 
            Array.Empty<ArgTypes>()//ScriptEnd
        };

        public static void UpdateVM1993()
        {
            //AdvHD ver 1.9.9.3 updated VM Functions
            func_args[17] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[20] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[21] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[22] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[30] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[40] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[95] = new ArgTypes[] { ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[96] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[97] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[98] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[99] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[105] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[106] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[107] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[108] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[110] = new ArgTypes[] { ArgTypes.ARG_STR2, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[120] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[127] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[128] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[129] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[130] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[131] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[132] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[133] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[134] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[135] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[136] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[140] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[141] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[142] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[143] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[144] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[150] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[151] = new ArgTypes[] { ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[152] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[153] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[154] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[155] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[156] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[157] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[158] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[159] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[200] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[201] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[202] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[203] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[204] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[205] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[206] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
            func_args[207] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_R4, ArgTypes.ARG_END };
            func_args[208] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[209] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[210] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[211] = new ArgTypes[] { ArgTypes.ARG_STR1, ArgTypes.ARG_UNK8, ArgTypes.ARG_END };
            func_args[212] = new ArgTypes[] { ArgTypes.ARG_STR3, ArgTypes.ARG_UNK8, ArgTypes.ARG_VT_I2, ArgTypes.ARG_VT_I2, ArgTypes.ARG_END };
            func_args[230] = new ArgTypes[] { ArgTypes.ARG_VT_UI4, ArgTypes.ARG_VT_UI4, ArgTypes.ARG_END };
            func_args[231] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[232] = new ArgTypes[] { ArgTypes.ARG_END };
            func_args[240] = new ArgTypes[] { ArgTypes.ARG_VT_UI1, ArgTypes.ARG_END };
        }

        static readonly Dictionary<int, string> func_name = new Dictionary<int, string>()
        {
            { 0x01, "//JX( byte flag, short unk1, float unk2, uint dest1, uint dest2 );\nJX" },
            { 0x02, "//JMP( uint dest );\nJMP" },
            { 0x04, "//CallFunc( string script_name );\nCallFunc" },
            { 0x05, "Return" },
            { 0x06, "//JMP( uint dest );\nJMP" },
            { 0x07, "//JumpTarget( string target_script_name );\nJumpTarget" },
            { 0x0F, "//Selection( byte count );\nSelection"},
            { 0x14, "//Message( uint index, string type? string text );\nMessage" },
            { 0x15, "//SetName( string name );\nSetName" }
        };
        
        class Argstruc
        {
            public string? closure;
            public string? arrdef;
        }

        static string GetFunctionName(int op)
        {
            if (func_name.ContainsKey(op))
                return func_name[op];
            else
                return $"F_{op:X2}";
        }

        public static string Decompile(List<Command> commands)
        {
            StringBuilder sb = new StringBuilder();
            int arrc = 0;
            foreach (Command cmd in commands)
            {
                var tmp = ParseArgs(cmd.op, ref arrc, cmd.args);

                sb.Append(tmp.arrdef);
                sb.Append(cmd.name);
                sb.Append(tmp.closure);               
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static Argstruc ParseArgs(byte command, ref int arrc, List<Arg> args)
        {
            StringBuilder sb = new StringBuilder();
            Argstruc struc = new Argstruc();
            sb.Append("( ");
            var limit = command == 0x0F ? 1 : args.Count;
            var i = 0;
            for (; i < limit; i++)
            {
                var arg = args[i];
                if(arg.GetArgType() == ArgTypes.ARG_UNK8)
                {
                    continue;
                }
                else if(arg.GetArgType() != ArgTypes.ARG_ARRAY)
                {
                    if(arg.GetArgType() == ArgTypes.ARG_STR1 || arg.GetArgType() == ArgTypes.ARG_STR2 || arg.GetArgType() == ArgTypes.ARG_STR3)
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
                    
                    if(arg.data.GetType() == typeof(List<byte>))
                    {
                        if (arg.data is List<byte> tdata)
                        {
                            sb2.Append(string.Format("byte a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<short>))
                    {
                        if (arg.data is List<short> tdata)
                        {
                            sb2.Append(string.Format("short a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<ushort>))
                    {
                        if (arg.data is List<ushort> tdata)
                        {
                            sb2.Append(string.Format("unsigned short a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<float>))
                    {
                        if (arg.data is List<float> tdata)
                        {
                            sb2.Append(string.Format("float a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<int>))
                    {
                        if (arg.data is List<int> tdata)
                        {
                            sb2.Append(string.Format("int a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<uint>))
                    {
                        if (arg.data is List<uint> tdata)
                        {
                            sb2.Append(string.Format("unsigned int a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach(var t in tdata)
                            {
                                sb2.Append(string.Format("{0}, ", t));
                            }
                        }
                    }
                    else if(arg.data.GetType() == typeof(List<string>))
                    {
                        if (arg.data is List<string> tdata)
                        {
                            sb2.Append(string.Format("string a{0}[{1}] = {{ ", arrc, tdata.Count));
                            foreach (var s in tdata)
                            {
                                sb2.Append(string.Format("\"{0}\", ", s));
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("type of arg.data is not valid");
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
            if(i < args.Count - 1)//This means there are some extra params, and that is (at least now) the selection command.
            {
                struc.closure = sb.ToString().TrimEnd(new char[] { ' ', ',' }) + " );\n";
                var c = 0;
                while(i < args.Count)
                {
                    if (args[i+4].data is Command callback_cmd)
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

        static List<Arg> GetArgs(byte command, byte[] script, ref int veip)
        {
            var args = func_args[command];
            var encoding = Encoding.GetEncoding("shift_jis");
            List<Arg> ret = new List<Arg>();
            for(var argi = 0; argi < args.Length; ++argi)
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
                    case ArgTypes.ARG_UNK8:     //for null-terminated str's '\0' ?
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
                        ret.Add(new Arg(arg, encoding.GetString(bin_arg)));
                        veip += bin_arg.Length;
                        break;
                    }
                    case ArgTypes.ARG_ARRAY:
                    {
                        var array_length = script[veip++];
                        argi++;
                        var elem_type = args[argi];
                        switch(elem_type)
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
                                    array_val.Add(encoding.GetString(bin_arg));
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
                    for(var i = 0; i < num; i++)
                    {
                        ret.Add(new Arg(ArgTypes.ARG_VT_UI2, BitConverter.ToUInt16(script, veip)));
                        veip += 2;
                        var length = strlen(script, veip);
                        var bin_arg = new byte[length - 1];
                        Array.Copy(script, veip, bin_arg, 0, bin_arg.Length);
                        ret.Add(new Arg(ArgTypes.ARG_STR1, encoding.GetString(bin_arg)));
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

        static int strlen(byte[] input, int offset)
        {
            var i = 0;
            while (input[offset + i++] != '\0') ;
            return i;
        }

        public static void ExportStrings(List<Command> script, string output)
        {
            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(output, "txt")))
            {
                for (var i = 0; i < script.Count; ++i)
                {
                    if (script[i].op == 0x14)
                    {
                        if (script[i - 1].op != 0x15)
                        {
                            throw new Exception("No name was set before message command.");
                        }
                        sw.WriteLine(string.Format("☆{0:X8}☆{1}☆{2}\n★{0:X8}★{1}★{2}\n", script[i].args[0].data, script[i - 1].args[0].data, script[i].args[2].data));
                    }
                    else if (script[i].op == 0x0F)
                    {
                        if (script[i].args[0].data is byte count)
                        {
                            for(var j = 0; j < count; ++j)
                            {
                                sw.WriteLine("☆{0:X8}☆选项{1}☆{2}\n★{0:X8}★选项{1}★{2}\n", script[i].args[j * 5 + 1].data, j + 1, script[i].args[j * 5 + 2].data);
                            }
                        }
                    }
                }
            }
        }
    }
}