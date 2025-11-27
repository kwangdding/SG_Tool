using SG_Tool.Log;

namespace SG_Tool.EP7_Tool.ServerPatch
{
    public enum EnType { Command, Docker, Rolling, None };
    public class EP7Data
    {
        EP7_CommandType m_type = EP7_CommandType.Game;
        TextBox m_textBox = null!;
        List<ServerData> m_serverList = new List<ServerData>();

        public TextBox TextBox { get { return m_textBox; } set { m_textBox = value; }}
        public EP7_CommandType Type { get { return m_type; } set { m_type = value; }}
        public List<ServerData> Servers { get { return m_serverList; } set { m_serverList = value; } }

        public EP7Data(EP7_CommandType type)
        {
            this.m_type = type;
            m_serverList.Clear();
        }

        public void AddServer(string tag, string ip, string command, TextBox textBox)
        {
            if (textBox != null)
            {
                this.m_textBox = textBox;
            }
            m_serverList.Add(new ServerData(tag, ip, command));
        }
    }

    public class ServerData
    {
        string m_tag = string.Empty;
        string m_ip = string.Empty;
        string m_command = string.Empty;

        public string Tag { get { return m_tag; } set { m_tag = value; }}
        public string Ip { get { return m_ip; } set { m_ip = value; }}
        public string Command { get { return m_command; } set { m_command = value; }}

        public ServerData(string tag, string ip, string command)
        {
            m_tag = tag;
            m_ip = ip;
            m_command = command;
        }
    }

    public class CommandData
    {
        string m_tag = string.Empty;
        string m_ip = string.Empty;
        string m_command = string.Empty;

        TextBox m_textBox = null!;
        EP7_CommandType m_enType = EP7_CommandType.Game;

        public string Tag { get { return m_tag; } set { m_tag = value; }}
        public string Ip { get { return m_ip; } set { m_ip = value; }}
        public string Command { get { return m_command; } set { m_command = value; }}
        public TextBox TextBox { get { return m_textBox; } set { m_textBox = value; }}
        public EP7_CommandType Type { get { return m_enType; } set { m_enType = value; }}

        public CommandData(string tag, string ip, string command, TextBox textBox, EP7_CommandType commandType)
        {
            m_tag = tag;
            m_ip = ip;
            m_command = command;
            m_textBox = textBox;
            m_enType = commandType;
        }
    }

    public class Ep7_Patch_QA_Form : UserControl
    {
        Button m_btnUp = null!;
        Button m_btnDockerCheck = null!;
        Button m_btnRolling = null!;
        //Button m_btnCDN = null!;

        TextBox m_txtLog = null!;
        ComboBox m_cmbServerList = null!;
        Label m_lblServerSelect = null!;
        FlowLayoutPanel m_flowPanel = null!;
        FlowLayoutPanel m_checkBoxPanel = null!;

        string m_strServerPath = string.Empty; // 서버 목록 파일 경로
        UserData m_userData = new UserData(string.Empty, string.Empty);
        const string PlaceholderText = "ex 20290214a";

        readonly Dictionary<string, string> m_dicTagIp = new Dictionary<string, string>();
        Dictionary<EP7_CommandType, EP7Data> m_dicEP7Data = new Dictionary<EP7_CommandType, EP7Data>
        {
            { EP7_CommandType.Game, new EP7Data(EP7_CommandType.Game) },
            { EP7_CommandType.Battle, new EP7Data(EP7_CommandType.Battle) },
            { EP7_CommandType.Log, new EP7Data(EP7_CommandType.Log) },
            { EP7_CommandType.Chan, new EP7Data(EP7_CommandType.Chan) },
            { EP7_CommandType.Verinfo, new EP7Data(EP7_CommandType.Verinfo) }
        };

        //===========================================================================================
        public Ep7_Patch_QA_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // 메인 레이아웃 (2열: 왼쪽 기능 + 오른쪽 체크박스)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); // 좌측
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // 우측

            // 왼쪽 패널 (서버 선택, 버튼, 로그)
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // 서버 선택
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // 버튼
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 로그

            // 서버 선택 영역
            var serverLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            m_lblServerSelect = new Label
            {
                Text = "서버 선택:",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            m_cmbServerList = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            m_cmbServerList.Items.AddRange(new string[] { "QA1", "QA2", "QA3", "Review" });
            m_cmbServerList.SelectedIndexChanged += ServerChangeList;

            serverLayout.Controls.Add(m_lblServerSelect);
            serverLayout.Controls.Add(m_cmbServerList);

            // 버튼 영역
            m_flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                AutoSize = true,
                BackColor = Color.LightBlue
            };

