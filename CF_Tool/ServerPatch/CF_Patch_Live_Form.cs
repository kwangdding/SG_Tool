namespace SG_Tool.CF_Tool.ServerPatch
{
    public class CF_Patch_Live_Form : UserControl
    {
        bool m_bSetting = false;
        Button m_btnFileMove = null!;
        Button m_btnUpload_Purge = null!;
        Button m_btnPatch = null!;
        Button m_btnPatch2 = null!;
        TextBox m_txtLog = null!;
        TextBox m_txtParameter = null!;
        TextBox m_txtParameter2 = null!;
        TextBox m_txtParameter3 = null!;
        TextBox m_txtParameter4 = null!;
        CheckBox m_checkDownload = null!;
        CheckBox m_checkCopy = null!;
        CheckBox m_checkSVN = null!;

        Dictionary<CF_DataType, string> m_dicData = new()
        {
            { CF_DataType.URL, "" }, { CF_DataType.ID, "" }, { CF_DataType.PW, "" },
            { CF_DataType.Akamai, "" }, { CF_DataType.Akamai_ID, "" }, { CF_DataType.Akamai_Key, "" },
            { CF_DataType.LocalPath, "" }, { CF_DataType.TargetPath, "" }, { CF_DataType.Version, "" },
            { CF_DataType.PatchLocalPath, "" }, { CF_DataType.PatchPath, "" }, { CF_DataType.Login_ServerInfo, "" },
            { CF_DataType.WinPath, "" }, { CF_DataType.Patcher, "" }, { CF_DataType.Purge, "" },
            { CF_DataType.RemotePath, "" }, { CF_DataType.SVNPath, "" }, { CF_DataType.VersionPath, "" },
        };

        const string c_strConfigFile = @"CF\CF_liveData.cfg";

        public CF_Patch_Live_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            m_bSetting = SG_Common.SetPatchData(c_strConfigFile, m_dicData);
            this.BackColor = Color.WhiteSmoke;

            var topPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                WrapContents = true,
                BackColor = Color.AliceBlue
            };

            m_btnFileMove = new Button { Text = "패치파일 복사", Width = 120, Height = 30, Margin = new Padding(5) };
            m_btnUpload_Purge = new Button { Text = "CDN 업로드", Width = 120, Height = 30, Margin = new Padding(5) };
            m_btnPatch = new Button { Text = "Patch", Width = 100, Height = 30, Margin = new Padding(5) };
            m_btnPatch2 = new Button { Text = "Patch(통합파일)", Width = 140, Height = 30, Margin = new Padding(5) };

            m_btnFileMove.Click += FileMove_Click;
            m_btnUpload_Purge.Click += Upload_Purge_Click;
            m_btnPatch.Click += Patch_Click;
            m_btnPatch2.Click += PatchTotal_Click;

            m_txtParameter = SG_Common.CreateLabeledTextBox("Version:", 200, m_dicData[CF_DataType.Version]);
            m_txtParameter2 = SG_Common.CreateLabeledTextBox("라이브 점검일:", 60, "0604");
            m_txtParameter3 = SG_Common.CreateLabeledTextBox("ClientVersion:", 60, "0");
            m_txtParameter4 = SG_Common.CreateLabeledTextBox("통합파일 버전:", 150, "QA01~QA03");

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

            m_checkDownload = new CheckBox { Text = "다운로드", AutoSize = true, Checked = true, Margin = new Padding(5) };
            m_checkCopy = new CheckBox { Text = "파일복사", AutoSize = true, Checked = true, Margin = new Padding(5) };
            m_checkSVN = new CheckBox { Text = "SVN 커밋", AutoSize = true, Checked = true, Margin = new Padding(5) };

            List<Control> controlGroup = new();
            switch (CF_Tool_Form.CurrentViewType)
            {
                case CF_Tool_Form.ViewType.All:
                    controlGroup.AddRange(new Control[] { m_btnFileMove, m_btnUpload_Purge, m_btnPatch, m_btnPatch2, m_checkDownload, m_checkCopy, m_checkSVN });
                    break;
                case CF_Tool_Form.ViewType.QA:
                case CF_Tool_Form.ViewType.CDN:
                    controlGroup.AddRange(new Control[] { m_btnFileMove, m_btnUpload_Purge });
                    break;
                case CF_Tool_Form.ViewType.Live:
                    controlGroup.AddRange(new Control[] { m_btnPatch, m_btnPatch2, m_checkDownload, m_checkCopy, m_checkSVN });
                    break;
            }

            foreach (var txt in new[] { m_txtParameter, m_txtParameter2, m_txtParameter3, m_txtParameter4 })
            {
                if (txt?.Parent != null) controlGroup.Add(txt.Parent);
            }

            topPanel.Controls.AddRange(controlGroup.ToArray());

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
        void PatchTotal_Click(object? sender, EventArgs e) => ProcessPatchTotal();

        void ProcessFileMove()
        {
            if (!m_bSetting)
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 Live환경 실행 할 수 없습니다.", 0, false, true);
            else
                SG_Common.FileMove(m_txtLog, m_dicData, m_txtParameter.Text.Trim(), 1, m_txtParameter2.Text.Trim(), c_strConfigFile);
        }

        async void CDNUploadAndPurge()
        {
            if (!m_bSetting)
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 Live환경 실행 할 수 없습니다.", 0, false, true);
            else
            {
                await SG_Common.UploadFTP(m_txtLog, m_dicData);

                await SG_Common.CDNSVNCommit(m_txtLog, m_dicData[CF_DataType.SVNPath], m_dicData, m_txtParameter.Text.Trim(), true, 1, m_txtParameter2.Text.Trim());
            }
        }

        async void ProcessPatch()
        {
            
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, "[ProcessPatch] cfg 파일이 없어 Live환경 실행 할 수 없습니다.", 0, false, true);
                return;
            }

            using var progressForm = new Progress_Form("서버 패치파일 작업");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "서버 패치 시작...");
                string strParam = m_txtParameter.Text.Trim(); // ex) CF_PH_Patch_2505_QA12
                string strDate = m_txtParameter2.Text.Trim();
                string[] parts = strParam.Split('_');

                SG_Common.Log(m_txtLog, $"[ProcessPatch] 패치 시작 {strParam}, {strDate}");

                if (parts.Length < 5 || !parts[4].StartsWith("QA"))
                {
                    SG_Common.Log(m_txtLog, $"[ProcessPatch] 파라미터 형식이 잘못되었습니다: {strParam}", 0, false, true);
                    return;
                }

                string patchLocalPath = m_dicData[CF_DataType.PatchLocalPath];
                string target = parts[3]; // ex) 2505
                int curQAVersion = int.Parse(parts[4].Replace("QA", ""));

                // 1. 최신 날짜_MA 폴더 3개 찾기
                var latestMAFolders = new DirectoryInfo(patchLocalPath)
                    .GetDirectories("*_MA")
                    .OrderByDescending(d => d.Name)
                    .Take(3) // 최근 3개
                    .ToList();

                if (latestMAFolders == null)
                {
                    SG_Common.Log(m_txtLog, "[ProcessPatch] 최신 MA 폴더를 찾을 수 없습니다.", 0, false, true);
                    return;
                }

                await Task.Delay(10000);
                progressForm.UpdateProgress(30, "패치파일 버전 확인 중..");

            
                // 2. 최신 QA 버전 확인
                int latestAppliedQA = 0;

                foreach (var latestMAFolder in latestMAFolders)
                {
                    SG_Common.Log(m_txtLog, $"[ProcessPatch]  : {latestMAFolder.Name} : {latestMAFolder.GetDirectories().Length} : {latestAppliedQA}");

                    foreach (var dir in latestMAFolder.GetDirectories())
                    {
                        if (dir.Name.StartsWith($"CF_PH_HOST_Patch_{target}_QA"))
                        {
                            string versionPart = dir.Name.Replace($"CF_PH_HOST_Patch_{target}_QA", "");
                            if (int.TryParse(versionPart, out int ver) && ver > latestAppliedQA)
                                latestAppliedQA = ver;
                        }
                        else if (dir.Name.StartsWith($"CF_PH_Server_Patch_{target}_QA"))
                        {
                            string versionPart = dir.Name.Replace($"CF_PH_Server_Patch_{target}_QA", "");
                            if (int.TryParse(versionPart, out int ver) && ver > latestAppliedQA)
                                latestAppliedQA = ver;
                        }
                    }
                }
                
                SG_Common.Log(m_txtLog, $"[ProcessPatch] 적용된 마지막 QA 버전: {latestAppliedQA}, 현재 버전: {curQAVersion}");

                string strPath = $"{parts[0]}_{parts[1]}_{parts[2]}_{parts[3]}";
                string localDownloadPath = Path.Combine(m_dicData[CF_DataType.PatchLocalPath], $"{strDate}_MA");

                SG_Common.SetDirectory(localDownloadPath);
                string targetHostPath = Path.Combine(m_dicData[CF_DataType.PatchPath], "HOSTSERVER");
                string targetServerPath = Path.Combine(m_dicData[CF_DataType.PatchPath], "SERVER");

                progressForm.UpdateProgress(60, "서버 패치파일 FTP 다운로드 후 파일복사 중..");
                await Task.Delay(5000);

                if (latestAppliedQA == curQAVersion)
                {
                    latestAppliedQA = curQAVersion - 1;
                    SG_Common.Log(m_txtLog, $"[ProcessPatch] 마지막 버전 동일해서 최신 파일 {curQAVersion} 다음 프로세스 진행.");
                }

                // 3. 최신 → 현재 버전까지 순차 복사
                for (int qa = latestAppliedQA + 1; qa <= curQAVersion; qa++)
                {
                    string qaVersion = $"QA{qa:D2}";
                    string ftpUrl = $"{m_dicData[CF_DataType.URL]}/{strPath}/{qaVersion}";
                    SG_Common.Log(m_txtLog, $"[ProcessPatch] Download URL {m_checkDownload.Checked} : {ftpUrl}");

                    if (m_checkDownload.Checked)
                    {
                        await SG_Common.DownloadFromFtp(m_txtLog, ftpUrl, m_dicData[CF_DataType.ID], m_dicData[CF_DataType.PW], localDownloadPath, 2, strParam, c_strConfigFile, progressForm);
                    }
                    else
                    {
                        SG_Common.Log(m_txtLog, $"[ProcessPatch] Download skipped for {qaVersion}");
                    }

                    string hostPatchRoot = Path.Combine(localDownloadPath, $"CF_PH_HOST_Patch_{target}_{qaVersion}");
                    string serverPatchRoot = Path.Combine(localDownloadPath, $"CF_PH_Server_Patch_{target}_{qaVersion}");

                    var patchTargets = new[]
                    {
                        new { Type = "HOST", Keyword = "cf_hostsrv", Root = hostPatchRoot, TargetPath = targetHostPath },
                        new { Type = "SERVER", Keyword = "cf_broadcastsrv", Root = hostPatchRoot, TargetPath = targetServerPath },
                        new { Type = "SERVER", Keyword = "CF_PH_Server_Patch", Root = serverPatchRoot, TargetPath = targetServerPath }
                    };

                    foreach (var patch in patchTargets)
                    {
                        // D:\Build\0723_MA\CF_PH_Server_Patch....
                        var sourceDirs = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                            .Where(dir => dir.Contains(patch.Keyword))
                            .ToArray();

                        SG_Common.Log(m_txtLog, $"[ProcessPatch][01] Keyword : {patch.Keyword} 대상 폴더 수: {sourceDirs.Length} {patch.TargetPath}");

                        if (sourceDirs.Length == 0)
                        {
                            var sourceDirs1 = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                                .Where(dir => dir.Contains("CF_PH_HOST_Patch"))
                                .ToArray();

                            SG_Common.Log(m_txtLog, $"[ProcessPatch][02] Keyword : {patch.Keyword} 대상 폴더 수: {sourceDirs1.Length}");

                            foreach (string SourcePath in sourceDirs1)
                            {
                                // D:\Build\0723_MA\CF_PH_Server_Patch\cf_hostsrv
                                sourceDirs = Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories)
                                    .Where(dir => dir.Contains(patch.Keyword))
                                    .ToArray();

                                foreach (string fullSourcePath in sourceDirs)
                                {
                                    string relativePath = SG_Common.GetRelativePath(patch.Root, fullSourcePath);
                                    string targetSubPath = Path.Combine(patch.TargetPath, relativePath);
                                    if (patch.Type == "HOST")
                                    {
                                        var partsRel = relativePath.Split(Path.DirectorySeparatorChar, (char)StringSplitOptions.RemoveEmptyEntries).Skip(1);
                                        targetSubPath = Path.Combine(patch.TargetPath, Path.Combine(partsRel.ToArray()));
                                    }
                                    SG_Common.Log(m_txtLog, $"[ProcessPatch][COPY 0] {m_checkCopy.Checked} : {fullSourcePath} → {targetSubPath}");

                                    if (m_checkCopy.Checked)
                                    {
                                        SG_Common.CopyAllFiles(m_txtLog, fullSourcePath, targetSubPath);
                                    }
                                    else
                                    {
                                        SG_Common.Log(m_txtLog, $"[ProcessPatch] Copy skipped for {relativePath}");
                                    }
                                    await Task.Delay(1000);
                                }
                            }
                        }
                        else
                        {
                            foreach (string fullSourcePath in sourceDirs)
                            {
                                string relativePath = SG_Common.GetRelativePath(patch.Root, fullSourcePath);
                                string targetSubPath = Path.Combine(patch.TargetPath, relativePath);

                                if (patch.Type == "HOST")
                                {
                                    var partsRel = relativePath.Split(Path.DirectorySeparatorChar, (char)StringSplitOptions.RemoveEmptyEntries).Skip(1);
                                    targetSubPath = Path.Combine(patch.TargetPath, Path.Combine(partsRel.ToArray()));
                                }

                                SG_Common.Log(m_txtLog, $"[ProcessPatch][COPY 1] {m_checkCopy.Checked} : {fullSourcePath} → {targetSubPath}");

                                if (m_checkCopy.Checked)
                                {
                                    SG_Common.CopyAllFiles(m_txtLog, fullSourcePath, targetSubPath);
                                }
                                else
                                {
                                    SG_Common.Log(m_txtLog, $"[ProcessPatch] Copy skipped for {relativePath}");
                                }
                                await Task.Delay(1000);
                            }
                        }
                    }
                }

                if (m_checkSVN.Checked)
                {
                    await Task.Delay(5000);
                    progressForm.UpdateProgress(70, "LoginIni 파일 업데이트 중..");
                    await SG_Common.UpdateLoginIni(m_txtLog, m_txtParameter3.Text.Trim(), m_dicData[CF_DataType.Login_ServerInfo]);

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(80, "SVN 커밋 중..");
                    await SG_Common.CDNSVNCommit(m_txtLog, m_dicData[CF_DataType.PatchPath], m_dicData, strParam, false);
                }
                else
                {
                    SG_Common.Log(m_txtLog, "[ProcessPatch] SVN commit skipped");
                }

                SG_Common.Log(m_txtLog, "✅ [ProcessPatch] 모든 패치가 성공적으로 완료되었습니다.", 0, false, true);
                await Task.Delay(5000);
                progressForm.UpdateProgress(100, "ProcessPatch 완료");
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"❌ [ProcessPatch] 오류 발생: {ex.Message}", 0, false, true);
                throw new Exception($"❌ [ProcessPatch EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }

        async void ProcessPatchTotal()
        {
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, "cfg 파일이 없어 Live환경 실행 할 수 없습니다.", 0, false, true);
                return;
            }

            using var progressForm = new Progress_Form("서버 패치파일 작업");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "서버 패치 시작...");
                string strParam = m_txtParameter.Text.Trim(); // ex) CF_PH_Patch_2505_QA12
                string strDate = m_txtParameter2.Text.Trim();
                string[] parts = strParam.Split('_');

                if (parts.Length < 5 || !parts[4].StartsWith("QA"))
                {
                    SG_Common.Log(m_txtLog, $"파라미터 형식이 잘못되었습니다: {strParam}", 0, false, true);
                    return;
                }

                string strPath = $"{parts[0]}_{parts[1]}_{parts[2]}_{parts[3]}";
                string localDownloadPath = Path.Combine(m_dicData[CF_DataType.PatchLocalPath], $"{strDate}_MA");

                SG_Common.SetDirectory(localDownloadPath);
                string[] allDirs = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories);
                string targetHostPath = Path.Combine(m_dicData[CF_DataType.PatchPath], "HOSTSERVER");
                string targetServerPath = Path.Combine(m_dicData[CF_DataType.PatchPath], "SERVER");

                await Task.Delay(5000);
                progressForm.UpdateProgress(50, "서버 패치파일 FTP 다운로드 후 파일복사 중..");
                
                // 3. QA 통합 압축 파일 다운로드.
                string qaVersion = m_txtParameter4.Text.Trim();
                string ftpUrl = $"{m_dicData[CF_DataType.URL]}/{strPath}/{qaVersion}.zip";
                SG_Common.Log(m_txtLog, $"[ProcessPatchTotal] Download URL : {ftpUrl}");

                if (m_checkDownload.Checked)
                {
                    await SG_Common.DownloadFromFtp(m_txtLog, ftpUrl, m_dicData[CF_DataType.ID], m_dicData[CF_DataType.PW], localDownloadPath, 2, strParam, c_strConfigFile, progressForm);
                }
                else
                {
                    SG_Common.Log(m_txtLog, $"[ProcessPatchTotal] Download skipped for {qaVersion}");
                }
                
                string hostPatchRoot = Path.Combine(localDownloadPath, qaVersion, $"CF_PH_HOST_Patch");
                string serverPatchRoot = Path.Combine(localDownloadPath, qaVersion, $"CF_PH_Server_Patch");

                var patchTargets = new[]
                {
                    new { Type = "HOST", Keyword = "cf_hostsrv", Root = hostPatchRoot, TargetPath = targetHostPath },
                    new { Type = "SERVER", Keyword = "cf_broadcastsrv", Root = hostPatchRoot, TargetPath = targetServerPath },
                    new { Type = "SERVER", Keyword = "CF_PH_Server_Patch", Root = serverPatchRoot, TargetPath = targetServerPath }
                };

                foreach (var patch in patchTargets)
                {
                    // D:\Build\0723_MA\QA13~QA14\....
                    var sourceDirs = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                        .Where(dir => dir.Contains(patch.Keyword))
                        .ToArray();

                    SG_Common.Log(m_txtLog, $"[ProcessPatchTotal][01] Keyword : {patch.Keyword} 대상 폴더 수: {sourceDirs.Length} {patch.TargetPath}");

                    if (sourceDirs.Length == 0)
                    {
                        var sourceDirs1 = Directory.GetDirectories(localDownloadPath, "*", SearchOption.AllDirectories)
                            .Where(dir => dir.Contains("CF_PH_HOST_Patch"))
                            .ToArray();

                        SG_Common.Log(m_txtLog, $"[ProcessPatchTotal][02] Keyword : {patch.Keyword} 대상 폴더 수: {sourceDirs1.Length}");

                        foreach (string SourcePath in sourceDirs1)
                        {
                            // D:\Build\0723_MA\CF_PH_Server_Patch\cf_hostsrv
                            sourceDirs = Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories)
                                .Where(dir => dir.Contains(patch.Keyword))
                                .ToArray();

                            foreach (string fullSourcePath in sourceDirs)
                            {
                                string relativePath = SG_Common.GetRelativePath(patch.Root, fullSourcePath);
                                string targetSubPath = Path.Combine(patch.TargetPath, relativePath);
                                SG_Common.Log(m_txtLog, $"[ProcessPatchTotal][COPY] {m_checkCopy.Checked} : {fullSourcePath} → {targetSubPath}");

                                if (m_checkCopy.Checked)
                                {
                                    SG_Common.CopyAllFiles(m_txtLog, fullSourcePath, targetSubPath);
                                    await Task.Delay(1000);
                                }
                                else
                                {
                                    SG_Common.Log(m_txtLog, $"[ProcessPatchTotal] Copy skipped / Root : {patch.Root} , relativePath : {relativePath}");
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string fullSourcePath in sourceDirs)
                        {
                            string relativePath = SG_Common.GetRelativePath(patch.Root, fullSourcePath);
                            string targetSubPath = Path.Combine(patch.TargetPath, relativePath);
                            SG_Common.Log(m_txtLog, $"[ProcessPatchTotal][COPY] {m_checkCopy.Checked} : {fullSourcePath} → {targetSubPath}");

                            if (m_checkCopy.Checked)
                            {
                                SG_Common.CopyAllFiles(m_txtLog, fullSourcePath, targetSubPath);
                                await Task.Delay(1000);
                            }
                            else
                            {
                                SG_Common.Log(m_txtLog, $"[ProcessPatchTotal] Copy skipped for {relativePath}");
                            }
                        }
                    }
                }
                
                if (m_checkSVN.Checked)
                {
                    await Task.Delay(5000);
                    progressForm.UpdateProgress(70, "LoginIni 파일 업데이트 중..");
                    await SG_Common.UpdateLoginIni(m_txtLog, m_txtParameter3.Text.Trim(), m_dicData[CF_DataType.Login_ServerInfo]);

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(80, "SVN 커밋 중..");
                    await SG_Common.CDNSVNCommit(m_txtLog, m_dicData[CF_DataType.PatchPath], m_dicData, strParam, false);
                }
                else
                {
                    SG_Common.Log(m_txtLog, "[ProcessPatchTotal] SVN commit skipped");
                }

                await Task.Delay(5000);
                SG_Common.Log(m_txtLog, "✅ [ProcessPatchTotal] 모든 패치가 성공적으로 완료되었습니다.", 0, false, true);
                progressForm.UpdateProgress(100, "ProcessPatchTotal 완료");
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"❌ [ProcessPatchTotal] 오류 발생: {ex.Message}", 0, false, true);
                throw new Exception($"❌ [ProcessPatchTotal EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }
    }
}