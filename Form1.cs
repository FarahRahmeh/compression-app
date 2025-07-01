using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace MMProject
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource cts;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Button btnCancel;
        private Button btnPauseContinue;
        private Button btnCompress;
        private Button btnDecompressAll;
        private Button btnExtractSingle;
        private Button btnCompare;
        private Button btnCompressFolder;
        private ComboBox comboAlgorithm;
        private bool isPaused;
        private TaskCompletionSource<bool> pauseTcs;

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //
        }

        private void SetupUI()
        {
            this.Text = "File Compression Tool";
            this.Size = new Size(600, 700); 
            this.BackColor = Color.FromArgb(220, 230, 255); // Light blue background for the entire app
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // ComboBox for algorithm selection
            Label lblAlgorithm = new Label
            {
                Text = "Select Compression Algorithm:",
                Location = new Point(40, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold), 
                ForeColor = Color.Navy 
            };
            this.Controls.Add(lblAlgorithm);

            comboAlgorithm = new ComboBox
            {
                Top = 30,
                Left = 300,
                Width = 200, // Wider combo box
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            comboAlgorithm.Items.AddRange(new string[] { "Huffman", "Shannon-Fano" });
            comboAlgorithm.SelectedIndex = 0;
            this.Controls.Add(comboAlgorithm);

            // Buttons
            btnCompress = CreateButton("Compress File/Files", 80, BtnCompress_Click);
            btnDecompressAll = CreateButton("Decompress ", 150, BtnDecompressAll_Click);
            btnExtractSingle = CreateButton("Extract Single File", 220, BtnExtractSingle_Click);
            btnCompare = CreateButton("Compare Algorithms", 290, BtnCompare_Click);
            btnCompressFolder = CreateButton("Compress Folder", 360, BtnCompressFolder_Click);

            // Progress controls
            progressBar = new ProgressBar
            {
                Width = 300, 
                Height = 25,
                Top = 450,
                Left = 40,
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.DodgerBlue 
            };
            this.Controls.Add(progressBar);

            lblProgress = new Label
            {
                Top = 450,
                Left = 360,
                Width = 120,
                Text = "0%",
                Font = new Font("Segoe UI", 11, FontStyle.Bold), 
                ForeColor = Color.Navy
            };
            this.Controls.Add(lblProgress);

            btnPauseContinue = new Button
            {
                Text = "Pause",
                Width = 100, 
                Height = 40,
                Top = 490,
                Left = 40,
                Enabled = false,
                Font = new Font("Segoe UI", 11), 
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnPauseContinue.FlatAppearance.BorderSize = 0;
            btnPauseContinue.Click += BtnPauseContinue_Click;
            this.Controls.Add(btnPauseContinue);

            btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100, 
                Height = 40,
                Top = 490,
                Left = 150,
                Enabled = false,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.LightCoral,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        private Button CreateButton(string text, int top, EventHandler handler)
        {
            var btn = new Button
            {
                Text = text,
                Width = 200, 
                Height = 50, 
                Top = top,
                Left = 40,
                BackColor = Color.FromArgb(100, 149, 237), // Cornflower blue for buttons
                ForeColor = Color.White, // White text for better contrast
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold) // Larger, bold font
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += handler;
            this.Controls.Add(btn);
            return btn;
        }

        

        private string SelectedAlgorithm => comboAlgorithm.SelectedItem?.ToString() ?? "Huffman";

        private async Task CheckPause(CancellationToken token)
        {
            if (isPaused)
            {
                pauseTcs = new TaskCompletionSource<bool>();
                await pauseTcs.Task;
                token.ThrowIfCancellationRequested();
            }
        }

        private async void BtnCompressFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = SelectedAlgorithm == "Huffman" ? "Huffman Archive|*.huff" : "Shannon-Fano Archive|*.sfan";
                        sfd.FileName = "folder_archive" + (SelectedAlgorithm == "Huffman" ? ".huff" : ".sfan");

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            SetControlsState(false);
                            cts = new CancellationTokenSource();
                            isPaused = false;
                            btnPauseContinue.Text = "Pause";
                            btnCancel.Enabled = true;

                            try
                            {
                                CompressionInfo result;
                                if (SelectedAlgorithm == "Huffman")
                                    result = await Huffman.CompressFolderAsync(fbd.SelectedPath, sfd.FileName, UpdateProgress, cts.Token, CheckPause);
                                else
                                    result = await ShannonFanoHelper.CompressFolderAsync(fbd.SelectedPath, sfd.FileName, UpdateProgress, cts.Token, CheckPause);

                                MessageBox.Show($"تم ضغط المجلد بنجاح!\n\n" +
                                              $"الحجم الأصلي: {FormatSize(result.OriginalSize)}\n" +
                                              $"الحجم بعد الضغط: {FormatSize(result.CompressedSize)}\n" +
                                              $"نسبة الضغط: {result.CompressionRatio:0.00}%\n" +
                                              $"المساحة المحفوظة: {result.SpaceSaved}\n" +
                                              $"الوقت المستغرق: {FormatTime(result.TimeTaken)}",
                                              "تم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (OperationCanceledException)
                            {
                                if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                                lblProgress.Text = isPaused ? "Paused" : "تم الإلغاء";
                            }
                            catch (Exception ex)
                            {
                                if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                SetControlsState(true);
                                btnPauseContinue.Text = "Pause";
                                btnCancel.Enabled = false;
                            }
                        }
                    }
                }
            }
        }

        private async void BtnCompress_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Multiselect = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = SelectedAlgorithm == "Huffman" ? "Huffman Archive|*.huff.secure" : "Shannon-Fano Archive|*.sfan.secure";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            using (var passwordForm = new PasswordInputForm())
                            {
                                if (passwordForm.ShowDialog() != DialogResult.OK)
                                {
                                    return;
                                }

                                SetControlsState(false);
                                cts = new CancellationTokenSource();
                                isPaused = false;
                                btnPauseContinue.Text = "Pause";
                                btnCancel.Enabled = true;

                                string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                                try
                                {
                                    CompressionInfo result;
                                    if (SelectedAlgorithm == "Huffman")
                                    {
                                        result = await Huffman.CompressFilesAsync(ofd.FileNames, tempFile, UpdateProgress, cts.Token, CheckPause);
                                    }
                                    else
                                    {
                                        result = await ShannonFanoHelper.CompressFilesAsync(ofd.FileNames, tempFile, UpdateProgress, cts.Token, CheckPause);
                                    }

                                    EncryptFile(tempFile, sfd.FileName, passwordForm.Password);
                                    MessageBox.Show(
                                        $"تم الضغط والتشفير بنجاح!\n\nالحجم الأصلي: {FormatSize(result.OriginalSize)}\nالحجم بعد الضغط: {FormatSize(result.CompressedSize)}",
                                        "تم بنجاح",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                    );
                                }
                                catch (OperationCanceledException)
                                {
                                    if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                                    lblProgress.Text = isPaused ? "Paused" : "تم الإلغاء";
                                }
                                catch (Exception ex)
                                {
                                    if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                finally
                                {
                                    SetControlsState(true);
                                    btnPauseContinue.Text = "Pause";
                                    btnCancel.Enabled = false;

                                    await Task.Yield();
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();

                                    if (File.Exists(tempFile))
                                    {
                                        try { File.Delete(tempFile); }
                                        catch (IOException)
                                        {
                                            await Task.Delay(200);
                                            if (File.Exists(tempFile)) File.Delete(tempFile);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void BtnDecompressAll_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = SelectedAlgorithm == "Huffman"
                    ? "Huffman Archive|*.huff;*.huff.secure"
                    : "Shannon-Fano Archive|*.sfan;*.sfan.secure";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() != DialogResult.OK)
                        return;

                    bool isEncrypted = ofd.FileName.EndsWith(".secure");
                    string password = null;

                    if (isEncrypted)
                    {
                        using (var passwordForm = new PasswordInputForm())
                        {
                            if (passwordForm.ShowDialog() != DialogResult.OK)
                                return;
                            password = passwordForm.Password;
                        }
                    }

                    SetControlsState(false);
                    cts = new CancellationTokenSource();
                    isPaused = false;
                    btnPauseContinue.Text = "Pause";
                    btnCancel.Enabled = true;

                    string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dec");
                    try
                    {
                        // Decrypt if necessary
                        if (isEncrypted)
                        {
                            if (!DecryptFile(ofd.FileName, tempFile, password))
                            {
                                MessageBox.Show("Failed to decrypt the archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else
                        {
                            File.Copy(ofd.FileName, tempFile, true);
                        }

                        // Verify temp file exists
                        if (!File.Exists(tempFile))
                        {
                            MessageBox.Show("Temporary file could not be created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        CompressionInfo result;
                        try
                        {
                            if (SelectedAlgorithm == "Huffman")
                                result = await Huffman.DecompressAllAsync(tempFile, fbd.SelectedPath, UpdateProgress, cts.Token, CheckPause);
                            else
                                result = await ShannonFanoHelper.DecompressAllAsync(tempFile, fbd.SelectedPath, UpdateProgress, cts.Token, CheckPause);

                            // Verify that files were actually written
                            var outputFiles = Directory.GetFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories);
                            if (outputFiles.Length == 0)
                            {
                                MessageBox.Show("No files were decompressed. The archive may be empty or corrupted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            MessageBox.Show($"تم فك الضغط بنجاح!\n\nالحجم الأصلي: {FormatSize(result.OriginalSize)}\nالوقت المستغرق: {FormatTime(result.TimeTaken)}",
                                           "تم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error during decompression: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        lblProgress.Text = isPaused ? "Paused" : "تم الإلغاء";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        SetControlsState(true);
                        btnPauseContinue.Text = "Pause";
                        btnCancel.Enabled = false;
                        if (File.Exists(tempFile))
                        {
                            try { File.Delete(tempFile); }
                            catch { /* Ignore cleanup errors */ }
                        }
                    }
                }
            }
        }

        private async void BtnExtractSingle_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = SelectedAlgorithm == "Huffman" ? "Huffman Archive|*.huff.secure" : "Shannon-Fano Archive|*.sfan.secure";
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                using (var passwordForm = new PasswordInputForm())
                {
                    if (passwordForm.ShowDialog() != DialogResult.OK)
                        return;

                    string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dec");
                    try
                    {
                        DecryptFile(ofd.FileName, tempFile, passwordForm.Password);

                        if (!File.Exists(tempFile))
                        {
                            MessageBox.Show("Temporary decrypted file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var fileList = SelectedAlgorithm == "Huffman"
                            ? await Huffman.GetFileListAsync(tempFile)
                            : await ShannonFanoHelper.GetFileListAsync(tempFile);

                        if (fileList.Count == 0)
                        {
                            MessageBox.Show("No files found in the archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        using (var selector = new FileSelectorForm(fileList))
                        {
                            if (selector.ShowDialog() != DialogResult.OK)
                                return;

                            if (string.IsNullOrEmpty(selector.SelectedFile))
                            {
                                MessageBox.Show("No file was selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            using (var sfd = new SaveFileDialog { FileName = selector.SelectedFile })
                            {
                                if (sfd.ShowDialog() != DialogResult.OK)
                                    return;

                                if (string.IsNullOrEmpty(sfd.FileName))
                                {
                                    MessageBox.Show("Output file path is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }

                                cts = new CancellationTokenSource();
                                isPaused = false;
                                btnPauseContinue.Text = "Pause";
                                btnCancel.Enabled = true;

                                try
                                {
                                    bool success = SelectedAlgorithm == "Huffman"
                                        ? await Huffman.ExtractSingleFileAsync(tempFile, selector.SelectedFile, sfd.FileName, cts.Token, CheckPause)
                                        : await ShannonFanoHelper.ExtractSingleFileAsync(tempFile, selector.SelectedFile, sfd.FileName, cts.Token, CheckPause);

                                    if (success)
                                        MessageBox.Show("File extracted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    else
                                        MessageBox.Show("File not found in archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                catch (OperationCanceledException)
                                {
                                    lblProgress.Text = isPaused ? "Paused" : "Extraction canceled";
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error during extraction:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                finally
                                {
                                    btnPauseContinue.Text = "Pause";
                                    btnCancel.Enabled = false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (File.Exists(tempFile))
                        {
                            try { File.Delete(tempFile); }
                            catch { /* تجاهل أي خطأ أثناء الحذف */ }
                        }
                    }
                }
            }
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            using (var ofd = new OpenFileDialog { Multiselect = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var compareForm = new ComparisonForm(ofd.FileNames);
                    compareForm.ShowDialog();
                }
            }
        }

        private void BtnPauseContinue_Click(object sender, EventArgs e)
        {
            if (isPaused)
            {
                btnPauseContinue.Text = "Pause";
                isPaused = false;
                pauseTcs?.SetResult(true);
            }
            else
            {
                btnPauseContinue.Text = "Continue";
                isPaused = true;
                lblProgress.Text = "Pausing...";
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            btnCancel.Enabled = false;
            btnPauseContinue.Enabled = false;
            lblProgress.Text = "Canceling...";
            isPaused = false;

            // Use TrySetResult to safely unblock the task if it's paused,
            // without throwing an exception if it's already been completed.
            pauseTcs?.TrySetResult(true);
        }

        private async Task ExecuteOperation(Func<Task> operation, string successMessage, string tempFileToDelete = null)
        {
            SetControlsState(false);
            cts = new CancellationTokenSource();
            isPaused = false;
            btnPauseContinue.Text = "Pause";
            btnCancel.Enabled = true;

            try
            {
                await operation();
                MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                if (tempFileToDelete != null && File.Exists(tempFileToDelete))
                    File.Delete(tempFileToDelete);

                lblProgress.Text = isPaused ? "Paused" : "Operation canceled";
            }
            catch (Exception ex)
            {
                if (tempFileToDelete != null && File.Exists(tempFileToDelete))
                    File.Delete(tempFileToDelete);

                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsState(true);
                btnPauseContinue.Text = "Pause";
                btnCancel.Enabled = false;
            }
        }

        private void SetControlsState(bool enabled)
        {
            btnCompress.Enabled = enabled;
            btnDecompressAll.Enabled = enabled;
            btnExtractSingle.Enabled = enabled;
            btnCompare.Enabled = enabled;
            btnPauseContinue.Enabled = !enabled;
            btnCancel.Enabled = !enabled;

            if (enabled && !isPaused)
            {
                progressBar.Value = 0;
                lblProgress.Text = "0%";
            }
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

        private static void EncryptFile(string inputPath, string outputPath, string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var key = new Rfc2898DeriveBytes(password, salt, 100_000).GetBytes(32);
            aes.Key = key;
            aes.GenerateIV();

            var fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(aes.IV, 0, aes.IV.Length);

            var encryptor = aes.CreateEncryptor();
            var crypto = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);
            var fsIn = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

            fsIn.CopyTo(crypto);

            fsIn.Close();
            crypto.FlushFinalBlock();
            crypto.Close();
            fsOut.Close();
            aes.Dispose();
        }

        private static bool DecryptFile(string inputPath, string outputPath, string password)
        {
            try
            {
                using (var fsIn = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
                using (var fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] salt = new byte[16];
                    fsIn.Read(salt, 0, salt.Length);
                    byte[] iv = new byte[16];
                    fsIn.Read(iv, 0, iv.Length);

                    using (var aes = Aes.Create())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        var key = new Rfc2898DeriveBytes(password, salt, 100_000).GetBytes(32);
                        aes.Key = key;
                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor())
                        using (var crypto = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read))
                        {
                            crypto.CopyTo(fsOut);
                        }
                    }
                }
                return true;
            }
            catch (CryptographicException)
            {
                MessageBox.Show(
                    "كلمة السر خاطئة أو الملف تالف.",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                if (File.Exists(outputPath))
                {
                    try { File.Delete(outputPath); } catch { }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء فك التشفير:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                if (File.Exists(outputPath))
                {
                    try { File.Delete(outputPath); } catch { }
                }
                return false;
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "بايت", "كيلوبايت", "ميجابايت", "جيجابايت" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalSeconds < 1)
                return $"{time.TotalMilliseconds:0} ملي ثانية";
            if (time.TotalMinutes < 1)
                return $"{time.TotalSeconds:0.##} ثانية";
            return $"{time.TotalMinutes:0.##} دقيقة";
        }
    }

    public class PasswordInputForm : Form
    {
        public string Password { get; private set; }
        private TextBox txtPassword;
        private Button btnOk;
        private Button btnCancel;

        public PasswordInputForm()
        {
            this.Text = "Enter Password";
            this.Size = new Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Label for "Password:"
            var lblPassword = new Label { Text = "Password:", Top = 20, Left = 20 };

            // TextBox below "Password:"
            txtPassword = new TextBox { Top = 45, Left = 20, Width = 250, PasswordChar = '*' };

            // Note label in bright color below the TextBox
            var lblNote = new Label
            {
                Text = "No password ? just click ok.",
                Top = 75,
                Left = 20,
                Width = 270,
                ForeColor = Color.DimGray,
                AutoSize = false
            };


            // OK button
            btnOk = new Button { Text = "OK", Top = 110, Left = 120, Width = 75 };
            btnOk.Click += (s, e) => { Password = txtPassword.Text; DialogResult = DialogResult.OK; };

            // Cancel button
            btnCancel = new Button { Text = "Cancel", Top = 110, Left = 200, Width = 75 };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            // Add controls to form
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblNote);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

        public class FileSelectorForm : Form
    {
        public string SelectedFile { get; private set; }

        public FileSelectorForm(List<string> files)
        {
            this.Text = "Select File to Extract";
            this.Size = new Size(300, 400);

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DataSource = files
            };

            var btnSelect = new Button
            {
                Text = "Extract",
                Dock = DockStyle.Bottom
            };
            btnSelect.Click += (s, e) =>
            {
                SelectedFile = listBox.SelectedItem?.ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(listBox);
            this.Controls.Add(btnSelect);
        }
    }

    public class ComparisonForm : Form
    {
        private readonly string[] filePaths;
        private DataGridView dataGridView;

        public ComparisonForm(string[] files)
        {
            filePaths = files;
            InitializeComponents();
            this.Text = "Algorithm Comparison";
            this.Size = new Size(800, 400);
        }

        private void InitializeComponents()
        {
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false
            };

            dataGridView.Columns.Add("FileName", "File Name");
            dataGridView.Columns.Add("OriginalSize", "Original Size");
            dataGridView.Columns.Add("HuffmanSize", "Huffman Size");
            dataGridView.Columns.Add("HuffmanRatio", "Huffman Ratio (%)");
            dataGridView.Columns.Add("HuffmanTime", "Huffman Time");
            dataGridView.Columns.Add("ShannonFanoSize", "Shannon-Fano Size");
            dataGridView.Columns.Add("ShannonFanoRatio", "Shannon-Fano Ratio (%)");
            dataGridView.Columns.Add("ShannonFanoTime", "Shannon-Fano Time");

            var btnCompare = new Button
            {
                Text = "Run Comparison",
                Dock = DockStyle.Bottom
            };
            btnCompare.Click += BtnCompare_Click;

            this.Controls.Add(dataGridView);
            this.Controls.Add(btnCompare);
        }

        private async void BtnCompare_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            btn.Enabled = false;

            for (int i = 0; i < filePaths.Length; i++)
            {
                var file = filePaths[i];
                var fileInfo = new FileInfo(file);

                var huffmanResult = await TestAlgorithm(
                    (files, output, progress, token) => Huffman.CompressFilesAsync(files, output, progress, token, async (t) => await Task.CompletedTask),
                    file);
                var shannonResult = await TestAlgorithm(
                    (files, output, progress, token) => ShannonFanoHelper.CompressFilesAsync(files, output, progress, token, async (t) => await Task.CompletedTask),
                    file);

                dataGridView.Rows.Add(
                    fileInfo.Name,
                    fileInfo.Length,
                    huffmanResult.CompressedSize,
                    ((fileInfo.Length - huffmanResult.CompressedSize) / (double)fileInfo.Length * 100).ToString("0.00"),
                    huffmanResult.TimeTaken,
                    shannonResult.CompressedSize,
                    ((fileInfo.Length - shannonResult.CompressedSize) / (double)fileInfo.Length * 100).ToString("0.00"),
                    shannonResult.TimeTaken);
            }

            btn.Enabled = true;
        }

        private async Task<AlgorithmTestResult> TestAlgorithm(
            Func<string[], string, Action<int>, CancellationToken, Task> compressFunc,
            string filePath)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                await compressFunc(new[] { filePath }, tempFile, null, CancellationToken.None);
                stopwatch.Stop();

                return new AlgorithmTestResult
                {
                    CompressedSize = new FileInfo(tempFile).Length,
                    TimeTaken = stopwatch.Elapsed
                };
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }

    public class CompressionResult
    {
        public string FileName { get; }
        public long OriginalSize { get; }
        public long HuffmanSize { get; }
        public TimeSpan HuffmanTime { get; }
        public long ShannonFanoSize { get; }
        public TimeSpan ShannonFanoTime { get; }

        public CompressionResult(string fileName, long originalSize,
                               long huffmanSize, TimeSpan huffmanTime,
                               long shannonFanoSize, TimeSpan shannonFanoTime)
        {
            FileName = fileName;
            OriginalSize = originalSize;
            HuffmanSize = huffmanSize;
            HuffmanTime = huffmanTime;
            ShannonFanoSize = shannonFanoSize;
            ShannonFanoTime = shannonFanoTime;
        }

        public double GetCompressionRatioHuffman() =>
            (OriginalSize - HuffmanSize) / (double)OriginalSize * 100;

        public double GetCompressionRatioShannonFano() =>
            (OriginalSize - ShannonFanoSize) / (double)OriginalSize * 100;
    }

    public class AlgorithmTestResult
    {
        public long CompressedSize { get; set; }
        public TimeSpan TimeTaken { get; set; }
    }

    public static class Huffman
    {
        public class Node
        {
            public byte? Symbol { get; set; }
            public int Frequency { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }
            public bool IsLeaf => Left == null && Right == null;
        }

        public static async Task<List<string>> GetFileListAsync(string archivePath)
        {
            return await Task.Run(() =>
            {
                var fileList = new List<string>();

                try
                {
                    using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                    {
                        int fileCount = archive.ReadInt32();
                        if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                        for (int i = 0; i < fileCount; i++)
                        {
                            int filenameLen = archive.ReadInt32();
                            if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                            string relativePath = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                            fileList.Add(relativePath);

                            int originalSize = archive.ReadInt32();

                            int freqCount = archive.ReadInt32();
                            if (freqCount < 0) throw new InvalidDataException($"Corrupted archive: invalid frequency count ({freqCount}).");

                            for (int j = 0; j < freqCount; j++)
                            {
                                archive.ReadByte();
                                archive.ReadInt32();
                            }

                            int compressedLength = archive.ReadInt32();
                            if (compressedLength < 0) throw new InvalidDataException($"Corrupted archive: invalid compressed data length ({compressedLength}).");

                            archive.ReadBytes(compressedLength);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Failed to read archive file list: {ex.Message}", ex);
                }

                return fileList;
            });
        }

        public static async Task<CompressionInfo> CompressFolderAsync(string folderPath, string archivePath,
                                                    Action<int> progressCallback,
                                                    CancellationToken token,
                                                    Func<CancellationToken, Task> checkPause)
        {
            return await Task.Run(async () =>
            {
                var info = new CompressionInfo();
                var stopwatch = Stopwatch.StartNew();

                var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                info.OriginalSize = allFiles.Sum(f => new FileInfo(f).Length);

                using (var archive = new BinaryWriter(File.Create(archivePath)))
                {
                    archive.Write(allFiles.Length);

                    int totalFiles = allFiles.Length;
                    for (int i = 0; i < totalFiles; i++)
                    {
                        await checkPause(token);
                        token.ThrowIfCancellationRequested();

                        string filePath = allFiles[i];
                        string relativePath = GetRelativePath(filePath, folderPath);
                        byte[] input = File.ReadAllBytes(filePath);
                        var freq = BuildFrequencyTable(input);

                        var root = BuildHuffmanTree(freq);
                        var table = BuildEncodingTable(root);

                        string bits = EncodeData(input, table);
                        List<byte> packed = PackBits(bits);

                        WriteFileToArchive(archive, relativePath, input.Length, freq, packed);

                        int percent = (int)(((i + 1) / (double)totalFiles) * 100);
                        progressCallback?.Invoke(percent);
                    }
                }

                stopwatch.Stop();
                info.CompressedSize = new FileInfo(archivePath).Length;
                info.TimeTaken = stopwatch.Elapsed;

                return info;
            }, token);
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            Uri fullUri = new Uri(fullPath);
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
                                        .Replace('/', Path.DirectorySeparatorChar));
        }
        public static async Task<CompressionInfo> CompressFilesAsync(string[] inputPaths, string archivePath,
                                                           Action<int> progressCallback,
                                                           CancellationToken token,
                                                           Func<CancellationToken, Task> checkPause)
        {
            var info = new CompressionInfo();
            info.OriginalSize = inputPaths.Sum(f => new FileInfo(f).Length);
            var stopwatch = Stopwatch.StartNew();
            var compressedDataList = new List<(string relativePath, int originalSize, Dictionary<byte, int> freq, List<byte> packed)>();
            int totalFiles = inputPaths.Length;
            int processedFiles = 0;

            try
            {
                // Process files sequentially to allow for proper async pause/continue
                await Task.Run(async () =>
                {
                    foreach (var filePath in inputPaths)
                    {
                        await checkPause(token); // Correctly await the pause check
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            byte[] input = File.ReadAllBytes(filePath);
                            var freq = BuildFrequencyTable(input);
                            var root = BuildHuffmanTree(freq);
                            var table = BuildEncodingTable(root);
                            string bits = EncodeData(input, table);
                            List<byte> packed = PackBits(bits);
                            string relativePath = Path.GetFileName(filePath);

                            lock (compressedDataList)
                            {
                                compressedDataList.Add((relativePath, input.Length, freq, packed));
                            }

                            // Progress update
                            processedFiles++;
                            int percent = processedFiles * 100 / totalFiles;
                            progressCallback?.Invoke(percent);
                        }
                        catch (Exception ex)
                        {
                            // Log error and rethrow to cancel operation
                            Debug.WriteLine($"Error compressing {filePath}: {ex.Message}");
                            throw;
                        }
                    }
                }, token);

                // Sort compressed data by original file order to ensure consistent archive structure
                compressedDataList.Sort((a, b) => Array.IndexOf(inputPaths, Path.Combine(Path.GetDirectoryName(inputPaths[0]), a.relativePath))
                                                .CompareTo(Array.IndexOf(inputPaths, Path.Combine(Path.GetDirectoryName(inputPaths[0]), b.relativePath))));

                // Write to archive sequentially
                using (var archive = new BinaryWriter(File.Create(archivePath)))
                {
                    archive.Write(inputPaths.Length);
                    foreach (var data in compressedDataList)
                    {
                        token.ThrowIfCancellationRequested();
                        WriteFileToArchive(archive, data.relativePath, data.originalSize, data.freq, data.packed);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
                throw;
            }
            catch (Exception ex)
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
                throw new InvalidOperationException($"Compression failed: {ex.Message}", ex);
            }

            stopwatch.Stop();
            info.CompressedSize = new FileInfo(archivePath).Length;
            info.TimeTaken = stopwatch.Elapsed;

            return info;
        }


        public static async Task<CompressionInfo> DecompressAllAsync(string archivePath, string outputFolder,
                                                      Action<int> progressCallback,
                                                      CancellationToken token,
                                                      Func<CancellationToken, Task> checkPause)
        {
            return await Task.Run(async () =>
            {
                var info = new CompressionInfo();
                info.CompressedSize = new FileInfo(archivePath).Length;
                var stopwatch = Stopwatch.StartNew();
                long decompressedSize = 0;

                try
                {
                    using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                    {
                        int fileCount = archive.ReadInt32();
                        if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                        for (int i = 0; i < fileCount; i++)
                        {
                            await checkPause(token);
                            token.ThrowIfCancellationRequested();

                            int filenameLen = archive.ReadInt32();
                            if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                            string relativePath = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                            int originalSize = archive.ReadInt32();

                            var freq = ReadFrequencyTable(archive);
                            var root = BuildHuffmanTree(freq);

                            int compressedDataLength = archive.ReadInt32();
                            if (compressedDataLength < 0) throw new InvalidDataException($"Corrupted archive: invalid compressed data length ({compressedDataLength}).");

                            byte[] compressedData = archive.ReadBytes(compressedDataLength);

                            string outputFilePath = Path.Combine(outputFolder, Path.GetFileName(relativePath)); // Use file name only
                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                            DecodeData(compressedData, root, originalSize, outputFilePath);

                            if (File.Exists(outputFilePath))
                            {
                                decompressedSize += new FileInfo(outputFilePath).Length;
                            }
                            else
                            {
                                throw new IOException($"Failed to create output file: {outputFilePath}");
                            }

                            int percent = (int)(((i + 1) / (double)fileCount) * 100);
                            progressCallback?.Invoke(percent);
                        }
                    }

                    stopwatch.Stop();
                    info.OriginalSize = decompressedSize;
                    info.TimeTaken = stopwatch.Elapsed;

                    return info;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Failed to decompress archive: {ex.Message}", ex);
                }
            }, token);
        }

        public static async Task<bool> ExtractSingleFileAsync(
            string archivePath,
            string fileToExtract,
            string outputFilePath,
            CancellationToken token,
            Func<CancellationToken, Task> checkPause)
        {
            return await Task.Run(async () =>
            {
                using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                {
                    int fileCount = archive.ReadInt32();
                    if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                    for (int i = 0; i < fileCount; i++)
                    {
                        await checkPause(token);
                        token.ThrowIfCancellationRequested();

                        int filenameLen = archive.ReadInt32();
                        if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                        string relativePath = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                        int originalSize = archive.ReadInt32();

                        int freqCount = archive.ReadInt32();
                        if (freqCount < 0) throw new InvalidDataException($"Corrupted archive: invalid frequency count ({freqCount}).");

                        var freq = new Dictionary<byte, int>();
                        for (int j = 0; j < freqCount; j++)
                        {
                            byte sym = archive.ReadByte();
                            int count = archive.ReadInt32();
                            freq[sym] = count;
                        }

                        int compressedLength = archive.ReadInt32();
                        if (compressedLength < 0) throw new InvalidDataException($"Corrupted archive: invalid compressed length ({compressedLength}).");

                        byte[] compressedData = archive.ReadBytes(compressedLength);

                        if (Path.GetFileName(relativePath).Equals(Path.GetFileName(fileToExtract), StringComparison.OrdinalIgnoreCase))
                        {
                            var root = BuildHuffmanTree(freq);
                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                            DecodeData(compressedData, root, originalSize, outputFilePath);
                            return File.Exists(outputFilePath);
                        }
                    }
                }

                return false;
            }, token);
        }

        private static Dictionary<byte, int> BuildFrequencyTable(byte[] input)
        {
            return input.GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());
        }

        private static Node BuildHuffmanTree(Dictionary<byte, int> freq)
        {
            var pq = new PriorityQueue<Node>();
            foreach (var pair in freq)
                pq.Enqueue(new Node { Symbol = pair.Key, Frequency = pair.Value }, pair.Value);

            while (pq.Count > 1)
            {
                var left = pq.Dequeue();
                var right = pq.Dequeue();
                pq.Enqueue(new Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency },
                           left.Frequency + right.Frequency);
            }

            return pq.Dequeue();
        }

        private static Dictionary<byte, string> BuildEncodingTable(Node root)
        {
            var table = new Dictionary<byte, string>();
            BuildTableRecursive(root, "", table);
            return table;
        }

        private static void BuildTableRecursive(Node node, string code, Dictionary<byte, string> table)
        {
            if (node.IsLeaf)
            {
                table[node.Symbol.Value] = code.Length > 0 ? code : "0"; // Ensure non-empty code
                return;
            }
            BuildTableRecursive(node.Left, code + "0", table);
            BuildTableRecursive(node.Right, code + "1", table);
        }

        private static string EncodeData(byte[] input, Dictionary<byte, string> table)
        {
            return string.Join("", input.Select(b => table[b]));
        }

        private static List<byte> PackBits(string bits)
        {
            var packed = new List<byte>();
            for (int i = 0; i < bits.Length; i += 8)
            {
                string byteStr = bits.Substring(i, Math.Min(8, bits.Length - i)).PadRight(8, '0');
                packed.Add(Convert.ToByte(byteStr, 2));
            }
            return packed;
        }

        private static void WriteFileToArchive(BinaryWriter archive, string relativePath,
                                       int originalSize, Dictionary<byte, int> freq,
                                       List<byte> packed)
        {
            byte[] filenameBytes = Encoding.UTF8.GetBytes(relativePath);
            archive.Write(filenameBytes.Length);
            archive.Write(filenameBytes);
            archive.Write(originalSize);
            archive.Write(freq.Count);
            foreach (var kv in freq)
            {
                archive.Write(kv.Key);
                archive.Write(kv.Value);
            }
            archive.Write(packed.Count);
            archive.Write(packed.ToArray());
        }

        private static Dictionary<byte, int> ReadFrequencyTable(BinaryReader archive)
        {
            int freqCount = archive.ReadInt32();
            var freq = new Dictionary<byte, int>();
            for (int j = 0; j < freqCount; j++)
            {
                byte sym = archive.ReadByte();
                int count = archive.ReadInt32();
                freq[sym] = count;
            }
            return freq;
        }

        private static void DecodeData(byte[] compressedData, Node root,
                                     int originalSize, string outputFilePath)
        {
            string bitString = string.Join("", compressedData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

            try
            {
                using (var output = new BinaryWriter(File.Create(outputFilePath)))
                {
                    Node current = root;
                    int written = 0;

                    foreach (char bit in bitString)
                    {
                        current = (bit == '0') ? current.Left : current.Right;
                        if (current == null)
                            throw new InvalidDataException("Invalid bit sequence in compressed data.");

                        if (current.IsLeaf)
                        {
                            output.Write(current.Symbol.Value);
                            written++;
                            current = root;
                            if (written >= originalSize) break;
                        }
                    }

                    if (written != originalSize)
                        throw new InvalidDataException($"Expected {originalSize} bytes, but wrote {written} bytes.");
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(outputFilePath))
                {
                    try { File.Delete(outputFilePath); } catch { }
                }
                throw new InvalidDataException($"Failed to decode data: {ex.Message}", ex);
            }
        }
    }

    public static class ShannonFanoHelper
    {
        private static Dictionary<byte, int> ReadFrequencyTable(BinaryReader archive)
        {
            int freqCount = archive.ReadInt32();
            var freq = new Dictionary<byte, int>();
            for (int j = 0; j < freqCount; j++)
            {
                byte sym = archive.ReadByte();
                int count = archive.ReadInt32();
                freq[sym] = count;
            }
            return freq;
        }

        private static Huffman.Node BuildHuffmanTree(Dictionary<byte, int> freq)
        {
            var pq = new PriorityQueue<Huffman.Node>();
            foreach (var pair in freq)
                pq.Enqueue(new Huffman.Node { Symbol = pair.Key, Frequency = pair.Value }, pair.Value);

            while (pq.Count > 1)
            {
                var left = pq.Dequeue();
                var right = pq.Dequeue();
                pq.Enqueue(new Huffman.Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency },
                           left.Frequency + right.Frequency);
            }

            return pq.Dequeue();
        }

        private static void DecodeData(byte[] compressedData, Huffman.Node root,
                                     int originalSize, string outputFilePath)
        {
            string bitString = string.Join("", compressedData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

            try
            {
                using (var output = new BinaryWriter(File.Create(outputFilePath)))
                {
                    Huffman.Node current = root;
                    int written = 0;

                    foreach (char bit in bitString)
                    {
                        current = (bit == '0') ? current.Left : current.Right;
                        if (current == null)
                            throw new InvalidDataException("Invalid bit sequence in compressed data.");

                        if (current.IsLeaf)
                        {
                            output.Write(current.Symbol.Value);
                            written++;
                            current = root;
                            if (written >= originalSize) break;
                        }
                    }

                    if (written != originalSize)
                        throw new InvalidDataException($"Expected {originalSize} bytes, but wrote {written} bytes.");
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(outputFilePath))
                {
                    try { File.Delete(outputFilePath); } catch { }
                }
                throw new InvalidDataException($"Failed to decode data: {ex.Message}", ex);
            }
        }

        public static async Task<List<string>> GetFileListAsync(string archivePath)
        {
            return await Task.Run(() =>
            {
                var fileList = new List<string>();

                try
                {
                    using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                    {
                        int fileCount = archive.ReadInt32();
                        if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                        for (int i = 0; i < fileCount; i++)
                        {
                            int filenameLen = archive.ReadInt32();
                            if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                            string relativePath = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                            fileList.Add(relativePath);

                            archive.ReadInt32();

                            int codeTableSize = archive.ReadInt32();
                            for (int j = 0; j < codeTableSize; j++)
                            {
                                archive.ReadByte();
                                archive.ReadString();
                            }

                            int compressedLength = archive.ReadInt32();
                            if (compressedLength < 0) throw new InvalidDataException($"Corrupted archive: invalid compressed data length ({compressedLength}).");

                            archive.ReadBytes(compressedLength);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Failed to read archive file list: {ex.Message}", ex);
                }

                return fileList;
            });
        }

        public static async Task<CompressionInfo> CompressFolderAsync(string folderPath, string archivePath,
                                                      Action<int> progressCallback,
                                                      CancellationToken token,
                                                      Func<CancellationToken, Task> checkPause)
        {
            var info = new CompressionInfo();
            var stopwatch = Stopwatch.StartNew();
            var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            info.OriginalSize = allFiles.Sum(f => new FileInfo(f).Length);

            await Task.Run(async () =>
            {
                using (var archive = new BinaryWriter(File.Create(archivePath)))
                {
                    archive.Write(allFiles.Length);
                    int total = allFiles.Length;

                    for (int i = 0; i < total; i++)
                    {
                        await checkPause(token);
                        token.ThrowIfCancellationRequested();
                        string filePath = allFiles[i];
                        string relativePath = GetRelativePath(filePath, folderPath);
                        byte[] input = File.ReadAllBytes(filePath);

                        var (compressedData, codeTable) = ShannonFano.Compress(input);
                        WriteFileToArchive(archive, relativePath, input.Length, codeTable, compressedData);

                        int percent = (int)(((i + 1) / (double)total) * 100);
                        progressCallback?.Invoke(percent);
                    }
                }
            }, token);

            stopwatch.Stop();
            info.CompressedSize = new FileInfo(archivePath).Length;
            info.TimeTaken = stopwatch.Elapsed;

            return info;
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            Uri fullUri = new Uri(fullPath);
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
                                    .Replace('/', Path.DirectorySeparatorChar));
        }



        public static async Task<CompressionInfo> CompressFilesAsync(string[] inputPaths, string archivePath,
                                                              Action<int> progressCallback,
                                                              CancellationToken token,
                                                              Func<CancellationToken, Task> checkPause)
        {
            var info = new CompressionInfo();
            info.OriginalSize = inputPaths.Sum(f => new FileInfo(f).Length);
            var stopwatch = Stopwatch.StartNew();
            var compressedDataList = new List<(string relativePath, int originalSize, Dictionary<byte, string> codeTable, byte[] compressedData)>();
            int totalFiles = inputPaths.Length;
            int processedFiles = 0;

            try
            {
                // Process files sequentially to allow for proper async pause/continue
                await Task.Run(async () =>
                {
                    foreach (var filePath in inputPaths)
                    {
                        await checkPause(token); // Correctly await the pause check
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            byte[] input = File.ReadAllBytes(filePath);
                            var (compressedData, codeTable) = ShannonFano.Compress(input);
                            string relativePath = Path.GetFileName(filePath);

                            lock (compressedDataList)
                            {
                                compressedDataList.Add((relativePath, input.Length, codeTable, compressedData));
                            }

                            // Progress update
                            processedFiles++;
                            int percent = processedFiles * 100 / totalFiles;
                            progressCallback?.Invoke(percent);
                        }
                        catch (Exception ex)
                        {
                            // Log error and rethrow to cancel operation
                            Debug.WriteLine($"Error compressing {filePath}: {ex.Message}");
                            throw;
                        }
                    }
                }, token);

                // Sort compressed data by original file order to ensure consistent archive structure
                compressedDataList.Sort((a, b) => Array.IndexOf(inputPaths, Path.Combine(Path.GetDirectoryName(inputPaths[0]), a.relativePath))
                                                .CompareTo(Array.IndexOf(inputPaths, Path.Combine(Path.GetDirectoryName(inputPaths[0]), b.relativePath))));

                // Write to archive sequentially
                using (var archive = new BinaryWriter(File.Create(archivePath)))
                {
                    archive.Write(inputPaths.Length);
                    foreach (var data in compressedDataList)
                    {
                        token.ThrowIfCancellationRequested();
                        WriteFileToArchive(archive, data.relativePath, data.originalSize, data.codeTable, data.compressedData);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
                throw;
            }
            catch (Exception ex)
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
                throw new InvalidOperationException($"Compression failed: {ex.Message}", ex);
            }

            stopwatch.Stop();
            info.CompressedSize = new FileInfo(archivePath).Length;
            info.TimeTaken = stopwatch.Elapsed;

            return info;
        }

        public static async Task<CompressionInfo> DecompressAllAsync(string archivePath, string outputFolder,
                                                             Action<int> progressCallback,
                                                             CancellationToken token,
                                                             Func<CancellationToken, Task> checkPause)
        {
            var info = new CompressionInfo();
            info.CompressedSize = new FileInfo(archivePath).Length;
            var stopwatch = Stopwatch.StartNew();
            long decompressedSize = 0;

            try
            {
                using (var archive = new BinaryReader(File.OpenRead(archivePath)))
                {
                    int fileCount = archive.ReadInt32();
                    if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                    for (int i = 0; i < fileCount; i++)
                    {
                        await checkPause(token);
                        token.ThrowIfCancellationRequested();

                        int filenameLen = archive.ReadInt32();
                        if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                        string filename = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));
                        int originalSize = archive.ReadInt32();

                        int freqCount = archive.ReadInt32();
                        if (freqCount < 0) throw new InvalidDataException($"Corrupted archive: invalid code table size ({freqCount}).");

                        var codeTable = new Dictionary<byte, string>();
                        for (int j = 0; j < freqCount; j++)
                        {
                            byte sym = archive.ReadByte();
                            string code = archive.ReadString();
                            codeTable[sym] = code;
                        }

                        int compressedLength = archive.ReadInt32();
                        if (compressedLength < 0) throw new InvalidDataException($"Corrupted archive: invalid compressed data length ({compressedLength}).");

                        byte[] compressedData = archive.ReadBytes(compressedLength);

                        string outputFilePath = Path.Combine(outputFolder, Path.GetFileName(filename)); // Use file name only
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                        var outputBytes = ShannonFano.Decompress(compressedData, codeTable, originalSize);
                        File.WriteAllBytes(outputFilePath, outputBytes);

                        if (File.Exists(outputFilePath))
                        {
                            decompressedSize += new FileInfo(outputFilePath).Length;
                        }
                        else
                        {
                            throw new IOException($"Failed to create output file: {outputFilePath}");
                        }

                        int percent = (int)(((i + 1) / (double)fileCount) * 100);
                        progressCallback?.Invoke(percent);
                    }
                }

                stopwatch.Stop();
                info.OriginalSize = decompressedSize;
                info.TimeTaken = stopwatch.Elapsed;

                return info;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to decompress archive: {ex.Message}", ex);
            }
        }

        public static async Task<bool> ExtractSingleFileAsync(string archivePath,
                                                     string fileToExtract,
                                                     string outputFilePath,
                                                     CancellationToken token,
                                                     Func<CancellationToken, Task> checkPause)
        {
            return await Task.Run(async () =>
            {
                using (var stream = File.Open(archivePath, FileMode.Open))
                using (var archive = new BinaryReader(stream))
                {
                    int fileCount = archive.ReadInt32();
                    if (fileCount < 0) throw new InvalidDataException("Corrupted archive: invalid file count.");

                    for (int i = 0; i < fileCount; i++)
                    {
                        await checkPause(token);
                        token.ThrowIfCancellationRequested();

                        long fileStartPosition = stream.Position;

                        try
                        {
                            int filenameLen = archive.ReadInt32();
                            if (filenameLen <= 0) throw new InvalidDataException($"Corrupted archive: invalid filename length ({filenameLen}).");

                            string filename = Encoding.UTF8.GetString(archive.ReadBytes(filenameLen));

                            if (!Path.GetFileName(filename).Equals(Path.GetFileName(fileToExtract), StringComparison.OrdinalIgnoreCase))
                            {
                                int originalSize = archive.ReadInt32();
                                int codeTableSize = archive.ReadInt32();
                                for (int j = 0; j < codeTableSize; j++)
                                {
                                    archive.ReadByte();
                                    archive.ReadString();
                                }
                                int compressedSize = archive.ReadInt32();
                                stream.Position += compressedSize;
                                continue;
                            }

                            int fileOriginalSize = archive.ReadInt32();
                            int fileCodeTableSize = archive.ReadInt32();

                            var codeTable = new Dictionary<byte, string>();
                            for (int j = 0; j < fileCodeTableSize; j++)
                            {
                                byte symbol = archive.ReadByte();
                                string code = archive.ReadString();
                                codeTable[symbol] = code;
                            }

                            int compressedDataLength = archive.ReadInt32();
                            byte[] compressedData = archive.ReadBytes(compressedDataLength);

                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                            var decompressedData = ShannonFano.Decompress(compressedData, codeTable, fileOriginalSize);
                            File.WriteAllBytes(outputFilePath, decompressedData);

                            return File.Exists(outputFilePath);
                        }
                        catch (Exception ex)
                        {
                            stream.Position = fileStartPosition;
                            Debug.WriteLine($"Error processing file {i}: {ex.Message}");
                            continue;
                        }
                    }
                }
                return false;
            }, token);
        }

        private static void WriteFileToArchive(BinaryWriter archive, string filePath,
                                     int originalSize, Dictionary<byte, string> codeTable,
                                     byte[] compressedData)
        {
            byte[] filenameBytes = Encoding.UTF8.GetBytes(filePath); // Store full relative path
            archive.Write(filenameBytes.Length);
            archive.Write(filenameBytes);
            archive.Write(originalSize);
            archive.Write(codeTable.Count);
            foreach (var kv in codeTable)
            {
                archive.Write(kv.Key);
                archive.Write(kv.Value);
            }
            archive.Write(compressedData.Length);
            archive.Write(compressedData);
        }
    }

    public static class ShannonFano
    {
        public class Symbol
        {
            public byte Value;
            public int Frequency;
            public string Code = "";
        }

        public static (byte[] compressedData, Dictionary<byte, string> codeTable) Compress(byte[] input)
        {
            var table = BuildTable(input);
            var bitString = string.Join("", input.Select(b => table[b]));
            var compressed = PackBits(bitString);
            return (compressed, table);
        }

        public static byte[] Decompress(byte[] compressedData, Dictionary<byte, string> codeTable, int originalSize)
        {
            try
            {
                var reverseTable = codeTable.ToDictionary(kv => kv.Value, kv => kv.Key);
                var bitString = string.Join("", compressedData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                var output = new List<byte>();
                var buffer = "";
                int written = 0;

                foreach (char bit in bitString)
                {
                    buffer += bit;
                    if (reverseTable.TryGetValue(buffer, out byte value))
                    {
                        output.Add(value);
                        buffer = "";
                        written++;
                        if (written >= originalSize) break;
                    }
                }

                if (written != originalSize)
                    throw new InvalidDataException($"Expected {originalSize} bytes, but wrote {written} bytes.");

                return output.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to decompress data: {ex.Message}", ex);
            }
        }

        private static Dictionary<byte, string> BuildTable(byte[] input)
        {
            var freq = input.GroupBy(b => b)
                            .Select(g => new Symbol { Value = g.Key, Frequency = g.Count() })
                            .OrderByDescending(s => s.Frequency)
                            .ToList();

            GenerateCodes(freq, 0, freq.Count - 1);
            return freq.ToDictionary(s => s.Value, s => s.Code.Length > 0 ? s.Code : "0");
        }

        private static void GenerateCodes(List<Symbol> symbols, int start, int end)
        {
            if (start >= end) return;

            int total = symbols.Skip(start).Take(end - start + 1).Sum(s => s.Frequency);
            int sum = 0;
            int split = start;

            for (int i = start; i <= end; i++)
            {
                sum += symbols[i].Frequency;
                if (sum >= total / 2) { split = i; break; }
            }

            for (int i = start; i <= split; i++) symbols[i].Code += "0";
            for (int i = split + 1; i <= end; i++) symbols[i].Code += "1";

            GenerateCodes(symbols, start, split);
            GenerateCodes(symbols, split + 1, end);
        }

        private static byte[] PackBits(string bitString)
        {
            var packed = new List<byte>();
            for (int i = 0; i < bitString.Length; i += 8)
            {
                string byteStr = bitString.Substring(i, Math.Min(8, bitString.Length - i)).PadRight(8, '0');
                packed.Add(Convert.ToByte(byteStr, 2));
            }
            return packed.ToArray();
        }
    }

    public class PriorityQueue<T>
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

    public class CompressionInfo
    {
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public TimeSpan TimeTaken { get; set; }

        public double CompressionRatio =>
            OriginalSize > 0 ? (OriginalSize - CompressedSize) / (double)OriginalSize * 100 : 0;

        public string SpaceSaved =>
            FormatSize(OriginalSize - CompressedSize);

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}