using SG_Tool.Log;

namespace SG_Tool.OP_Tool.ServerPatch
{
    public class ServerPatch_Live_Form : UserControl
    {
        Button m_btnDown = null!;
        Button m_btnUp = null!;
        Button m_btnDockerCheck = null!;
        Button m_btnUserCheck = null!;
        TextBox m_txtLog = null!;
        Label m_lblServerSelect = null!;
        Label m_lblRegionSelect = null!;
        FlowLayoutPanel m_flowPanel = null!;
        FlowLayoutPanel m_checkRegionPanel = null!;
        FlowLayoutPanel m_checkBoxPanel = null!;

        string m_strServerPath = string.Empty; // 서버 목록 파일 경로
        UserData m_userData = new UserData(string.Empty, string.Empty);
        const string PlaceholderText = "ex 20250214a";

        readonly Dictionary<string, List<string>> m_dicGroup = new Dictionary<string, List<string>>();
        readonly Dictionary<string, string> m_dicTagIp = new Dictionary<string, string>();
        readonly Dictionary<string, TextBox> m_dicServerParameters = new Dictionary<string, TextBox>();
        string[] m_strGroup = { "outr-mch", "outr-lgin", "outr-game-1", "outr-game-2", "outr-chat", "outr-adm", "outr-farm" };
        string[] m_strRegion = { "Asia1", "Asia2", "Global", "Japan" ,"Korea" };

        public ServerPatch_Live_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // ===== 메인 패널 =====
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(mainPanel);

            // ===== 좌측 구성 =====
            m_flowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.LightPink
            };

            m_btnDown = new Button { Text = "서버다운", Width = 100 };
            m_btnDown.Click += ServerDown_Click;

            m_btnUp = new Button { Text = "서버업", Width = 100 };
            m_btnUp.Click += ServerUp_Click;

            m_btnDockerCheck = new Button { Text = "도커체크", Width = 100 };
            m_btnDockerCheck.Click += DockerCheck_Click;

            m_btnUserCheck = new Button { Text = "유저체크", Width = 100 };
            m_btnUserCheck.Click += UserCheck_Click;

            m_flowPanel.Controls.AddRange(new Control[] {m_btnDown, m_btnUp, m_btnDockerCheck, m_btnUserCheck});

