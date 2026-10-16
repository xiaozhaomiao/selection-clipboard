using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SelectionClipboard {
 partial class ClipForm {
  void ShowUsage(bool firstLaunch){
   string marker=Path.Combine(folder,"usage-guide-v1.seen");if(firstLaunch&&File.Exists(marker))return;
   using(var dialog=new Form{Text="随选剪贴板 · 使用说明",ClientSize=new Size(520,460),StartPosition=FormStartPosition.CenterParent,TopMost=true,Font=Font,MinimizeBox=false,MaximizeBox=false}){
    var heading=new Label{Text="几步开始使用",Dock=DockStyle.Top,Height=55,Padding=new Padding(18,14,0,0),Font=new Font(Font.FontFamily,15,FontStyle.Bold)};
    var text=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=SystemColors.Window,Font=new Font(Font.FontFamily,10),TabStop=false,DetectUrls=false,Text=
     "1. 打开即可使用\n本地复制、历史和设置可直接使用。此版本无账号、云同步或联网更新功能。\n\n"+
     "2. 选中文字，自动复制\n拖选文字或双击选词即可。按住 Alt 可临时跳过，Ctrl+Alt+C 可暂停或恢复。\n\n"+
     "3. 打开复制历史\n单击悬浮按钮展开或收起，按住按钮可拖动位置，下次启动会恢复。右键悬浮按钮可调整透明度（0%–80%），设置会保留。\n\n"+
     "4. 从历史粘贴\n先点击目标输入框放好光标，再双击历史条目，或选中后点“粘贴”。面板保持展开，条目不会因此重新排序。\n\n"+
     "5. 管理与保存\n支持搜索、置顶和删除。最近 100 条普通历史保存在本机，退出或更换程序版本后恢复；所有内容只保存在本机。\n\n"+
     "提示：选中文字准备替换时，请按住 Alt，避免覆盖原来要粘贴的内容。可在“生效应用”中排除不需要自动复制的软件。\n\n"+
     "退出程序请右键托盘图标选择“退出”。以后可从托盘菜单再次打开本说明。"};
    var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(20,5,20,5)};body.Controls.Add(text);
    var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=54,Padding=new Padding(10),FlowDirection=FlowDirection.RightToLeft};var done=new Button{Text=firstLaunch?"知道了，开始使用":"关闭",AutoSize=true,DialogResult=DialogResult.OK};buttons.Controls.Add(done);dialog.Controls.Add(body);dialog.Controls.Add(heading);dialog.Controls.Add(buttons);body.BringToFront();dialog.AcceptButton=done;
    if(dialog.ShowDialog(this)==DialogResult.OK&&firstLaunch){try{Directory.CreateDirectory(folder);File.WriteAllText(marker,"acknowledged");}catch{status.Text="使用说明的已读状态未能保存，下次会再次显示";}}
   }
  }
 }
}
