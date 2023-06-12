using System.Text;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;
using ws2Parse.PE;

namespace ws2Parse
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //CLegacyFunctions.UpdateVM1993();
            //CLegacyFunctions.SaveArgTypes(@"E:\GalGames_Work\OnWork\个人研究向\懱尡斉杮懱\1993.txt");
            
            if (args.Length < 3)
            {
                Console.WriteLine("使用方法：ws2Parse.exe [AdvHD主程序路径] [存放ws2文件的文件夹路径] [功能（d：解包|r：封包）]");
                return;
            }

            CLegacyFunctions.SetArgTypes(ArgAnalyzer.ExtractArgTypes(args[0]));
            //CLegacyFunctions.SaveArgTypes(@"E:\GalGames_Work\OnWork\个人研究向\懱尡斉杮懱\1996.txt");
            switch(args[2])
            {
                case "d":
                    ExportAllStrings(args[1]);
                    break;
                case "p":
                    ReassembleAllScripts(args[1]);
                    break;
                case "dc":
                    DecompileScripts(args[1]);
                    break;
                default: 
                    Console.WriteLine("未知模式，请检查您的输入！");
                    break;
            }
            
            //ReAssemWithUtf8(args[0]);
            //ReassembleAllScripts(args[0]);
            //DecryptScripts(args[0]);
            //ws.Decompile(args[0]);
            //ws.ImportStrings(args[0]);
            //ws.Assem(args[0]);
            //WS2Script.Encrypt(ref script);
            //File.WriteAllBytes(BaseBame + ".ws2", script);
        }

        static void ReAssemWithUtf8(string folder)
        {
            WS2Script ws = new();
            ws.Load(@"start.ws2");
            //ws.LoadJson(@"start.json");
            ws.Assemble(@"start.ws2", true);



            foreach (string file in Directory.EnumerateFiles(folder, "*.ws2", SearchOption.TopDirectoryOnly))
            {
                ws.Load(file);
                ws.Assemble(file, true);
            }
        }


        static void ExportAllStrings(string folder, bool decrypt = false)
        {
            WS2Script ws = new();
            foreach (string file in Directory.EnumerateFiles(folder, "*.ws2", SearchOption.TopDirectoryOnly))
            {
                ws.Load(file, decrypt);
                ws.ExportStrings(file);
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(folder, "人名表.txt")))
            {
                sw.WriteLine(JsonConvert.SerializeObject(ws.char_names, Formatting.Indented));
                sw.Flush();
                sw.Close();
            }
            Console.WriteLine(string.Format("总字数统计:约{0}字", ws.total_chars));
        }

        static void ReassembleAllScripts(string folder, bool importstrings = true, bool decrypt = false, bool encrypt = false)
        {
            WS2Script ws = new();
            try
            {
                if(JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(folder, "人名表.txt"))) is Dictionary<string, string> dic) 
                {
                    ws.char_names = dic;
                }
                else
                {
                    throw new Exception("人名表反序列化错误。");
                }

            }catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("读取人名表的时候出错啦！检查一下吧");
                return;
            }

            foreach (string file in Directory.EnumerateFiles(folder, "*.ws2", SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($"Processing: {file}");
                ws.Load(file, decrypt);
                string txt_file = Path.ChangeExtension(file, "txt");
                if(File.Exists(txt_file) && importstrings)
                {
                    ws.ImportStrings(txt_file);
                }
                ws.Assemble(file, encrypt);
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
    
}