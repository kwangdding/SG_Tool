using SG_Tool.Log;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.Text.RegularExpressions;

namespace SG_Tool.L9_Tool.FTP
{
    public class JsonUpdate : UserControl
    {
        bool m_bSetting = false;
        string m_strSelectedServer = string.Empty;
        ComboBox m_comboBox = null!;
        Button m_btnJsonUpdate = null!;
        TextBox m_txtParameter = null!;
        TextBox m_txtLog = null!;

        string m_strConfigFile = $@"L9\l9_Data.cfg";
        Dictionary<L9FTP_DataType, string> m_dicData = new Dictionary<L9FTP_DataType, string>();

        public JsonUpdate(EnLoad9_Type enLoad9_Type)
        {
            m_strConfigFile = $@"{enLoad9_Type}\L9_Data.cfg";
            InitializeUI();
        }

        void InitializeUI()
        {
            m_bSetting = SG_Common.SetPatchData(m_strConfigFile, m_dicData);
            Text = "L9 Manager";
            Size = new Size(900, 800);
            MinimumSize = new Size(700, 500);
            BackColor = Color.WhiteSmoke;

            // 상단 버튼 및 파라미터 영역
            var topPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(10),
                AutoSize = true,
                BackColor = Color.AliceBlue
            };

            m_comboBox = new ComboBox
            {
                Items = {
                    "qa0",
                    "qa1",
                    "qa2",
                    "qa3",
                    "review",
                    "live"
                },
                Width = 100,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5, 6, 5, 0), // 높이에 따라 조절 (예: 6)
                Anchor = AnchorStyles.Left
            };
            m_comboBox.SelectedIndexChanged += ServerChangeList;

            m_txtParameter = SG_Common.CreateLabeledTextBox("Version:", 180, m_dicData.ContainsKey(L9FTP_DataType.JsonUpdate) ? m_dicData[L9FTP_DataType.JsonUpdate] : "없음");
            m_btnJsonUpdate = new Button
            {
                Text = "패치파일 복사",
                Width = 120,
                Height = 30,
                Margin = new Padding(5, 6, 5, 0),
                Anchor = AnchorStyles.Left
            };
            m_btnJsonUpdate.Click += FileMove_Click;

            var controlsToAdd = new List<Control> { m_comboBox };
            if (m_txtParameter.Parent != null) controlsToAdd.Add(m_txtParameter.Parent);
            controlsToAdd.Add(m_btnJsonUpdate);

            topPanel.Controls.AddRange(controlsToAdd.ToArray());

            // 로그 영역
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

            // 메인 패널 구성
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(m_txtLog, 0, 1);

