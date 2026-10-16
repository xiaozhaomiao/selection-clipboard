using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SelectionClipboard {
 class BubblePreferences {
  public bool hasPosition; public int x,y; public int transparency;
  public static BubblePreferences Load(string folder){try{string path=Path.Combine(folder,"bubble.json");if(File.Exists(path)){var value=new JavaScriptSerializer().Deserialize<BubblePreferences>(File.ReadAllText(path));if(value!=null){value.transparency=Math.Max(0,Math.Min(80,value.transparency));return value;}}}catch{}return new BubblePreferences();}
  public void Save(string folder){Directory.CreateDirectory(folder);string path=Path.Combine(folder,"bubble.json"),temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
  public static Point Clamp(Point point,Size size,Rectangle area){return new Point(Math.Max(area.Left,Math.Min(point.X,area.Right-size.Width)),Math.Max(area.Top,Math.Min(point.Y,area.Bottom-size.Height)));}
 }
 class Bubble:Form {
  Point start,origin;bool moved;readonly string folder;readonly BubblePreferences preferences;
  public Bubble(Action open,string dataFolder){
   folder=dataFolder;preferences=BubblePreferences.Load(folder);Text="打开复制历史";FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;TopMost=true;Size=new Size(82,48);BackColor=Color.FromArgb(42,91,160);
   var area=Screen.PrimaryScreen.WorkingArea;var desired=preferences.hasPosition?new Point(preferences.x,preferences.y):new Point(area.Right-98,area.Top+area.Height/2);Location=BubblePreferences.Clamp(desired,Size,Screen.FromPoint(desired).WorkingArea);Opacity=(100-preferences.transparency)/100.0;
   var label=new Label{Text="复制历史",ForeColor=Color.White,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand};Controls.Add(label);
   var menu=new ContextMenuStrip();menu.Items.Add("透明度…",null,(s,e)=>AdjustOpacity(null));label.ContextMenuStrip=menu;
   var tip=new ToolTip();tip.SetToolTip(label,"单击展开或收起历史，拖动记住位置；右键调整透明度。");label.Disposed+=(s,e)=>{tip.Dispose();menu.Dispose();};
   label.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;start=Cursor.Position;origin=Location;moved=false;};label.MouseMove+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;var p=Cursor.Position;int dx=p.X-start.X,dy=p.Y-start.Y;if(Math.Abs(dx)+Math.Abs(dy)>4)moved=true;if(moved)Location=new Point(origin.X+dx,origin.Y+dy);};label.MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;if(moved){Location=BubblePreferences.Clamp(Location,Size,Screen.FromPoint(Location).WorkingArea);preferences.hasPosition=true;preferences.x=Left;preferences.y=Top;SavePreferences();}else open();};
  }
  void SavePreferences(){try{preferences.Save(folder);}catch{MessageBox.Show("悬浮窗设置未能保存，请检查本机目录权限。","随选剪贴板");}}
  public void AdjustOpacity(IWin32Window owner){int previous=preferences.transparency;using(var dialog=new Form{Text="悬浮窗透明度",ClientSize=new Size(340,165),StartPosition=FormStartPosition.CenterScreen,TopMost=true,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font}){
   var caption=new Label{Left=18,Top=15,Width=300,Height=25};var slider=new TrackBar{Left=12,Top=45,Width=310,Minimum=0,Maximum=80,TickFrequency=10,Value=previous};Action preview=()=>{caption.Text="透明度："+slider.Value+"%（0% 为完全不透明）";Opacity=(100-slider.Value)/100.0;};slider.ValueChanged+=(s,e)=>preview();preview();var save=new Button{Text="保存",Left=155,Top=115,Width=75,DialogResult=DialogResult.OK};var cancel=new Button{Text="取消",Left=240,Top=115,Width=75,DialogResult=DialogResult.Cancel};dialog.Controls.AddRange(new Control[]{caption,slider,save,cancel});dialog.AcceptButton=save;dialog.CancelButton=cancel;
   if(dialog.ShowDialog(owner)==DialogResult.OK){preferences.transparency=slider.Value;SavePreferences();}else Opacity=(100-previous)/100.0;
  }}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 }
}
