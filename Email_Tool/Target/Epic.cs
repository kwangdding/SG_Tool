namespace SG_Tool.Email_Tool.Target
{
    public class Epic_Form : UserControl
    {
        FlowLayoutPanel m_txtName = SG_Common.CreateLabeledPanel("이름", 60, "안광열", false, 20);
        Label m_lblServerSelect = null!;
        ComboBox m_cmbServerList = null!;
        FlowLayoutPanel m_flowPanel = null!;
        Button m_btnUp = null!;
        TextBox m_txtLog = null!;
        const string c_strEpic = @"Email\Email_Epic.cfg";
        Dictionary<Email_DataType, string> m_dicData = new Dictionary<Email_DataType, string>();
        FlowLayoutPanel[] m_aParamter = new FlowLayoutPanel[]
        {
            SG_Common.CreateLabeledPanel("작업 내용", 400, "20250731 타겟 빌드용 SQL 스키마 변경", true, 40)
        };
        List<FlowLayoutPanel> m_listSqlPathPanels = new List<FlowLayoutPanel>();
        List<FlowLayoutPanel> m_listSqlValue = new List<FlowLayoutPanel>();
        Button m_btnAddSqlPath = null!;

        public Epic_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            SG_Common.SetPatchData(c_strEpic, m_dicData);
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
            m_cmbServerList.Items.AddRange(new string[] { "QA1", "QA2", "QA3", "Review" });
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

            m_btnUp = new Button { Text = "Open OutLook", Width = 100 , BackColor = Color.LightGreen };
            m_btnUp.Click += ServerUp_Click;

            m_btnAddSqlPath = new Button { Text = "Patch", Width = 150, Margin = new Padding(5) , BackColor = Color.LightGreen };
            m_btnAddSqlPath.Click += (s, e) =>
            {
                AddSqlPathPanel();
            };

            m_txtName = SG_Common.CreateLabeledPanel("이름", 60, m_dicData[Email_DataType.Name], false, 20);

            m_flowPanel.Controls.Add(m_btnUp);
            m_flowPanel.Controls.Add(m_txtName);

            foreach (var control in m_aParamter)
                m_flowPanel.Controls.Add(control);

            m_flowPanel.Controls.Add(m_btnAddSqlPath);

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

        int m_nCount = 0;
        void AddSqlPathPanel()
        {
            m_nCount++;
            var panel = SG_Common.CreateLabeledPanel($"{m_nCount}. SQL 경로", 850, "/svn/epic7_deploy/3_server_db/epic7-star-diff-epic7-dev-20250731.sql", false, 20);
            panel.Margin = new Padding(3, 3, 3, 5);
            m_listSqlPathPanels.Add(panel);

            var panel2 = SG_Common.CreateLabeledPanel($"{m_nCount}. SQL 내용", 850, "1. PvpSASeasons 테이블에 is_rookie_arena 컬럼 추가 ; 루키 아레나 구현\r\n2. PvpSeasonPass 테이블 추가 ; 아레나 대전 패스 개선에 따른 전용 테이블 생성\r\n3. BattleRecords_202503, PvpSABattleRecords_202503 ; 테이블 삭제", true, 60);
            panel2.Margin = new Padding(3, 3, 3, 5);
            m_listSqlValue.Add(panel2);

            m_flowPanel.Controls.Add(panel);
            m_flowPanel.Controls.Add(panel2);
        }

        const string c_strArena = "월드아레나 서버 SQL";
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

            string strSQL = string.Empty;


            foreach (var panel in m_aParamter)
            {
                var lines = panel.Controls.OfType<TextBox>().FirstOrDefault()?.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    strList += $"\t    - {line}\r\n";
                }
            }
            //(게임 서버 SQL)   epic7-dev
            //(월드아레나 서버 SQL)  epic7-arena
            for (int i = 0; i < m_listSqlPathPanels.Count; i++)
            {
                if (i > 0)
                    strSQL += $"\r\n";

                string strText = m_listSqlPathPanels[i].Controls.OfType<TextBox>().FirstOrDefault()?.Text;

                strText += strText.Contains(c_strArena) ? " (월드아레나 서버 SQL)" : " (게임 서버 SQL)";
                strSQL += $"\t◍ {strText}\r\n";

                var lines = m_listSqlValue[i].Controls.OfType<TextBox>().FirstOrDefault()?.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    strSQL += $"\t    {line}\r\n";
                }
            }

            string strMain = $@"
