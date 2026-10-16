using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace SelectionClipboard {
 class HistoryDocument {public int version=1;public List<Entry> items=new List<Entry>();}
 static class LocalHistoryStore {
  static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=50000000};}
  public static void Save(string folder,List<Entry> entries){
   Directory.CreateDirectory(folder);string path=Path.Combine(folder,"history.bin");string temp=path+".new";
   string json=Serializer().Serialize(new HistoryDocument{items=entries});
   byte[] protectedBytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(json),null,DataProtectionScope.CurrentUser);
   using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,8192,FileOptions.WriteThrough)){stream.Write(protectedBytes,0,protectedBytes.Length);stream.Flush(true);}
   if(File.Exists(path))File.Replace(temp,path,path+".bak",true);else File.Move(temp,path);
  }
  static List<Entry> Read(string path){
   byte[] encoded=File.ReadAllBytes(path);var doc=Serializer().Deserialize<HistoryDocument>(Encoding.UTF8.GetString(ProtectedData.Unprotect(encoded,null,DataProtectionScope.CurrentUser)));
   if(doc==null||doc.version!=1||doc.items==null)throw new InvalidDataException("历史文件格式不正确");
   var result=new List<Entry>();var seen=new HashSet<string>();int ordinary=0;
   foreach(var entry in doc.items){if(entry==null||String.IsNullOrWhiteSpace(entry.Text)||!seen.Add(entry.Text))continue;if(!entry.Pin&&++ordinary>100)continue;result.Add(entry);}return result;
  }
  public static List<Entry> Load(string folder,out bool recovered){
   recovered=false;string path=Path.Combine(folder,"history.bin");if(!File.Exists(path)&&!File.Exists(path+".bak"))return null;
   try{return Read(path);}catch{try{var entries=Read(path+".bak");recovered=true;return entries;}catch{throw new InvalidDataException("本地历史及备份未能读取，已保留原文件，暂停写入以防覆盖");}}
  }
 }
}
