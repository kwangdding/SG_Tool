using SG_Tool.Log;

namespace SG_Tool.OP_Tool.ServerPatch
{
    public class OP_Patch_QA_Form : UserControl
    {
        Button m_btnDown = null!;
        Button m_btnUp = null!;
        Button m_btnDockerCheck = null!;
        TextBox m_txtLog = null!;
        ComboBox m_cmbServerList = null!;
        Label m_lblServerSelect = null!;
        FlowLayoutPanel m_flowPanel = null!;
        FlowLayoutPanel m_checkBoxPanel = null!;

        string m_strServerPath = string.Empty; // 서버 목록 파일 경로
        UserData m_userData = new UserData(string.Empty, string.Empty);
        const string PlaceholderText = "ex 20250214a";

        readonly Dictionary<string, string> m_dicTagIp = new Dictionary<string, string>();
        readonly Dictionary<string, TextBox> m_dicServerParameters = new Dictionary<string, TextBox>();
 

        public OP_Patch_QA_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // 메인 패널
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(mainPanel);

            // 서버 선택 라벨
            m_lblServerSelect = new Label
            {
                Text = "서버 선택:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            // 서버 선택 콤보박스
            m_cmbServerList = new ComboBox
            {
                Width = 200,
                Anchor = AnchorStyles.Left
            };
            m_cmbServerList.Items.AddRange(new string[] { "QA0", "QA1", "QA2", "Review" });
            m_cmbServerList.SelectedIndex = 0;
            m_cmbServerList.SelectedIndexChanged += ServerChangeList;

            // 실행 버튼들 패널
            m_flowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.LightBlue
            };

            m_btnDown = new Button { Text = "서버다운", Width = 100 };
            m_btnDown.Click += ServerDown_Click;

            m_btnUp = new Button { Text = "서버업", Width = 100 };
            m_btnUp.Click += ServerUp_Click;

            m_btnDockerCheck = new Button { Text = "도커체크", Width = 100 };
            m_btnDockerCheck.Click += DockerCheck_Click;

            m_flowPanel.Controls.AddRange(new Control[] { m_btnDown, m_btnUp, m_btnDockerCheck });

            // 로그 텍스트 박스
            m_txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8),
                Dock = DockStyle.Fill
            };

            // 서버 체크박스 패널 (오른쪽)
            m_checkBoxPanel = new FlowLayoutPanel
            {
                Width = 250,
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                BackColor = Color.LightGray
            };

            // 레이아웃: 좌측 내용 정렬
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 200));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 0: Label + ComboBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 1: Button Panel
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 200)); // Row 2: Log box

            layout.Controls.Add(m_lblServerSelect, 0, 0);
            layout.Controls.Add(m_cmbServerList, 1, 0);

            layout.Controls.Add(m_flowPanel, 0, 1);
            layout.SetColumnSpan(m_flowPanel, 2);

            layout.Controls.Add(m_txtLog, 0, 2);
            layout.SetColumnSpan(m_txtLog, 2);

                   // 좌측 레이아웃 + 우측 체크박스 패널 배치
            mainPanel.Controls.Add(layout);
            mainPanel.Controls.Add(m_checkBoxPanel);

                   // 서버 정보 불러오기
            m_userData = SG_Common.LoadCredentials(m_txtLog, EnProjectType.OP);
        }


        void ServerChangeList(object sender, EventArgs e)
        {
            string selectedServer = m_cmbServerList.SelectedItem?.ToString() ?? string.Empty;
            m_strServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OP", $"OP_Serverlist_{selectedServer}.cfg");
            LoadServerList();
        }

        void LoadServerList()
        {
            m_checkBoxPanel.Controls.Clear();
            m_dicServerParameters.Clear();
            m_dicTagIp.Clear();

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
                        m_dicTagIp[tag] = ip;

                        var checkBox = new CheckBox
                        {
                            Text = tag,
                            AutoSize = true,
                            Checked = true
                        };

                        var textBox = new TextBox
                        {
                            Width = 150,
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
                        m_dicServerParameters.Add(tag, textBox);
                    }
                }
            }

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
                await ExecuteOnServersAsync("removeDown.sh", "서버다운", EnCommandType.Scripts);

            await ExecuteOnServersAsync(@"docker ps --format ""@{{.Image}}, {{.RunningFor}}""", "도커확인", EnCommandType.Command);
        }

        async void ServerUp_Click(object sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버시작", m_userData.IsConnect))
                await ExecuteOnServersAsync("pullUp_common.sh", "서버시작", EnCommandType.Scripts);

            await ExecuteOnServersAsync(@"docker ps --format ""@{{.Image}}, {{.RunningFor}}""", "도커확인", EnCommandType.Command);
        }

        async void DockerCheck_Click(object sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "도커확인", m_userData.IsConnect))
                await ExecuteOnServersAsync(@"docker ps --format ""@{{.Image}}, {{.RunningFor}}""", "도커확인", EnCommandType.Command);
        }

        async Task ExecuteOnServersAsync(string scriptName, string CommnadName, EnCommandType CommandType)
        {
            LogMessage($"✅======= {CommnadName} Start =======");

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

            bool isPullUp = scriptName.Contains("pullUp_common.sh");

            var tasks = selectedTags.Select(async tag =>
            {
                if (m_dicTagIp.TryGetValue(tag, out var serverIp))
                {
                    var parameter = isPullUp && m_dicServerParameters[tag].Text != PlaceholderText ? m_dicServerParameters[tag].Text:"";

                    if (isPullUp)
                    {
                        scriptName = GetOPCommandQA(tag);
                    }

                    if (!SG_Common.Servers.ContainsKey(serverIp) || !SG_Common.Servers[serverIp].IsConnected)
                    {
                        await SG_Common.ConnectServersAsync(serverIp, m_txtLog, false, tag, m_userData.User, m_userData.Pass);
                    }
                    
                    string strCommand = CommandType == EnCommandType.Command ? scriptName : $"sh /home/outer/scripts/{scriptName} {parameter}";
                    await SG_Common.CommandServersAsync(serverIp, strCommand, tag, m_txtLog, m_userData.User, m_userData.Pass, CommandType);
                }
            });

            await Task.WhenAll(tasks);
            LogMessage($"✅ ======= {CommnadName} Finish =======");
        }

        string GetOPCommandQA(string tag)
        {
            switch (tag)
            {
                case var t when t.Contains("adm"):
                    return "pullUp_admin.sh";
                case var t when t.Contains("game"):
                    return "pullUp_game.sh";
                case var t when t.Contains("mch"):
                    return "pullUp_match.sh";
                case var t when t.Contains("lgin"):
                    return "pullUp_login.sh";
                case var t when t.Contains("chat"):
                    return "pullUp_chat.sh";
                case var t when t.Contains("farm"):
                    return "pullUp_farm.sh";
                default:
                    return "pullUp_common.sh";
            }
        }

        void LogMessage(string message)
        {
            SystemLog_Form.LogMessage(m_txtLog, message);
        }
    }
}