안녕하세요 
스마일게이트 메가포트 협력사 {strName}입니다. 
{selectedServer} 그룹 테스트를 위해 아래 {selectedServer} 그룹 DB 스크립트 작업 부탁드립니다.

	■ JiRA
	    - 

	■ {GetDBName(selectedServer)}
{strList}
{strSQL}

{GetTargetText(selectedServer)}
 
추가 문의는 언제든지 말씀하여 주시기 바랍니다.

감사합니다.
{strName} 드림

";

            SG_Common.Log(m_txtLog, $"======================= {selectedServer} Start =======================", 1);
            SG_Common.Log(m_txtLog, strMain, 1);
            SG_Common.Log(m_txtLog, $"======================= {selectedServer} End =======================", 1);
            SG_Common.ReplyToLatestMail(m_txtLog, GetMailName(selectedServer), strMain);

            // 저장.
            m_dicData[Email_DataType.Name] = strName;
            m_dicData[Email_DataType.MailMain] = strMain;
            SG_Common.SavePatchData(m_txtLog, c_strEpic, m_dicData);
        }

        string GetDBName(string selectedServer)
        {
            switch (selectedServer)
            {
                default:
                case "QA1":
                    return "DB 스크립트(SVN) QA1_KOR / QA1_GLB";
                case "QA2":
                    return "DB 스크립트(SVN) QA2_KOR / QA2_GLB / QA2_JPN";
                case "QA3":
                    return "DB 스크립트(SVN) QA3_KOR / QA3_JPN";
                case "Review":
                    return "DB 스크립트(SVN) Review_KOR";
            }
        }

        string GetMailName(string selectedServer)
        {
            switch (selectedServer)
            {
                default:
                case "QA1":
                    return $"RE: [에픽세븐] {selectedServer} 그룹 환경 업데이트의 건으로 메일 드립니다";
                case "QA2":
                    return $"RE: [에픽세븐] QA 2그룹 환경 업데이트의 건으로 메일 드립니다";
                case "QA3":
                    return $"RE: [에픽세븐] {selectedServer} 그룹 환경 업데이트의 건으로 메일 드립니다";
                case "Review":
                    return $"RE: [에픽세븐] {selectedServer} 그룹 환경 업데이트의 건으로 메일 드립니다";
            }
        }

        string GetTargetText(string selectedServer)
        {
            switch (selectedServer)
            {
                default:
                case "QA1":
                    return "\t⇨ Game_DB KOR(qa1-k - epic7 - dev2.cluster - cn9mi3sh330b.ap - northeast - 2.rds.amazonaws.com)\r\n\t⇨ Game_DB GLOBAL(qa1-g - epic7 - gamedb - 01.cluster - cxkfgzwjh5j6.us - west - 2.rds.amazonaws.com)\r\n";
                case "QA2":
                    return "\t⇨ Game_DB KOR2 (qa2-k-epic7-gamedb-01.cluster-cn9mi3sh330b.ap-northeast-2.rds.amazonaws.com)\r\n\t⇨ Game_DB GLOBAL2 (qa2-g-epic7-gamedb-01.cluster-cxkfgzwjh5j6.us-west-2.rds.amazonaws.com)\r\n\t⇨ Game_DB JPN2 (qa2-j-epic7-gamedb-01.cluster-cq6trqveuuvb.ap-northeast-1.rds.amazonaws.com)\r\n";
                case "QA3":
                    return "\t⇨ Game_DB KOR3 (qa3-k-epic7-gamedb-01.cluster-cn9mi3sh330b.ap-northeast-2.rds.amazonaws.com)\r\n\t⇨ Game_DB JPN3 (qa3-j-epic7-gamedb-01.cluster-cq6trqveuuvb.ap-northeast-1.rds.amazonaws.com)\r\n";
                case "Review":
                    return "\t⇨ Game_DB kor (review-epic7.cluster-cn9mi3sh330b.ap-northeast-2.rds.amazonaws.com)\r\n";
            }
        }
    }
}