            m_txtLog = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8)
            };

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            leftLayout.Controls.Add(m_flowPanel, 0, 0);
            leftLayout.Controls.Add(m_txtLog, 0, 1);

            // ===== 우측 구성 =====
            m_lblRegionSelect = new Label
            {
                Text = "국가선택",
                AutoSize = true
            };

            m_lblServerSelect = new Label
            {
                Text = "서버선택",
                AutoSize = true
            };

            m_checkRegionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true,
                BackColor = Color.Beige
            };

            m_checkBoxPanel = new FlowLayoutPanel
            {
                //Width = 400,  // 폭 제한 추가
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                BackColor = Color.LightGray
            };

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5),
                AutoSize = true
            };

            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 국가선택 라벨
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));    // 국가 체크
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 서버선택 라벨
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // 서버그룹 체크

            rightLayout.Controls.Add(m_lblRegionSelect, 0, 0);
            rightLayout.Controls.Add(m_checkRegionPanel, 0, 1);
            rightLayout.Controls.Add(m_lblServerSelect, 0, 2);
            rightLayout.Controls.Add(m_checkBoxPanel, 0, 3);

            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250
            };
            rightPanel.Controls.Add(rightLayout);

            // ===== 최종 배치 =====
            mainPanel.Controls.Add(leftLayout);
            mainPanel.Controls.Add(rightPanel);

            // ===== 서버정보 로드 =====
            LoadServerList();
        }

        void LoadServerList()
        {
            m_userData = SG_Common.LoadCredentials(m_txtLog, EnProjectType.OP);

                   // 라이브 서버 정보 로드
            m_strServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OP", $"OP_Serverlist_Live.cfg");

            m_checkBoxPanel.Controls.Clear();
            m_checkRegionPanel.Controls.Clear();
            m_dicTagIp.Clear();
            m_dicServerParameters.Clear();
            m_dicGroup.Clear();
            
            if (File.Exists(m_strServerPath))
            {
                var lines = File.ReadAllLines(m_strServerPath);
                for (int i = 0; i < m_strGroup.Length; i++)
                {
                    m_dicGroup.Add(m_strGroup[i], new List<string>());
                }

                for (int i = 0; i < m_strRegion.Length; i++)
                {
                    var checkRegionBox = new CheckBox
                    {
                        Text = m_strRegion[i],
                        AutoSize = true,
                        Checked = true
                    };

                    var panel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.LeftToRight,
                        Margin = new Padding(5),
                        AutoSize = true
                    };
                    panel.Controls.Add(checkRegionBox);
                    m_checkRegionPanel.Controls.Add(panel);
                }
                
                foreach (var line in lines)
                {
                    var parts = line.Split(' ');
                    if (parts.Length == 2)
                    {
                        var tag = parts[0];
                        var ip = parts[1];
                        m_dicTagIp[tag] = ip;

                        var commonTag = SG_Common.GetCommonTag(m_strGroup, tag);
                        if (m_dicGroup.ContainsKey(commonTag))
                        {
                            m_dicGroup[commonTag].Add(tag);
                        }
                        else
                        {
                            LogMessage($"❌ {commonTag} 그룹이 존재하지 않습니다.");                        
                        }
                    }
                }

                LogMessage("===== LoadServerList() m_dicGroup.Count: " + m_dicGroup.Count);

                foreach (var dicGroup in m_dicGroup)
                {
                    var checkBox = new CheckBox
                    {
                        Text = dicGroup.Key,
                        AutoSize = true,
                        Checked = true
                    };

                    var textBox = new TextBox
                    {
                        Width = 100,
                        AutoSize = true,
                        Text = PlaceholderText,
                        ForeColor = Color.Gray,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };
                    textBox.Enter += (sender, e) => Parameter_Enter(textBox, e, textBox);
                    textBox.Leave += (sender, e) => Parameter_Leave(textBox, e, textBox);

                    var panel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.LeftToRight,
                        AutoSize = true
                    };
                    panel.Controls.Add(checkBox);
                    panel.Controls.Add(textBox);

                    m_checkBoxPanel.Controls.Add(panel);
                    m_dicServerParameters.Add(dicGroup.Key, textBox);
                }
            }

            m_checkRegionPanel.Refresh();
            m_checkBoxPanel.Refresh();
            SG_Common.ConnectStart(m_txtLog, m_dicTagIp, m_userData);
        }

        void Parameter_Enter(object sender, EventArgs e, TextBox textBox)
        {
            if (textBox.Text == PlaceholderText)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        }

        void Parameter_Leave(object sender, EventArgs e, TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = PlaceholderText;
                textBox.ForeColor = Color.Gray;
            }
        }

        async void ServerDown_Click(object sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버다운", m_userData.IsConnect))
                await ExecuteOnServersAsync("removeDown.sh", EnCommandType.Scripts);
        }

        async void ServerUp_Click(object sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버시작", m_userData.IsConnect))
                await ExecuteOnServersAsync("pullUp_common.sh", EnCommandType.Scripts);
        }

        async void DockerCheck_Click(object sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "도커확인", m_userData.IsConnect))
                await ExecuteOnServersAsync(@"docker ps --format ""@{{.Image}}, {{.RunningFor}}""", EnCommandType.Command);
        }


        int nCurrent_size = 0;
        async void UserCheck_Click(object sender, EventArgs e)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            
            string strPath = $"du -s --apparent-size -B1 /var/log/outer/{today}/info";
            if (SG_Common.ClickCheck(m_txtLog, "유저접근 확인", m_userData.IsConnect))
                await ExecuteOnServersAsync(strPath, EnCommandType.UserCheck);
        }

        async Task ExecuteOnServersAsync(string scriptName, EnCommandType CommandType)
        {
            LogMessage($"✅======= Command Start =======");
            int nCount = 0;

            try
            {
                var selectedRegions = m_checkRegionPanel.Controls.OfType<FlowLayoutPanel>()
                        .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                        .Where(cb => cb.Checked)
                        .Select(cb => cb.Text)
                        .ToList();

                if (!selectedRegions.Any())
                {
                    LogMessage("선택된 국가가 없습니다.");
                    return;
                }

                var selectedTags = m_checkBoxPanel.Controls.OfType<FlowLayoutPanel>()
                    .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                    .Where(cb => cb.Checked)
                    .Select(cb => cb.Text)
                    .ToList();

                if (!selectedTags.Any())
                {
                    LogMessage("선택된 서버가 없습니다.");
                    return;
                }

                if (string.IsNullOrEmpty(m_userData.User) || string.IsNullOrEmpty(m_userData.Pass))
                {
                    LogMessage($"❌ ID 또는 비밀번호가 설정되지 않았습니다. {m_userData.User} {m_userData.Pass}");
                    return;
                }

                bool bPullUp = scriptName.Contains("pullUp_common.sh");
                string parameter = "";

                var tasks = selectedTags.SelectMany(tag =>
                {
                    if (bPullUp)
                    {
                        parameter = m_dicServerParameters[tag.Trim()].Text;
                        scriptName = GetOPCommand(tag.Trim());
                    }
                    
                    //SG_Common.Log(m_txtLog, $"[ExecuteOnServersAsync] {CommandType} {tag.Trim()}, m_dicGroup : {m_dicGroup.ContainsKey(tag.Trim())}");
                    return m_dicGroup[tag.Trim()]
                        .Where(strTag => selectedRegions.Any(region => region.Contains(GetRegion(strTag))))
                        .Select(async strTag =>
                        {
                            if (m_dicTagIp.TryGetValue(strTag, out var serverIp))
                            {
                                if (!SG_Common.Servers.ContainsKey(serverIp) || !SG_Common.Servers[serverIp].IsConnected)
                                {
                                    await SG_Common.ConnectServersAsync(serverIp, m_txtLog, false, tag, m_userData.User, m_userData.Pass);
                                }

                                nCount++;

                                switch (CommandType)
                                {
                                    case EnCommandType.UserCheck:
                                        if (strTag.Contains("outr-game-")) // 게임 서버만 유저 상태 확인. // 롤링패치 확인용.
                                        {
                                            await SG_Common.CommandServersAsync(serverIp, scriptName, strTag, m_txtLog, m_userData.User, m_userData.Pass, CommandType);
                                        }
                                        break;
                                    case EnCommandType.Command:
                                        await SG_Common.CommandServersAsync(serverIp, scriptName, strTag, m_txtLog, m_userData.User, m_userData.Pass, CommandType);
                                        break;
                                    case EnCommandType.Scripts:
                                        await SG_Common.CommandServersAsync(serverIp, $"sh /home/outer/scripts/{scriptName} {parameter}", strTag, m_txtLog, m_userData.User, m_userData.Pass, CommandType);
                                        break;
                                }
                            }
                        })
                        .ToList();
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"❌ {CommandType} {scriptName} 실행 중 오류 발생: {ex.Message}");
            }
            LogMessage($"✅ ======= Command Finish {nCount} =======");
        }

        string GetRegion(string strTag)
        {
            switch (strTag)
            {
                case var t when t.Contains("l-a-outr"):
                    return "Asia1";
                case var t when t.Contains("l-a2-outr"):
                    return "Asia2";
                case var t when t.Contains("l-g-outr"):
                    return "Global";
                case var t when t.Contains("l-j-outr"):
                    return "Japan";
                case var t when t.Contains("l-k-outr"):
                    return "Korea";
                default:
                    return strTag;
            }   
        }

        string GetOPCommand(string tag)
        {
            switch (tag)
            {
                case var t when t.Contains("outr-adm"):
                    return "pullUp_admin.sh";
                default:
                case var t when t.Contains("outr-game"):
                    return "pullUp_game.sh";
                case var t when t.Contains("outr-mch"):
                    return "pullUp_match.sh";
                case var t when t.Contains("outr-lgin"):
                    return "pullUp_login.sh";
                case var t when t.Contains("outr-chat"):
                    return "pullUp_chat.sh";
                case var t when t.Contains("outr-farm"):
                    return "pullUp_farm.sh";
            }
        }

        void LogMessage(string message)
        {
            SystemLog_Form.LogMessage(m_txtLog, message);
        }
    }
}