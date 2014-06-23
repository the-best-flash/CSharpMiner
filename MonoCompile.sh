mcs -optimize+ -recurse:CSharpMiner/CSharpMiner/*.cs -out:CSharpMinerLib.dll -target:library -reference:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Security,System.Runtime.Serialization,System.Xml,System.Xml.Linq
mcs -target:exe CSharpMiner/CSharpMinerProgram/Program.cs -out:CSharpMiner.exe -r:CSharpMinerLib.dll


