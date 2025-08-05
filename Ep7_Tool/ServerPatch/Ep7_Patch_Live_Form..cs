using SG_Tool.Log;

namespace SG_Tool.EP7_Tool.ServerPatch
{
    public class ServerPatch_Live_Form : UserControl
    {
        Button m_btnDown = null!;
        Button m_btnLogDown = null!;
        Button m_btnUp = null!;
        Button m_btnDockerCheck = null!;
        Button m_btnRollingPatch = null!;

        TextBox m_txtLog = null!;
        Label m_lblServerSelect = null!;
        Label m_lblRegionSelect = null!;

        FlowLayoutPanel m_flowPanel = null!;
        FlowLayoutPanel m_checkBoxPanel = null!;
        FlowLayoutPanel m_textBoxPanel = null!;

        string m_strServerPath = string.Empty; // 서버 목록 파일 경로
        UserData m_userData = new UserData(string.Empty, string.Empty);
        const string PlaceholderText = "ex 20290214a";

        readonly Dictionary<string, string> m_dicTagIp = new Dictionary<string, string>();
        readonly Dictionary<EP7_CommandType, TextBox> m_dicServerParameters = new Dictionary<EP7_CommandType, TextBox>();

        Dictionary<string, string> m_dicCommand = new Dictionary<string, string>
        {
            { "star_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_star.sh" },
            { "gmtool_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_gmtool.sh" },
            { "battle_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_battle.sh" },
            { "chan_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_gate_chan.sh" },
            { "opt_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_opt.sh" },
            { "arena_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_arena.sh" },
            { "match_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_match.sh" },
            { "json_tsv_down", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Down_json_tsv.sh" },
            { "json_tsv_restart", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Restart_json_tsv.sh" },
            { "star_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_star.sh" },
            { "gmtool_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_gmtool.sh" },
            { "battle_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_battle.sh" },
            { "chan_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_gate_chan.sh" },
            { "opt_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_opt.sh" },
            { "arena_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_arena.sh" },
            { "match_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Up_match.sh" },
            { "json_tsv_start", "y | sh /home/techpm/scripts/remoteDistribute/allTogether_Restart_json_tsv.sh" },
            { "monitoring", "sh /home/techpm/scripts/remoteDistribute/allTogether_Tool_Monitoring.sh" },
            { "rolling_star", "sh /home/techpm/scripts/remoteDistribute/rolling_star.sh" }
        };

//===========================================================================================
        
        public ServerPatch_Live_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // 메인 전체 레이아웃 (2열: 왼쪽 컨트롤, 오른쪽 옵션)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); // 왼쪽 컨트롤
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // 오른쪽 설정

            // ▶ 왼쪽: 버튼 영역 + 로그창
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 버튼 영역
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 로그

            // 버튼 영역
            m_flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.LightPink,
                Padding = new Padding(5)
            };

            m_btnDown = SG_Common.GetButton("서버다운(로그재시작)", Color.AliceBlue, 150);
            m_btnDown.Click += ServerDown_Click;

            m_btnLogDown = SG_Common.GetButton("서버다운(로그다운)", Color.AliceBlue, 150);
            m_btnLogDown.Click += LogServerDown_Click;

            m_btnUp = SG_Common.GetButton("서버업", Color.AliceBlue);
            m_btnUp.Click += ServerUp_Click;

            m_btnDockerCheck = SG_Common.GetButton("모니터링", Color.AliceBlue);
            m_btnDockerCheck.Click += Monitoring_Click;

            m_btnRollingPatch = SG_Common.GetButton("롤링패치", Color.AliceBlue);
            m_btnRollingPatch.Click += Monitoring_Click;

            m_flowPanel.Controls.AddRange(new Control[] {m_btnDown, m_btnLogDown, m_btnUp, m_btnDockerCheck, m_btnRollingPatch});

            // 로그 박스
            m_txtLog = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8)
            };

            leftLayout.Controls.Add(m_flowPanel);
            leftLayout.Controls.Add(m_txtLog);