            m_btnUp = SG_Common.GetButton("서버업", Color.AliceBlue);// new Button { Text = "서버업", Width = 100 };
            m_btnUp.Click += ServerUp_Click;

            m_btnDockerCheck = SG_Common.GetButton("도커체크", Color.AliceBlue);
            m_btnDockerCheck.Click += DockerCheck_Click;

            m_btnRolling = SG_Common.GetButton("롤링(star)", Color.AliceBlue);
            m_btnRolling.Click += Rolling_Click;

            //m_btnCDN = SG_Common.GetButton("CDN 업로드", Color.AliceBlue);
            //m_btnCDN.Click += CDN_Click;

            m_flowPanel.Controls.AddRange(new Control[] { m_btnUp, m_btnDockerCheck, m_btnRolling, });
            //m_flowPanel.Controls.AddRange(new Control[] { m_btnUp, m_btnDockerCheck, m_btnRolling, m_btnCDN });

            // 로그 영역
            m_txtLog = SG_Common.GetLogBox(null, "EP7_QA");

            // 체크박스 패널 (오른쪽)
            m_checkBoxPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke
            };

            // 패널 조립
            leftPanel.Controls.Add(serverLayout);
            leftPanel.Controls.Add(m_flowPanel);
            leftPanel.Controls.Add(m_txtLog);

            mainLayout.Controls.Add(leftPanel, 0, 0);         // 왼쪽: 서버 + 버튼 + 로그
            mainLayout.Controls.Add(m_checkBoxPanel, 1, 0);   // 오른쪽: 체크박스 패널

            this.Controls.Add(mainLayout);

