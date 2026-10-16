using System;
using System.Collections;
using System.IO;
using System.Reflection;
class HistoryStoreTest {
 static int Main(string[] args){if(args.Length==3&&args[0]=="--read"){var a=Assembly.LoadFrom(args[1]);var t=a.GetType("SelectionClipboard.LocalHistoryStore",true);var items=(IList)t.GetMethod("Load").Invoke(null,new object[]{args[2],false});return items.Count==2?0:8;}string folder=Path.Combine(Path.GetTempPath(),"clipboard-persistence-"+Guid.NewGuid().ToString("N"));try{
  var assembly=Assembly.LoadFrom(args[0]);var entryType=assembly.GetType("SelectionClipboard.Entry",true);var store=assembly.GetType("SelectionClipboard.LocalHistoryStore",true);
  var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(entryType));
  Func<string,bool,object> entry=(value,pin)=>{var item=Activator.CreateInstance(entryType,true);entryType.GetField("Text").SetValue(item,value);entryType.GetField("Pin").SetValue(item,pin);return item;};
  var save=store.GetMethod("Save");var load=store.GetMethod("Load");list.Add(entry("普通历史\r\n持久保存",false));list.Add(entry("置顶历史",true));save.Invoke(null,new object[]{folder,list});
  var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,"--read \""+args[0]+"\" \""+folder+"\""){UseShellExecute=false,CreateNoWindow=true});process.WaitForExit();if(process.ExitCode!=0)throw new Exception("cross process recovery failed");
  list.Insert(0,entry("更新前新增内容",false));save.Invoke(null,new object[]{folder,list});object[] input={folder,false};var read=(IList)load.Invoke(null,input);if(read.Count!=3||(bool)input[1])throw new Exception("normal restore");
  File.WriteAllBytes(Path.Combine(folder,"history.bin"),new byte[]{1,2,3});input=new object[]{folder,false};read=(IList)load.Invoke(null,input);if(!(bool)input[1]||read.Count!=2)throw new Exception("backup recovery");
  File.WriteAllBytes(Path.Combine(folder,"history.bin.bak"),new byte[]{9});bool refused=false;try{load.Invoke(null,new object[]{folder,false});}catch(TargetInvocationException){refused=true;}if(!refused||!File.Exists(Path.Combine(folder,"history.bin.bak")))throw new Exception("corruption protection");
  Console.WriteLine("PASS: ordinary/pinned persistence, cross-process restore, atomic backup, corruption recovery and preservation");return 0;
 }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}}
}
