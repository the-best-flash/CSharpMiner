mkdir bin
rm bin/*
mcs -optimize- -debug+ -d:DEBUG -recurse:CSharpMiner/CSharpMiner/*.cs -out:CSharpMinerLib.dll -target:library -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Security,System.Runtime.Serialization,System.Xml,System.Xml.Linq
mcs -optimize- -debug+ -d:DEBUG -recurse:CSharpMiner/StratumPlugin/*.cs -out:bin/StratumPlugin.dll -target:library -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Runtime.Serialization,CSharpMinerLib.dll
mcs -optimize- -debug+ -d:DEBUG -recurse:CSharpMiner/ZeusMinerGen1Plugin/*.cs -out:bin/ZeusMinerGen1Plugin.dll -target:library -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Runtime.Serialization,CSharpMinerLib.dll
mcs -optimize- -debug+ -d:DEBUG -target:exe CSharpMiner/CSharpMinerProgram/Program.cs -out:CSharpMiner.exe -r:CSharpMinerLib.dll


