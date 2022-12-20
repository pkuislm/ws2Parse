# A tool for AdvHD's `.ws2` script

#### NOTE: This tool is not completed yet! 

Currently, it implements the following features: 

- Decrypt/Encrypt `.ws2` files 
- Export strings related to message command(for translation) 
- Import strings with a different encoding(gbk, utf-8) 
- Disassemble `.ws2` file into `.json` file 

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

