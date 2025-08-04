using System.IO.Compression;
using System.Net;

namespace SG_Tool.CF_Tool.ServerPatch
{
    public class CF_Patch_QA_Form : UserControl
    {
        bool m_bSetting = false;
        Button m_btnFileMove = null!;
        Button m_btnUpload_Purge = null!;
        Button m_btnPatch = null!;
        Button m_btnApply = null!;
        Button m_btnFTPUpload = null!;
        TextBox m_txtLog = null!;
        TextBox m_txtParameter = null!;
        TextBox m_txtParameter3 = null!;

        TextBox m_txtParamStart = null!;
        TextBox m_txtParamEnd = null!;
        const string c_strUpdatePath = $@"C:\pmang\crossfire";

        Dictionary<CF_DataType, string> m_dicData = new Dictionary<CF_DataType, string>
        {
            { CF_DataType.URL, "" },
            { CF_DataType.ID, "" },
            { CF_DataType.PW, "" },
            { CF_DataType.Akamai, "" },
            { CF_DataType.Akamai_ID, "" },
            { CF_DataType.Akamai_Key, "" },
            { CF_DataType.LocalPath, "" },
            { CF_DataType.TargetPath, "" },
            { CF_DataType.Version, "" },
            { CF_DataType.PatchLocalPath, "" },
            { CF_DataType.PatchPath, "" },
            { CF_DataType.Login_ServerInfo, "" },
            { CF_DataType.WinPath, "" },
            { CF_DataType.Patcher, "" },
            { CF_DataType.Purge, "" },
            { CF_DataType.RemotePath, "" },
            { CF_DataType.SVNPath, "" },
            { CF_DataType.VersionPath, "" },
        };

        const string c_strConfigFile = @"CF\CF_qaData.cfg";

        public CF_Patch_QA_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            m_bSetting = SG_Common.SetPatchData(c_strConfigFile, m_dicData);
            this.BackColor = Color.WhiteSmoke;

            // 상단 패널 (버튼 + 파라미터)
            var topPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(10),
                AutoSize = true,
                BackColor = Color.LightBlue
            };

            m_btnFileMove = new Button { Text = "패치 다운로드", Width = 120, Height = 30, Margin = new Padding(5) };
            m_btnUpload_Purge = new Button { Text = "CDN 업로드", Width = 120, Height = 30, Margin = new Padding(5) };
            m_btnPatch = new Button { Text = "Patch", Width = 100, Height = 30, Margin = new Padding(5) };
            m_btnApply = new Button { Text = "적용", Width = 100, Height = 30, Margin = new Padding(5) };
            m_btnFTPUpload = new Button { Text = "FTPUpload", Width = 100, Height = 30, Margin = new Padding(5) };

            m_btnFileMove.Click += FileMove_Click;
            m_btnUpload_Purge.Click += Upload_Purge_Click;
            m_btnPatch.Click += Patch_Click;
            m_btnApply.Click += Apply_Click;
            m_btnFTPUpload.Click += FTPUpload_Click;

            m_txtParameter = SG_Common.CreateLabeledTextBox("Version:", 180, m_dicData[CF_DataType.Version]);
            m_txtParameter3 = SG_Common.CreateLabeledTextBox("ClientVersion:", 60, "0");

            m_txtParamStart = SG_Common.CreateLabeledTextBox("Start:", 60, "QA01");
            m_txtParamEnd = SG_Common.CreateLabeledTextBox("End:", 60, "QA11");

