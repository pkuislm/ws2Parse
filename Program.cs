using System.Text;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;

namespace ws2Parse
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //CLegacyFunctions.UpdateVM1993();

            /*            foreach (var item in Directory.EnumerateFiles(args[0], "*.ws2"))
                        {
                            Dis(item);

                        }*/
            //ExportAllStrings(args[0], true);
            DecryptScripts(args[0]);
            //ws.Decompile(args[0]);
            //ws.ImportStrings(args[0]);
            //ws.Assem(args[0]);
            //WS2Script.Encrypt(ref script);
            //File.WriteAllBytes(BaseBame + ".ws2", script);
        }

        static void ExportAllStrings(string folder, bool decrypt = false)
        {
            WS2Script ws = new();
            foreach (string file in Directory.EnumerateFiles(folder, "*.ws2", SearchOption.TopDirectoryOnly))
            {
                ws.Load(file, decrypt);
                ws.ExportStrings(file);
            }
            Console.WriteLine(string.Format("总字数统计:约{0}字", ws.total_chars));
        }

        static void ReassembleAllScripts(string folder, bool importstrings = true, bool decrypt = false, bool encrypt = false)
        {
            WS2Script ws = new();
            foreach (string file in Directory.EnumerateFiles(folder, "*.ws2", SearchOption.TopDirectoryOnly))
            {
                ws.Load(file, decrypt);
                string txt_file = Path.ChangeExtension(file, "txt");
                if(File.Exists(txt_file) && importstrings)
                {
                    ws.ImportStrings(txt_file);
                }
                ws.Assem(file, encrypt);
            }
        }

        static void DecompileScripts(string input, bool decrypt = false)
        {
            WS2Script ws = new();
            if (Directory.Exists(input))
            {
                foreach (string file in Directory.EnumerateFiles(input, "*.ws2", SearchOption.TopDirectoryOnly))
                {
                    ws.Load(file, decrypt);
                    ws.Decompile(file);
                }
            }
            else
            {
                ws.Load(input, decrypt);
                ws.Decompile(input);
            }
        }
        static void DecryptScripts(string input)
        {
            WS2Script ws = new();
            if (Directory.Exists(input))
            {
                foreach (string file in Directory.EnumerateFiles(input, "*.ws2", SearchOption.TopDirectoryOnly))
                {
                    ws.DecryptScript(file);
                }
            }
            else
            {
                ws.DecryptScript(input);
            }
        }

    }
    public class WS2Script
    {
        List<CLegacyFunctions.Command> commands;
        byte[] param;
        public int total_chars { get; private set; }
        int single_chars;
        void Encrypt(ref byte[] script)
        {
            for (var i = 0; i < script.Length; ++i)
            {
                script[i] = (byte)((byte)(script[i] >> 6) | (byte)(script[i] << 2));
            }
        }
        void Decrypt(ref byte[] script)
        {
            for (var i = 0; i < script.Length; ++i)
            {
                script[i] = (byte)((byte)(script[i] >> 2) | (byte)(script[i] << 6));
            }
        }

        public WS2Script()
        {
            commands = new List<CLegacyFunctions.Command>();
            param = new byte[8];
            total_chars = 0;
            single_chars = 0;
        }

        public void DecryptScript(string BaseName)
        {
            var bytes = File.ReadAllBytes(BaseName);
            Decrypt(ref bytes);
            File.WriteAllBytes(Path.ChangeExtension(BaseName, ".ws2.dec"), bytes);
        }
        public void EncryptScript(string BaseName)
        {
            var bytes = File.ReadAllBytes(BaseName);
            Encrypt(ref bytes);
            File.WriteAllBytes(Path.ChangeExtension(BaseName, ".ws2.enc"), bytes);
        }

        public void Load(string BaseName, bool decrypt = false)
        {
            if (commands.Count != 0) commands.Clear();
            var script = File.ReadAllBytes(BaseName);
            if(decrypt) Decrypt(ref script);
            var veip = 0;
            while (veip < script.Length)
            {
                //var offset = veip;
                var command = script[veip++];
                if (command == 0xFF)
                {
                    //脚本末端
                    Debug.Assert((script.Length - veip) == 8);
                    Array.Copy(script, veip, param, 0, 8);//不知道这个是什么，暂时按原样复制过去
                    break;
                }
                commands.Add(new CLegacyFunctions.Command(ref veip, command, script));
            }
        }

        public void Decompile(string BaseName)
        {
            StringBuilder sb = new();
            int arrc = 0;
            foreach (CLegacyFunctions.Command cmd in commands)
            {
                var tmp = CLegacyFunctions.ParseArgs(cmd.op, ref arrc, cmd.args);

                sb.Append(tmp.arrdef);
                sb.Append(cmd.name);
                sb.Append(tmp.closure);
                sb.Append('\n');
            }
            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(BaseName, ".dis.txt")))
            {
                sw.WriteLine(sb.ToString());
                sw.Flush();
                sw.Close();
            }
            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(BaseName, "json")))
            {
                sw.WriteLine(JsonConvert.SerializeObject(commands, Formatting.Indented));
                sw.Flush();
                sw.Close();
            }
        }

        public void Assem(string BaseName, bool encrypt = false)
        {
            using (MemoryStream ms = new())
            {
                //old/new
                Dictionary<uint, uint> offset_dic = new Dictionary<uint, uint>();
                //index/start_offset
                Dictionary<int, int> offset_offsets = new Dictionary<int, int>();
                for (var i = 0; i < commands.Count; ++i)
                {
                    //i.offset.cur = (int)ms.Position;
                    offset_dic.Add(commands[i].offset.old, (uint)ms.Position);
                    if (commands[i].op == 1 || commands[i].op == 2 || commands[i].op == 6)
                    {
                        offset_offsets.Add(i, (int)ms.Position);
                    }
                    ms.Write(commands[i].GetBytes());
                }
                ms.WriteByte(0xFF);
                ms.Write(param);

                //fix jumps
                foreach (var i in offset_offsets.Keys)
                {
                    ms.Seek(offset_offsets[i], SeekOrigin.Begin);
                    switch (commands[i].op)
                    {
                        case 1:
                            if (commands[i].args[3].data is uint dst1) commands[i].args[3].data = offset_dic[dst1];
                            if (commands[i].args[4].data is uint dst2) commands[i].args[4].data = offset_dic[dst2];
                            break;
                        case 2:
                        case 6:
                            if (commands[i].args[0].data is uint dst3) commands[i].args[0].data = offset_dic[dst3];
                            break;
                    }
                    ms.Write(commands[i].GetBytes());
                }

                byte[] bytes = ms.ToArray();
                if(encrypt) Encrypt(ref bytes);
                File.WriteAllBytes(Path.ChangeExtension(BaseName, ".ws2.new"), bytes);
            }
        }

        public void ExportStrings(string output)
        {
            single_chars = 0;
            using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(output, "txt")))
            {
                for (var i = 0; i < commands.Count; ++i)
                {
                    if (commands[i].op == 0x14)
                    {
                        if (commands[i].args[2].data is string s)
                        {
                            single_chars += s.Length;
                            if(s == "%P")
                            {
                                continue;
                            }
                        }
                        if (i == 0)
                        {
                            sw.WriteLine(string.Format("☆{0:X8}☆☆{1}\n★{0:X8}★★{1}\n", commands[i].args[0].data, commands[i].args[2].data));
                        }
                        else
                        {
                            if (commands[i - 1].op != 0x15)
                            {
                                bool b = false;
                                //他在有message的时候，除了前面是个选项，其他情况下理论上都会调用一次SetName。所以向前搜索一下看看
                                for (var t = 1; t <= 2; t++)
                                {
                                    if (commands[i - 1 - t].op == 0x15)
                                    {
                                        sw.WriteLine(string.Format("☆{0:X8}☆{1}☆{2}\n★{0:X8}★{1}★{2}\n", commands[i].args[0].data, commands[i - 1 - t].args[0].data, commands[i].args[2].data));
                                        b = true;
                                        break;
                                    }
                                }
                                //如果真找不到那确实就有点离谱了，可能是没见过的模式，需要Decompile这个脚本然后找到那个位置看看
                                if(!b) throw new Exception("No name was set before message command.");
                            }
                            else
                            {
                                sw.WriteLine(string.Format("☆{0:X8}☆{1}☆{2}\n★{0:X8}★{1}★{2}\n", commands[i].args[0].data, commands[i - 1].args[0].data, commands[i].args[2].data));
                            }
                        }
                        
                    }
                    else if (commands[i].op == 0x0F)
                    {
                        if (commands[i].args[0].data is byte count)
                        {
                            for (var j = 0; j < count; ++j)
                            {
                                sw.WriteLine("☆{0:X8}☆选项{1}☆{2}\n★{0:X8}★选项{1}★{2}\n", commands[i].args[j * 5 + 1].data, j + 1, commands[i].args[j * 5 + 2].data);
                                if (commands[i].args[j * 5 + 2].data is string s)
                                {
                                    single_chars += s.Length;
                                }
                            }
                        }
                    }
                }
            }
            total_chars += single_chars;
            Console.WriteLine(string.Format("{0} :约{1}字", output[(output.LastIndexOf('\\') + 1)..], single_chars));
        }

        public void ImportStrings(string input)
        {
            List<string> text_strings = new List<string>();
            using (StreamReader sr = new StreamReader(input))
            {
                string? line;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line != null && line.StartsWith('★'))
                    {
                        text_strings.Add(line[10..]);
                    }
                }
            }
            var text_index = 0;
            for (var i = 0; i < commands.Count; ++i)
            {
                if (commands[i].op == 0x14)
                {
                    if (commands[i].args[2].data is string s)
                    {
                        if (s == "%P")
                        {
                            continue;
                        }
                    }
                    if (i == 0)
                    {
                        commands[i].args[2].data = text_strings[text_index][(text_strings[text_index].IndexOf('★') + 1)..];
                        text_index++;
                    }
                    else
                    {
                        if (commands[i - 1].op != 0x15)
                        {
                            bool b = false;
                            for (var t = 1; t <= 2; t++)
                            {
                                if (commands[i - 1 - t].op == 0x15)
                                {
                                    commands[i - 1 - t].args[0].data = text_strings[text_index][..text_strings[text_index].IndexOf('★')];
                                    commands[i].args[2].data = text_strings[text_index][(text_strings[text_index].IndexOf('★') + 1)..];
                                    text_index++;
                                    b = true;
                                    break;
                                }
                            }
                            if (!b) throw new Exception("No name was set before message command.");
                        }
                        else
                        {
                            commands[i - 1].args[0].data = text_strings[text_index][..text_strings[text_index].IndexOf('★')];
                            commands[i].args[2].data = text_strings[text_index][(text_strings[text_index].IndexOf('★') + 1)..];
                            text_index++;
                        }
                    }

                }
                else if (commands[i].op == 0x0F)
                {
                    if (commands[i].args[0].data is byte count)
                    {
                        for (var j = 0; j < count; ++j)
                        {
                            commands[i].args[j * 5 + 2].data = text_strings[text_index][(text_strings[text_index].IndexOf('★') + 1)..];
                            text_index++;
                        }
                    }
                }
            }
        }
    }

    public static class CLegacyFunctions
    {
        static readonly Encoding read_encoding = Encoding.GetEncoding("shift_jis");
        static readonly Encoding write_encoding = Encoding.GetEncoding("utf-8");
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
                args = ReadArgs(op, script, ref veip);
                name = GetFunctionName(op);
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
        
        public class Argstruc
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

        public static List<Arg> ReadArgs(byte command, byte[] script, ref int veip)
        {
            var args = func_args[command];
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
                        ret.Add(new Arg(arg, read_encoding.GetString(bin_arg)));
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
                    for(var i = 0; i < num; i++)
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
                        if(o.data is byte b) ret.Add(b);
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
                                switch(elm)
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
                        if(o.data is Command cmd)
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