            // ▶ 오른쪽: 체크박스 + 텍스트 + 라벨들
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 국가 선택 라벨
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); // 체크박스 패널
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 서버 선택 라벨
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); // 텍스트 박스 패널

            m_lblRegionSelect = new Label
            {
                Text = "국가선택",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5)
            };

            m_checkBoxPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true
            };

            m_lblServerSelect = new Label
            {
                Text = "서버선택",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5)
            };

            m_textBoxPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true
            };

            rightLayout.Controls.Add(m_lblRegionSelect);
            rightLayout.Controls.Add(m_checkBoxPanel);
            rightLayout.Controls.Add(m_lblServerSelect);
            rightLayout.Controls.Add(m_textBoxPanel);

            // ▶ 조립
            mainLayout.Controls.Add(leftLayout, 0, 0);     // 왼쪽: 버튼 + 로그
            mainLayout.Controls.Add(rightLayout, 1, 0);    // 오른쪽: 라벨 + 패널들

            this.Controls.Add(mainLayout);

            // 데이터 로드
            m_userData = SG_Common.LoadCredentials(m_txtLog, EnProjectType.EP7);
            LoadServerList();
        }


        void LoadServerList()
        {
            // 라이브 서버 정보 로드
            m_strServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EP7", $"EP7_Serverlist_Live.cfg");

            m_checkBoxPanel.Controls.Clear();
            m_dicTagIp.Clear();
            m_dicServerParameters.Clear();

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
                        m_dicTagIp[parts[0]] = parts[1];

                        var checkRegionBox = new CheckBox
                        {
                            Text = GetRegion(tag),
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
                        m_checkBoxPanel.Controls.Add(panel);
                    }
                }

                foreach (var tag in Enum.GetValues(typeof(EP7_CommandType)).Cast<EP7_CommandType>())
                {
                    var lbTag = new Label
                    {
                        Text = $"{tag, -10}",
                        AutoSize = true,
                        BorderStyle = BorderStyle.Fixed3D,
                        Anchor = AnchorStyles.Left
                    };

                    var textBox = new TextBox
                    {
                        Width = 100,
                        AutoSize = true,
                        Text = PlaceholderText,
                        ForeColor = Color.Gray,
                        TextAlign = HorizontalAlignment.Left,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };

                    textBox.Enter += (sender, e) => Parameter_Enter(textBox, e, textBox);
                    textBox.Leave += (sender, e) => Parameter_Leave(textBox, e, textBox);

                    var panel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.LeftToRight,
                        AutoSize = true
                    };

                    panel.Controls.Add(lbTag);
                    panel.Controls.Add(textBox);

                    m_textBoxPanel.Controls.Add(panel);
                    m_dicServerParameters.Add(tag, textBox);
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

        async void ServerDown_Click(object? sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버다운(로그재시작)", m_userData.IsConnect, m_userData.IsCountdown))
                await ServerStopAsync(false);
        }

        async void LogServerDown_Click(object? sender, EventArgs e)
        {            
            if (SG_Common.ClickCheck(m_txtLog, "서버다운(로그종료)", m_userData.IsConnect, m_userData.IsCountdown))
                await ServerStopAsync(true);
        }

        async void ServerUp_Click(object? sender, EventArgs e)
        {
            if (SG_Common.ClickCheck(m_txtLog, "서버시작", m_userData.IsConnect, m_userData.IsCountdown))
                await ServerStartAsync();
        }

        async void Monitoring_Click(object? sender, EventArgs e)
        {         
            if (SG_Common.ClickCheck(m_txtLog, "서버 모니터링", m_userData.IsConnect, m_userData.IsCountdown))
                await MonitoringAsync();            
        }
        async void RollingPatch_Click(object? sender, EventArgs e)
        {         
            if (SG_Common.ClickCheck(m_txtLog, "서버 모니터링", m_userData.IsConnect, m_userData.IsCountdown))
                await RollingPatchAsync();            
        }
        
//===========================================================================================
        
        async Task ServerStopAsync(bool bLogDown)
        {
            SG_Common.CountDownStart(m_txtLog, m_userData, 600);
            await Task.WhenAll(GetTaskList(1, bLogDown));
            LogMessage($"서버다운 {bLogDown} Finish", 1);
        }

        async Task ServerStartAsync()
        {
            await Task.WhenAll(GetTaskList(2));
            LogMessage($"서버시작 Finish", 1);
        }

        async Task MonitoringAsync()
        {
            await Task.WhenAll(GetTaskList(3));
            LogMessage($"서버 모니터링 Finish", 1);
        }

        async Task RollingPatchAsync()
        {
            await Task.WhenAll(GetTaskList(4));
            LogMessage($"서버 롤링패치 Finish", 1);
        }
        
