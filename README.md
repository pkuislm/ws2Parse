

# .ws2脚本工具

#### 注意：这个工具现在还没有写完！现在只支持1.9.9.10及某些更旧的版本（虽然已经可以用来处理文本了）

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

## 以下是对ws2进行的一点说明（进阶内容）

`ws2`是`AdvHD`引擎所使用的一种文件格式，内部存储的是`AdvHD`内置的`vm`能够解析并执行的字节码。

对于`AdvHD`来说，一条ws2指令有两个主要组成部分：

+ 指令：一字节大小，不同指令对应不同的功能，多种功能组合在一起组成了游戏内各种各样的演出效果
+ 参数：不定长，参数个数以及参数类型由指令所决定

在`AdvHD`中，指令的取值范围是0~255。但并不是每一个值都有其对应的指令。即便在最新版中，在0xA2以后也有很多空指令存在。体现在代码中，则如下面片段所示：

```c
void FillCLegacyFunctions(legacygame *this)
{
  result = memset(&this->CLegacyFuncs, 0, sizeof(this->CLegacyFuncs));//256*4
  this->CLegacyFuncs.handler[0] = sub_44FFC0;         // always return TRUE
  this->CLegacyFuncs.handler[1] = VM_ConditionalJump;
  this->CLegacyFuncs.handler[2] = VM_Jump;
  this->CLegacyFuncs.handler[4] = VM_CallFunction;    // call1(string script)
  this->CLegacyFuncs.handler[5] = VM_FunctionReturn;  // return
  this->CLegacyFuncs.handler[6] = VM_Jump;            // jump (inside of the script)
  this->CLegacyFuncs.handler[7] = VM_CallScript;      // noreturn_call(string script)
  this->CLegacyFuncs.handler[8] = sub_4505B0;         // post_message
  this->CLegacyFuncs.handler[9] = sub_450600;         // var_calc
  this->CLegacyFuncs.handler[0xA] = sub_4509F0;       // mod
  this->CLegacyFuncs.handler[0xF] = VM_Select;
  this->CLegacyFuncs.handler[0x11] = VM_Timer;
  this->CLegacyFuncs.handler[0x12] = VM_TimerState;
  this->CLegacyFuncs.handler[0x14] = VM_Mes;
  this->CLegacyFuncs.handler[0x15] = VM_MesName;
  this->CLegacyFuncs.handler[0x16] = VM_MesWinState;  // SetState(bool state) -> open/close
  this->CLegacyFuncs.handler[0x1A] = VM_CallLua;
  this->CLegacyFuncs.handler[0x1B] = VM_OpenConfig;
  this->CLegacyFuncs.handler[0x1D] = VM_ChangeMesWin;
  //...
  this->CLegacyFuncs.handler[0xFE] = sub_462050;
  this->CLegacyFuncs.handler[0xFF] = sub_462200;
}
```

如果要执行这些空指令，则会触发`空指针异常`。

`ws2`指令在被执行之前，必须要得到他的参数。一条`ws2`指令可以消耗一个或多个参数，无论这个指令使用了多少参数，`AdvHD`都会将他们存入`std::vector`中。由于每个参数都会使用`VARIANTARG`保存，即该`vector`的类型为`std::vector<VARIANTARG>`。

那么`AdvHD`是怎么知道某个指令到底有哪些参数、每个参数又是什么样的类型的呢？原来，`AdvHD`主程序内部硬编码了一个数组，这个数组存储着每个指令所对应的参数类型数组的指针。`AdvHD`会在这里用下标获得每个指令所需的参数。对应到代码中如下：

```c
byte a[] = { 0x1, 0x2, 0xFF };
byte b[] = { 0x1, 0x2, 0x03, 0xFF };
byte c[] = { 0x1, 0x2, 0x04, 0x05, 0xFF };
//...

static byte* arg_types[256] = { a, b, c };
```

`ws2`共有如下参数类型：

| 参数名       | 枚举值 | 等价类型       | 参数大小（字节） | 备注                                                         |
| ------------ | ------ | -------------- | ---------------- | ------------------------------------------------------------ |
| ARG_VT_UI1   | 0x00   | unsigned char  | 1                |                                                              |
| ARG_VT_I2    | 0x01   | short          | 2                |                                                              |
| ARG_VT_UI2   | 0x02   | unsigned short | 2                |                                                              |
| ARG_VT_INT   | 0x03   | int            | 4                |                                                              |
| ARG_VT_UI4   | 0x04   | unsigned int   | 4                |                                                              |
| ARG_VT_R4    | 0x05   | float          | 4                |                                                              |
| ARG_STR1     | 0x06   | char*          | 不定长           |                                                              |
| ARG_ARRAY    | 0x07   |                | 1                | 不单独出现，其在参数列表中的后一个参数类型代表该数组的元素类型 |
| ARG_PERIOD   | 0x08   | '\0'           | 1                | 不单独出现，必定在`ARG_STR1`或`ARG_STR2`或`ARG_STR3`的后面   |
| ARG_STR2     | 0x09   | char*          | 不定长           |                                                              |
| ARG_STR3     | 0a0A   | char*          | 不定长           |                                                              |
| ARG_CALLBACK | 0xFE   |                | 不定长           | 参数内容是一条ws2指令，通常用于选项跳转                      |
| ARG_END      | 0xFF   |                | 0                | 代表参数列表结尾，遇到该值时停止获取参数                     |

因为我们的最终目标是翻译游戏，因此我们将目光聚焦在这三个字符串类型上。通过对`AdvHD`的代码的分析我们可以知道，游戏内显示的文本其类型为`ARG_STR1`。我们找到处理该参数读入的代码处查看具体实现：

![image-20230613194524700](.\note_imgs\image-20230613194524700.png)

我们能看到三种类型的字符串都在使用同一个逻辑。点开`sub_4A8030`看看：

![image-20230613194548453](.\note_imgs\image-20230613194548453.png)

可以看到是这里的函数将`ANSI`编码的字符串转换成了`Unicode`字符串，也就是说`AdvHD`内部的编码是`Unicode`的。（新版本的`AdvHD`在这里的编码转换会写成0x3A4也就是CP932，但是除此之外，代码逻辑在各个版本中并没有太大变化。）

若要调整文本编码，主要有下面两种方式：

+ 直接暴力该值，把这里的值改成0x3A8（GBK）或者0xFDE9（UTF-8）。
+ 复制一份sub_4A8030，并对其副本进行改动，最后将字符串处理里面的编码转换函数改成这个副本

我个人不推荐第一种方案，因为不能保证这个编码转换函数只被这里调用。为保证其余功能的正常运行，我们不能随意更改原始函数。这里我推荐第二种方法。第二种方法实行起来有很多种方式，可以写hook，也可以直接复制粘贴汇编。我在这里就不多做赘述。

ps：还有一种更好的方案，就是仅对`case 6`做改动。我们知道`switch`用的是跳转表，我们仅需在其跳转表的位置做出改动即可调整这个`case`的行为。这样做虽然会稍微麻烦一些，但比起第二种方案要更加稳妥（因为没人知道`ARG_STR2`和`ARG_STR3`会不会传入多字节字符）

---



# A tool for AdvHD's `.ws2` script

#### NOTE: This tool is not completed yet!  It only supports AdvHD v1.9.9.10 and some older versions.

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

