namespace SelectionClipboard {
 // Match both the clipboard version and content: a later real copy remains recordable.
 class ClipboardReplay {
  bool active,writing;uint sequence;string text;
  public void Begin(string value){active=true;writing=true;text=value;}
  public void Commit(uint version){sequence=version;writing=false;}
  public void Cancel(){active=false;writing=false;text=null;}
  public bool IsInternal(uint version,string value){return active&&value==text&&(writing||version==sequence);}
 }
 static class ReplayRegression {
  public static bool Run(){
   var history=new History();history.Add("older entry");history.Add("newest entry");var guard=new ClipboardReplay();
   guard.Begin("older entry");if(!guard.IsInternal(10,"older entry"))return false;guard.Commit(11);
   for(int n=0;n<2;n++){if(!guard.IsInternal(11,"older entry"))history.Add("older entry");}
   if(history.Items.Count!=2||history.Items[0].Text!="newest entry")return false;
   // A real later copy of exactly the same text must still move it to the top.
   if(!guard.IsInternal(12,"older entry"))history.Add("older entry");
   if(history.Items[0].Text!="older entry"||guard.IsInternal(11,"unrelated copy"))return false;
   guard.Begin("failed write");guard.Cancel();return !guard.IsInternal(12,"failed write");
  }
 }
}
