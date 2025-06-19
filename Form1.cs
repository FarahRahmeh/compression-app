using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MMProject
{
    public partial class MainForm : Form
    {
        CancellationTokenSource cts;

        ProgressBar progressBar;
        Label lblProgress;
        Button btnCancel;
        Button btnCompress;
        Button btnDecompressAll;
        Button btnExtractSingle;

        public MainForm()
        {
            InitializeComponent();

            // Compress multiple files button
            btnCompress = new Button();
            btnCompress.Text = "Compress Files";
            btnCompress.Width = 120;
            btnCompress.Height = 40;
            btnCompress.Top = 20;
            btnCompress.Left = 30;
            btnCompress.Click += BtnCompress_Click;
            this.Controls.Add(btnCompress);

            // Decompress all files button
            btnDecompressAll = new Button();
            btnDecompressAll.Text = "Decompress All";
            btnDecompressAll.Width = 120;
            btnDecompressAll.Height = 40;
            btnDecompressAll.Top = 80;
            btnDecompressAll.Left = 30;
            btnDecompressAll.Click += BtnDecompressAll_Click;
            this.Controls.Add(btnDecompressAll);

            // Extract single file button
            btnExtractSingle = new Button();
            btnExtractSingle.Text = "Extract Single File";
            btnExtractSingle.Width = 120;
            btnExtractSingle.Height = 40;
            btnExtractSingle.Top = 140;
            btnExtractSingle.Left = 30;
            btnExtractSingle.Click += BtnExtractSingle_Click;
            this.Controls.Add(btnExtractSingle);

            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Width = 200;
            progressBar.Height = 20;
            progressBar.Top = 200;
            progressBar.Left = 30;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            this.Controls.Add(progressBar);

            // Progress label
            lblProgress = new Label();
            lblProgress.Top = 200;
            lblProgress.Left = 240;
            lblProgress.Width = 50;
            this.Controls.Add(lblProgress);

            // Cancel button
            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Width = 80;
            btnCancel.Height = 30;
            btnCancel.Top = 230;
            btnCancel.Left = 30;
            btnCancel.Click += BtnCancel_Click;
            btnCancel.Enabled = false;
            this.Controls.Add(btnCancel);
        }

        private async void BtnCompress_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Huffman Archive|*.huff";
                        sfd.FileName = "archive.huff";

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            btnCompress.Enabled = false;
                            btnDecompressAll.Enabled = false;
                            btnExtractSingle.Enabled = false;
                            btnCancel.Enabled = true;
                            progressBar.Value = 0;
                            lblProgress.Text = "0%";

                            cts = new CancellationTokenSource();

                            try
                            {
                                await Task.Run(() =>
                                {
                                    try
                                    {
                                        Huffman.CompressFiles(ofd.FileNames, sfd.FileName, UpdateProgress, cts.Token);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        // Re-throw inside task so it can be caught in UI layer
                                        throw;
                                    }
                                });

                            }
                            catch (OperationCanceledException)
                            {
                                if (File.Exists(sfd.FileName))
                                {
                                    try { File.Delete(sfd.FileName); } catch { }
                                }
                                MessageBox.Show("Compression canceled. Archive deleted.");
                            }
                            catch (Exception ex)
                            {
                                if (File.Exists(sfd.FileName))
                                {
                                    try { File.Delete(sfd.FileName); } catch { }
                                }
                                MessageBox.Show("Error: " + ex.Message);
                            }
                            finally
                            {
                                btnCompress.Enabled = true;
                                btnDecompressAll.Enabled = true;
                                btnExtractSingle.Enabled = true;
                                btnCancel.Enabled = false;
                                UpdateProgress(0);
                            }
                        }
                    }
                }
            }
        }


        private async void BtnDecompressAll_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Huffman Archive|*.huff";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    {
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            btnCompress.Enabled = false;
                            btnDecompressAll.Enabled = false;
                            btnExtractSingle.Enabled = false;
                            btnCancel.Enabled = true;
                            progressBar.Value = 0;
                            lblProgress.Text = "0%";

                            cts = new CancellationTokenSource();

                            try
                            {
                                await Task.Run(() =>
                                    Huffman.DecompressAll(ofd.FileName, fbd.SelectedPath, UpdateProgress, cts.Token));
                                MessageBox.Show("Decompression completed!");
                            }
                            catch (OperationCanceledException)
                            {
                                MessageBox.Show("Decompression canceled.");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error: " + ex.Message);
                            }
                            finally
                            {
                                btnCompress.Enabled = true;
                                btnDecompressAll.Enabled = true;
                                btnExtractSingle.Enabled = true;
                                btnCancel.Enabled = false;
                                UpdateProgress(0);
                            }
                        }
                    }
                }
            }
        }

        private async void BtnExtractSingle_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Huffman Archive|*.huff";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string archivePath = ofd.FileName;

                    // Read file list inside archive
                    List<string> filesInArchive = new List<string>();
                    using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                    {
                        int fileCount = archive.ReadInt32();
                        for (int i = 0; i < fileCount; i++)
                        {
                            int filenameLen = archive.ReadInt32();
                            string filename = System.Text.Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));

                            filesInArchive.Add(filename);

                            // Skip rest of this file's metadata so next filename can be read
                            int originalSize = archive.ReadInt32();

                            int freqCount = archive.ReadInt32();
                            for (int j = 0; j < freqCount; j++)
                            {
                                archive.ReadByte(); // symbol
                                archive.ReadInt32(); // count
                            }

                            int compressedDataLength = archive.ReadInt32();
                            archive.BaseStream.Seek(compressedDataLength, SeekOrigin.Current);
                        }
                    }

                    if (filesInArchive.Count == 0)
                    {
                        MessageBox.Show("Archive is empty.");
                        return;
                    }

                    // Show selection dialog
                    using (var dlg = new FileSelectionDialog(filesInArchive))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            string selectedFile = dlg.SelectedFile;

                            using (SaveFileDialog sfd = new SaveFileDialog())
                            {
                                sfd.FileName = selectedFile;
                                if (sfd.ShowDialog() == DialogResult.OK)
                                {
                                    btnCompress.Enabled = false;
                                    btnDecompressAll.Enabled = false;
                                    btnExtractSingle.Enabled = false;
                                    btnCancel.Enabled = true;
                                    progressBar.Value = 0;
                                    lblProgress.Text = "0%";

                                    cts = new CancellationTokenSource();

                                    try
                                    {
                                        bool found = false;
                                        await Task.Run(() =>
                                        {
                                            found = Huffman.ExtractSingleFile(archivePath, selectedFile, sfd.FileName, cts.Token);
                                        });
                                        if (found)
                                            MessageBox.Show("Extraction completed!");
                                        else
                                            MessageBox.Show("File not found in archive.");
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        MessageBox.Show("Extraction canceled.");
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("Error: " + ex.Message);
                                    }
                                    finally
                                    {
                                        btnCompress.Enabled = true;
                                        btnDecompressAll.Enabled = true;
                                        btnExtractSingle.Enabled = true;
                                        btnCancel.Enabled = false;
                                        UpdateProgress(0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        private void BtnCancel_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
        }

        private void UpdateProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(percent)));
                return;
            }
            progressBar.Value = percent;
            lblProgress.Text = $"{percent}%";
        }

        // Helper prompt dialog for input
        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                Form prompt = new Form()
                {
                    Width = 400,
                    Height = 150,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                Label lbl = new Label() { Left = 20, Top = 20, Text = text, Width = 340 };
                TextBox input = new TextBox() { Left = 20, Top = 50, Width = 340 };
                Button ok = new Button() { Text = "OK", Left = 270, Width = 90, Top = 80, DialogResult = DialogResult.OK };
                prompt.Controls.Add(lbl);
                prompt.Controls.Add(input);
                prompt.Controls.Add(ok);
                prompt.AcceptButton = ok;

                return prompt.ShowDialog() == DialogResult.OK ? input.Text : "";
            }
        }
    }

    public static class Huffman
    {
        private class Node
        {
            public byte? Symbol;
            public int Frequency;
            public Node Left, Right;
            public bool IsLeaf => Left == null && Right == null;
        }

        public static void CompressFiles(string[] inputPaths, string archivePath, Action<int> progressCallback, CancellationToken token)
        {
            using (var archive = new BinaryWriter(File.Create(archivePath)))
            {
                archive.Write(inputPaths.Length);

                int totalFiles = inputPaths.Length;
                for (int i = 0; i < totalFiles; i++)
                {         
                    string filePath = inputPaths[i];

                    token.ThrowIfCancellationRequested();

                    byte[] input = File.ReadAllBytes(filePath);
                    var freq = input.GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());

                    var pq = new SimplePriorityQueue<Node>();
                    foreach (var pair in freq)
                        pq.Enqueue(new Node { Symbol = pair.Key, Frequency = pair.Value }, pair.Value);

                    while (pq.Count > 1)
                    {
                        var left = pq.Dequeue();
                        var right = pq.Dequeue();
                        pq.Enqueue(new Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency }, left.Frequency + right.Frequency);
                    }

                    var root = pq.Dequeue();
                    var table = new Dictionary<byte, string>();
                    BuildTable(root, "", table);

                    string bits = string.Join("", input.Select(b => table[b]));

                    List<byte> packed = new List<byte>();
                    for (int j = 0; j < bits.Length; j += 8)
                    {
                        string byteStr = bits.Substring(j, Math.Min(8, bits.Length - j)).PadRight(8, '0');
                        packed.Add(Convert.ToByte(byteStr, 2));
                    }

                    string filename = Path.GetFileName(filePath);
                    byte[] filenameBytes = System.Text.Encoding.UTF8.GetBytes(filename);
                    archive.Write(filenameBytes.Length);
                    archive.Write(filenameBytes);
                    archive.Write(input.Length);

                    archive.Write(freq.Count);
                    foreach (var kv in freq)
                    {
                        archive.Write(kv.Key);
                        archive.Write(kv.Value);
                    }

                    archive.Write(packed.Count);
                    archive.Write(packed.ToArray());

                    int percent = (int)(((i + 1) / (double)totalFiles) * 100);
                    progressCallback?.Invoke(percent);
                }
            }
        }

        public static void DecompressAll(string archivePath, string outputFolder, Action<int> progressCallback, CancellationToken token)
        {
            using (var archive = new BinaryReader(File.OpenRead(archivePath)))
            {
                int fileCount = archive.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    token.ThrowIfCancellationRequested();

                    int filenameLen = archive.ReadInt32();
                    string filename = System.Text.Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                    int originalSize = archive.ReadInt32();

                    int freqCount = archive.ReadInt32();
                    var freq = new Dictionary<byte, int>();
                    for (int j = 0; j < freqCount; j++)
                    {
                        byte sym = archive.ReadByte();
                        int count = archive.ReadInt32();
                        freq[sym] = count;
                    }

                    int compressedDataLength = archive.ReadInt32();
                    byte[] compressedData = archive.ReadBytes(compressedDataLength);

                    var pq = new SimplePriorityQueue<Node>();
                    foreach (var pair in freq)
                        pq.Enqueue(new Node { Symbol = pair.Key, Frequency = pair.Value }, pair.Value);

                    while (pq.Count > 1)
                    {
                        var left = pq.Dequeue();
                        var right = pq.Dequeue();
                        pq.Enqueue(new Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency }, left.Frequency + right.Frequency);
                    }

                    var root = pq.Dequeue();

                    string bitString = string.Join("", compressedData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    string outputFilePath = Path.Combine(outputFolder, filename);

                    using (var output = new BinaryWriter(File.Create(outputFilePath)))
                    {
                        Node current = root;
                        int written = 0;

                        foreach (char bit in bitString)
                        {
                            current = (bit == '0') ? current.Left : current.Right;
                            if (current.IsLeaf)
                            {
                                output.Write(current.Symbol.Value);
                                written++;
                                current = root;
                                if (written >= originalSize) break;
                            }
                        }
                    }

                    int percent = (int)(((i + 1) / (double)fileCount) * 100);
                    progressCallback?.Invoke(percent);
                }
            }
        }

        public static bool ExtractSingleFile(string archivePath, string filenameToExtract, string outputFilePath, CancellationToken token)
        {
            using (var archive = new BinaryReader(File.OpenRead(archivePath)))
            {
                int fileCount = archive.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    token.ThrowIfCancellationRequested();

                    int filenameLen = archive.ReadInt32();
                    string filename = System.Text.Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                    int originalSize = archive.ReadInt32();

                    int freqCount = archive.ReadInt32();
                    var freq = new Dictionary<byte, int>();
                    for (int j = 0; j < freqCount; j++)
                    {
                        byte sym = archive.ReadByte();
                        int count = archive.ReadInt32();
                        freq[sym] = count;
                    }

                    int compressedDataLength = archive.ReadInt32();
                    byte[] compressedData = archive.ReadBytes(compressedDataLength);

                    if (filename == filenameToExtract)
                    {
                        var pq = new SimplePriorityQueue<Node>();
                        foreach (var pair in freq)
                            pq.Enqueue(new Node { Symbol = pair.Key, Frequency = pair.Value }, pair.Value);

                        while (pq.Count > 1)
                        {
                            var left = pq.Dequeue();
                            var right = pq.Dequeue();
                            pq.Enqueue(new Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency }, left.Frequency + right.Frequency);
                        }

                        var root = pq.Dequeue();

                        string bitString = string.Join("", compressedData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                        using (var output = new BinaryWriter(File.Create(outputFilePath)))
                        {
                            Node current = root;
                            int written = 0;

                            foreach (char bit in bitString)
                            {
                                current = (bit == '0') ? current.Left : current.Right;
                                if (current.IsLeaf)
                                {
                                    output.Write(current.Symbol.Value);
                                    written++;
                                    current = root;
                                    if (written >= originalSize) break;
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private static void BuildTable(Node node, string code, Dictionary<byte, string> table)
        {
            if (node.IsLeaf)
            {
                table[node.Symbol.Value] = code;
                return;
            }
            BuildTable(node.Left, code + "0", table);
            BuildTable(node.Right, code + "1", table);
        }
    }

    public class SimplePriorityQueue<T>
    {
        private List<(T item, int priority)> heap = new List<(T item, int priority)>();

        public int Count => heap.Count;

        public void Enqueue(T item, int priority)
        {
            heap.Add((item, priority));
            heap.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public T Dequeue()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            var item = heap[0];
            heap.RemoveAt(0);
            return item.item;
        }
    }
}
public class FileSelectionDialog : Form
{
    public string SelectedFile { get; private set; }
    private ListBox listBox;
    private Button btnOk;
    private Button btnCancel;

    public FileSelectionDialog(List<string> files)
    {
        this.Text = "Select File to Extract";
        this.Width = 400;
        this.Height = 300;
        this.StartPosition = FormStartPosition.CenterParent;

        listBox = new ListBox()
        {
            Dock = DockStyle.Top,
            Height = 200,
            SelectionMode = SelectionMode.One
        };
        listBox.Items.AddRange(files.ToArray());
        this.Controls.Add(listBox);

        btnOk = new Button() { Text = "Extract", Dock = DockStyle.Left, Width = 100 };
        btnOk.Click += (s, e) =>
        {
            if (listBox.SelectedItem != null)
            {
                SelectedFile = listBox.SelectedItem.ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a file.");
            }
        };
        this.Controls.Add(btnOk);

        btnCancel = new Button() { Text = "Cancel", Dock = DockStyle.Right, Width = 100 };
        btnCancel.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };
        this.Controls.Add(btnCancel);
    }
}
