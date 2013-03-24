using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Collections;
using INI;

namespace SCOToolBox
{
    public partial class frmCode : Form
    {
        Alsing.SourceCode.TextPoint tp = new Alsing.SourceCode.TextPoint(0, 0);
        private static bool isFunctionsShown = false;
        private static bool isNativesShown = false;
        private static Dictionary<string, string> offsets = new Dictionary<string, string>();
        //private static string labellocs = "";
        private static List<string> labellocs = new List<string>();

        private static List<byte> segheadercomp = new List<byte>();
        private static List<byte> segcodecomp = new List<byte>();
        private static List<byte> segglobalcomp = new List<byte>();
        private static List<byte> segpubliccomp = new List<byte>();
                
        private static Int32 progress = 0;
        private static Int32 progressOf = 0;
        private static string progressstr = "";

        private static bool compileDecoded = false;
        private static bool compileError = false;
        
        public frmCode()
        {
            InitializeComponent();
        }

        private void frmCode_Load(object sender, EventArgs e)
        {    
            backgroundWorkerCompile1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(compileNext);
            backgroundWorkerCompile2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(compileDone);
            backgroundWorkerRead1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(readNext);
            backgroundWorkerRead2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(readDone);

            frmCode.CheckForIllegalCrossThreadCalls = false;

            syntaxDocument.SyntaxFile = Application.StartupPath + "\\Data\\SCO.syn";

            syntaxBoxControl.AutoListIcons = imageList;
            syntaxBoxControl.AutoListClear();
            syntaxBoxControl.AutoListBeginLoad();

            StreamReader reader;
            
            reader = File.OpenText(Application.StartupPath + "\\Data\\Functions.acl"); 
            while (!reader.EndOfStream){
                listBoxFunctions.Items.Add(reader.ReadLine());       
            }
            reader.Close();

            reader = File.OpenText(Application.StartupPath + "\\Data\\Natives.acl"); 
            while (!reader.EndOfStream){
                listBoxNatives.Items.Add(reader.ReadLine());                             
            }
            reader.Close();

            syntaxBoxControl.AutoListEndLoad();
            syntaxBoxControl.AutoListAutoSelect = true;

            toolStripButtonPrint.Enabled = syntaxBoxControl.Visible;
            toolStripButtonPrintPreview.Enabled = syntaxBoxControl.Visible;
            toolStripButtonSaveAsHTML.Enabled = syntaxBoxControl.Visible;
            toolStripButtonIndent.Enabled = syntaxBoxControl.Visible;
            toolStripButtonOutdent.Enabled = syntaxBoxControl.Visible;
            toolStripButtonGoTo.Enabled = syntaxBoxControl.Visible;
            toolStripButtonFind.Enabled = syntaxBoxControl.Visible;
            toolStripButtonReplace.Enabled = syntaxBoxControl.Visible;
            toolStripButtonToggleBookmark.Enabled = syntaxBoxControl.Visible;
            toolStripButtonPreviousBookmark.Enabled = syntaxBoxControl.Visible;
            toolStripButtonNextBookmark.Enabled = syntaxBoxControl.Visible;
            toolStripButtonInsertFunction.Enabled = syntaxBoxControl.Visible;
            toolStripButtonInsertNative.Enabled = syntaxBoxControl.Visible;

            timerStartUp.Enabled = true;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void addAutoCompleteFunctions()
        {
            syntaxBoxControl.AutoListClear();
            syntaxBoxControl.AutoListBeginLoad();

            string line = ""; string[] temp;
            for (int i = 0; i < listBoxFunctions.Items.Count; i++){
                line = listBoxFunctions.Items[i].ToString();
                if (line.Contains("|")){
                    temp = line.Split(Convert.ToChar("|"));
                    syntaxBoxControl.AutoListAdd(temp[0], temp[0], temp[0] + " " + temp[1], 0);                    
                }else{
                    syntaxBoxControl.AutoListAdd(line, line, line, 0);
                }                
            }

            syntaxBoxControl.AutoListEndLoad();
            syntaxBoxControl.AutoListAutoSelect = true;
        }

        public void addAutoCompleteNatives()
        {
            syntaxBoxControl.AutoListClear();
            syntaxBoxControl.AutoListBeginLoad();

            for (int i = 0; i < listBoxNatives.Items.Count; i++) { syntaxBoxControl.AutoListAdd(listBoxNatives.Items[i].ToString(), listBoxNatives.Items[i].ToString(), listBoxNatives.Items[i].ToString(), 1); }

            syntaxBoxControl.AutoListEndLoad();
            syntaxBoxControl.AutoListAutoSelect = true;
        }

        public void showAutoComplete()
        {
            tp = syntaxBoxControl.Caret.Position;
            syntaxBoxControl.AutoListPosition = syntaxBoxControl.Caret.Position;
            syntaxBoxControl.AutoListVisible = true;
        }

        public void showAutoCompleteFunctions()
        {
            if(isFunctionsShown == false){
                addAutoCompleteFunctions();                
                isFunctionsShown = true;
                isNativesShown = false;
            }
            showAutoComplete();
        }

        public void showAutoCompleteNatives()
        {
            if (isNativesShown == false){                
                addAutoCompleteNatives();                
                isFunctionsShown = false;
                isNativesShown = true;
            }
            showAutoComplete();
        }

        public int startTabs()
        {
            return syntaxBoxControl.Caret.CurrentRow.Text.Length - syntaxBoxControl.Caret.CurrentRow.Text.TrimStart().Length;
        }

        private void syntaxBoxControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            int c = Convert.ToInt32(e.KeyChar);
            
            if (((c > 47) && (c < 58)) || ((c > 64) && (c < 91)) || ((c > 96) && (c < 123))){                
                if (syntaxBoxControl.Caret.CurrentRow.Text.Trim().Replace("\t","") == ""){                    
                    showAutoCompleteFunctions();
                }
                if ((syntaxBoxControl.Caret.CurrentRow.Text.Trim().Replace("\t","").StartsWith("CallNative") == true)&&((syntaxBoxControl.Caret.Position.X - startTabs()) == 11)){
                    showAutoCompleteNatives();
                }
            } 
            if (c == 32){
                if ((syntaxBoxControl.Caret.CurrentRow.Text.Trim().Replace("\t","").StartsWith("CallNative") == true)&&((syntaxBoxControl.Caret.Position.X - startTabs()) == 10)){
                    syntaxBoxControl.Document.InsertText(" x", syntaxBoxControl.Caret.Position.X, syntaxBoxControl.Caret.Position.Y, false);
                    //syntaxBoxControl.Caret.CurrentRow.Text = syntaxBoxControl.Caret.CurrentRow.Text + " x";
                    syntaxBoxControl.Caret.MoveRight(false); syntaxBoxControl.Caret.MoveRight(true);
                    showAutoCompleteNatives();
                }
            }   
        }

