using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ws2Parse
{
    public class WS2Script
    {
        List<CLegacyFunctions.Command> commands;
        byte[] param;
        public int total_chars { get; private set; }
        public Dictionary<string, string> char_names { get; set; } = new Dictionary<string, string>();
        int single_chars;

        public WS2Script()
        {
            commands = new List<CLegacyFunctions.Command>();
            param = new byte[8];
            total_chars = 0;
            single_chars = 0;
        }

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
            if (decrypt) Decrypt(ref script);
            var veip = 0;
            while (veip < script.Length)
            {
                //var offset = veip;
                var command = script[veip++];
                if (command == 0xFF)
                {
                    //脚本末端
                    commands.Add(new CLegacyFunctions.Command(ref veip, command));
                    Debug.Assert((script.Length - veip) == 8);
                    Array.Copy(script, veip, param, 0, 8);//不知道这个是什么，暂时按原样复制过去
                    break;
                }
                commands.Add(new CLegacyFunctions.Command(ref veip, command, script));
            }
        }
        public void LoadJson(string BaseName, bool decrypt = false)
        {
            if (commands.Count != 0) commands.Clear();

            var obj = JsonConvert.DeserializeObject(File.ReadAllText(BaseName));
            if (obj is IList list)
            {
                foreach (var i in list)
                {
                    commands.Add((CLegacyFunctions.Command)i);
                }
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

        public void Assemble(string BaseName, bool encrypt = false)
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
                //ms.WriteByte(0xFF);
                ms.Write(param);

                //fix jumps (Notice that in the future version of AdvHD, you probably need to add more vm functions here to ensure offset capabilities)
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
                if (encrypt) Encrypt(ref bytes);
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
                            if (s == "%P")
                            {
                                continue;
                            }

                            if (i == 0)
                            {
                                sw.WriteLine(string.Format("☆{0:X8}☆☆{1}\n★{0:X8}★★{1}\n", commands[i].args[0].data, s));
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
                                            sw.WriteLine(string.Format("☆{0:X8}☆{1}☆{2}\n★{0:X8}★{1}★{2}\n", commands[i].args[0].data, commands[i - 1 - t].args[0].data, s));
                                            if (commands[i - 1 - t].args[0].data is string ch_name)
                                            {
                                                if (!char_names.TryGetValue(ch_name, out string? a))
                                                {
                                                    char_names.Add(ch_name, ch_name);
                                                }
                                            }

                                            b = true;
                                            break;
                                        }
                                    }
                                    //如果真找不到那确实就有点离谱了，可能是没见过的模式，需要Decompile这个脚本然后找到那个位置看看
                                    if (!b) throw new Exception("No name was set before message command.");
                                }
                                else
                                {
                                    sw.WriteLine(string.Format("☆{0:X8}☆{1}☆{2}\n★{0:X8}★{1}★{2}\n", commands[i].args[0].data, commands[i - 1].args[0].data, s));
                                    if (commands[i - 1].args[0].data is string ch_name)
                                    {
                                        if (!char_names.TryGetValue(ch_name, out string? a))
                                        {
                                            char_names.Add(ch_name, ch_name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (commands[i].op == 0x0F)
                    {
                        if (commands[i].args[0].data is byte count)
                        {
                            for (var j = 0; j < count; ++j)
                            {
                                if (commands[i].args[j * 5 + 2].data is string s)
                                {
                                    sw.WriteLine("☆{0:X8}☆选项{1}☆{2}\n★{0:X8}★选项{1}★{2}\n", commands[i].args[j * 5 + 1].data, j + 1, s);
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
                                    if (char_names.TryGetValue(text_strings[text_index][..text_strings[text_index].IndexOf('★')], out string? name_cn))
                                    {
                                        commands[i - 1 - t].args[0].data = name_cn;
                                    }
                                    else
                                    {
                                        commands[i - 1 - t].args[0].data = text_strings[text_index][..text_strings[text_index].IndexOf('★')];
                                    }

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
                            if (char_names.TryGetValue(text_strings[text_index][..text_strings[text_index].IndexOf('★')], out string? name_cn))
                            {
                                commands[i - 1].args[0].data = name_cn;
                            }
                            else
                            {
                                commands[i - 1].args[0].data = text_strings[text_index][..text_strings[text_index].IndexOf('★')];
                            }
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
}