            // ini 파일에서 ClientVersion 추출
            if (File.Exists(m_dicData[CF_DataType.Login_ServerInfo]))
            {
                var lines = File.ReadAllLines(m_dicData[CF_DataType.Login_ServerInfo]);
                foreach (var line in lines)
                {
                    if (line.Contains("#")) continue;
                    if (line.Contains("ClientVersion"))
                    {
                        var ver_parts = line.Split('=', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                        if (ver_parts.Length == 2)
                            m_txtParameter3.Text = ver_parts[1].Trim();
                        break;
                    }
                }
            }

            switch (CF_Tool_Form.CurrentViewType)
            {
                default:
                case CF_Tool_Form.ViewType.All:
                    var controlsToAdd = new List<Control> { m_btnFileMove, m_btnUpload_Purge, m_btnPatch, m_btnApply, m_btnFTPUpload };
                    if (m_txtParameter.Parent != null) controlsToAdd.Add(m_txtParameter.Parent);
                    if (m_txtParameter3.Parent != null) controlsToAdd.Add(m_txtParameter3.Parent);
                    if (m_txtParamStart.Parent != null) controlsToAdd.Add(m_txtParamStart.Parent);
                    if (m_txtParamEnd.Parent != null) controlsToAdd.Add(m_txtParamEnd.Parent);
                    topPanel.Controls.AddRange(controlsToAdd.ToArray());
                    break;
                case CF_Tool_Form.ViewType.Live:
                case CF_Tool_Form.ViewType.CDN:
                    var controlsToAdd2 = new List<Control> { m_btnFileMove, m_btnUpload_Purge };
                    if (m_txtParameter.Parent != null) controlsToAdd2.Add(m_txtParameter.Parent);
                    topPanel.Controls.AddRange(controlsToAdd2.ToArray());
                    break;
                case CF_Tool_Form.ViewType.QA:
                    var controlsToAdd3 = new List<Control> { m_btnPatch, m_btnApply, m_btnFTPUpload };
                    if (m_txtParameter.Parent != null) controlsToAdd3.Add(m_txtParameter.Parent);
                    if (m_txtParameter3.Parent != null) controlsToAdd3.Add(m_txtParameter3.Parent);
                    if (m_txtParamStart.Parent != null) controlsToAdd3.Add(m_txtParamStart.Parent);
                    if (m_txtParamEnd.Parent != null) controlsToAdd3.Add(m_txtParamEnd.Parent);
                    topPanel.Controls.AddRange(controlsToAdd3.ToArray());
                    break;
            }

            // 로그창 설정
            m_txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Window
            };

            // 전체 레이아웃 구성
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(m_txtLog, 0, 1);