        private void syntaxBoxControl_CaretChange(object sender, EventArgs e)
        {
            if ((tp != syntaxBoxControl.Caret.Position) && ((tp.Y != syntaxBoxControl.Caret.Position.Y) && ((tp.X + 1) != syntaxBoxControl.Caret.Position.X))) { syntaxBoxControl.AutoListVisible = false; isNativesShown = false; isFunctionsShown = false; }
        }

        private void timerStartUp_Tick(object sender, EventArgs e)
        {
            timerStartUp.Enabled = false;
            Application.DoEvents();
            loadCode();
        }       

        public void loadCode()
        {
            if (SCO.writeNatives == true){
                FileStream fs = new FileStream(Application.StartupPath + "\\OutputHashes" + SCO.nattype + ".log", FileMode.Create);
                fs.Close();
            }

            if (SCO.writeNatives == true) { IniFile debug = new IniFile(Application.StartupPath + "\\OutputHashes" + SCO.nattype + ".log"); debug.IniWriteValue("Code", "Size", SCO.segcodedec.Length.ToString()); }

            syntaxBoxControl.Document = null;
            syntaxDocument.Clear();
            richTextBox.Text = "";

            disableForProgress("Reading: Step 1/7 - Reading Global Variables...");

            timerProgressRead.Enabled = true;

            listViewGlobals.Items.Clear();
            listViewGlobals.Items.AddRange(SCO.getGlobals());

            timerProgressRead.Enabled = false;

            labelStatus.Text = "Reading: Step 2/7 - Reading Public Variables...";
            labelStatus.Refresh();

            timerProgressRead.Enabled = true;

            listViewPublics.Items.Clear();
            listViewPublics.Items.AddRange(SCO.getPublics());

            timerProgressRead.Enabled = false;

            groupBoxGlobals.Text = "Global Variables (" + listViewGlobals.Items.Count + ")"; 
            groupBoxPublics.Text = "Public Variables (" + listViewPublics.Items.Count + ")";

            labelStatus.Text = "Reading: Step 3/7 - Reading Code Flags...";
            labelStatus.Refresh();

            int flags = BitConverter.ToInt32(SCO.flags, 0);
            uint[] checks = { 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80, 0x100, 0x200, 0x400, 0x800, 0x1000, 0x2000, 0x4000, 0x8000, 
                               0x10000, 0x20000, 0x40000, 0x80000, 0x100000, 0x200000, 0x400000, 0x800000, 0x1000000, 0x2000000, 0x4000000, 0x8000000, 0x10000000, 0x20000000, 0x40000000, 0x80000000 };

            for (int i = 0; i < 31; i++){
                if ((flags & checks[i]) > 0) checkedListBox.SetItemChecked(i, true);
            }

            labelStatus.Text = "Reading: Step 4/7 - Reading Code...";
            labelStatus.Refresh();           

            timerProgressRead.Enabled = true;

            backgroundWorkerRead1.RunWorkerAsync();
        }

        private void backgroundWorkerRead1_DoWork(object sender, DoWorkEventArgs e)
        {
            SCO.offsets.Clear();

            SCO.codereader = new MemoryStream(SCO.segcodedec);
            byte[] block = new byte[1];
            SCO.codeCounter = 1;

            SCO.codereader.Position = 0; SCO.progressOf = Convert.ToInt32(SCO.codereader.Length); SCO.progressstr = "Reading: Step 4/7- Reading Code...";
            
            while (SCO.codereader.Position < SCO.codereader.Length){
                SCO.progress = Convert.ToInt32(SCO.codereader.Position);
                SCO.codereader.Read(block, 0, 1);
                SCO.codeFirstRun(block[0]);
            }
        }                
        
        public void readNext(object sender, RunWorkerCompletedEventArgs e)
        {
            timerProgressRead.Enabled = false;

            labelStatus.Text = "Reading: Step 5/7 - Decompiling Code...";
            labelStatus.Refresh();

            timerProgressRead.Enabled = true;

            backgroundWorkerRead2.RunWorkerAsync();
        }

        private void backgroundWorkerRead2_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] block = new byte[1];
            string temp = "";
            string temp2 = "";
            
            SCO.codereader.Position = 0; SCO.progressOf = SCO.codereader.Length; SCO.progressstr = "Reading: Step 5/7 - Decompiling Code...";
            timerProgressRead.Enabled = true;

            StringBuilder sb = new StringBuilder();
             
