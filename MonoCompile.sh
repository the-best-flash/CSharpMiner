mcs -optimize+ -recurse:CSharpMiner/CSharpMiner/*.cs -out:CSharpMiner.dll -target:library -r:System,System.Core,System.Data,System.Data.DataSetExtensions,System.Security,System.Runtime.Serialization,System.Xml,System.Xml.Linq
mcs CSharpMiner/CSharpMinerProgram/Program.cs -out:CSharpMiner.exe -r:CSharpMiner

