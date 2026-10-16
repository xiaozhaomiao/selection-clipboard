using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace SelectionClipboard {
 class BubbleSettingsTest {
 [STAThread]static int Main(){string root=Path.Combine(Path.GetTempPath(),"bubble-test-"+Guid.NewGuid().ToString("N"));try{
  var initial=BubblePreferences.Load(root);if(initial.hasPosition||initial.transparency!=0)throw new Exception("defaults");
  var area=Screen.PrimaryScreen.WorkingArea;var p=new BubblePreferences{hasPosition=true,x=area.Left+20,y=area.Top+20,transparency=45};p.Save(root);var read=BubblePreferences.Load(root);if(!read.hasPosition||read.x!=p.x||read.y!=p.y||read.transparency!=45)throw new Exception("round trip");
  using(var bubble=new Bubble(()=>{},root)){if(bubble.Location!=new Point(p.x,p.y)||Math.Abs(bubble.Opacity-0.55)>0.01)throw new Exception("restored window settings");}
  p.transparency=200;p.Save(root);if(BubblePreferences.Load(root).transparency!=80)throw new Exception("opacity bounds");
  var bounded=BubblePreferences.Clamp(new Point(9999,-9999),new Size(82,48),new Rectangle(-1920,0,1920,1080));if(bounded!=new Point(-82,0))throw new Exception("multi monitor clamp");
  File.WriteAllText(Path.Combine(root,"bubble.json"),"broken");if(BubblePreferences.Load(root).hasPosition)throw new Exception("corruption fallback");
  Console.WriteLine("PASS: defaults, position/transparency persistence, restored window properties, atomic overwrite, bounds and corrupt config fallback");return 0;
 }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
 }
}
