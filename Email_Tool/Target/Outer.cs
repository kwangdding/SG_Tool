namespace SG_Tool.Email_Tool.Target
{
    public class Outer_Form : UserControl
    {
        FlowLayoutPanel m_txtName = SG_Common.CreateLabeledPanel("이름", 60, "안광열", false, 20);
        Label m_lblServerSelect = null!;
        ComboBox m_cmbServerList = null!;
        FlowLayoutPanel m_flowPanel = null!;
        Button m_btnUp = null!;
        TextBox m_txtLog = null!;
        const string c_strOuter = @"Email\Email_Outer.cfg";
        Dictionary<Email_DataType, string> m_dicData = new Dictionary<Email_DataType, string>();
        FlowLayoutPanel[] m_aParamter = new FlowLayoutPanel[]
        {
            SG_Common.CreateLabeledPanel("작업 내용", 400, "컨텐츠 수정", true, 40)
        };
        List<FlowLayoutPanel> m_listSqlPathPanels = new List<FlowLayoutPanel>();
        List<FlowLayoutPanel> m_listRedisPanels = new List<FlowLayoutPanel>();
        Button m_btnAddSqlPath = null!;
        Button m_btnAddRedis = null!;
        public Outer_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            SG_Common.SetPatchData(c_strOuter, m_dicData);
            Width = 1000;
            Height = 800;
            MinimumSize = new Size(600, 500);

            m_lblServerSelect = new Label
            {
                Text = "서버 선택:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            m_cmbServerList = new ComboBox
            {
                Location = new Point(100, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            m_cmbServerList.Items.AddRange(new string[] { "QA0", "QA1", "QA2", "Review", "Live" });

            m_flowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 60),
                Size = new Size(940, 250),
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightBlue
            };

            m_btnUp = new Button
            {
                Text = "Open OutLook",
                BackColor= Color.LightGreen,
                Width = 100
            };
            m_btnUp.Click += ServerUp_Click;

            // [SQL 경로 추가] 버튼
            m_btnAddSqlPath = new Button
            {
                Text = "SQL 경로 추가",
                BackColor = Color.LightGreen,
                Width = 150,
                Margin = new Padding(5)
            };
            m_btnAddSqlPath.Click += (s, e) =>
            {
                AddSqlPathPanel($"SQL 경로 {m_listSqlPathPanels.Count + 1}", $"/svn/OuterPlane/2_DB/날짜/OP_Game_Update1.sql");
            };

            // [SQL 경로 추가] 버튼
            m_btnAddRedis = new Button
            {
                Text = "Redis 요청",
                BackColor = Color.LightGreen,
                Width = 150,
                Margin = new Padding(5)
            };
            m_btnAddRedis.Click += (s, e) =>
            {
                AddSqlPathPanel($"Redis 변경 내용 : ", $"서비스 업데이트(보안 및 엔진 업데이트 누적 건)");
            };

            m_txtName = SG_Common.CreateLabeledPanel("이름", 60, m_dicData[Email_DataType.Name], false, 20);
            m_flowPanel.Controls.Add(m_btnUp);
            m_flowPanel.Controls.Add(m_txtName);

            foreach (var control  in m_aParamter)
                m_flowPanel.Controls.Add(control);
           
            m_flowPanel.Controls.Add(m_btnAddRedis);
            m_flowPanel.Controls.Add(m_btnAddSqlPath);

            m_txtLog = new TextBox
            {
                Name = "Email",
                Multiline = true,
                Width = 970,
                Height = 400,
                Location = new Point(20, m_flowPanel.Bottom + 10),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(m_lblServerSelect);
            Controls.Add(m_cmbServerList);
            Controls.Add(m_flowPanel);
            Controls.Add(m_txtLog);
            SG_Common.Log(m_txtLog, $"MailMain : {m_dicData[Email_DataType.MailMain]}"); 
        }

        void ServerUp_Click(object sender, EventArgs e)
        {
            SetEmail();
        }

        void AddSqlPathPanel(string strName, string defaultValue)
        {
            var panel = SG_Common.CreateLabeledPanel(strName, 600, defaultValue, false, 20);

            if (strName.Contains("Redis"))
            {
                m_listRedisPanels.Add(panel);
                m_flowPanel.Controls.Add(panel);
                m_flowPanel.Controls.SetChildIndex(panel, m_flowPanel.Controls.Count - (m_listRedisPanels.Count + m_listSqlPathPanels.Count + 1));
            }
            else
            {
                m_listSqlPathPanels.Add(panel);
                m_flowPanel.Controls.Add(panel);
                m_flowPanel.Controls.SetChildIndex(m_btnAddSqlPath, m_flowPanel.Controls.Count - m_listSqlPathPanels.Count - 1);
            }

            if (m_listSqlPathPanels.Count + m_listRedisPanels.Count > 2)
            {
                m_flowPanel.Height += 35;
                m_txtLog.Location = new Point(25, m_flowPanel.Bottom + 10);
            }
        }

        void SetEmail()
        {
            if (m_cmbServerList.SelectedItem == null)
            {
                SG_Common.Log(m_txtLog, $"패치 대상 환경 서버 선택해주세요.");
                return;
            }

            string selectedServer = m_cmbServerList.SelectedItem?.ToString() ?? string.Empty;
            string strServer = selectedServer.Equals("Review") ? "REV" : selectedServer;
            string strName = m_txtName.Controls.OfType<TextBox>().FirstOrDefault()?.Text ?? "";
            string strList = string.Empty;
            string strSQL = string.Empty;
            string strRedis = string.Empty;

            foreach (var panel in m_aParamter)
            {
                var lines = panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    strList += $"\t    - {line}\r\n";
                }
            }

            foreach (var panel in m_listSqlPathPanels)
            {
                strSQL += $"\t\t- {panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text}\r\n";
            }

            foreach (var panel in m_listRedisPanels)
            {
                strRedis += @$"
    4. Redis
      - {panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text}
";
            }

            string strMain = $@"
안녕하세요
스마일게이트 메가포트 협력사 {strName}입니다
아우터플레인 {selectedServer} 환경에 아래 내용 적용 요청 드립니다.

	0. 티켓
	    • 

	1. 대상
	    • {selectedServer}(GameDB)    
	    • q0-outr-gldb-1(10.248.16.250)

	2. 작업 내용
{strList}                                                                      
	    • GameDB(OP_Game_{strServer})
{strSQL}
 
관련하여 궁금하신 사항 있으시면 말씀 부탁드립니다.
 
고맙습니다.

{strName} 드림

";

            string strTarget = 
                string.IsNullOrEmpty(strRedis) ?

                $"@정용현 (Brady)/SGP DB플랫폼팀 과장님, @김영환B/SGP 서비스인프라팀 과장님(cc, @김선우 (Alonso)/ SGP DB플랫폼팀)" :
                $"@정용현 (Brady)/SGP DB플랫폼팀 과장님, @김영환B/SGP 서비스인프라팀 과장님, @김수아D (Vanellope)/SGP 빅데이터2팀 대리님 (cc, @김선우 (Alonso)/SGP DB플랫폼팀)";

            string strLive = $@"
안녕하세요
스마일게이트 메가포트 협력사 {strName}입니다.

{strTarget}

아우터플레인 점검 시 Live 환경에 적용이 필요한 작업 요청 드립니다.
내용 확인해 보시고 관련해서 추가 / 수정이 필요한 내용은 말씀 부탁드립니다.

    1.일정
	  • 07/01(화) 08:30 ~13:00
	  • Slack 알럿 OFF
	  • 점검 시작, 서비스 중지 후 아래 작업 진행은 별도 전달

    2.백업(전체월드)
	  • GameDB, LogDB 스냅샷 생성

    3.DB 작업(전체 월드)
{strSQL}
{strRedis}

고맙습니다.

{strName} 드림.
";

            // RE: [아우터플레인] QA0 그룹 DB 작업 요청
            // RE: [아우터플레인] QA1 그룹 DB 작업 요청
            // RE: [아우터플레인] QA2 그룹 DB 작업 요청
            // RE: [아우터플레인] Review 그룹 DB 작업 요청
            // string subject = $"RE: [아우터플레인] {selectedServer} 그룹 DB 작업 요청";
            // RE: [아우터플레인] Live 환경 작업 요청

            SG_Common.Log(m_txtLog, $"======================= {selectedServer} Start =======================", 1);
            if (selectedServer.Equals("Live"))
            {
                SG_Common.Log(m_txtLog, strLive, 1);
                SG_Common.Log(m_txtLog, $"======================= {selectedServer} End =======================", 1);
                string subject = "RE: [아우터플레인] Live 환경 작업 요청";
                SG_Common.ReplyToLatestMail(m_txtLog, subject, strLive);
            }
            else
            {
                SG_Common.Log(m_txtLog, strMain, 1);
                SG_Common.Log(m_txtLog, $"======================= {selectedServer} End =======================", 1);
                string subject = $"RE: [아우터플레인] {selectedServer} 그룹 DB 작업 요청";
                SG_Common.ReplyToLatestMail(m_txtLog, subject, strMain);
            }

            // 저장.
            m_dicData[Email_DataType.Name] = strName;
            m_dicData[Email_DataType.MailMain] = strMain;
            SG_Common.SavePatchData(m_txtLog, c_strOuter, m_dicData);
        }
    }
}