            while (SCO.codereader.Position < SCO.codereader.Length){
                SCO.progress = SCO.codereader.Position;

                temp2 = SCO.getSavedOffset(SCO.codereader.Position.ToString(), "UNKNOWN");
                if (temp2 != "UNKNOWN") sb.AppendLine(Environment.NewLine + ":" + temp2.Substring(1));

                SCO.codereader.Read(block, 0, 1);
                temp = SCO.getOpCodeMeaning(block[0]);
                sb.AppendLine(temp);
            }            
            
            timerProgressRead.Enabled = false;

            labelStatus.Text = "Reading: Step 6/7 - Loading Code in Editor...";
            labelStatus.Refresh();                      

            if(syntaxBoxControl.Visible){
                syntaxDocument.Text = sb.ToString();
                syntaxBoxControl.Document = syntaxDocument; 
            }else{
                richTextBox.Text = sb.ToString();
            }
        }  

        public void readDone(object sender, RunWorkerCompletedEventArgs e)
        {            
            
            if(syntaxBoxControl.Visible){
                labelStatus.Text = "Reading: Step 7/7 - Placing Bookmarks...";
                labelStatus.Refresh();

                progressstr = "Reading: Step 7/7 - Placing Bookmarks..."; timerProgress.Enabled = true;
                progressOf = syntaxBoxControl.Document.Count; progress = 0;

                for (int i = 0; i < syntaxBoxControl.Document.Count; i++){
                    progress = i;
                    if (syntaxBoxControl.Document[i].Text.StartsWith(":")) syntaxBoxControl.Document[i].Bookmarked = true;
                }

                timerProgress.Enabled = false;
            }

            restoreAfterProgress("Read successfully.");
        }

        private void toolStripButtonSelectAll_Click(object sender, EventArgs e)
        {
            if (syntaxBoxControl.Visible) syntaxBoxControl.SelectAll(); else richTextBox.SelectAll();
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e)
        {
            if (syntaxBoxControl.Visible) { syntaxBoxControl.Cut(); } else { richTextBox.Cut(); }
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            if(syntaxBoxControl.Visible){ 
                if (syntaxBoxControl.CanCopy) syntaxBoxControl.Copy();
            }else{
                richTextBox.Copy();
            }
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e)
        {
            if(syntaxBoxControl.Visible){
                if (syntaxBoxControl.CanPaste) syntaxBoxControl.Paste();
            }else{
                richTextBox.Paste();
            }
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            if(syntaxBoxControl.Visible){
                if (syntaxBoxControl.CanUndo) syntaxBoxControl.Undo();
            }else{
                if (richTextBox.CanUndo) richTextBox.Undo();
            }
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            if(syntaxBoxControl.Visible){
                if (syntaxBoxControl.CanRedo) syntaxBoxControl.Redo();
            }else{
                if (richTextBox.CanRedo) richTextBox.Redo();
            }
        }

        private void toolStripButtonGoTo_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.ShowGotoLine();
        }

        private void toolStripButtonFind_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.ShowFind();
        }

        private void toolStripButtonReplace_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.ShowReplace();
        }

        private void toolStripButtonToggleBookmark_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.ToggleBookmark();
        }

        private void toolStripButtonPreviousBookmark_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.GotoPreviousBookmark();
        }

        private void toolStripButtonNextBookmark_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.GotoNextBookmark();
        }             

        private void toolStripButtonSaveAsText_Click(object sender, EventArgs e)
        {
            if (saveFileDialogText.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                try{
                    disableForProgress("Saving Code as Text...");

                    if (syntaxBoxControl.Visible){
                        //TextWriter tw = new StreamWriter(saveFileDialogText.FileName);
                        //tw.Write(syntaxBoxControl.Document.Text);
                        //tw.Close();
                        syntaxBoxControl.Save(saveFileDialogText.FileName);
                    }else{
                        richTextBox.SaveFile(saveFileDialogText.FileName, RichTextBoxStreamType.PlainText);
                    }

                    restoreAfterProgress("Saved successfully!");
                }catch(Exception ex){
                    MessageBox.Show("An error occured while saving your code. The error message is as follows:\n\n" + ex.Message + " (from " + ex.TargetSite + ")\n\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    restoreAfterProgress("Saving failed!");
                    return;
                }
            }
        }

        private void toolStripButtonSaveAsHTML_Click(object sender, EventArgs e)
        {
            if (saveFileDialogHTML.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                try{
                    disableForProgress("Saving Code as HTML...");

                    Alsing.SourceCode.SyntaxDocumentExporters.CollapsingHTMLExporter html = new Alsing.SourceCode.SyntaxDocumentExporters.CollapsingHTMLExporter();

                    TextWriter tw = new StreamWriter(saveFileDialogHTML.FileName);
                    tw.Write(html.Export(syntaxBoxControl.Document, "").Replace("<img src=\"clear.gif\"  align=top>",""));
                    tw.Close();

                    restoreAfterProgress("Saved successfully!");
                }catch(Exception ex){
                    MessageBox.Show("An error occured while saving your code. The error message is as follows:\n\n" + ex.Message + " (from " + ex.TargetSite + ")\n\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    restoreAfterProgress("Saving failed!");
                    return;
                }
            }
        }

        private void toolStripButtonPrint_Click(object sender, EventArgs e)
        {            
            Alsing.SourceCode.SourceCodePrintDocument pd = new Alsing.SourceCode.SourceCodePrintDocument(syntaxBoxControl.Document);
            printDialog.Document = pd;
            if (printDialog.ShowDialog() == DialogResult.OK) pd.Print();  
        }

        private void toolStripButtonPrintPreview_Click(object sender, EventArgs e)
        {
            Alsing.SourceCode.SourceCodePrintDocument pd = new Alsing.SourceCode.SourceCodePrintDocument(syntaxBoxControl.Document);
            printPreviewDialog.Document = pd;
            printPreviewDialog.ShowDialog(); 
        }

        private void toolStripButtonOutdent_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.Selection.Outdent();
        }

        private void toolStripButtonIndent_Click(object sender, EventArgs e)
        {
            syntaxBoxControl.Selection.Indent();
        }

        private void toolStripButtonInsertFunction_Click(object sender, EventArgs e)
        {
            showAutoCompleteFunctions();
        }

        private void toolStripButtonInsertNative_Click(object sender, EventArgs e)
        {
            showAutoCompleteNatives();
        }

        private void toolStripButtonReload_Click(object sender, EventArgs e)
        {
            loadCode();
        }

        private void toolStripButtonSaveAsSCO_Click(object sender, EventArgs e)
        {
            if (saveFileDialogSCO.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                compileDecoded = true;
                compileSCO();
            }
        }

        public void compileSCO()
        {
            disableForProgress("Compiling: Step 1/7 - Compiling Header...");
            compileError = false;

            offsets.Clear();
            labellocs.Clear();
            segheadercomp.Clear();
            segcodecomp.Clear();
            segglobalcomp.Clear();
            segpubliccomp.Clear();

            if (compileDecoded == false){
                segheadercomp.Add(0x73); // Header ID
                segheadercomp.Add(0x63);
                segheadercomp.Add(0x72);
                segheadercomp.Add(0x0e);                 
            }else{
                segheadercomp.Add(0x53); // Header ID
                segheadercomp.Add(0x43);
                segheadercomp.Add(0x52);
                segheadercomp.Add(0x0d); 
            }

            segheadercomp.Add(0x0); // Temp Code Size
            segheadercomp.Add(0x0);
            segheadercomp.Add(0x0);
            segheadercomp.Add(0x0);

            segheadercomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(listViewGlobals.Items.Count))); // Globals Count
            segheadercomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(listViewPublics.Items.Count))); // Publics Count
            
            BitArray ba = new BitArray(32);

            for (int i = 0; i < 31; i++){
                ba.Set(i, checkedListBox.GetItemChecked(i));
            }

            segheadercomp.AddRange(BitArrayToByteArray(ba)); // Code Flags
            segheadercomp.AddRange(SCO.signature); // Code Signature
            
            labelStatus.Text = "Compiling: Step 2/7 - Compiling Code...";
            labelStatus.Refresh();

            progressstr = "Compiling: Step 2/7 - Compiling Code..."; timerProgress.Enabled = true;
            backgroundWorkerCompile1.RunWorkerAsync();
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[bits.Length / 8];
            bits.CopyTo(ret, 0);
            return ret;
        }

        private void backgroundWorkerCompile1_DoWork(object sender, DoWorkEventArgs e)
        {
            string line = "";
            string[] splitter; string[] splitter2; string temp = ""; int tempint = 0; short tempshort = 0; float tempfloat = 0;
            byte[] ph = {0x0, 0x0, 0x0, 0x0};
            IniFile hashes = new IniFile(Application.StartupPath + "\\Data\\Hashes" + SCO.nattype + ".dat");
             
            List<string> lst = new List<string>();
            lst.AddRange(richTextBox.Lines);

            if (syntaxBoxControl.Visible) progressOf = syntaxBoxControl.Document.Count; else progressOf = lst.Count;

            for (int i = 0; i < progressOf; i++){   
                try{
                progress = i;
                if (syntaxBoxControl.Visible) line = syntaxBoxControl.Document[i].Text.Trim(); else line = lst[i].Trim();
                if (line == ""){
                }else if(line.StartsWith(":")){ offsets.Add("@" + line.Substring(1), segcodecomp.Count.ToString());
                }else if(line.StartsWith("//")){                    
                }else if(line == "NOp"){ segcodecomp.Add(0);
                }else if(line == "Add"){ segcodecomp.Add(1);
                }else if(line == "Sub"){ segcodecomp.Add(2);
                }else if(line == "Mul"){ segcodecomp.Add(3);
                }else if(line == "Div"){ segcodecomp.Add(4);
                }else if(line == "Mod"){ segcodecomp.Add(5);
                }else if(line == "IsZero"){ segcodecomp.Add(6);
                }else if(line == "Neg"){ segcodecomp.Add(7);
                }else if(line == "CmpEq"){ segcodecomp.Add(8);
                }else if(line == "CmpNe"){ segcodecomp.Add(9);
                }else if(line == "CmpGt"){ segcodecomp.Add(10);
                }else if(line == "CmpGe"){ segcodecomp.Add(11);
                }else if(line == "CmpLt"){ segcodecomp.Add(12);
                }else if(line == "CmpLe"){ segcodecomp.Add(13);
                }else if(line == "AddF"){ segcodecomp.Add(14);
                }else if(line == "SubF"){ segcodecomp.Add(15);
                }else if(line == "MulF"){ segcodecomp.Add(16);
                }else if(line == "DivF"){ segcodecomp.Add(17);
                }else if(line == "ModF"){ segcodecomp.Add(18);
                }else if(line == "NegF"){ segcodecomp.Add(19);
                }else if(line == "CmpEqF"){ segcodecomp.Add(20);
                }else if(line == "CmpNeF"){ segcodecomp.Add(21);
                }else if(line == "CmpGtF"){ segcodecomp.Add(22);
                }else if(line == "CmpGeF"){ segcodecomp.Add(23);
                }else if(line == "CmpLtF"){ segcodecomp.Add(24);
                }else if(line == "CmpLeF"){ segcodecomp.Add(25);
                }else if(line == "AddVec"){ segcodecomp.Add(26);
                }else if(line == "SubVec"){ segcodecomp.Add(27);
                }else if(line == "MulVec"){ segcodecomp.Add(28);
                }else if(line == "DivVec"){ segcodecomp.Add(29);
                }else if(line == "NegVec"){ segcodecomp.Add(30);
                }else if(line == "And"){ segcodecomp.Add(31);
                }else if(line == "Or"){ segcodecomp.Add(32);
                }else if(line == "Xor"){ segcodecomp.Add(33);
                }else if(line == "ToF"){ segcodecomp.Add(37);
                }else if(line == "FromF"){ segcodecomp.Add(38);
                }else if(line == "VecFromF"){ segcodecomp.Add(39);
                }else if(line == "Dup"){ segcodecomp.Add(43);
                }else if(line == "Pop"){ segcodecomp.Add(44);
                }else if(line == "RefGet"){ segcodecomp.Add(49);
                }else if(line == "RefSet"){ segcodecomp.Add(50);
                }else if(line == "RefPeekSet"){ segcodecomp.Add(51);
                }else if(line == "ArrayExplode"){ segcodecomp.Add(52);
                }else if(line == "ArrayImplode"){ segcodecomp.Add(53);
                }else if(line == "Var0"){ segcodecomp.Add(54);
                }else if(line == "Var1"){ segcodecomp.Add(55);
                }else if(line == "Var2"){ segcodecomp.Add(56);
                }else if(line == "Var3"){ segcodecomp.Add(57);
                }else if(line == "Var4"){ segcodecomp.Add(58);
                }else if(line == "Var5"){ segcodecomp.Add(59);
                }else if(line == "Var6"){ segcodecomp.Add(60);
                }else if(line == "Var7"){ segcodecomp.Add(61);
                }else if(line == "Var"){ segcodecomp.Add(62);
                }else if(line == "LocalVar"){ segcodecomp.Add(63);
                }else if(line == "GlobalVar"){ segcodecomp.Add(64);
                }else if(line == "ArrayRef"){ segcodecomp.Add(65);
                }else if(line == "NullObj"){ segcodecomp.Add(68);
                }else if(line == "StrCpy"){ segcodecomp.Add(69);
                }else if(line == "IntToStr"){ segcodecomp.Add(70);
                }else if(line == "StrCat"){ segcodecomp.Add(71);
                }else if(line == "StrCatI"){ segcodecomp.Add(72);
                }else if(line == "Catch"){ segcodecomp.Add(73);
                }else if(line == "Throw"){ segcodecomp.Add(74);
                }else if(line == "StrVarCpy"){ segcodecomp.Add(75);
                }else if(line == "GetProtect"){ segcodecomp.Add(76);
                }else if(line == "SetProtect"){ segcodecomp.Add(77);
                }else if(line == "RefProtect"){ segcodecomp.Add(78);
                }else if(line == "Abort"){ segcodecomp.Add(79);
                }else if(line.StartsWith("PushD ")){
                    temp = line.Substring(6).Trim();
                    tempint = Convert.ToInt32(temp);
                    if ((tempint >= -16)&&(tempint <= 159)){
                        tempint = tempint + 96;
                        segcodecomp.Add(Convert.ToByte(tempint));
                    }else{                        
                        MessageBox.Show("An error occured while compiling your code. Please check the following line for errors and try again.\n\nLine " + i.ToString() + ":\n" + line + "\n\nThe parameter for PushD should be -16 or higher and 159 or lower.", "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        restoreAfterProgress("Compiling failed!");
                        compileError = true;
                        return;
                    }                    
                }else if(line.StartsWith("PushS ")){
                    temp = line.Substring(6).Trim();
                    tempshort = short.Parse(temp);
                    segcodecomp.Add(40);
                    segcodecomp.AddRange(BitConverter.GetBytes(tempshort));
                }else if(line.StartsWith("FnEnd ")){
                    temp = line.Substring(6).Trim();
                    segcodecomp.Add(48);
                    splitter = temp.Split(Convert.ToChar(" "));
                    tempint = Convert.ToInt32(splitter[0]);
                    segcodecomp.Add(Convert.ToByte(tempint));
                    tempint = Convert.ToInt32(splitter[1]);
                    segcodecomp.Add(Convert.ToByte(tempint));
                }else if(line.StartsWith("FnBegin ")){
                    temp = line.Substring(8).Trim();
                    segcodecomp.Add(47);
                    splitter = temp.Split(Convert.ToChar(" "));
                    tempint = Convert.ToInt32(splitter[0]);
                    tempshort = short.Parse(splitter[1]);
                    segcodecomp.Add(Convert.ToByte(tempint));
                    segcodecomp.AddRange(BitConverter.GetBytes(tempshort));
                }else if(line.StartsWith("Jump ")){
                    segcodecomp.Add(34);
                    temp = line.Substring(5).Trim();
                    if (temp.StartsWith("0x")) { 
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(temp.Replace("0x", ""), 16))); 
                    }else{
                        labellocs.Add(temp);
                        labellocs.Add(segcodecomp.Count.ToString());
                        segcodecomp.AddRange(ph);   // Placeholder
                    }                    
                }else if(line.StartsWith("JumpFalse ")){
                    segcodecomp.Add(35);
                    temp = line.Substring(10).Trim();
                    if (temp.StartsWith("0x")) { 
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(temp.Replace("0x", ""), 16))); 
                    }else{
                        labellocs.Add(temp);
                        labellocs.Add(segcodecomp.Count.ToString());
                        segcodecomp.AddRange(ph);   // Placeholder
                    }
                }else if(line.StartsWith("JumpTrue ")){
                    segcodecomp.Add(36);
                    temp = line.Substring(9).Trim();
                    if (temp.StartsWith("0x")) { 
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(temp.Replace("0x", ""), 16))); 
                    }else{
                        labellocs.Add(temp);
                        labellocs.Add(segcodecomp.Count.ToString());
                        segcodecomp.AddRange(ph);   // Placeholder
                    }
                }else if(line.StartsWith("Push ")){
                    temp = line.Substring(5).Trim();
                    temp = temp.Replace("0x", "");
                    segcodecomp.Add(41);
                    segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(temp, 16)));
                }else if(line.StartsWith("PushF ")){
                    temp = line.Substring(6).Trim();
                    tempfloat = float.Parse(temp);
                    segcodecomp.Add(42);
                    segcodecomp.AddRange(BitConverter.GetBytes(tempfloat));
                }else if(line.StartsWith("Call ")){
                    segcodecomp.Add(46);
                    temp = line.Substring(5).Trim();
                    if (temp.StartsWith("0x")) { 
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(temp.Replace("0x", ""), 16))); 
                    }else{
                        labellocs.Add(temp);
                        labellocs.Add(segcodecomp.Count.ToString());
                        segcodecomp.AddRange(ph);   // Placeholder
                    }
                }else if(line.StartsWith("CallNative ")){
                    temp = line.Substring(11).Trim();
                    segcodecomp.Add(45);
                    splitter = temp.Split(Convert.ToChar(" "));
                    tempint = Convert.ToInt32(splitter[1]);
                    segcodecomp.Add(Convert.ToByte(tempint));
                    tempint = Convert.ToInt32(splitter[2]);
                    segcodecomp.Add(Convert.ToByte(tempint));
                    if(splitter[0].StartsWith("0x")){
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(splitter[0].Replace("0x",""), 16)));
                    }else{
                        temp = hashes.IniReadValue("Hashes", splitter[0], "UNKNOWN");
                        if (temp == "UNKNOWN"){
                            MessageBox.Show("An error occured while compiling your code. Please check the following line for errors and try again.\n\nLine " + i.ToString() + ":\n" + line + "\n\nCould not find a hash for the following native:\n" + splitter[0], "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            restoreAfterProgress("Compiling failed!");
                            compileError = true;
                            return;
                        }else{
                            segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(temp)));
                        }
                    }
                }else if(line.StartsWith("PushString ")){
                    temp = line.Substring(12);                    
                    temp = temp.Remove(temp.Length - 1);
                    segcodecomp.Add(67);
                    tempint = temp.Replace("\\n","X").Length + 1;
                    segcodecomp.Add(Convert.ToByte(tempint));
                    segcodecomp.AddRange(System.Text.Encoding.GetEncoding(1251).GetBytes(temp.Replace("\\n", "\n")));
                    segcodecomp.Add(0x0);
                }else if(line.StartsWith("Switch ")){
                    temp = line.Substring(7).Trim();
                    segcodecomp.Add(66);
                    splitter = temp.Split(Convert.ToChar(" "));
                    tempint = splitter.Length;
                    segcodecomp.Add(Convert.ToByte(tempint));
                    for (int x = 0; x < splitter.Length; x++){
                        splitter2 = splitter[x].Split(Convert.ToChar(":"));
                        segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(splitter2[0], 16)));
                        if (splitter2[1].StartsWith("0x")) {
                            segcodecomp.AddRange(BitConverter.GetBytes(Convert.ToInt32(splitter2[1].Replace("0x", ""), 16))); 
                        }else{
                            labellocs.Add(splitter2[1]);
                            labellocs.Add(segcodecomp.Count.ToString());
                            segcodecomp.AddRange(ph);   // Placeholder
                        }
                    }
                }else{
                    MessageBox.Show("An error occured while compiling your code. Please check the following line for errors and try again.\n\nLine " + i.ToString() + ":\n" + line, "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    restoreAfterProgress("Compiling failed!");
                    compileError = true;
                    return;
                }
                }catch (Exception ex){
                    MessageBox.Show("An error occured while compiling your code. The error message is as follows:\n\n" + ex.Message + " (from " + ex.TargetSite + ")\n\n" + ex.StackTrace, "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    restoreAfterProgress("Compiling failed!");
                    compileError = true;
                    return;
                }
            } 
        }

        public void disableForProgress(string status)
        {
            labelStatus.Text = status;
            labelStatus.Refresh();
            Cursor.Current = Cursors.WaitCursor;
            this.Cursor = Cursors.WaitCursor;
            syntaxBoxControl.Enabled = false;
            richTextBox.Enabled = false;
            toolStrip.Enabled = false;
            groupBoxGlobals.Enabled = false;
            groupBoxPublics.Enabled = false;
            groupBoxFlags.Enabled = false;
        }

        public void restoreAfterProgress(string status)
        {
            labelStatus.Text = status;
            labelStatus.Refresh();
            Cursor.Current = Cursors.Default;
            this.Cursor = Cursors.Default;
            syntaxBoxControl.Enabled = true;
            richTextBox.Enabled = true;
            toolStrip.Enabled = true;
            groupBoxGlobals.Enabled = true;
            groupBoxPublics.Enabled = true;
            groupBoxFlags.Enabled = true;
        }

        public void compileNext(object sender, RunWorkerCompletedEventArgs e)
        {
            timerProgress.Enabled = false;

            if (compileError == true) return;
            
            labelStatus.Text = "Compiling: Step 3/7 - Updating Offsets...";
            labelStatus.Refresh();

            //if(labellocs.Length > 2) labellocs = labellocs.Remove(labellocs.Length - 1);

            progressstr = "Compiling: Step 3/7 - Updating Offsets..."; timerProgress.Enabled = true;
            backgroundWorkerCompile2.RunWorkerAsync();
        }

        private void backgroundWorkerCompile2_DoWork(object sender, DoWorkEventArgs e)
        {
            string newoffset = ""; UInt32 newloc = 0; int loc = 0; byte[] newbytes = new byte[4];
            //locsplit = labellocs.Split(Convert.ToChar("|"));
            if ((labellocs.Count > 1) == false) return;

            progressOf = labellocs.Count;
            for (int i = 0; i < labellocs.Count; i += 2){
                progress = i;
                //label|offset
                newoffset = getSavedOffset(labellocs[i], "UNKNOWN");
                if (newoffset == "UNKNOWN"){
                    MessageBox.Show("An error occured while compiling your code. Could not find a offset for the following label:\n" + labellocs[i], "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    restoreAfterProgress("Compiling failed!");
                    compileError = true;
                    return;
                }else{
                    loc = Convert.ToInt32(labellocs[i + 1]);
                    newloc = Convert.ToUInt32(newoffset);
                    newbytes = BitConverter.GetBytes(newloc);
                    segcodecomp[loc] = newbytes[0];
                    segcodecomp[loc + 1] = newbytes[1];
                    segcodecomp[loc + 2] = newbytes[2];
                    segcodecomp[loc + 3] = newbytes[3];
                }
            }
        }

        public static string getSavedOffset(string get, string defval)
        {
            try{
                string temp = defval;
                offsets.TryGetValue(get, out temp);
                if (temp == null) return defval;
                return temp;
            }catch{
                return defval;
            }
        }

        public void compileDone(object sender, RunWorkerCompletedEventArgs e)
        {   
            timerProgress.Enabled = false;

            if (compileError == true) return;

            labelStatus.Text = "Compiling: Step 4/7 - Updating Header...";
            labelStatus.Refresh();
            
            byte[] cdsz = new byte[4];
            cdsz = BitConverter.GetBytes(segcodecomp.Count);
            segheadercomp[4] = cdsz[0]; segheadercomp[5] = cdsz[1]; segheadercomp[6] = cdsz[2]; segheadercomp[7] = cdsz[3];
            
            labelStatus.Text = "Compiling: Step 5/7 - Compiling Global Variables...";
            labelStatus.Refresh();

            progressstr = "Compiling: Step 5/7 - Compiling Global Variables..."; timerProgress.Enabled = true;
            progress = 0; progressOf = listViewGlobals.Items.Count;

            for (int i = 0; i < listViewGlobals.Items.Count; i++){
                progress = i;
                segglobalcomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(listViewGlobals.Items[i].SubItems[1].Text)));
                //SCO.writeToSCO(BitConverter.GetBytes(Convert.ToUInt32(listViewGlobals.Items[i].SubItems[1].Text)));
            }

            timerProgress.Enabled = false;

            labelStatus.Text = "Compiling: Step 6/7 - Compiling Public Variables...";
            labelStatus.Refresh();

            progressstr = "Compiling: Step 6/7 - Compiling Public Variables..."; timerProgress.Enabled = true;
            progress = 0; progressOf = listViewPublics.Items.Count;

            for (int i = 0; i < listViewPublics.Items.Count; i++){
                progress = i;
                segpubliccomp.AddRange(BitConverter.GetBytes(Convert.ToUInt32(listViewPublics.Items[i].SubItems[1].Text)));
                //SCO.writeToSCO(BitConverter.GetBytes(Convert.ToUInt32(listViewPublics.Items[i].SubItems[1].Text)));
            }

            timerProgress.Enabled = false;

            labelStatus.Text = "Compiling: Step 7/7 - Writing file...";
            labelStatus.Refresh();

            if (compileDecoded == false){
                byte[] temp;
                temp = new byte[segcodecomp.Count]; temp = segcodecomp.ToArray();
                segcodecomp.Clear(); segcodecomp.AddRange(SCO.encryptAES(temp));
                temp = new byte[segglobalcomp.Count]; temp = segglobalcomp.ToArray();
                segglobalcomp.Clear(); segglobalcomp.AddRange(SCO.encryptAES(temp));
                temp = new byte[segpubliccomp.Count]; temp = segpubliccomp.ToArray();
                segpubliccomp.Clear(); segpubliccomp.AddRange(SCO.encryptAES(temp));
            }

            if (compileDecoded == false) { SCO.createSCO(saveFileDialogSCOEncoded.FileName); } else { SCO.createSCO(saveFileDialogSCO.FileName); }
            SCO.writeToSCO(segheadercomp.ToArray());
            SCO.writeToSCO(segcodecomp.ToArray());
            SCO.writeToSCO(segglobalcomp.ToArray());
            SCO.writeToSCO(segpubliccomp.ToArray());
            SCO.closeSCO();

            restoreAfterProgress("Compiled successfully!");        
        }

        private void timerProgress_Tick(object sender, EventArgs e)
        {
            labelStatus.Text = progressstr + " (" + Math.Round(Convert.ToDouble(Convert.ToDouble(progress) / Convert.ToDouble(progressOf)) * 100, 0) + "%)";
            labelStatus.Refresh();
        }

        private void timerProgressRead_Tick(object sender, EventArgs e)
        {
            labelStatus.Text = SCO.progressstr + " (" + Math.Round(Convert.ToDouble(Convert.ToDouble(SCO.progress) / Convert.ToDouble(SCO.progressOf)) * 100, 0) + "%)";
            labelStatus.Refresh();
        }

        private void toolStripButtonSaveAsSCOEncoded_Click(object sender, EventArgs e)
        {
            if (saveFileDialogSCOEncoded.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                compileDecoded = false;
                compileSCO();
            }
        }

        private void toolStripComboBoxNatType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (toolStripComboBoxNatType.Text.StartsWith(SCO.nattype) == false){
                SCO.nattype = toolStripComboBoxNatType.Text.Substring(0, 3); string other = "";
                if(SCO.nattype == "New") other = "Old"; else other = "New";

                MessageBox.Show(String.Format("You've changed the Natives Mode from {1} to {0}.\nIf the natives are currently displayed as hashes because they are from the {0} mode, please reload the code.\nIf the natives are displayed correctly and you want to convert the natives of this file from {1} to {0}, you can now save this document to a new file.", SCO.nattype, other), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonAddG_Click(object sender, EventArgs e)
        {
            string val = ""; Int64 lval = 0;
            if (InputBox.Show("Add Global Variable", "Please enter a value between 0 and 65535:", ref val) == System.Windows.Forms.DialogResult.OK) {
                try { lval = Convert.ToInt64(val); }catch{ MessageBox.Show("Invalid value.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error); return; }
                if((lval >= 0) && (lval <= 65535)){
                    listViewGlobals.Items.Add((listViewGlobals.Items.Count + 1).ToString()).SubItems.Add(lval.ToString());
                    groupBoxGlobals.Text = "Global Variables (" + listViewGlobals.Items.Count + ")";
                  }else{
                    MessageBox.Show("Invalid value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                }
            }            
        }

        private void buttonAddP_Click(object sender, EventArgs e)
        {
            string val = ""; Int64 lval = 0;
            if (InputBox.Show("Add Public Variable", "Please enter a value between 0 and 65535:", ref val) == System.Windows.Forms.DialogResult.OK) {
                try { lval = Convert.ToInt64(val); }catch{ MessageBox.Show("Invalid value.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error); return; }
                if((lval >= 0) && (lval <= 65535)){
                    listViewPublics.Items.Add((listViewPublics.Items.Count + 1).ToString()).SubItems.Add(lval.ToString());
                    groupBoxPublics.Text = "Public Variables (" + listViewPublics.Items.Count + ")";
                }else{
                    MessageBox.Show("Invalid value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                }
            }            
        }

        private void buttonEditG_Click(object sender, EventArgs e)
        {
            if (listViewGlobals.SelectedItems.Count != 1){
                MessageBox.Show("Please select one value to edit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                return;
            }

            string val = listViewGlobals.SelectedItems[0].SubItems[1].Text; Int64 lval = 0;
            if (InputBox.Show("Edit Global Variable", "Please enter a value between 0 and 65535:", ref val) == System.Windows.Forms.DialogResult.OK) {
                try { lval = Convert.ToInt64(val); }catch{ MessageBox.Show("Invalid value.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error); return; }
                if((lval >= 0) && (lval <= 65535)){
                    listViewGlobals.Items[listViewGlobals.SelectedItems[0].Index].SubItems[1].Text = lval.ToString();
                }else{
                    MessageBox.Show("Invalid value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                }
            }
        }

        private void buttonEditP_Click(object sender, EventArgs e)
        {
            if (listViewPublics.SelectedItems.Count != 1){
                MessageBox.Show("Please select one value to edit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                return;
            }

            string val = listViewPublics.SelectedItems[0].SubItems[1].Text; Int64 lval = 0;
            if (InputBox.Show("Edit Public Variable", "Please enter a value between 0 and 65535:", ref val) == System.Windows.Forms.DialogResult.OK) {
                try { lval = Convert.ToInt64(val); }catch{ MessageBox.Show("Invalid value.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error); return; }
                if((lval >= 0) && (lval <= 65535)){
                    listViewPublics.Items[listViewPublics.SelectedItems[0].Index].SubItems[1].Text = lval.ToString();
                }else{
                    MessageBox.Show("Invalid value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                }
            }
        }

        private void buttonDeleteG_Click(object sender, EventArgs e)
        {
            if (listViewGlobals.SelectedItems.Count == 0){
                MessageBox.Show("Please select one or more values to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                return;
            }
            if (MessageBox.Show("Are you sure you want to delete these " + listViewGlobals.SelectedItems.Count + " items?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes){
                disableForProgress("Deleting Global Variables...");
                foreach (ListViewItem lvi in listViewGlobals.SelectedItems){
                    lvi.Remove();
                }
                for (int i = 0; i < listViewGlobals.Items.Count; i++){
                    listViewGlobals.Items[i].Text = (i + 1).ToString();
                }
                groupBoxGlobals.Text = "Global Variables (" + listViewGlobals.Items.Count + ")";
                restoreAfterProgress("Deleted successfully!");
            }            
        }

        private void buttonDeleteP_Click(object sender, EventArgs e)
        {
            if (listViewPublics.SelectedItems.Count == 0){
                MessageBox.Show("Please select one or more values to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                return;
            }
            if (MessageBox.Show("Are you sure you want to delete these " + listViewPublics.SelectedItems.Count + " items?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes){
                disableForProgress("Deleting Public Variables...");
                foreach (ListViewItem lvi in listViewPublics.SelectedItems){
                    lvi.Remove();
                }
                for (int i = 0; i < listViewPublics.Items.Count; i++){
                    listViewPublics.Items[i].Text = (i + 1).ToString();
                }
                groupBoxPublics.Text = "Public Variables (" + listViewPublics.Items.Count + ")";
                restoreAfterProgress("Deleted successfully!");
            } 
        }

        private void buttonClearG_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all items?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes){
                listViewGlobals.Items.Clear();
                groupBoxGlobals.Text = "Global Variables (" + listViewGlobals.Items.Count + ")";
            }
        }

        private void buttonClearP_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all items?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes){
                listViewPublics.Items.Clear();
                groupBoxPublics.Text = "Public Variables (" + listViewPublics.Items.Count + ")";
            }
        }
    }
}
