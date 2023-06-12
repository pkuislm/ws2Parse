

# .ws2脚本工具

#### 注意：这个工具现在还没有写完！（虽然已经可以用来处理文本了）

目前它支持以下功能：

- 加密/解密 `.ws2` 文件 
- 导出所有对话相关指令的字符串参数（用于翻译）
- 将上面导出的字符串使用另一种编码导入回去（比如gbk或者utf-8，不过utf8编码支持需要稍微魔改一下游戏主程序才行） 
- 把 `.ws2` 文件拆成 `.json` 和`.txt`文件（类似于反编译，可以查看脚本里面到底是怎么样的命令）
- 自动从exe里提取出vm的参数列表数组用于提取文本/反编译（目前还只支持少数几个版本，因为每个版本的特征码都不一样，很烦）

使用方法（命令行）：

```
ws2Parse.exe [AdvHD的主程序的路径（不支持加壳的版本）] [存放ws2文件的文件夹路径] [功能（d：解包|r：封包）]
```

使用例：

```
ws2Parse.exe "E:\Game\AdvHD.exe" "E:\Game\Rio" "d"
```

---



# A tool for AdvHD's `.ws2` script

#### NOTE: This tool is not completed yet! 

Currently, it implements the following features: 

- Decrypt/Encrypt `.ws2` files 
- Export strings related to message command(for translation) 
- Import strings with a different encoding(gbk, utf-8) 
- Disassemble `.ws2` file into `.json` file 
- Automatically extract vm functions' arguments layout from exe(only support a few versions)

***

Notice that different versions of AdvHD are not the same in terms of instructions, for example, the newer version will likely add some instructions or modify some existing instructions, so it's necessary to figure out the difference between them. 

#### Some details about the `.ws2 ` 

The script basically consists of a list of opcodes and arguments like this: 

```
[opcode][args][opcode][args]...[end of script][extra params]
```

*The `end of script` is byte 0xFF, and `extra params` is two `Int32` (Currently I don't know what these two numbers mean at the moment, maybe they have something to do with the VM's stack size?) 

When the engine reads a certain opcode, it will find the number of parameters it has and their types according to the opcode, and then read these parameters in sequence. 

AdvHD has its own enum to distinguish different types of arguments, and it uses `VARIANTARG` to store them. 

For more details, you can see the source code.

