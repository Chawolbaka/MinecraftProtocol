# PlayersMonitor
QAQ不要看这个项目的代码,写的太辣鸡了。 
这是我为了学习一下怎么使用git所以丢github上来试试看的(不过应该不会删除,感觉git好好用呀,可以随便删代码啦qwq)

### Building
[安装.Net Core SDK 2.1](https://www.microsoft.com/net/download/dotnet-core/2.1 "安装.Net Core SDK 2.1")

    git clone https://github.com/chawolbaka/PlayersMonitor.git
    cd MinecraftProtocol\PlayersMonitor
    sudo dotnet publish -c Release -r win-x86
编译好后你可以在: bin\Release\netcoreapp2.1\win-x86\publish 里找到"PlayersMonitor.exe"  
(其它文件无法删除,如果需要单文件编译请使用:https://github.com/dotnet/corert)

其它平台的编译请参考这个文档，将 -r 后面的参数改成对应平台的id.  
https://docs.microsoft.com/en-us/dotnet/core/rid-catalog  

## 抄袭项目
https://github.com/Nsiso/MinecraftOutClient  
https://gist.github.com/csh/2480d14fbbb33b4bbae3  
https://github.com/ORelio/Minecraft-Console-Client  
### 引用的开源库
https://www.newtonsoft.com  
http://dotnetzip.codeplex.com  
(数据包压缩部分我现在是直接复制了Minecraft-Console-Client这个项目里面的代码,不知道是不是上面那个)
### 参考资料
https://wiki.vg/Protocol  
https://github.com/bangbang93/minecraft-protocol  