            m_userData = SG_Common.LoadCredentials(m_txtLog, EnProjectType.EP7);
            SetPatchData();
        }


        void ServerChangeList(object? sender, EventArgs e)
        {
            string selectedServer = m_cmbServerList.SelectedItem?.ToString() ?? string.Empty;
            m_strServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EP7", $"EP7_Serverlist_{selectedServer}.cfg");
            LoadServerList();
        }

        void LoadServerList()
        {
            LogMessage($"서버 리스트 로드 시작", 1);
            m_checkBoxPanel.Controls.Clear();
            m_dicTagIp.Clear();

            foreach (var tag in Enum.GetValues(typeof(EP7_CommandType)).Cast<EP7_CommandType>())
            {
                m_dicEP7Data[tag] = new EP7Data(tag);
            }

            if (File.Exists(m_strServerPath))
            {
                var lines = File.ReadAllLines(m_strServerPath);
                foreach (var line in lines)
                {
                    var parts = line.Split(' ');
                    if (parts.Length == 2)
                    {
                        var tag = parts[0];
                        var ip = parts[1];
                        m_dicTagIp.Add(tag, ip);

                        // gate, gt, star, match
                        // 서버 태그에 따라 적절한 CommandType에 서버 추가
                        switch (tag)
                        {
                            case var t when t.Contains("gate"):
                                AddServerToEP7Data(EP7_CommandType.Log, tag, ip);
                                AddServerToEP7Data(EP7_CommandType.Log, tag, ip, 1);
                                AddServerToEP7Data(EP7_CommandType.Chan, tag, ip);
                                break;
                            case var t when t.Contains("gt"):
                                AddServerToEP7Data(EP7_CommandType.Game, tag, ip);
                                AddServerToEP7Data(EP7_CommandType.Verinfo, tag, ip);
                                break;
                            case var t when t.Contains("star"):
                                AddServerToEP7Data(EP7_CommandType.Game, tag, ip);
                                AddServerToEP7Data(EP7_CommandType.Battle, tag, ip);
                                break;
                            case var t when t.Contains("match"):
                                AddServerToEP7Data(EP7_CommandType.Log, tag, ip);
                                AddServerToEP7Data(EP7_CommandType.Game, tag, ip);
                                AddServerToEP7Data(EP7_CommandType.Battle, tag, ip);
                                break;
                            default:
                                LogMessage($"조건에 맞지 않는 데이터가 존재 합니다. 확인 해주세요.", 1);
                                break;
                        }
                    }
                }
            }

            m_checkBoxPanel.Refresh();
            SG_Common.ConnectStart(m_txtLog, m_dicTagIp, m_userData);
        }

        //===========================================================================================
        void Parameter_Enter(object? sender, EventArgs e, TextBox textBox)
        {
            if (textBox.Text == PlaceholderText)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        }

        void Parameter_Leave(object? sender, EventArgs e, TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = PlaceholderText;
                textBox.ForeColor = Color.Gray;
            }
        }

        async void ServerUp_Click(object? sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버재시작", m_userData.IsConnect))
                await ExecuteOnServersAsync("pullUp_common.sh", EnType.Command);
            LogMessage($"서버재시작 종료", 1);

            LogMessage($"도커확인 시작", 1);
            await ExecuteOnServersAsync(@"sudo docker ps --format ""@{{.Image}}, {{.RunningFor}}""", EnType.Docker);
            LogMessage($"도커확인 종료", 1);
        }

        async void DockerCheck_Click(object? sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "도커확인", m_userData.IsConnect))
                await ExecuteOnServersAsync(@"sudo docker ps --format ""@{{.Image}}, {{.RunningFor}}""", EnType.Docker);
            LogMessage($"도커확인 종료", 1);
        }

        async void Rolling_Click(object? sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "롤링패치", m_userData.IsConnect))
                await ExecuteOnServersAsync("pullUp_common.sh", EnType.Rolling);
            LogMessage($"롤링패치 종료", 1);

            LogMessage($"도커확인 시작", 1);
            await ExecuteOnServersAsync(@"sudo docker ps --format ""@{{.Image}}, {{.RunningFor}}""", EnType.Docker);
            LogMessage($"도커확인 종료", 1);
        }

        //void CDN_Click(object? sender, EventArgs e)
        //{
        //    if (SG_Common.ClickCheck(m_txtLog, "SVN 업로드 후 S3 업로드", m_userData.IsConnect))
        //        SVNUPdateAndUploadFileToS3();

        //    LogMessage($"SVN 업로드 후 S3 업로드 종료", 1);
        //}
        //===========================================================================================
        async Task ExecuteOnServersAsync(string scriptName, EnType enType)
        {
            var selectedTags = m_checkBoxPanel.Controls.OfType<FlowLayoutPanel>()
                .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                .Where(cb => cb.Checked)
                .Select(cb => cb.Text)
                .ToList();

            if (!selectedTags.Any())
            {
                LogMessage($"❌ 선택된 서버가 없습니다.");
                return;
            }

            if (string.IsNullOrEmpty(m_userData.User) || string.IsNullOrEmpty(m_userData.Pass))
            {
                LogMessage($"❌ ID 또는 비밀번호가 설정되지 않았습니다. {m_userData.User} {m_userData.Pass}");
                return;
            }

            switch (enType)
            {
                default:
                case EnType.Command:
                    List<CommandData> GameList = new List<CommandData>();
                    List<CommandData> BattleList = new List<CommandData>();
                    List<CommandData> LogList = new List<CommandData>();
                    List<CommandData> LogTsvList = new List<CommandData>();
                    List<CommandData> ChanList = new List<CommandData>();
                    List<CommandData> VersionList = new List<CommandData>();

                    foreach (var tag in selectedTags)
                    {
                        EP7_CommandType commandType = GetCommandType(tag);
                        EP7Data ep7Data = m_dicEP7Data[commandType];
                        string parameter = ep7Data.TextBox.Text == PlaceholderText ? string.Empty : ep7Data.TextBox.Text;

                        foreach (var server in ep7Data.Servers)
                        {
                            string strCommand = $"sh /home/techpm/scripts/{server.Command} {parameter}";

                            switch (commandType)
                            {
                                default:
                                case EP7_CommandType.Game:
                                    GameList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                    break;
                                case EP7_CommandType.Battle:
                                    BattleList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                    break;
                                case EP7_CommandType.Log:
                                    LogList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                    break;
                                case EP7_CommandType.Chan:
                                    ChanList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                    break;
                                case EP7_CommandType.Verinfo:
                                    VersionList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                    break;
                            }
                        }
                    }

                    await Task.WhenAll(GetTaskAsync(BattleList, "Battle"));
                    await Task.WhenAll(GetTaskAsync(GameList, "Game"));
                    await Task.WhenAll(GetTaskAsync(LogList, "Log"));
                    await Task.WhenAll(GetTaskAsync(ChanList, "Chan"));
                    await Task.WhenAll(GetTaskAsync(VersionList, "Version"));
                    break;
                case EnType.Rolling:
                    List<CommandData> RollingList = new List<CommandData>();
                    foreach (var tag in selectedTags)
                    {
                        EP7_CommandType commandType = GetCommandType(tag);
                        EP7Data ep7Data = m_dicEP7Data[commandType];

                        if (ep7Data.TextBox.Text == PlaceholderText)
                        {
                            SG_Common.ProsessCheck("파라미터를 입력 해주세요");
                            break;
                        }
                        else
                        {
                            string parameter = ep7Data.TextBox.Text == PlaceholderText ? string.Empty : ep7Data.TextBox.Text;
                            foreach (var server in ep7Data.Servers)
                            {
                                // star 서버의 Game 이미지만 변경 진행.
                                if (server.Tag.Contains("star"))
                                {
                                    string strCommand = $"sh /home/techpm/scripts/{server.Command} {parameter}";
                                    switch (commandType)
                                    {
                                        case EP7_CommandType.Game:
                                            RollingList.Add(new CommandData(server.Tag, server.Ip, strCommand, m_txtLog, commandType));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            await Task.WhenAll(GetTaskAsync(RollingList, "Rolling_Star"));
                        }
                    }
                    break;
                case EnType.Docker:
                    foreach (var server in m_dicTagIp)
                    {
                        await SG_Common.CommandServersAsync(server.Value, scriptName, server.Key, m_txtLog, m_userData.User, m_userData.Pass, EnCommandType.Command);
                    }
                    break;
            }
        }

        async Task ExecuteOnStartAsync(CommandData commandData)
        {
            await SG_Common.CommandServersAsync(commandData.Ip, commandData.Command, commandData.Tag, m_txtLog, m_userData.User, m_userData.Pass, EnCommandType.Scripts);
        }

        //===========================================================================================
        void AddServerToEP7Data(EP7_CommandType commandType, string tag, string ip, int nType = 0)
        {
            m_dicEP7Data[commandType].AddServer(tag, ip, GetCommandQA(tag, commandType, nType), SetTextBox(commandType));
        }

        void LogMessage(string message, int nType = 0, bool overwrite = false)
        {
            SystemLog_Form.LogMessage(m_txtLog, message, nType, overwrite);
        }

        List<Task> GetTaskAsync(List<CommandData> commandDatas, string strName)
        {
            LogMessage($" {strName} Start {commandDatas.Count}", 1);

            if (commandDatas.Count != 0)
            {
                var tasks = commandDatas
                            .Select(data => Task.Run(() => ExecuteOnStartAsync(data)))
                            .ToList();
                return tasks;
            }
            return new List<Task>();
        }

        string GetCommandQA(string tag, EP7_CommandType commandType, int nType)
        {
            switch (commandType)
            {
                case EP7_CommandType.Game:
                    return "oneCommand_star.sh";
                case EP7_CommandType.Battle:
                    return "oneCommand_battle.sh";
                case EP7_CommandType.Log:
                    switch (tag)
                    {
                        default:
                            if (nType == 0)
                                return "oneCommand_json.sh"; // return "oneCommand_tsv.sh"; // 두개 실행 해야하기 때문에 우선 개별 처리.
                            else
                                return "oneCommand_tsv.sh";                           
                        case var t when t.Contains("match"):
                            return "oneCommand_arena_tsv.sh";
                    }

                case EP7_CommandType.Chan:
                    return "oneCommand_gate_chan.sh";
                case EP7_CommandType.Verinfo:
                    return "oneCommand_version.sh";
                default:
                    return string.Empty;
            }
        }

        TextBox SetTextBox(EP7_CommandType commandType)
        {
            if (m_dicEP7Data[commandType].TextBox != null) return null!;

            var checkBox = new CheckBox
            {
                Text = $"{commandType,-7}",
                AutoSize = true,
                Checked = true
            };

            var textBox = new TextBox
            {
                Width = 150,
                AutoSize = true,
                Text = PlaceholderText,
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Right
            };
            textBox.Enter += (sender, e) => Parameter_Enter(sender, e, textBox);
            textBox.Leave += (sender, e) => Parameter_Leave(sender, e, textBox);

            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            panel.Controls.Add(checkBox);
            panel.Controls.Add(textBox);
            m_checkBoxPanel.Controls.Add(panel);
            return textBox;
        }

        EP7_CommandType GetCommandType(string tag)
        {
            switch (tag)
            {
                case var t when t.Contains("Game"):
                    return EP7_CommandType.Game;
                case var t when t.Contains("Battle"):
                    return EP7_CommandType.Battle;
                case var t when t.Contains("Log"):
                    return EP7_CommandType.Log;
                case var t when t.Contains("Chan"):
                    return EP7_CommandType.Chan;
                case var t when t.Contains("Verinfo"):
                    return EP7_CommandType.Verinfo;
                default:
                    return EP7_CommandType.Game;
            }
        }


        //===========================================================================================
        public enum DataType { SVNPath, S3Bucket, S3Key, AWSAccessKey, AWSsecretKey }

        Dictionary<DataType, string> m_dicData;
        const string c_strPath = "qaData.cfg";
        // SVNPath: @"D:\Projects\MyProject",
        // S3Bucket: "cdn-cluster-origin/",
        // S3Key: "epic7-app/qa/QA_group/CDN/",
        // localFilePath: @"D:\Projects\MyProject\build.zip",
        // accessKey: "YOUR_AWS_ACCESS_KEY",
        // secretKey: "YOUR_AWS_SECRET_KEY"

        bool SetPatchData()
        {
            if (File.Exists(c_strPath))
            {
                var lines = File.ReadAllLines(c_strPath);

                foreach (var line in lines)
                {
                    // 공백 줄 또는 주석 처리된 줄 무시
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    // Enum.TryParse를 이용한 안전한 파싱
                    if (Enum.TryParse(parts[0], out DataType key))
                    {
                        if (m_dicData.ContainsKey(key))
                        {
                            m_dicData[key] = parts[1].Trim();
                        }
                        else
                        {
                            m_dicData.Add(key, parts[1].Trim());    
                        }
                    }
                }
                return true;
            }

            return false;
        }

        //void SVNUPdateAndUploadFileToS3()
        //{
        //    try
        //    {
        //        string svnPath = "";
        //        string s3Bucket = "";
        //        string s3Key = "";
        //        LogMessage($"============ [AutoSvnUpdate] SVN 업데이트 시작: {svnPath} ============");
        //        var psi = new ProcessStartInfo
        //        {
        //            FileName = "svn",
        //            Arguments = $"update \"{svnPath}\"",
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        };

        //        using var process = Process.Start(psi);
        //        if (process == null)
        //        {
        //            LogMessage("[AutoSvnUpdate ERROR] 프로세스 실행 실패.");
        //            return;
        //        }

        //        string output = process.StandardOutput.ReadToEnd();
        //        string error = process.StandardError.ReadToEnd();
        //        process.WaitForExit();

        //        if (!string.IsNullOrWhiteSpace(error))
        //        {
        //            LogMessage($"[AutoSvnUpdate ERROR] {error}");
        //            return;
        //        }

        //        if (string.IsNullOrWhiteSpace(output))
        //        {
        //            LogMessage("[AutoSvnUpdate] 변경 사항 없음. S3 업데이트 생략.");
        //            return;
        //        }

        //        LogMessage($"[AutoSvnUpdate] 업데이트 결과:\n{output}");

        //        // 업데이트
        //        string localFilePath = Path.Combine(svnPath, "");

        //        // ✅ SVN 업데이트 후 S3 업로드
        //        UploadFileToS3(localFilePath, s3Bucket, s3Key).Wait();
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage($"[AutoSvnUpdate ERROR] 예외 발생: {ex.Message}");
        //    }
        //}
        
        //async Task UploadFileToS3(string directory, string bucket, string prefix)
        //{
        //    try
        //    {
        //        string accessKey = "";
        //        string secretKey = "";
        //        LogMessage($"[S3 Upload] {directory} → s3://{bucket}/{prefix}");
        //        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        //        using var s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.APNortheast2);
        //        var transferUtility = new TransferUtility(s3Client);

        //        foreach (var filePath in files)
        //        {
        //            var relativePath = SG_Common.GetRelativePath(directory, filePath).Replace("\\", "/"); // S3 호환 경로
        //            var key = $"{prefix.TrimEnd('/')}/{relativePath}";

        //            Console.WriteLine($"[S3 Upload] {filePath} → s3://{bucket}/{key}");
        //            await transferUtility.UploadAsync(filePath, bucket, key);
        //        }
        //        LogMessage("[S3 Upload] 업로드 완료!");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage($"[S3 Upload ERROR] {ex.Message}");
        //    }            
        //}
    }
}