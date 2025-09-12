using SG_Tool.Log;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace SG_Tool.L9_Tool.FTP
{
    public class DBUpload : UserControl
    {
        bool m_bSetting = false;
        string m_strSelectedServer = string.Empty;
        ComboBox m_comboBox = null!;
        Button m_btnDBUpload = null!;
        TextBox m_txtParameter = null!;
        TextBox m_txtLog = null!;
        EnLoad9_Type m_enLoad9_Type = EnLoad9_Type.L9;

        string m_strConfigFile = $@"L9\l9_Data.cfg";
        Dictionary<L9DataType, string> m_dicData = new Dictionary<L9DataType, string>();

        public DBUpload (EnLoad9_Type enLoad9_Type)
        {
            m_enLoad9_Type = enLoad9_Type;
            m_strConfigFile = $@"{enLoad9_Type}\L9_Data.cfg";
            InitializeUI();
        }

        void InitializeUI()
        {
            m_bSetting = SG_Common.SetPatchData(m_strConfigFile, m_dicData);
            Text = "DBUpload";
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

            m_txtParameter = SG_Common.CreateLabeledTextBox("패치날짜:", 180, m_dicData.ContainsKey(L9DataType.DBUpload) ? m_dicData[L9DataType.DBUpload] : "없음");
            m_btnDBUpload = new Button
            {
                Text = "DBUpload",
                Width = 120,
                Height = 30,
                Margin = new Padding(5, 6, 5, 0), // 버튼도 정렬 맞춰
                Anchor = AnchorStyles.Left
            };
            m_btnDBUpload.Click += DBUpload_Click;

            var controlsToAdd = new List<Control> { m_comboBox };
            if (m_txtParameter.Parent != null) controlsToAdd.Add(m_txtParameter.Parent);
            controlsToAdd.Add(m_btnDBUpload);

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
                RowCount = 2,
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

        void DBUpload_Click(object sender, EventArgs e) => ProcessDBUpload();
        void ProcessDBUpload()
        {
            if (!m_bSetting)
                SystemLog_Form.LogMessage(m_txtLog, $"cfg 파일이 없어 Live환경 실행 할 수 없습니다.");
            else
                FileUpload();
        }

        async void FileUpload()
        {
            try
            {
                SystemLog_Form.LogMessage(m_txtLog, $"[DBUpload()] 다운로드 시작..");
                // 1. FTP로 파일을 다운로드 받는다. // project-lord/Web/PatchConfig/{strDate}/Operation.ProjectL.Protocol.json
                var s3Client = new AmazonS3Client(m_dicData[L9DataType.NX3AwsAccessKey], m_dicData[L9DataType.NX3AwsSecretKey], RegionEndpoint.APNortheast2);
                var transferUtility = new TransferUtility(s3Client);
                string strKey = @$"Web_DataTable/{m_txtParameter.Text.Trim()}/DBPlan.db";
                string strlocalFilePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\DBPlan\{m_txtParameter.Text.Trim()}\DBPlan.db";
                await SG_Common.DownloadAsyncToS3(m_txtLog, transferUtility, strlocalFilePath, m_dicData[L9DataType.S3FileBucket], strKey);

                if (m_strSelectedServer == string.Empty)
                {
                    SystemLog_Form.LogMessage(m_txtLog, $"[DBUpload()] {m_strSelectedServer}서버를 선택해주세요.");
                    return;
                }

                var s3UploadClient = m_enLoad9_Type == EnLoad9_Type.L9 ?
                    new AmazonS3Client(m_dicData[L9DataType.AwsAccessKey], m_dicData[L9DataType.AwsSecretKey], RegionEndpoint.APNortheast1) :
                    new AmazonS3Client(m_dicData[L9DataType.AwsAccessKey], m_dicData[L9DataType.AwsSecretKey], RegionEndpoint.APEast1);


                var transferUploadUtility = new TransferUtility(s3UploadClient);

                strKey = @$"{m_strSelectedServer}/InGameTableData/DBPlan.db";
                SystemLog_Form.LogMessage(m_txtLog, $"[DBUpload()] {strKey}  업로드 시작..");
                await SG_Common.UploadAsyncToS3(m_txtLog, transferUploadUtility, strlocalFilePath, m_dicData[L9DataType.S3UploadBucket], strKey);

                m_dicData[L9DataType.DBUpload] = m_txtParameter.Text.Trim();
                SG_Common.SaveData(m_txtLog, m_strConfigFile, m_dicData);
                SystemLog_Form.LogMessage(m_txtLog, $"✅ [DBUpload()] 업로드 완료..");
                // 다운 받은 DBPlan.db 파일 폴더 삭제
                // strlocalFilePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\DBPlan\{m_txtParameter.Text.Trim()}";
                // Directory.Delete(strlocalFilePath, recursive: true);
            }
            catch (Exception ex)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"❌ [DBUpload ERROR] {ex.Message}");
            }
        }
    }
}