            Controls.Add(mainPanel);
        }

        void ServerChangeList(object sender, EventArgs e)
        {
            m_strSelectedServer = m_comboBox.SelectedItem?.ToString() ?? string.Empty;
        }


        void FileMove_Click(object sender, EventArgs e) => ProcessFileMove();

        void ProcessFileMove()
        {
            if (!m_bSetting)
                SystemLog_Form.LogMessage(m_txtLog, $"cfg 파일이 없어 Live환경 실행 할 수 없습니다.");
            else
                UploadFileToS3();
        }

        async void UploadFileToS3()
        {
            try
            {
                SystemLog_Form.LogMessage(m_txtLog, $"[UploadFileToS3()] 다운로드 시작..");

                // 1. FTP로 파일을 다운로드 받는다. // project-lord/Web/PatchConfig/{strDate}/Operation.ProjectL.Protocol.json
                var s3Client = new AmazonS3Client(m_dicData[L9FTP_DataType.AwsAccessKey], m_dicData[L9FTP_DataType.AwsSecretKey], RegionEndpoint.APNortheast2);
                var transferUtility = new TransferUtility(s3Client);
                string strKey = @$"Web/PatchConfig/{m_txtParameter.Text.Trim()}/Operation.ProjectL.Protocol.json";
                string strlocalFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lordnine-config", $"{m_txtParameter.Text.Trim()}", "Operation.ProjectL.Protocol.json");

                await SG_Common.DownloadAsyncToS3(m_txtLog, transferUtility, strlocalFilePath, m_dicData[L9FTP_DataType.S3FileBucket], strKey);

                if (m_strSelectedServer == string.Empty)
                {
                    SystemLog_Form.LogMessage(m_txtLog, $"[UploadFileToS3()] {m_strSelectedServer}서버를 선택해주세요.");
                    return;
                }

                // 2. 다운 로드 받은 파일을 qa0~3, review, live 환경에 맞춰 이름 변경 및 파일 변경 후 저장.
                string strOld = GetURL(strlocalFilePath);// m_dicData[L9FTP_DataType.NX3URL];
                string strNew = $"https://{m_strSelectedServer}-lord-op-api.game.playstove.com:443";
                ReplaceJsonUrls(strlocalFilePath, strOld, strNew);

                // 3. 서버 시간조정 파라미터 도메인 변경.
                strOld = "http://127.0.0.1:55001";
                if (m_strSelectedServer.Contains("qa1"))
                {
                    ReplaceJsonUrls(strlocalFilePath, strOld, "http://10.162.4.56:55001");
                }
                else if (m_strSelectedServer.Contains("qa2"))
                {
                    ReplaceJsonUrls(strlocalFilePath, strOld, "http://10.168.192.23:55001");
                }
                else if (m_strSelectedServer.Contains("qa3"))
                {
                    ReplaceJsonUrls(strlocalFilePath, strOld, "http://10.162.4.28:55001");
                }
                else
                {
                    RemoveSpecificKeys(strlocalFilePath); // 서버 시간조정 파라미터 제거.
                }
                
                var s3UploadClient = new AmazonS3Client(m_dicData[L9FTP_DataType.AwsAccessKey], m_dicData[L9FTP_DataType.AwsSecretKey], RegionEndpoint.APNortheast1);
                var transferUploadUtility = new TransferUtility(s3UploadClient);

                // 4. 파일 s3 환경에 업로드
                strKey = @$"{m_strSelectedServer}/Operation.ProjectL.Protocol.json";
                SystemLog_Form.LogMessage(m_txtLog, $"[UploadFileToS3()] {strKey} 업로드 시작..");
                await SG_Common.UploadAsyncToS3(m_txtLog, transferUploadUtility, strlocalFilePath, m_dicData[L9FTP_DataType.S3UploadBucket], strKey);

                m_dicData[L9FTP_DataType.JsonUpdate] = m_txtParameter.Text.Trim();
                SG_Common.SaveData(m_txtLog, m_strConfigFile, m_dicData);
                SystemLog_Form.LogMessage(m_txtLog, $"✅ [UploadFileToS3] 업로드 완료");

                // 5. 다운 받은 Operation.ProjectL.Protocol.json 파일 폴더 삭제
                // strlocalFilePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lordnine-config\{m_txtParameter.Text.Trim()}";
                // Directory.Delete(strlocalFilePath, recursive: true);
                // SystemLog_Form.LogMessage(m_txtLog, $"✅ [UploadFileToS3] {strlocalFilePath} 제거 완료");
            }
            catch (Exception ex)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"❌ [UploadFileToS3 ERROR] {ex.Message}");
            }
        }

        string GetURL(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    SystemLog_Form.LogMessage(m_txtLog, "❌ [GetURL] JSON 파일을 찾을 수 없습니다.");
                    return m_dicData[L9FTP_DataType.NX3URL];
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    SystemLog_Form.LogMessage(m_txtLog, "❌ [GetURL] 파일이 읽기 전용입니다.");
                    return m_dicData[L9FTP_DataType.NX3URL];
                }

                string content = File.ReadAllText(filePath);

                // 정규식으로 제일 처음 "https://도메인:포트" 추출
                var match = Regex.Match(content, @"https?:\/\/[a-zA-Z0-9\.\-]+(:\d+)?");
                if (!match.Success)
                {
                    SystemLog_Form.LogMessage(m_txtLog, "❌ [GetURL] 기존 URL을 찾을 수 없습니다.");
                    return m_dicData[L9FTP_DataType.NX3URL];
                }

                return match.Value;
            }
            catch (Exception ex)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"❌ [GetURL] 예외 발생: {ex.Message}");
                return m_dicData[L9FTP_DataType.NX3URL];
            }
        }

        void ReplaceJsonUrls(string filePath, string strOld, string strNew)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    SystemLog_Form.LogMessage(m_txtLog, "❌ JSON 파일을 찾을 수 없습니다.");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    SystemLog_Form.LogMessage(m_txtLog, "❌ 파일이 읽기 전용입니다.");
                    return;
                }

                string content = File.ReadAllText(filePath);

                if (!content.Contains(strOld))
                {
                    SystemLog_Form.LogMessage(m_txtLog, $"⚠️ 치환 대상 '{strOld}' 문자열이 존재하지 않습니다.");
                }

                // 문자열 단순 치환 (공백, 주석, 줄 위치 모두 유지됨)
                content = content.Replace(strOld, strNew);
                File.WriteAllText(filePath, content);

                SystemLog_Form.LogMessage(m_txtLog, $"✅[ReplaceJsonUrls] {strOld} -> {strNew}");
            }
            catch (Exception ex)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"❌ 예외 발생: {ex.Message}");
            }
        }

        void RemoveSpecificKeys(string filePath)
        {
            var keysToRemove = new[]
            {
                "ONLY_QA_GET_SERVER_DATETIME",
                "ONLY_QA_SET_SERVER_DATETIME_CUSTOM",
                "ONLY_QA_SET_SERVER_DATETIME_DEFAULT"
            };

            if (!File.Exists(filePath))
            {
                SystemLog_Form.LogMessage(m_txtLog, "❌ [RemoveSpecificKeys] JSON 파일이 존재하지 않습니다.");
                return;
            }

            var lines = File.ReadAllLines(filePath).ToList();
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                bool shouldRemove = keysToRemove.Any(key =>
                    line.TrimStart().StartsWith($"\"{key}\"") || line.TrimStart().StartsWith($"'{key}'"));

                if (shouldRemove) continue;
                filteredLines.Add(line);
            }

            // 마지막 유효한 문자열이 있는 줄만 쉼표 제거
            for (int i = filteredLines.Count - 1; i >= 0; i--)
            {
                string line = filteredLines[i].Trim();

                if (!string.IsNullOrWhiteSpace(line) && line != "{" && line != "}")
                {
                    if (line.EndsWith(","))
                    {
                        filteredLines[i] = Regex.Replace(filteredLines[i], @",\s*$", "");
                    }
                    break;
                }
            }

            File.WriteAllLines(filePath, filteredLines);
            SystemLog_Form.LogMessage(m_txtLog, $"🎉[RemoveSpecificKeys] {m_strSelectedServer} 환경 서버 시간 변경 관련 키 삭제 완료");
        }
    }
}