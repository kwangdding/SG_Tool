using SG_Tool.Log;
using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SG_Tool.L9_Tool.AWS
{
    public class EcsPatch_Live_Form : UserControl
    {
        AmazonECSClient m_ecsClient;
        bool m_bSetting = false;
        Button m_btnUpdateImage = null!;
        Button m_btnUpdateAll = null!;
        Button m_btnECS_Off = null!;
        Button m_btnECS_On = null!;

        bool m_bEditingImage = false;
        bool m_bEditingCount = false;

        TextBox m_txtLog = null!;
        TableLayoutPanel m_checkBoxPanel = null!;
        CancellationTokenSource m_statusCts = null;
        Dictionary<EcsDataEnum, EcsData> m_dicecsData = new Dictionary<EcsDataEnum, EcsData>();
        string m_strConfigFile = $@"L9\L9_LiveData.cfg";
        Dictionary<L9DataType, string> m_dicData = new Dictionary<L9DataType, string>();
        Dictionary<EcsDataEnum, TextBox[]> m_dicParameters = new Dictionary<EcsDataEnum, TextBox[]>();
        EnLoad9_Type m_enLoad9_Type;
        public EcsPatch_Live_Form (EnLoad9_Type enLoad9_Type)
        {
            m_enLoad9_Type = enLoad9_Type;
            m_strConfigFile = $@"{enLoad9_Type}\L9_LiveData.cfg";
            InitializeUI();
        }

        void InitializeUI()
        {
            SG_Common.Log(m_txtLog, $"✅ InitializeUI");
            m_bSetting = SG_Common.SetPatchL9Data(m_strConfigFile, m_dicData);
            BackColor = Color.LightCyan;

            // 상단 버튼 및 파라미터 영역
            var topPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Padding = new Padding(5),
                AutoSize = true,
                //BackColor = Color.LightCyan
                BackColor = m_enLoad9_Type == EnLoad9_Type.L9 ? Color.LightCyan : Color.LightPink
            };

            m_btnUpdateImage = SG_Common.GetButton("Update Image", Color.AliceBlue);
            m_btnUpdateAll = SG_Common.GetButton("Update ECS", Color.AliceBlue);
            m_btnECS_Off = SG_Common.GetButton("ECS Off", Color.AliceBlue);
            m_btnECS_On = SG_Common.GetButton("ECS On", Color.AliceBlue);

            m_btnUpdateImage.Click += UpdateImage_Click;
            m_btnUpdateAll.Click += UpdateService_Click;
            m_btnECS_Off.Click += ECS_Off_Click;
            m_btnECS_On.Click += ECS_On_Click;

            var ecsButtonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,   // 수직 정렬
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Margin = new Padding(5, 6, 5, 0), // 높이에 따라 조절 (예: 6)
            };
            ecsButtonPanel.Controls.AddRange(new Control[] { m_btnUpdateAll, m_btnECS_Off, m_btnECS_On });

            m_checkBoxPanel = new TableLayoutPanel
            {
                Width = 500,
                Height = 180,
                Margin = new Padding(5),
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // 상단에 배치할 컨트롤 구성
            topPanel.Controls.Add(m_btnUpdateImage);
            topPanel.Controls.Add(ecsButtonPanel); // 여기에 ECS 버튼 묶은 패널 추가
            topPanel.Controls.Add(m_checkBoxPanel);

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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));

            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(m_txtLog, 0, 1);

            Controls.Add(mainPanel);
            LoadServerList();
        }

        void LoadServerList()
        {
            string strServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "L9", "L9_Live.cfg");
            m_bEditingImage = false;
            m_bEditingCount = false;
            m_checkBoxPanel.Controls.Clear();
            m_dicParameters.Clear();
            m_dicecsData.Clear();

            // ===========================================================
            // 지역은 필요 시 설정
            var config = new AmazonECSConfig
            {
                RegionEndpoint = m_enLoad9_Type == EnLoad9_Type.L9 ? RegionEndpoint.APNortheast1 :RegionEndpoint.APEast1 // 도쿄리전 : 홍콩리전
            };

            // AWS 자격 증명 설정
            //SystemLog_Form.LogMessage(m_txtLog, $"[LoadServerList()] AwsAccessKey : {m_dicData[L9DataType.AwsAccessKey]}, AwsSecretKey : {m_dicData[L9DataType.AwsSecretKey]}");
            m_ecsClient = new AmazonECSClient(m_dicData[L9DataType.AwsAccessKey], m_dicData[L9DataType.AwsSecretKey], config);
            // ===========================================================

            if (File.Exists(strServerPath))
            {
                SG_Common.Log(m_txtLog, $"✅ Live AWS ECS Load Start");
                List<TextBox> imageBoxlist = new List<TextBox>();
                List<TextBox> countBoxlist = new List<TextBox>();
                List<Label> statusLabellist = new List<Label>();
                List<EcsData> ecsDatalist = new List<EcsData>();

                var lines = File.ReadAllLines(strServerPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#")) continue; // 주석 제외.

                    var parts = line.Split(' ');
                    if (parts.Length != 5)
                    {
                        SG_Common.Log(m_txtLog, $"✅ LoadServerList : {strServerPath} 파일 확인 파라미터 부족.");
                        continue;
                    }

                    EcsData ecsData = new EcsData(parts[0], parts[1], parts[2], parts[3], int.Parse(parts[4]));

                    var checkBox = new CheckBox
                    {
                        Text = ecsData.Tag,
                        AutoSize = true,
                        Checked = ecsData.Tag == "log"? false : true
                    };

                    var imageBox = new TextBox
                    {
                        Width = 180,
                        Text = "0000_00_00_0",
                        ForeColor = Color.Black,
                        TextAlign = HorizontalAlignment.Left,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };
                    imageBox.Enter += (s, e) => m_bEditingImage = true;

                    var countBox = new TextBox
                    {
                        Width = 40,
                        Text = "00",
                        ForeColor = Color.Black,
                        TextAlign = HorizontalAlignment.Center,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };
                    countBox.Enter += (s, e) => m_bEditingCount = true;

                    var statusLabel = new Label
                    {
                        Text = "서비스 정보 없음",
                        AutoSize = true,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Anchor = AnchorStyles.Left
                    };

                    var panel = new TableLayoutPanel
                    {
                        ColumnCount = 4,
                        AutoSize = true,
                        CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                        Margin = new Padding(2)
                    };


                    // 열 비율 또는 고정 폭 설정
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // CheckBox (Tag)
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // ImageTag TextBox
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));  // Count TextBox
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));  // Count TextBox

                    panel.Controls.Add(checkBox, 0, 0);
                    panel.Controls.Add(imageBox, 1, 0);
                    panel.Controls.Add(countBox, 2, 0);
                    panel.Controls.Add(statusLabel, 3, 0);

                    m_checkBoxPanel.Controls.Add(panel);
                    if (Enum.TryParse<EcsDataEnum>(ecsData.Tag, out var enumKey))
                    {
                        m_dicParameters.Add(enumKey, new TextBox[] { imageBox, countBox });
                        m_dicecsData.Add(enumKey, ecsData);
                    }

                    imageBoxlist.Add(imageBox);
                    countBoxlist.Add(countBox);
                    statusLabellist.Add(statusLabel);
                    ecsDatalist.Add(ecsData);
                }
                StartUpdateStatus(imageBoxlist, countBoxlist, statusLabellist, ecsDatalist);
            }
        }

        void StartUpdateStatus(List<TextBox> imageBox, List<TextBox> countBox, List<Label> statusLabel, List<EcsData> ecsData)
        {
            // 이전 작업이 실행 중이라면 취소
            if (m_statusCts != null)
            {
                SystemLog_Form.LogMessage(m_txtLog, "🔁 업데이트 상태 대상 변경.");
                m_statusCts.Cancel();     // 이전 작업 요청 중단
                m_statusCts.Dispose();
            }

            m_statusCts = new CancellationTokenSource();
            var token = m_statusCts.Token;

            // 새롭게 상태 업데이트 시작
            _ = UpdateStatusAsync(imageBox, countBox, statusLabel, ecsData, token);
        }

        async System.Threading.Tasks.Task UpdateStatusAsync(List<TextBox> imageBoxlist, List<TextBox> countBoxlist, List<Label> statusLabellist, List<EcsData> ecsDatalist, CancellationToken token)
        {
            //int nCount = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (token.IsCancellationRequested) break;
                    if (m_ecsClient == null) return;

                    SG_Common.Log(m_txtLog, $"[UpdateStatusAsync] Live ECS Start");

                    for (int i = 0; i < ecsDatalist.Count; i++)
                    {
                        var ecsData = ecsDatalist[i];
                        var imageBox = imageBoxlist[i];
                        var countBox = countBoxlist[i];
                        var statusLabel = statusLabellist[i];

                        //SG_Common.Log(m_txtLog, $"[UpdateStatusAsync] Cluster : {ecsData.Cluster} (1)");

                        var describeResponse = await m_ecsClient.DescribeServicesAsync(new DescribeServicesRequest
                        {
                            Cluster = ecsData.Cluster,
                            Services = new List<string> { ecsData.Service }
                        });
                        
                        var taskDefResponse = await m_ecsClient.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest
                        {
                            TaskDefinition = ecsData.Task,
                        });
                        var taskDef = taskDefResponse.TaskDefinition;
                        string imageTag = taskDef.ContainerDefinitions.Count > 0 ? taskDef.ContainerDefinitions[0].Image : "0000_00_00_0";

                        Service serviceDesc = describeResponse.Services.FirstOrDefault();
                        string currentStatus = "서비스 정보 없음";

                        if (serviceDesc != null)
                        {
                            currentStatus = $"🟢 {serviceDesc.RunningCount}/{serviceDesc.DesiredCount} Running";
                            if (serviceDesc.PendingCount > 0)
                                currentStatus += $", ⏳ {serviceDesc.PendingCount} Pending";

                            if (serviceDesc.Deployments.Count > 1)
                                currentStatus += ", ⚠️ Deploying...";
                        }

                        if (!m_bEditingImage)
                            imageBox.Text = imageTag.Contains(":") ? imageTag.Substring(imageTag.LastIndexOf(":") + 1) : imageTag;

                        if (!m_bEditingCount)
                            countBox.Text = serviceDesc != null ? serviceDesc.DesiredCount.ToString() : "00";

                        statusLabel.Text = currentStatus;
                        statusLabel.ForeColor = currentStatus.Contains("Pending") || currentStatus.Contains("Deploying") ? Color.Red : Color.Green;
                    }

                    // 5초 대기 후 다음 상태 확인
                    await System.Threading.Tasks.Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    SG_Common.Log(m_txtLog, $"❌ [UpdateStatusAsync] 오류 발생: {ex.Message}");
                    break;
                }
            }
        }

        void UpdateImage_Click(object sender, EventArgs e)
        {
            if (!m_bSetting)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"cfg 파일이 없어 Patch_QA 환경 실행 할 수 없습니다.");
                return;
            }
            UpdateTaskImageTags();
        }

        void UpdateService_Click(object sender, EventArgs e)
        {
            if (!m_bSetting)
            {
                SystemLog_Form.LogMessage(m_txtLog, $"cfg 파일이 없어 Patch_QA 환경 실행 할 수 없습니다.");
                return;
            }

            UpdateServiceWithLatestTaskDef();
        }

        void ECS_Off_Click(object sender, EventArgs e)
        {
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 Patch_QA 환경 실행 할 수 없습니다.");
                return;
            }

            SG_Common.UpdateECS(false, m_txtLog, m_ecsClient, m_checkBoxPanel, m_dicecsData);
            m_bEditingImage = false;
            m_bEditingCount = false;
        }

        void ECS_On_Click(object sender, EventArgs e)
        {
            if (!m_bSetting)
            {
                SG_Common.Log(m_txtLog, $"cfg 파일이 없어 Patch_QA 환경 실행 할 수 없습니다.");
                return;
            }
            SG_Common.UpdateECS(true, m_txtLog, m_ecsClient, m_checkBoxPanel, m_dicecsData);
            m_bEditingImage = false;
            m_bEditingCount = false;
        }

        async void UpdateTaskImageTags()
        {
            try
            {
                var selectedTags = m_checkBoxPanel.Controls.OfType<TableLayoutPanel>()
                    .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                    .Where(cb => cb.Checked)
                    .Select(cb => cb.Text)
                    .ToList();

                SG_Common.Log(m_txtLog, $"✅ UpdateTaskImageTags(1) {selectedTags.Count}개 선택됨");

                foreach (var task in m_dicecsData)
                {
                    if (!selectedTags.Contains(task.Key.ToString())) continue;

                    string newImageTag = m_dicParameters[task.Key][0].Text;
                    SG_Common.Log(m_txtLog, $"✅ UpdateTaskImageTags(2) Key: {task.Key}, newImageTag : {newImageTag}");

                    // 1. Describe Task Definition
                    var taskDefResponse = await m_ecsClient.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest
                    {
                        TaskDefinition = task.Value.Task
                    });

                    TaskDefinition taskDef = taskDefResponse.TaskDefinition;

                    // 2. JSON 직렬화
                    var json = JsonConvert.SerializeObject(taskDef);
                    var jObj = JObject.Parse(json);

                    // 3. 제거해야 할 필드(자동 생성 되는것으로 추정)
                    var removeKeys = new[]
                    {
                        "TaskDefinitionArn",
                        "Revision",
                        "Status",
                        "RequiresAttributes",
                        "Compatibilities",
                        "RegisteredAt",
                        "RegisteredBy"
                    };

                    foreach (var key in removeKeys)
                        jObj.Remove(key);

                    // 4. 이미지 태그만 수정
                    var containers = jObj["ContainerDefinitions"];

                    if (containers != null)
                    {
                        //SG_Common.Log(m_txtLog, $"🛠️ UpdateTaskImageTags(3) containers 수량 {containers.Count()}");
                        foreach (var container in containers)
                        {
                            //SG_Common.Log(m_txtLog, $"🛠️ UpdateTaskImageTags(4) containers {container}");
                            var image = container["Image"]?.ToString();
                            if (!string.IsNullOrEmpty(image))
                            {
                                container["Image"] = SG_Common.UpdateImageTag(image, newImageTag);
                                SG_Common.Log(m_txtLog, $"🛠️ {container["Name"]} 이미지 변경 → {container["Image"]}");
                            }
                            else
                            {
                                SG_Common.Log(m_txtLog, $"❌ {task.Value.Task}  >  {container["Image"]} 의 컨테이너 정의에서 이미지 정보를 찾을 수 없습니다.");
                            }
                        }

                        // 5. JSON → RegisterTaskDefinitionRequest로 역직렬화
                        var registerRequest = new RegisterTaskDefinitionRequest();
                        JsonConvert.PopulateObject(jObj.ToString(), registerRequest);

                        // 6. 태스크 정의 등록
                        var registerResponse = await m_ecsClient.RegisterTaskDefinitionAsync(registerRequest);
                        SG_Common.Log(m_txtLog, $"✅ 태스크 정의 {task.Value.Task} → TaskDefinitionArn : {registerResponse.TaskDefinition.TaskDefinitionArn} 로 등록됨");
                    }
                    else
                    {
                        SG_Common.Log(m_txtLog, $"❌ {task.Value.Task} 의 컨테이너 정의를 찾을 수 없습니다.");
                        continue;
                    }
                }

                m_bEditingImage = false;
            }
            catch (Exception ex)
            {
                SG_Common.Log(m_txtLog, $"❌ UpdateTaskImageTags() 처리 중 오류 발생: {ex.Message}");
            }


        }

        // 기능 2: 태스크 정의 최신화 및 태스크 수 조정
        async void UpdateServiceWithLatestTaskDef()
        {
            var selectedTags = m_checkBoxPanel.Controls.OfType<TableLayoutPanel>()
                .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                .Where(cb => cb.Checked)
                .Select(cb => cb.Text)
                .ToList();

            SG_Common.Log(m_txtLog, $"UpdateServiceWithLatestTaskDef {selectedTags.Count}개 선택됨");

            foreach (var task in m_dicecsData)
            {
                if (!selectedTags.Contains(task.Key.ToString())) continue;

                var taskDefs = await m_ecsClient.ListTaskDefinitionsAsync(new ListTaskDefinitionsRequest
                {
                    FamilyPrefix = task.Value.Task,
                    Sort = Amazon.ECS.SortOrder.DESC
                });

                var latestTaskDefArn = taskDefs.TaskDefinitionArns.FirstOrDefault();
                if (latestTaskDefArn == null)
                {
                    MessageBox.Show("❌ 최신 태스크 정의를 찾을 수 없습니다.");
                    return;
                }

                int desiredCount = int.Parse(m_dicParameters[task.Key][1].Text);
                await m_ecsClient.UpdateServiceAsync(new UpdateServiceRequest
                {
                    Cluster = task.Value.Cluster,
                    Service = task.Value.Service,
                    TaskDefinition = latestTaskDefArn,
                    DesiredCount = desiredCount
                });

                SG_Common.Log(m_txtLog, $"✅ 서비스 {task.Value.Tag} 최신 태스크로 {latestTaskDefArn}변경 및 태스크 {desiredCount}개 설정됨");
            }
            m_bEditingCount = false;
        }
        
        // async void UpdateECS(bool bOn)
        // {
        //     var selectedTags = m_checkBoxPanel.Controls.OfType<TableLayoutPanel>()
        //         .SelectMany(panel => panel.Controls.OfType<CheckBox>())
        //         .Where(cb => cb.Checked)
        //         .Select(cb => cb.Text)
        //         .ToList();

        //     SG_Common.Log(m_txtLog, $"UpdateECS {selectedTags.Count}개 선택됨");

        //     foreach (var task in m_dicecsData)
        //     {
        //         if (!selectedTags.Contains(task.Key.ToString())) continue;

        //         var taskDefs = await m_ecsClient.ListTaskDefinitionsAsync(new ListTaskDefinitionsRequest
        //         {
        //             FamilyPrefix = task.Value.Task,
        //             Sort = Amazon.ECS.SortOrder.DESC
        //         });

        //         var latestTaskDefArn = taskDefs.TaskDefinitionArns.FirstOrDefault();
        //         if (latestTaskDefArn == null)
        //         {
        //             MessageBox.Show("❌ 최신 태스크 정의를 찾을 수 없습니다.");
        //             return;
        //         }

        //         while (task.Value.ServiceCount == -1)
        //         {
        //             SG_Common.Log(m_txtLog, $"UpdateECS ECS Count 설정 되지 않아 대기중....");
        //             await System.Threading.Tasks.Task.Delay(2000); // 2초마다 갱신.
        //         }

        //         int desiredCount = bOn ? task.Value.ServiceCount : 0;
        //         await m_ecsClient.UpdateServiceAsync(new UpdateServiceRequest
        //         {
        //             Cluster = task.Value.Cluster,
        //             Service = task.Value.Service,
        //             TaskDefinition = latestTaskDefArn,
        //             DesiredCount = desiredCount
        //         });

        //         SG_Common.Log(m_txtLog, $"✅ UpdateECS {task.Value.Tag} : 태스크로 {latestTaskDefArn}변경, 서비스 {desiredCount}개 설정됨");
        //     }
        // }
    }
}
