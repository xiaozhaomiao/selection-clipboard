using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SelectionClipboard {
 static class Program {
  [STAThread] static void Main(string[] args) {
   if(args.Length==2&&args[0]=="--wait-for"){int pid;if(Int32.TryParse(args[1],out pid)){try{if(!Process.GetProcessById(pid).WaitForExit(15000))return;}catch(ArgumentException){}}} if(args.Length>0 && args[0]=="--self-test") { Environment.Exit(SelfTest.Run()); return; }
   bool created; using(var mutex=new Mutex(true,"Local\\SelectionClipboardPublicV1",out created)) {
    if(!created){Native.PostMessage((IntPtr)0xffff,Native.ShowMessage,IntPtr.Zero,IntPtr.Zero);return;}
    Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new ClipForm());
   }
  }
 }
 class Entry { public string Text; public bool Pin; public override string ToString(){return (Pin?"★ ":"")+Text.Replace("\r", " ").Replace("\n"," ↵ ");} }
 class History {
  public List<Entry> Items=new List<Entry>();
  public void Add(string value) { if(String.IsNullOrWhiteSpace(value))return; var old=Items.Find(x=>x.Text==value); if(old!=null)Items.Remove(old); Items.Insert(0,old??new Entry{Text=value}); int count=0; for(int i=0;i<Items.Count;i++){if(!Items[i].Pin && ++count>100)Items.RemoveAt(i--);} }
 }
 partial class ClipForm:Form {
  const int WM_CLIPBOARDUPDATE=0x31D, WM_HOTKEY=0x312; const uint MOD_CONTROL=2,MOD_ALT=1;
  bool historyWritable=true,lastSaveSucceeded=true; Panel historyPanel; FlowLayoutPanel actionPanel; ClipboardReplay replay=new ClipboardReplay(); AppSettings settings=new AppSettings(); History history=new History(); ListBox list=new ListBox(); TextBox search=new TextBox(); Label status=new Label(); CheckBox auto=new CheckBox(), capture=new CheckBox(); NotifyIcon tray; Form bubble; IntPtr hook,target,targetFocus,pendingTarget; bool pasteBusy; Native.HookProc callback; Point down,lastClick; DateTime lastUp; bool doubleClick; bool dragging,pending,ownCopy; DateTime due; uint sequence; System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
  string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SelectionClipboardPublic");
  public ClipForm(){
   try{settings=AppSettings.Load(folder);}catch{} Text="随选剪贴板 · 公开版 0.4.4";ControlBox=false;MinimizeBox=false;MaximizeBox=false; Size=new Size(430,540); MinimumSize=new Size(360,400); TopMost=true; StartPosition=FormStartPosition.Manual; Location=new Point(Screen.PrimaryScreen.WorkingArea.Right-450,100); Font=new Font("Microsoft YaHei UI",9); ShowInTaskbar=true;
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=130,Padding=new Padding(8)};
   auto.Text="选中自动复制";auto.Checked=true;auto.Width=140;capture.Text="记录剪贴板";capture.Checked=true;capture.Width=135; top.Controls.Add(auto);top.Controls.Add(capture);top.Controls.Add(new Label{Text="按住 Alt 跳过复制 · Ctrl+Alt+C 暂停/恢复",AutoSize=true}); AddButton(top,"生效应用…",()=>EditApps()); Controls.Add(top);
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=76,Padding=new Padding(6)};
   actionPanel=bottom;AddButton(bottom,"粘贴",()=>Paste()); AddButton(bottom,"仅复制",()=>CopySelected());AddButton(bottom,"置顶/取消",()=>{var e=list.SelectedItem as Entry;if(e!=null){e.Pin=!e.Pin;SavePins();RefreshList();}});AddButton(bottom,"删除",()=>{var e=list.SelectedItem as Entry;if(e!=null){history.Items.Remove(e);SavePins();RefreshList();}});AddButton(bottom,"清空未置顶",()=>{history.Items.RemoveAll(e=>!e.Pin);SaveHistory();RefreshList();});AddButton(bottom,"收起",()=>Hide());Controls.Add(bottom);
   status.Dock=DockStyle.Bottom;status.Height=30;status.Text="先点击目标输入位置，再展开悬浮窗选择内容";Controls.Add(status);
   var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(10)};search.Dock=DockStyle.Top;search.Height=28; search.TextChanged+=(s,e)=>RefreshList(); list.Dock=DockStyle.Fill;list.HorizontalScrollbar=true;list.IntegralHeight=false;list.DoubleClick+=(s,e)=>{if(list.IndexFromPoint(list.PointToClient(Cursor.Position))>=0)Paste();};list.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Paste();e.Handled=true;}};panel.Controls.Add(list);panel.Controls.Add(search);Controls.Add(panel);panel.BringToFront();historyPanel=panel;
   tray=new NotifyIcon{Icon=SystemIcons.Application,Text="随选剪贴板",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("打开剪贴板",null,(s,e)=>ShowPanel());menu.Items.Add("暂停/恢复自动复制",null,(s,e)=>Toggle());menu.Items.Add("生效应用…",null,(s,e)=>EditApps());menu.Items.Add("使用说明",null,(s,e)=>ShowUsage(false));menu.Items.Add("退出",null,(s,e)=>Application.Exit());tray.ContextMenuStrip=menu;tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)ShowPanel();};
   bubble=new Bubble(()=>TogglePanel());bubble.Show();LoadHistory();RefreshList(); callback=MouseHook; hook=Native.SetWindowsHookEx(14,callback,Native.GetModuleHandle(null),0);if(hook==IntPtr.Zero){auto.Checked=false;status.Text="鼠标监听启动失败，仍可使用剪贴板历史";}
   Native.AddClipboardFormatListener(Handle); Native.RegisterHotKey(Handle,1,MOD_CONTROL|MOD_ALT,(uint)Keys.C);Native.RegisterHotKey(Handle,2,MOD_CONTROL|MOD_ALT,(uint)Keys.V);
   timer.Interval=40;timer.Tick+=(s,e)=>Tick();timer.Start();Shown+=(s,e)=>ShowUsage(true);FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
  }
  void EditApps(){using(var dialog=new SettingsForm(settings)){if(dialog.ShowDialog(this)==DialogResult.OK){try{settings.Save(folder);status.Text=settings.AllApps?"自动复制范围：所有应用":"自动复制范围：指定应用";}catch{status.Text="设置保存失败，本次运行仍然生效";}}}} void AddButton(Control parent,string title,Action action){var b=new Button{Text=title,AutoSize=true};b.Click+=(s,e)=>action();parent.Controls.Add(b);}
  protected override void SetVisibleCore(bool value){base.SetVisibleCore(value);}
  void Toggle(){auto.Checked=!auto.Checked;tray.Text=auto.Checked?"随选剪贴板 · 自动复制开启":"随选剪贴板 · 自动复制暂停";status.Text=tray.Text;}
  void TogglePanel(){if(Visible&&WindowState!=FormWindowState.Minimized)Hide();else ShowPanel();} protected override bool ShowWithoutActivation{get{return true;}} void RememberTarget(IntPtr w){if(!IsExternal(w))return;target=w;uint pid;uint tid=Native.GetWindowThreadProcessId(w,out pid);var info=new Native.GuiThreadInfo();info.cbSize=Marshal.SizeOf(info);if(Native.GetGUIThreadInfo(tid,ref info))targetFocus=info.hwndFocus;else targetFocus=IntPtr.Zero;} void ShowPanel(){RememberTarget(Native.GetForegroundWindow());WindowState=FormWindowState.Normal;Show();RefreshList();}
  bool IsExternal(IntPtr w){if(w==IntPtr.Zero)return false;uint id;Native.GetWindowThreadProcessId(w,out id);return id!=(uint)Process.GetCurrentProcess().Id;}
  bool Allowed(IntPtr w){try{uint id;Native.GetWindowThreadProcessId(w,out id);string n=Process.GetProcessById((int)id).ProcessName.ToLowerInvariant();return IsExternal(w)&&settings.Allows(n);}catch{return false;}}
  IntPtr MouseHook(int code,IntPtr msg,IntPtr data){if(code>=0){try{var m=(Native.MouseData)Marshal.PtrToStructure(data,typeof(Native.MouseData));if(msg.ToInt32()==0x201){doubleClick=(DateTime.UtcNow-lastUp).TotalMilliseconds<=SystemInformation.DoubleClickTime&&Math.Abs(m.pt.X-lastClick.X)<=SystemInformation.DoubleClickSize.Width&&Math.Abs(m.pt.Y-lastClick.Y)<=SystemInformation.DoubleClickSize.Height;down=m.pt;dragging=true;pending=false;}if(msg.ToInt32()==0x202&&dragging){dragging=false;lastUp=DateTime.UtcNow;lastClick=m.pt;var w=Native.GetForegroundWindow();if(auto.Checked&&Allowed(w)&&!AltDown()&&(doubleClick||Math.Abs(m.pt.X-down.X)>5||Math.Abs(m.pt.Y-down.Y)>5)){target=w;pendingTarget=w;pending=true;due=DateTime.UtcNow.AddMilliseconds(140);}}}catch{}}return Native.CallNextHookEx(hook,code,msg,data);}
  bool AltDown(){return (Native.GetAsyncKeyState(0x12)&0x8000)!=0;}
  void Tick(){var w=Native.GetForegroundWindow();if(IsExternal(w))RememberTarget(w);if(!pending||DateTime.UtcNow<due)return;pending=false;if(!auto.Checked||AltDown()||(Native.GetAsyncKeyState(0x11)&0x8000)!=0||(Native.GetAsyncKeyState(0x10)&0x8000)!=0||w!=pendingTarget||!Allowed(w))return;sequence=Native.GetClipboardSequenceNumber();ownCopy=true;SendKeys.SendWait("^c");}
  protected override void WndProc(ref Message m){if(m.Msg==0x21&&!search.RectangleToScreen(search.ClientRectangle).Contains(Cursor.Position)){m.Result=(IntPtr)3;return;}if(m.Msg==Native.ShowMessage){ShowPanel();}if(m.Msg==WM_HOTKEY){if(m.WParam.ToInt32()==1)Toggle();else ShowPanel();}if(m.Msg==WM_CLIPBOARDUPDATE){if(ownCopy){ownCopy=false;if(sequence==Native.GetClipboardSequenceNumber()){base.WndProc(ref m);return;}}if(capture.Checked)ReadClipboard();}base.WndProc(ref m);}
  void ReadClipboard(){try{if(Clipboard.ContainsText()){string text=Clipboard.GetText();if(replay.IsInternal(Native.GetClipboardSequenceNumber(),text))return;if(text.Length>1000000){status.Text="内容过大，未加入历史";return;}history.Add(text);SaveHistory();RefreshList();}}catch(ExternalException){status.Text="剪贴板正忙，本次未记录";}}
  void RefreshList(){var selected=list.SelectedItem as Entry;list.BeginUpdate();list.Items.Clear();string q=search.Text;foreach(var e in history.Items.FindAll(x=>x.Pin))if(e.Text.IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0)list.Items.Add(e);foreach(var e in history.Items.FindAll(x=>!x.Pin))if(e.Text.IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0)list.Items.Add(e);if(selected!=null)list.SelectedItem=selected;list.EndUpdate();}
  bool Put(string text){try{if(Clipboard.ContainsText()&&Clipboard.GetText()==text)return true;replay.Begin(text);Clipboard.SetText(text);replay.Commit(Native.GetClipboardSequenceNumber());return true;}catch(ExternalException){replay.Cancel();status.Text="剪贴板正忙，请重试";return false;}catch{replay.Cancel();throw;}}
  void CopySelected(){var e=list.SelectedItem as Entry;if(e!=null&&Put(e.Text))status.Text="已复制，可在目标窗口粘贴";}
  void Paste(){
   if(pasteBusy)return;var entry=list.SelectedItem as Entry;if(entry==null)return;
   if(target==IntPtr.Zero||!Native.IsWindow(target)){status.Text="请先点击目标输入框，再选择历史内容";return;}
   var dest=target;var focus=targetFocus;if(!Put(entry.Text))return;
   pasteBusy=true;Native.SetForegroundWindow(dest);
   var pasteTimer=new System.Windows.Forms.Timer{Interval=180};
   pasteTimer.Tick+=(s,a)=>{pasteTimer.Stop();pasteTimer.Dispose();try{
    if(Native.GetForegroundWindow()!=dest){status.Text="未能切回输入窗口，已复制，可手动粘贴";return;}
    if(focus!=IntPtr.Zero&&Native.IsWindow(focus)&&Native.GetAncestor(focus,2)==dest){
     uint pid;uint tid=Native.GetWindowThreadProcessId(focus,out pid);uint current=Native.GetCurrentThreadId();
     bool attached=tid!=current&&Native.AttachThreadInput(current,tid,true);
     try{if(attached||tid==current)Native.SetFocus(focus);}finally{if(attached)Native.AttachThreadInput(current,tid,false);}
    }
    SendKeys.SendWait("^v");status.Text="已发送粘贴，历史面板保持展开";
   }catch{status.Text="自动粘贴未完成，内容已复制";}finally{pasteBusy=false;}};pasteTimer.Start();
  }
  void SaveHistory(){if(!historyWritable)return;try{LocalHistoryStore.Save(folder,history.Items);lastSaveSucceeded=true;}catch{lastSaveSucceeded=false;status.Text="本地保存失败，请勿退出，检查磁盘和目录权限";}}
  void LoadHistory(){try{bool recovered;var saved=LocalHistoryStore.Load(folder,out recovered);if(saved!=null){history.Items=saved;if(recovered)status.Text="已从本地备份恢复历史";return;}LoadPins();SaveHistory();}catch(Exception ex){historyWritable=false;status.Text=ex.Message;}}
  void SavePins(){SaveHistory();}
  void LoadPins(){try{var path=Path.Combine(folder,"pins.txt");if(File.Exists(path))foreach(var line in File.ReadAllLines(path)){var value=Encoding.UTF8.GetString(Convert.FromBase64String(line));history.Add(value);history.Items[0].Pin=true;}}catch{status.Text="旧版置顶文件未能完全读取，原文件保留";}}
  protected override void Dispose(bool disposing){if(disposing){SaveHistory();timer.Stop();if(hook!=IntPtr.Zero)Native.UnhookWindowsHookEx(hook);Native.RemoveClipboardFormatListener(Handle);Native.UnregisterHotKey(Handle,1);Native.UnregisterHotKey(Handle,2);tray.Dispose();bubble.Dispose();timer.Dispose();}base.Dispose(disposing);}
 }
 class Bubble:Form {
  Point start,origin;bool moved;public Bubble(Action open){Text="打开复制历史";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;Size=new Size(82,48);BackColor=Color.FromArgb(42,91,160);var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-98,area.Top+area.Height/2);var label=new Label{Text="复制历史",ForeColor=Color.White,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand};Controls.Add(label);var tip=new ToolTip();tip.SetToolTip(label,"单击展开或收起复制历史，拖动调整位置。双击历史条目粘贴，面板保持展开。");label.Disposed+=(s,e)=>tip.Dispose();label.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;start=Cursor.Position;origin=Location;moved=false;};label.MouseMove+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;var p=Cursor.Position;int dx=p.X-start.X,dy=p.Y-start.Y;if(Math.Abs(dx)+Math.Abs(dy)>4)moved=true;if(moved)Location=new Point(origin.X+dx,origin.Y+dy);};label.MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Left&&!moved)open();};}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 }
 static class Native {
  public static readonly int ShowMessage=RegisterWindowMessage("SelectionClipboard.ShowPanel.V1"); [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int RegisterWindowMessage(string name); [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr w,int msg,IntPtr wp,IntPtr lp); [StructLayout(LayoutKind.Sequential)]public struct GuiThreadInfo{public int cbSize;public uint flags;public IntPtr hwndActive,hwndFocus,hwndCapture,hwndMenuOwner,hwndMoveSize,hwndCaret;public int left,top,right,bottom;} [DllImport("user32.dll")]public static extern bool GetGUIThreadInfo(uint tid,ref GuiThreadInfo info); [DllImport("user32.dll")]public static extern bool AttachThreadInput(uint from,uint to,bool attach); [DllImport("user32.dll")]public static extern IntPtr SetFocus(IntPtr w); [DllImport("user32.dll")]public static extern IntPtr GetAncestor(IntPtr w,uint flags); [DllImport("kernel32.dll")]public static extern uint GetCurrentThreadId(); public delegate IntPtr HookProc(int code,IntPtr w,IntPtr l);
  [StructLayout(LayoutKind.Sequential)]public struct MouseData{public Point pt;public uint mouseData,flags,time;public UIntPtr extra;}
  [DllImport("user32.dll")]public static extern IntPtr SetWindowsHookEx(int id,HookProc proc,IntPtr module,uint thread);
  [DllImport("user32.dll")]public static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
  [DllImport("kernel32.dll",CharSet=CharSet.Auto)]public static extern IntPtr GetModuleHandle(string name);
  [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr w,out uint id);
  [DllImport("user32.dll")]public static extern short GetAsyncKeyState(int key);
  [DllImport("user32.dll")]public static extern bool AddClipboardFormatListener(IntPtr w);
  [DllImport("user32.dll")]public static extern bool RemoveClipboardFormatListener(IntPtr w);
  [DllImport("user32.dll")]public static extern uint GetClipboardSequenceNumber();
  [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr w);
  [DllImport("user32.dll")]public static extern bool IsWindow(IntPtr w);
  [DllImport("user32.dll")]public static extern bool RegisterHotKey(IntPtr w,int id,uint modifiers,uint key);
  [DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr w,int id);
 }
 static class SelfTest {public static int Run(){if(!ReplayRegression.Run())return 7;var settings=new AppSettings();if(!settings.Allows("notepad"))return 5;settings.AllApps=false;settings.Names=" Chrome.exe\r\nnotepad";if(!settings.Allows("CHROME")||!settings.Allows("notepad.exe")||settings.Allows("codex"))return 6;var h=new History();h.Add("A");h.Items[0].Pin=true;for(int i=0;i<150;i++)h.Add("item"+i);if(h.Items.Count!=101||!h.Items.Exists(x=>x.Text=="A"&&x.Pin))return 1;h.Add("item149");if(h.Items.Count!=101||h.Items[0].Text!="item149")return 2;h.Add("A");if(!h.Items[0].Pin)return 3;h.Add(" ");if(h.Items.Count!=101)return 4;return 0;}}
}
