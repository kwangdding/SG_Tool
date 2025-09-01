namespace SG_Tool.Email_Tool.Target
{
    public class LoadAsia_Form : UserControl
    {
        FlowLayoutPanel m_txtName = SG_Common.CreateLabeledPanel("이름", 60, "안광열", false, 20);
        Label m_lblServerSelect = null!;
        ComboBox m_cmbServerList = null!;
        FlowLayoutPanel m_flowPanel = null!;
        Button m_btnUp = null!;
        TextBox m_txtLog = null!;
        const string c_strLoad = @"Email\Email_L9Asia.cfg";
        Dictionary<Email_DataType, string> m_dicData = new Dictionary<Email_DataType, string>();
        FlowLayoutPanel[] m_aParamter = new FlowLayoutPanel[]
        {
            SG_Common.CreateLabeledPanel("작업 내용", 400, "250709 타겟 패치", true, 40)
        };
        List<FlowLayoutPanel> m_listSqlPathPanels = new List<FlowLayoutPanel>();
        List<FlowLayoutPanel> m_listBeforeSQLPanels = new List<FlowLayoutPanel>();
        List<FlowLayoutPanel> m_listAfterSQLPanels = new List<FlowLayoutPanel>();
        Button m_btnAddSqlPath = null!;
        Button m_btnAddBeforeSQL = null!;
        Button m_btnAddAfterSQL = null!;
        public LoadAsia_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            SG_Common.SetPatchData(c_strLoad, m_dicData);
            Width = 1000;
            Height = 800;
            MinimumSize = new Size(600, 500);

            // (1) 서버 선택: Label + ComboBox (한 줄 정렬용 TableLayoutPanel)
            var serverRowPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(10)
            };
            serverRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            serverRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            m_lblServerSelect = new Label
            {
                Text = "서버 선택:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5)
            };

            m_cmbServerList = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5)
            };
            m_cmbServerList.Items.AddRange(new string[] { "QA0", "QA1", "QA2", "qa3", "REVIEW" });
            m_cmbServerList.SelectedIndexChanged += ServerChangeList;

            serverRowPanel.Controls.Add(m_lblServerSelect, 0, 0);
            serverRowPanel.Controls.Add(m_cmbServerList, 1, 0);

            // (2) m_flowPanel - 상단 입력 컨트롤 영역
            m_flowPanel = new FlowLayoutPanel
            {
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightBlue,
                Dock = DockStyle.Fill,
            };

            m_btnUp = new Button { Text = "Open OutLook", Width = 100, BackColor = Color.LightGreen };
            m_btnUp.Click += ServerUp_Click;

            m_btnAddBeforeSQL = new Button { Text = "Before", Width = 150, BackColor = Color.LightGreen, Margin = new Padding(5) };
            m_btnAddBeforeSQL.Click += (s, e) =>
            {
                AddSqlPathPanel("Before SQL", @"\5.DBScript\NX3\2025\08\202500903_v2\update\mssql\before\DBAccount_tGameNotice_PK_tGameNotice_REBUILD.sql");
            };

            m_btnAddSqlPath = new Button { Text = "Patch", Width = 150, BackColor = Color.LightGreen, Margin = new Padding(5) };
            m_btnAddSqlPath.Click += (s, e) =>
            {
                AddSqlPathPanel("Patch SQL", @"\5.DBScript\NX3\2025\08\202500903_v2\update\mssql\DBShard_A_LIVE_UPDATE.sql");
            };

            m_btnAddAfterSQL = new Button { Text = "After", Width = 150, BackColor = Color.LightGreen, Margin = new Padding(5) };
            m_btnAddAfterSQL.Click += (s, e) =>
            {
                AddSqlPathPanel("After SQL", @"\5.DBScript\NX3\2025\08\202500903_v2\update\mssql\after\DBAccount_tAccountNotify테이블초기화.sql");
            };

            m_txtName = SG_Common.CreateLabeledPanel("이름", 60, m_dicData[Email_DataType.Name], false, 20);

            m_flowPanel.Controls.Add(m_btnUp);
            m_flowPanel.Controls.Add(m_txtName);

            foreach (var control in m_aParamter)
                m_flowPanel.Controls.Add(control);

            m_flowPanel.Controls.Add(m_btnAddBeforeSQL);
            m_flowPanel.Controls.Add(m_btnAddSqlPath);
            m_flowPanel.Controls.Add(m_btnAddAfterSQL);

            // (3) 로그 TextBox - 자동 스크롤, Dock으로 확장
            m_txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8),
                Dock = DockStyle.Fill
            };

            // (4) 상하 구조의 전체 Layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // 서버 선택
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));     // 상단 영역
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));     // 로그 영역

            layout.Controls.Add(serverRowPanel, 0, 0);   // 서버 선택 라벨+콤보박스
            layout.Controls.Add(m_flowPanel, 0, 1);      // 파라미터 입력 패널
            layout.Controls.Add(m_txtLog, 0, 2);         // 로그 박스

            Controls.Add(layout);
            SG_Common.Log(m_txtLog, $"MailMain : {m_dicData[Email_DataType.MailMain]}");
        }

        void ServerChangeList(object sender, EventArgs e)
        {
            string selectedServer = m_cmbServerList.SelectedItem?.ToString() ?? string.Empty;
            SG_Common.Log(m_txtLog, $"[ServerChangeList] Select {selectedServer}");
        }

        void ServerUp_Click(object sender, EventArgs e)
        {
            SetEmail();
        }

        void AddSqlPathPanel(string strName, string defaultValue)
        {
            var panel = SG_Common.CreateLabeledPanel(strName, 850, defaultValue, false, 20);
            panel.Margin = new Padding(3, 3, 3, 5);

            if (strName.Contains("Before"))
            {
                m_listBeforeSQLPanels.Add(panel);
                m_flowPanel.Controls.Add(panel);
                
                m_flowPanel.Controls.SetChildIndex(panel, m_flowPanel.Controls.Count - (m_listBeforeSQLPanels.Count + m_listSqlPathPanels.Count + m_listAfterSQLPanels.Count + 2));
            }
            else if (strName.Contains("Patch"))
            {
                m_listSqlPathPanels.Add(panel);
                m_flowPanel.Controls.Add(panel);
                m_flowPanel.Controls.SetChildIndex(panel, m_flowPanel.Controls.Count - (m_listSqlPathPanels.Count + m_listAfterSQLPanels.Count + 1));
            }
            else
            {
                m_listAfterSQLPanels.Add(panel);
                m_flowPanel.Controls.Add(panel);
                m_flowPanel.Controls.SetChildIndex(panel, m_flowPanel.Controls.Count - m_listAfterSQLPanels.Count);
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
            string strName = m_txtName.Controls.OfType<TextBox>().FirstOrDefault()?.Text ?? "";
            string strList = string.Empty;

            Email_Tool_Form.SQLData BeforeSQL = new Email_Tool_Form.SQLData();
            Email_Tool_Form.SQLData PatchSQL = new Email_Tool_Form.SQLData();
            Email_Tool_Form.SQLData AfterSQL = new Email_Tool_Form.SQLData();


            foreach (var panel in m_aParamter)
            {
                var lines = panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    strList += $"\t    - {line}\r\n";
                }
            }

            foreach (var panel in m_listBeforeSQLPanels)
            {
                BeforeSQL.SetSQLData(panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text);
            }

            foreach (var panel in m_listSqlPathPanels)
            {
                PatchSQL.SetSQLData(panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text);
            }

            foreach (var panel in m_listAfterSQLPanels)
            {
                AfterSQL.SetSQLData(panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text);
            }

            string strTarget = GetTargetText(selectedServer);
            string strBefore = GetSQL(m_listBeforeSQLPanels.Count, $@"	■ 패치 전 작업(Before)", BeforeSQL);
            string strPatch = GetSQL(m_listSqlPathPanels.Count, $@"	■ 패치 작업", PatchSQL);
            string strAfter = GetSQL(m_listAfterSQLPanels.Count, $@"	■ 패치 후 작업(After)", AfterSQL);
            

            string strMain = $@"
안녕하세요
스마일 게이트 메가포트 협력사 {strName}입니다.
{selectedServer}환경 패치를 위해 아래 DB스크립트 적용 요청 드립니다.

	■ Jira
	    - 

	■ 작업 대상
{strTarget}
	■ 패치 내용
{strList}
{strBefore}
{strPatch}
{strAfter}

관련해서 궁금하신 부분은 편하신 방법으로 말씀 부탁드립니다.

감사합니다.
{strName} 드림

";

            // RE: [로드나인] QA2환경 DB 작업 요청
            // string subject = $"RE: [로드나인] {selectedServer}환경 DB 작업 요청";
            SG_Common.Log(m_txtLog, $"======================= {selectedServer} Start =======================", 1);
            SG_Common.Log(m_txtLog, strMain, 1);
            SG_Common.Log(m_txtLog, $"======================= {selectedServer} End =======================", 1);
            string subject = $"RE: [로드나인_아시아]{selectedServer} 환경 작업 요청 드립니다.";
            SG_Common.ReplyToLatestMail(m_txtLog, subject, strMain);

            // 저장.
            m_dicData[Email_DataType.Name] = strName;
            m_dicData[Email_DataType.MailMain] = strMain;
            SG_Common.SavePatchData(m_txtLog, c_strLoad, m_dicData);
        }

        string GetSQL(int nCout, string strTarget, Email_Tool_Form.SQLData sQLData)
        {
            if (nCout == 0)
                return string.Empty;

            return $@"{strTarget}
		◆ Account DB
{sQLData.Account}
		◆ Shard DB
{sQLData.Shard}
		◆ World DB
{sQLData.World}
";
        }

        string GetTargetText(string selectedServer)
        {
            string strTarget = string.Empty;
            switch (selectedServer)
            {
                case "QA0":
                    strTarget = "\t    - QA0(AccountDB, ShardDB, WorldDB)\r\n\t    - DB IP : 10.168.194.190\r\n";
                    break;
                case "QA1":
                    strTarget = "\t    - QA1(AccountDB, ShardDB_000, WorldDB_1,2,3,4)\r\n\t    - DB IP : 10.168.196.187\r\n";
                    break;
                case "QA2":
                    strTarget = "\t    - QA2 (AccountDB, ShardDB, WorldDB1,2)\r\n\t    - DB IP : 10.168.196.241\r\n";
                    break;
                case "QA3":
                    strTarget = "\t    - QA3 (AccountDB, ShardDB 1,2, WorldDB 1,2)\r\n\t    - DB IP : lord-asia-q3-gdb-db-01.c9ywe8s2a44v.ap-east-1.rds.amazonaws.com\r\n";
                    break;
                case "Review":
                    strTarget = "\t    - REVIEW(AccountDB, ShardDB, WorldDB)\r\n\t    - DB IP : lord-asia-q3-gdb-db-01.c9ywe8s2a44v.ap-east-1.rds.amazonaws.com\r\n";
                    break;
                case "Live":
                    break;
            }
            return strTarget;
        }
    }
}