//===========================================================================================
        
        // 개별 서버에 대한 작업을 처리하는 비동기 메서드
        async Task ExecuteServerStopAsync(string tag, string ip, bool bLogDown)
        {
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "star_down", string.Empty, EnCommandType.Scripts), tag, "star_down");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "gmtool_down", string.Empty, EnCommandType.Scripts), tag, "gmtool_down");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "battle_down", string.Empty, EnCommandType.Scripts), tag, "battle_down");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "chan_down", string.Empty, EnCommandType.Scripts), tag, "chan_down");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "opt_down", string.Empty, EnCommandType.Scripts), tag, "opt_down");

            if (tag.Contains("l-g-ep-adm"))
            {
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "arena_down", string.Empty, EnCommandType.Scripts), tag, "arena_down");
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "match_down", string.Empty, EnCommandType.Scripts), tag, "match_down");
            }

            while (m_userData.IsCountdown) // CommonPatch.IsCountdown이 false가 될 때까지 대기
            {
                await Task.Delay(1000); // 100ms 간격으로 상태 확인
            }

            if (bLogDown)
            {
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "json_tsv_down", string.Empty, EnCommandType.Scripts), tag, "json_tsv_down");
            }
            else
            {
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "json_tsv_restart", string.Empty, EnCommandType.Scripts), tag, "json_tsv_restart");
            }
        }
        
        async Task ExecuteServerStartAsync(string tag, string ip)
        {
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "gmtool_start", GetParameter(EP7_CommandType.Game), EnCommandType.Scripts), tag, "gmtool_start");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "json_tsv_restart", GetParameter(EP7_CommandType.Log), EnCommandType.Scripts), tag, "json_tsv_restart");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "star_start", string.Empty, EnCommandType.Scripts), tag, "star_start");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "battle_start", GetParameter(EP7_CommandType.Battle), EnCommandType.Scripts), tag, "battle_start");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "chan_start", GetParameter(EP7_CommandType.Chan), EnCommandType.Scripts), tag, "chan_start");
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "opt_start", GetParameter(EP7_CommandType.Game), EnCommandType.Scripts), tag, "opt_start");

            if (tag.Contains("l-g-ep-adm"))
            {
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "arena_start", GetParameter(EP7_CommandType.Battle), EnCommandType.Scripts), tag, "arena_start");
                await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "match_start", GetParameter(EP7_CommandType.Game), EnCommandType.Scripts), tag, "match_start");
            }

            LogMessage($"서버 모니터링 {tag} Start", 1);
            await ExecuteMonitoringAsync(tag, ip);
            LogMessage($"서버 모니터링 {tag} Finish", 1);
        }

        async Task ExecuteMonitoringAsync(string tag, string ip)
        {
            await SG_Common.AwaitWithPeriodicLog(m_txtLog, GetTask(tag, ip, "monitoring", string.Empty, EnCommandType.Monitoring), tag, "monitoring");
        }

        async Task ExecuteRollingPatchAsync(string tag, string ip)
        {           
            await SG_Common.AwaitWithPeriodicLog(m_txtLog,  GetTask(tag, ip, "rolling_star", string.Empty, EnCommandType.Scripts), tag, "rolling_star");
        }

//===========================================================================================
        
        Task GetTask(string tag, string ip, string strCommand, string parameter, EnCommandType commandType)
        {
            if (parameter == string.Empty)
            {
                return SG_Common.CommandServersAsync(ip, $"{m_dicCommand[strCommand]}", tag, m_txtLog, m_userData.User, m_userData.Pass, commandType);
            }
            else 
            {
                return SG_Common.CommandServersAsync(ip, $"{m_dicCommand[strCommand]} {parameter} 2>&1", tag, m_txtLog, m_userData.User, m_userData.Pass, commandType);
            }
        }

        string GetRegion(string strTag)
        {
            switch (strTag)
            {
                case var t when t.Contains("l-a-ep-adm"):
                    return EP7_EnRegion.Asia.ToString();
                case var t when t.Contains("l-e-ep7-adm-01"):
                    return EP7_EnRegion.Europ.ToString();
                case var t when t.Contains("l-g-ep-adm"):
                    return EP7_EnRegion.Global.ToString();
                case var t when t.Contains("l-j-ep7-adm"):
                    return EP7_EnRegion.Japan.ToString();
                case var t when t.Contains("l-k-ep-adm"):
                    return EP7_EnRegion.Korea.ToString();
                default:
                    return EP7_EnRegion.Global.ToString(); // 기본값 설정
            }   
        }
        
        List<Task> GetTaskList (int nType, bool bLogDown = false) //1 정지, 리셋 2 시작, 3 모니터링
        {
            var selectedTags = m_checkBoxPanel.Controls.OfType<FlowLayoutPanel>()
                    .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                    .Where(cb => cb.Checked)
                    .Select(cb => cb.Text)
                    .ToList();

            switch (nType)
            {
                case 1:
                    return m_dicTagIp
                        .Where(tagIp => selectedTags.Contains(GetRegion(tagIp.Key)))
                        .Select(tagIp => Task.Run(() => ExecuteServerStopAsync(tagIp.Key, tagIp.Value, bLogDown)))                            
                        .ToList(); 
                case 2:
                    return m_dicTagIp
                        .Where(tagIp => selectedTags.Contains(GetRegion(tagIp.Key)))
                        .Select(tagIp => Task.Run(() => ExecuteServerStartAsync(tagIp.Key, tagIp.Value)))                            
                        .ToList(); 
                case 3:
                default:
                    return m_dicTagIp
                        .Where(tagIp => selectedTags.Contains(GetRegion(tagIp.Key)))
                        .Select(tagIp => Task.Run(() => ExecuteMonitoringAsync(tagIp.Key, tagIp.Value)))                            
                        .ToList(); 
                case 4:
                    return m_dicTagIp
                        .Where(tagIp => selectedTags.Contains(GetRegion(tagIp.Key)))
                        .Select(tagIp => Task.Run(() => ExecuteRollingPatchAsync(tagIp.Key, tagIp.Value)))                            
                        .ToList(); 
            }
        }

        string GetParameter(EP7_CommandType commandType)
        {
            return m_dicServerParameters[commandType].Text == PlaceholderText ? string.Empty : m_dicServerParameters[commandType].Text;
        }

        void LogMessage(string message, int nType = 0, bool overwrite = false)
        {
            SystemLog_Form.LogMessage(m_txtLog, message, nType, overwrite);
        }
    }
}