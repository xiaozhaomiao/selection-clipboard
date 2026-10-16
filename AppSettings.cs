using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SelectionClipboard {
 class AppSettings {
  public bool AllApps=true;
  public string Names="chrome\r\nmsedge\r\nfirefox\r\nnotepad\r\nwps\r\nweixin\r\nwechat\r\ncodex";
  public static string Normalize(string name){name=name.Trim().ToLowerInvariant();return name.EndsWith(".exe")?name.Substring(0,name.Length-4):name;}
  public bool Allows(string name){return AllApps || Names.Split(new[]{'\r','\n',',',';','，','；'},StringSplitOptions.RemoveEmptyEntries).Any(n=>Normalize(n)==Normalize(name));}
  public void Save(string folder){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"apps.txt"),(AllApps?"all":"list")+"\n"+Names);}
  public static AppSettings Load(string folder){var s=new AppSettings();string path=Path.Combine(folder,"apps.txt");if(File.Exists(path)){string text=File.ReadAllText(path);int split=text.IndexOf('\n');if(split>=0){s.AllApps=text.Substring(0,split).Trim()=="all";s.Names=text.Substring(split+1);}}return s;}
 }
 class SettingsForm:Form {
  public SettingsForm(AppSettings settings){
   Text="自动复制 · 生效应用";Size=new Size(500,500);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Microsoft YaHei UI",9);TopMost=true;MinimizeBox=false;MaximizeBox=false;
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),ColumnCount=1,RowCount=7};
   layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
   var all=new RadioButton{Text="所有应用（默认）",Checked=settings.AllApps,AutoSize=true};var only=new RadioButton{Text="仅在下面指定的应用中自动复制",Checked=!settings.AllApps,AutoSize=true};
   var names=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=settings.Names,Enabled=!settings.AllApps};
   all.CheckedChanged+=(s,e)=>names.Enabled=!all.Checked;
   var help=new Label{Text="每行一个进程名，例如 chrome.exe、notepad.exe。\n可从正在运行的应用中选择后添加；设置只控制自动复制。",Dock=DockStyle.Fill};
   var running=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
   var processes=new SortedSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var p in Process.GetProcesses()){try{if(p.MainWindowHandle!=IntPtr.Zero && p.Id!=Process.GetCurrentProcess().Id)processes.Add(p.ProcessName+".exe");}catch{}finally{p.Dispose();}}foreach(var n in processes)running.Items.Add(n);if(running.Items.Count>0)running.SelectedIndex=0;
   var add=new Button{Text="添加选中的应用",AutoSize=true};add.Click+=(s,e)=>{if(running.SelectedItem!=null){names.AppendText((names.Text.EndsWith("\n")||names.Text.Length==0?"":"\r\n")+running.SelectedItem);only.Checked=true;}};
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var save=new Button{Text="保存",DialogResult=DialogResult.OK};var cancel=new Button{Text="取消",DialogResult=DialogResult.Cancel};buttons.Controls.Add(save);buttons.Controls.Add(cancel);save.Click+=(s,e)=>{if(only.Checked&&String.IsNullOrWhiteSpace(names.Text)){MessageBox.Show(this,"请至少添加一个应用。");DialogResult=DialogResult.None;return;}settings.AllApps=all.Checked;settings.Names=names.Text;};
   layout.Controls.Add(all);layout.Controls.Add(only);layout.Controls.Add(help);layout.Controls.Add(names);layout.Controls.Add(running);layout.Controls.Add(add);layout.Controls.Add(buttons);Controls.Add(layout);AcceptButton=save;CancelButton=cancel;
  }
 }
}