            Controls.Add(mainPanel);
        }

        void FileMove_Click(object? sender, EventArgs e) => ProcessFileMove();
        void Upload_Purge_Click(object? sender, EventArgs e) => CDNUploadAndPurge();
        void Patch_Click(object? sender, EventArgs e) => ProcessPatch();
        void Apply_Click(object? sender, EventArgs e) => ApplyPatch();
        void FTPUpload_Click(object? sender, EventArgs e) => UploadFTP();

        void ProcessFileMove()
        {
            if (!m_bSetting)
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 QA환경 실행 할 수 없습니다.", 0, false, true);
            else
                SG_Common.FileMove(m_txtLog, m_dicData, m_txtParameter.Text.Trim(), 0, "0000", c_strConfigFile);
        }

        async void CDNUploadAndPurge()
        {
            if (!m_bSetting)
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 QA환경 실행 할 수 없습니다.", 0, false, true);
            else
            {
                await SG_Common.UploadFTP(m_txtLog, m_dicData);

                await SG_Common.CDNSVNCommit(m_txtLog, m_dicData[CF_DataType.SVNPath], m_dicData, m_txtParameter.Text.Trim(), true);
            }
        }

        async void ProcessPatch()
        {
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, "[ProcessPatch] cfg 파일이 없어 QA환경 실행 할 수 없습니다.", 0, false, true);
                return;
            }

            using var progressForm = new Progress_Form("서버 패치파일 작업");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "서버 패치 시작...");
                string parameter = m_txtParameter.Text;
                var parts = parameter.Split('_');
                if (parts.Length < 5)
                    throw new FormatException("버전 파라미터 형식이 잘못되었습니다.");

                string strTarget = parts[3];
                string strQA = parts[4];
                string strPath = $"{parts[0]}_{parts[1]}_{parts[2]}_{strTarget}/{strQA}";

                string ftpUrl = $"{m_dicData[CF_DataType.URL]}/{strPath}";
                string localDownloadPath = Path.Combine($@"{m_dicData[CF_DataType.PatchLocalPath]}\CF_PH_Patch_{strTarget}", strQA);
                string targetPath = m_dicData[CF_DataType.PatchPath];

                string hostRoot = Path.Combine(localDownloadPath, $"CF_PH_HOST_Patch_{strTarget}_{strQA}");
                string serverRoot = Path.Combine(localDownloadPath, $"CF_PH_Server_Patch_{strTarget}_{strQA}");

                SG_Common.Log(m_txtLog, $"[ProcessPatch] [INFO] HostPatchRoot: {hostRoot}, ServerPatchRoot: {serverRoot}");

                await Task.Delay(5000);
                progressForm.UpdateProgress(30, "서버 패치파일 FTP 다운로드 중..");

                SG_Common.SetDirectory(localDownloadPath);
                await SG_Common.DownloadFromFtp(m_txtLog, ftpUrl, m_dicData[CF_DataType.ID], m_dicData[CF_DataType.PW], localDownloadPath, 2, parameter, c_strConfigFile, progressForm);

                var patchTargets = new[]
                {
                    new { Type = "HOST", Keyword = "CF_PH_HOST_Patch", Root = hostRoot },
                    new { Type = "SERVER", Keyword = "CF_PH_Server_Patch", Root = serverRoot }
                };

                await Task.Delay(5000);
                progressForm.UpdateProgress(60, "패치파일 복사 중..");

                foreach (var patch in patchTargets)
                {
                    var sourceDirs = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                        .Where(dir => dir.Contains(patch.Keyword))
                        .ToArray();

                    SG_Common.Log(m_txtLog, $"[ProcessPatch] [INFO] {patch.Type} 복사 대상 폴더 수: {sourceDirs.Length}");

                    foreach (var src in sourceDirs)
                    {
                        string relativePath = SG_Common.GetRelativePath(patch.Root, src);
                        string targetSubPath = Path.Combine(targetPath, relativePath);

                        SG_Common.Log(m_txtLog, $"[ProcessPatch] [COPY] ({patch.Type}) {src} → {targetSubPath}");
                        SG_Common.CopyAllFiles(m_txtLog, src, targetSubPath);

                        // gamesrv 확장 복사
                        if (src.Contains("cf_gamesrv"))
                        {
                            for (int i = 2; i <= 4; i++)
                            {
                                string extraPath = Path.Combine(targetPath, relativePath + $"0{i}");
                                SG_Common.CopyAllFiles(m_txtLog, src, extraPath);
                            }
                        }
                    }
                }

                await Task.Delay(5000);
                progressForm.UpdateProgress(80, "LoginIni 파일 업데이트 중..");
                await SG_Common.UpdateLoginIni(m_txtLog, m_txtParameter3.Text.Trim(), m_dicData[CF_DataType.Login_ServerInfo]);

                await Task.Delay(5000);
                progressForm.UpdateProgress(90, "SVN 커밋 중..");
                await SG_Common.CDNSVNCommit(m_txtLog, targetPath, m_dicData, parameter, false);

                SG_Common.Log(m_txtLog, "[ProcessPatch] [완료] Patch process completed successfully!", 0, false, true);
                await Task.Delay(5000);
                progressForm.UpdateProgress(100, "ProcessPatch 완료");
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"[ProcessPatch] [ERROR] {ex.Message}", 0, false, true);
                throw new Exception($"❌ [ProcessPatch EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }

        async void ApplyPatch()
        {
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, "cfg 파일이 없어 QA환경 실행 할 수 없습니다.", 0, false, true);
            }
            else
            {
                try
                {
                    string iniFilePath = Path.Combine(m_dicData[CF_DataType.PatchPath], "DBGWMGR.ini");
                    if (File.Exists(iniFilePath))
                    {
                        // D:\SERVER\DBGWMGR.ini >>> C:\Windows\DBGWMGR.ini 복사
                        SG_Common.CopyFile(m_txtLog, iniFilePath, m_dicData[CF_DataType.WinPath]);
                    }

                    await SG_Common.SVNUpdateAsync(m_txtLog, c_strUpdatePath);
                }
                catch (Exception ex)
                {
                    SG_Common.Log(m_txtLog, $"[ApplyPatch ERROR] {ex.Message}", 0, false, true);
                    throw new Exception($"❌ [ApplyPatch EXCEPTION] : {ex.Message}");
                }
            }
        }

        async void UploadFTP()
        {
            SG_Common.Log(m_txtLog, $"[UploadFTP] 업로드 시작");
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, "[ProcessPatch] cfg 파일이 없어 QA환경 실행 할 수 없습니다.", 0, false, true);
                return;
            }

            using var progressForm = new Progress_Form("UploadFTP");
            progressForm.Show();
            await Task.Delay(1000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "UploadFTP 시작...");

                string parameter = m_txtParameter.Text;
                var parts = parameter.Split('_');
                if (parts.Length < 5)
                    throw new FormatException("버전 파라미터 형식이 잘못되었습니다.");

                string strTarget = parts[3];
                string strPath = $"{parts[0]}_{parts[1]}_{parts[2]}_{strTarget}";

                int start = int.Parse(m_txtParamStart.Text.Replace("QA", ""));
                int end = int.Parse(m_txtParamEnd.Text.Replace("QA", ""));
                string qaName = $"{m_txtParamStart.Text}~{m_txtParamEnd.Text}";
                string basePath = Path.Combine(m_dicData[CF_DataType.PatchLocalPath], $"CF_PH_Patch_{strTarget}");
                string targetPath = Path.Combine(basePath, qaName);
                SG_Common.SetDirectory(targetPath);

                progressForm.UpdateProgress(30, $"UploadFTP 파일 복사 시작 {qaName}...");
                await Task.Delay(5000);

                for (int i = start; i <= end; i++)
                {
                    string qa = $"QA{i:D2}";
                    string localDownloadPath = Path.Combine(basePath, qa);
                    string hostRoot = Path.Combine(localDownloadPath, $"CF_PH_HOST_Patch_{strTarget}_{qa}");
                    string serverRoot = Path.Combine(localDownloadPath, $"CF_PH_Server_Patch_{strTarget}_{qa}");

                    var patchTargets = new[]
                    {
                        new { Type = "HOST", Keyword = "CF_PH_HOST_Patch", Root = hostRoot },
                        new { Type = "SERVER", Keyword = "CF_PH_Server_Patch", Root = serverRoot }
                    };

                    if (!Directory.Exists(localDownloadPath))
                    {
                        SG_Common.Log(m_txtLog, $"⚠️[UploadFTP][Patch] 루트 경로 없음: {localDownloadPath} → 건너뜀");
                        continue;
                    }

                    foreach (var patch in patchTargets)
                    {
                        var sourceDirs = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                            .Where(dir => dir.Contains(patch.Keyword))
                            .ToArray();

                        if (sourceDirs.Length == 0) continue;

                        foreach (var src in sourceDirs)
                        {
                            if (!Directory.Exists(src))
                            {
                                SG_Common.Log(m_txtLog, $"⚠️[UploadFTP][Patch] 소스 경로 없음: {src} → 건너뜀");
                                continue;
                            }

                            string relativePath = SG_Common.GetRelativePath(patch.Root, src);
                            string targetSubPath = Path.Combine(targetPath, patch.Keyword, relativePath);
                            SG_Common.CopyAllFiles(m_txtLog, src, targetSubPath);
                        }
                    }
                }

                progressForm.UpdateProgress(60, $"UploadFTP 압축시작 {qaName}...");
                await Task.Delay(5000);

                // 압축
                string zipFilePath = $"{targetPath}.zip";
                ZipFile.CreateFromDirectory(targetPath, zipFilePath);

                // FTP 업로드
                string ftpUrl = $"{m_dicData[CF_DataType.URL]}/{strPath}/{Path.GetFileName(zipFilePath)}";
                SG_Common.Log(m_txtLog, $"[UploadFTP] 업로드 대상: {ftpUrl}");
                progressForm.UpdateProgress(80, $"UploadFTP FTP 업로드 시작 {qaName}...");
                await Task.Delay(5000);

                var uploadRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
                uploadRequest.Credentials = new NetworkCredential(m_dicData[CF_DataType.ID], m_dicData[CF_DataType.PW]);

                byte[] buffer = new byte[8192];
                int bytesRead;
                long uploadedBytes = 0;
                int currentPercent = 0;

                using (var reqStream = await uploadRequest.GetRequestStreamAsync())
                using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                {
                    long totalBytes = fileStream.Length;
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await reqStream.WriteAsync(buffer, 0, bytesRead);
                        uploadedBytes += bytesRead;

                        int progressPercent = (int)((uploadedBytes * 100L) / totalBytes);
                        if (progressPercent != currentPercent)
                        {
                            string msg = $"📤 FTP 업로드 중... {progressPercent}% ({uploadedBytes:N0}/{totalBytes:N0} bytes)";
                            progressForm.UpdateProgress(progressPercent, msg);
                            currentPercent = progressPercent;
                        }
                    }
                }

                await Task.Delay(1000);
                progressForm.UpdateProgress(100, "UploadFTP 완료");
                SG_Common.Log(m_txtLog, $"[UploadFTP] {zipFilePath} 업로드 완료");
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"[UploadFTP] [ERROR] {ex.Message}", 0, false, true);
                throw new Exception($"❌ [UploadFTP EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }
    }
}