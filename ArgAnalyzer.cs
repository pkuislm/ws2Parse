using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ws2Parse
{
    using System;
    using System.IO;
    using System.Data;
    using System.Collections;
    using System.Security.Cryptography;
    using ws2Parse.PE;
    using static ws2Parse.CLegacyFunctions;

    namespace PE
    {
        internal sealed class PeInfo
        {
            private DosHeader _DosHeader;
            private DosStub _DosStub;
            private PEHeader _PEHeader;
            private OptionalHeader _OptionalHeader;
            private OptionalDirAttrib _OptionalDirAttrib;
            private SectionTable _SectionTable;
            private byte[] _MountInfo;

            public PeInfo(string FileName)
            {
                using (FileStream fs = new FileStream(FileName, FileMode.Open))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    _DosHeader = new(br);
                    int StubSize = (int)(Convert.ToInt32(_DosHeader.e_PESTART) - br.BaseStream.Position);   //获得Stub的大小
                    _DosStub = new(br, StubSize);
                    _PEHeader = new(br);
                    _OptionalHeader = new(br);
                    _OptionalDirAttrib = new(br, _OptionalHeader.DirectCount);
                    _SectionTable = new(br, _PEHeader.NumberOfSections);

                    int MaxSize = 0;
                    foreach (var v in _SectionTable.Sections)
                    {
                        if (v.VirtualAddress + v.SizeOfRawData > MaxSize)
                        {
                            MaxSize = v.VirtualAddress + v.SizeOfRawData;
                        }
                    }
                    _MountInfo = new byte[MaxSize];

                    foreach (var sec in _SectionTable.Sections)
                    {
                        br.BaseStream.Position = sec.PointerToRawData;
                        br.Read(_MountInfo, sec.VirtualAddress, sec.SizeOfRawData);
                    }
                }
            }

            public int PatternSearch(byte[] bPattern)
            {
                int offset = 0;
                while(offset < _MountInfo.Length - bPattern.Length)
                {
                    bool found = true;

                    for (int i = 0; i < bPattern.Length; i++)
                    {
                        if (offset == _MountInfo.Length - i)
                        {
                            found = false;
                            break;
                        }

                        if (bPattern[i] != 0x2A && bPattern[i] != _MountInfo[offset + i])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return offset;
                    }

                    offset++;
                }
                return -1;
            }

            public int GetRVA32(int offset)
            {
                var addr = BitConverter.ToInt32(_MountInfo, offset);
                if (addr == 0)
                    return 0;
                return addr - 0x400000;
            }

            public int GetInt32(int offset)
            {
                return BitConverter.ToInt32(_MountInfo, offset);
            }

            public byte GetByte(int offset)
            {
                return _MountInfo[offset];
            }

            private class DosHeader
            {
                public byte[] e_magic; // 魔术数字
                public byte[] e_cblp;  // 文件最后页的字节数
                public byte[] e_cp;    // 文件页数
                public byte[] e_crlc; // 重定义元素个数
                public byte[] e_cparhdr; // 头部尺寸，以段落为单位
                public byte[] e_minalloc; // 所需的最小附加段
                public byte[] e_maxalloc; // 所需的最大附加段
                public byte[] e_ss; // 初始的SS值（相对偏移量）
                public byte[] e_sp; // 初始的SP值
                public byte[] e_csum; // 校验和
                public byte[] e_ip; // 初始的IP值
                public byte[] e_cs; // 初始的CS值（相对偏移量）
                public byte[] e_rva;
                public byte[] e_fg;
                public byte[] e_bl1;
                public byte[] e_oemid;
                public byte[] e_oeminfo;
                public byte[] e_bl2;
                public short e_PESTART; //PE开始 +自己的位置 


                public long FileStarIndex = 0;
                public long FileEndIndex = 0;

                public DosHeader(BinaryReader br)
                {
                    FileStarIndex = br.BaseStream.Position;
                    e_magic = br.ReadBytes(2);
                    e_cblp = br.ReadBytes(2);
                    e_cp = br.ReadBytes(2);
                    e_crlc = br.ReadBytes(2);
                    e_cparhdr = br.ReadBytes(2);
                    e_minalloc = br.ReadBytes(2);
                    e_maxalloc = br.ReadBytes(2);
                    e_ss = br.ReadBytes(2);
                    e_sp = br.ReadBytes(2);
                    e_csum = br.ReadBytes(2);
                    e_ip = br.ReadBytes(2);
                    e_cs = br.ReadBytes(2);
                    e_rva = br.ReadBytes(2);
                    e_fg = br.ReadBytes(2);
                    e_bl1 = br.ReadBytes(8);
                    e_oemid = br.ReadBytes(2);
                    e_oeminfo = br.ReadBytes(2);
                    e_bl2 = br.ReadBytes(20);
                    e_PESTART = br.ReadInt16();
                    FileEndIndex = br.BaseStream.Position;
                }
            }

            private class DosStub
            {
                public byte[] DosStubData;
                public long FileStarIndex = 0;
                public long FileEndIndex = 0;


                public DosStub(BinaryReader br, int StubSize)
                {
                    FileStarIndex = br.BaseStream.Position;
                    DosStubData = br.ReadBytes(StubSize);
                    FileEndIndex = br.BaseStream.Position;
                }
            }

            private class PEHeader
            {
                public byte[] Header;  //PE文件标记
                public byte[] Machine;//该文件运行所要求的CPU。对于Intel平台，该值是IMAGE_FILE_MACHINE_I386 (14Ch)。我们尝试了LUEVELSMEYER的pe.txt声明的14Dh和14Eh，但Windows不能正确执行。看起来，除了禁止程序执行之外，本域对我们来说用处不大。
                public short NumberOfSections;//文件的节数目。如果我们要在文件中增加或删除一个节，就需要修改这个值。
                public byte[] TimeDateStamp;//文件创建日期和时间。我们不感兴趣。
                public byte[] PointerToSymbolTable;//用于调试。
                public byte[] NumberOfSymbols;//用于调试。
                public byte[] SizeOfOptionalHeader;//指示紧随本结构之后的 OptionalHeader 结构大小，必须为有效值。
                public byte[] Characteristics;//关于文件信息的标记，比如文件是exe还是dll。

                public long FileStarIndex = 0;
                public long FileEndIndex = 0;

                public PEHeader(BinaryReader br)
                {
                    FileStarIndex = br.BaseStream.Position;
                    Header = br.ReadBytes(4);
                    Machine = br.ReadBytes(2);
                    NumberOfSections = br.ReadInt16();
                    TimeDateStamp = br.ReadBytes(4);
                    PointerToSymbolTable = br.ReadBytes(4);
                    NumberOfSymbols = br.ReadBytes(4);
                    SizeOfOptionalHeader = br.ReadBytes(2);
                    Characteristics = br.ReadBytes(2);
                    FileEndIndex = br.BaseStream.Position;
                }
            }

            private class OptionalHeader
            {
                public byte[] Magic; //Magic 010B=普通可以执行，0107=ROM映像
                public byte[] MajorLinkerVersion; //主版本号
                public byte[] MinorLinkerVersion; //副版本号
                public byte[] SizeOfCode; //代码段大小
                public byte[] SizeOfInitializedData; //已初始化数据大小
                public byte[] SizeOfUninitializedData; //未初始化数据大小
                public byte[] AddressOfEntryPoint; //执行将从这里开始（RVA）
                public byte[] BaseOfCode; //代码基址（RVA）
                public byte[] ImageBase; //数据基址（RVA）
                public byte[] ImageFileCode; //映象文件基址
                public byte[] SectionAlign; //区段列队
                public byte[] FileAlign; //文件列队

                public byte[] MajorOSV; //操作系统主版本号
                public byte[] MinorOSV; //操作系统副版本号
                public byte[] MajorImageVer; //映象文件主版本号
                public byte[] MinorImageVer; //映象文件副版本号
                public byte[] MajorSV; //子操作系统主版本号
                public byte[] MinorSV; //子操作系统副版本号
                public byte[] UNKNOW; //Win32版本值
                public byte[] SizeOfImage; //映象文件大小
                public byte[] SizeOfHeards; //标志头大小
                public byte[] CheckSum; //文件效验
                public byte[] Subsystem;//子系统（映象文件）1本地 2WINDOWS-GUI 3WINDOWS-CUI 4 POSIX-CUI
                public byte[] DLL_Characteristics;//DLL标记
                public byte[] Bsize; //保留栈的大小
                public byte[] TimeBsize; //初始时指定栈大小
                public byte[] AucBsize; //保留堆的大小
                public byte[] SizeOfBsize; //初始时指定堆大小
                public byte[] FuckBsize; //加载器标志
                public int DirectCount; //数据目录数

                public long FileStarIndex = 0;
                public long FileEndIndex = 0;

                public OptionalHeader(BinaryReader br)
                {
                    FileStarIndex = br.BaseStream.Position;
                    Magic = br.ReadBytes(2);
                    MajorLinkerVersion = br.ReadBytes(1);
                    MinorLinkerVersion = br.ReadBytes(1);
                    SizeOfCode = br.ReadBytes(4);
                    SizeOfInitializedData = br.ReadBytes(4);
                    SizeOfUninitializedData = br.ReadBytes(4);
                    AddressOfEntryPoint = br.ReadBytes(4);
                    BaseOfCode = br.ReadBytes(4);
                    ImageBase = br.ReadBytes(4);
                    ImageFileCode = br.ReadBytes(4);
                    SectionAlign = br.ReadBytes(4);
                    FileAlign = br.ReadBytes(4);

                    MajorOSV = br.ReadBytes(2);
                    MinorOSV = br.ReadBytes(2);
                    MajorImageVer = br.ReadBytes(2);
                    MinorImageVer = br.ReadBytes(2);
                    MajorSV = br.ReadBytes(2);
                    MinorSV = br.ReadBytes(2);
                    UNKNOW = br.ReadBytes(4);
                    SizeOfImage = br.ReadBytes(4);
                    SizeOfHeards = br.ReadBytes(4);
                    CheckSum = br.ReadBytes(4);
                    Subsystem = br.ReadBytes(2);
                    DLL_Characteristics = br.ReadBytes(2);
                    Bsize = br.ReadBytes(4);
                    TimeBsize = br.ReadBytes(4);
                    AucBsize = br.ReadBytes(4);
                    SizeOfBsize = br.ReadBytes(4);
                    FuckBsize = br.ReadBytes(4);
                    DirectCount = br.ReadInt32();

                    FileEndIndex = br.BaseStream.Position;
                }
            }

            private class OptionalDirAttrib
            {
                public class DirAttrib
                {
                    public int DirRva;   //地址
                    public int DirSize;  //大小

                    public DirAttrib(BinaryReader br)
                    {
                        DirRva = br.ReadInt32();
                        DirSize = br.ReadInt32();
                    }
                }

                public List<DirAttrib> DirByte;



                public long FileStarIndex = 0;
                public long FileEndIndex = 0;

                public OptionalDirAttrib(BinaryReader br, int count)
                {
                    FileStarIndex = br.BaseStream.Position;

                    DirByte = new();
                    for (int i = 0; i < count; i++)
                    {
                        DirByte.Add(new DirAttrib(br));
                    }

                    FileEndIndex = br.BaseStream.Position;
                }
            }

            private class SectionTable
            {
                public class SectionData
                {
                    public string SectName;   //名字
                    public int PhysicalAddress; //虚拟内存地址
                    public int VirtualAddress; //RVA偏移
                    public int SizeOfRawData; //RVA大小
                    public int PointerToRawData; //指向RAW数据
                    public int PointerToRelocations; //指向定位号
                    public int PointerToLinenumbers; //指向行数
                    public short NumberOfRelocations; //定位号
                    public short NumberOfLinenumbers; //行数号
                    public int Characteristics; //区段标记

                    public SectionData(BinaryReader br)
                    {
                        SectName = Encoding.ASCII.GetString(br.ReadBytes(8));
                        PhysicalAddress = br.ReadInt32();
                        VirtualAddress = br.ReadInt32();
                        SizeOfRawData = br.ReadInt32();
                        PointerToRawData = br.ReadInt32();
                        PointerToRelocations = br.ReadInt32();
                        PointerToLinenumbers = br.ReadInt32();
                        NumberOfRelocations = br.ReadInt16();
                        NumberOfLinenumbers = br.ReadInt16();
                        Characteristics = br.ReadInt32();
                    }
                }
                public List<SectionData> Sections;

                public long FileStarIndex = 0;
                public long FileEndIndex = 0;

                public SectionTable(BinaryReader br, int count)
                {
                    FileStarIndex = br.BaseStream.Position;
                    Sections = new();
                    for (int i = 0; i < count; i++)
                    {
                        Sections.Add(new SectionData(br));
                    }
                    FileEndIndex = br.BaseStream.Position;
                }
            }
        }

        public static class ArgAnalyzer
        {
            private static int GetOffset(PeInfo info)
            {
                int offset;
                //1056&1210
                offset = info.PatternSearch(new byte[] { 0x8B, 0x2C, 0x85, 0x2A, 0x2A, 0x2A, 0x2A, 0x85, 0xED });
                if (offset != -1)
                    return offset + 3;
                //1994
                offset = info.PatternSearch(new byte[] { 0x8B, 0x1C, 0x85, 0x2A, 0x2A, 0x2A, 0x2A, 0x85, 0xDB, 0x75, 0x1F });
                if (offset != -1)
                    return offset + 3;
                //1996
                offset = info.PatternSearch(new byte[] { 0x8B, 0x04, 0x85, 0x2A, 0x2A, 0x2A, 0x2A, 0x89, 0x45, 0xE4 });
                if (offset != -1)
                    return offset + 3;
                //1999
                offset = info.PatternSearch(new byte[] { 0x8B, 0x0C, 0x8D, 0x2A, 0x2A, 0x2A, 0x2A, 0x89, 0x4D, 0xDC, 0x85, 0xC9 });
                if (offset != -1)
                    return offset + 3;
                //19910
                offset = info.PatternSearch(new byte[] { 0x8B, 0x04, 0x85, 0x2A, 0x2A, 0x2A, 0x2A, 0x89, 0x45, 0xF0, 0x85, 0xC0 });
                if (offset != -1)
                    return offset + 3;
                return 0;
            }

            public static ArgTypes[][] ExtractArgTypes(string exePath)
            {
                PeInfo pf = new(exePath);
                int offset = GetOffset(pf);
                if (offset == 0)
                    return Array.Empty<ArgTypes[]>();

                int val = pf.GetRVA32(offset);

                List<ArgTypes[]> argTypes = new List<ArgTypes[]>();
                int curOffset;
                for(int i = 0; i<256; i++)
                {
                    curOffset = pf.GetRVA32(val);

                    if (curOffset != 0)
                    {
                        List<ArgTypes> typeList = new List<ArgTypes>();
                        bool isArgEnd;

                        do{
                            var type = (ArgTypes)pf.GetByte(curOffset);
                            curOffset++;
                            typeList.Add(type);
                            isArgEnd = type == ArgTypes.ARG_END;
                        }
                        while(!isArgEnd);

                        argTypes.Add(typeList.ToArray());
                    }
                    else
                    {
                        argTypes.Add(Array.Empty<ArgTypes>());
                    }

                    val += 4;
                }

                return argTypes.ToArray();
            }
        }
    }

}