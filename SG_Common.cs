
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.S3.Transfer;
using Renci.SshNet;
using SG_Tool.Log;
using System.Net.NetworkInformation;
#if NET48
using Outlook = Microsoft.Office.Interop.Outlook;
#endif
using Task = System.Threading.Tasks.Task;

namespace SG_Tool
{
    public enum EnProjectType { EP7, L9, Email, OP }
    public enum EnCommandType { Command, UserCheck, Monitoring, Scripts }
    public enum EP7_CommandType { Verinfo, Game, Battle, Log, Chan }
    public enum EP7_EnRegion { Asia, Europ, Global, Japan, Korea }
    public enum EcsDataEnum { front, auth, noti, op_api, op_front, log }
    public enum L9DataType { S3FileBucket, S3UploadBucket, AwsAccessKey, AwsSecretKey, NX3AwsAccessKey, NX3AwsSecretKey, JsonUpdate, DBUpload, NX3URL }
    public enum Email_DataType { Name, MailMain }
    public enum EnLoad9_Type { L9, L9_Asia }

    public class EcsData
    {
        public string Tag  { get; set; }
        public string Task  { get; set; }
        public string Cluster  { get; set; }
        public string Service  { get; set; }
        public int ServiceCount  { get; set; }

        public EcsData(string tag, string task, string cluster, string service, int servicecount)
        {
            Tag = tag;
            Task = task;
            Cluster = cluster;
            Service = service;
            ServiceCount = servicecount;
        }
    }

    public class UserData
    {
        public bool IsConnect { get; set; }
        public bool IsCountdown { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public UserData(string user = "", string pass = "")
        {
            User = user;
            Pass = pass;
            IsConnect = false;
            IsCountdown = false;
        }

        public void SetConnect(bool isConnect)
        {
            IsConnect = isConnect;
        }

        public void SetCountdown(bool isCountdown)
        {
            IsCountdown = isCountdown;
        }
    }

    internal class SG_Common
    {
        static Dictionary<string, SshClient> m_dicServer = new Dictionary<string, SshClient>();
        public static Dictionary<string, SshClient> Servers { get { return m_dicServer; } }

        public static Button GetButton(string strName, Color backColor, int width = 100)
        {
            Button button = new Button
            {
                Text = strName,
                Width = width,
                Height = 30,
                Margin = new Padding(5, 6, 5, 0),
                BackColor = backColor,
                Anchor = AnchorStyles.Left
            };
            return button;
        }

        public static FlowLayoutPanel CreateLabeledPanel(string label, int width, string defaultText, bool bMultiline = false, int height = 20)
        {
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(5, 10, 5, 0),
                WrapContents = false,
                Anchor = AnchorStyles.Left
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 5, 0)
            };

            var txt = new TextBox
            {
                Height = height,
                Width = width,
                Multiline = bMultiline,
                ScrollBars = ScrollBars.Vertical,
                Text = defaultText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 3, 0, 0),
                Font = new Font("Segoe UI", 9f) // 폰트 이름, 크기
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(txt);
            return panel;
        }

        public static TextBox CreateLabeledTextBox(string label, int width, string defaultText)
        {
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(5, 6, 5, 0),
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(5, 6, 5, 0),
                Anchor = AnchorStyles.Left
            };

            var txt = new TextBox
            {
                Width = width,
                Text = defaultText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5, 6, 5, 0)
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(txt);
            return txt;
        }

        #region EP7 & OP
        public static UserData LoadCredentials(TextBox txtLog, EnProjectType enProjectType)
        {
            string credentialsPath = string.Empty;
            UserData userData = new UserData();
            switch (enProjectType)
            {
                case EnProjectType.OP:
                    credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OP", "OP_Serverdata.cfg");
                    break;
                case EnProjectType.EP7:
                    credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EP7", "EP7_Serverdata.cfg");
                    break;
            }

            if (File.Exists(credentialsPath))
            {
                var lines = File.ReadAllLines(credentialsPath);
                if (lines.Length >= 2)
                {
                    userData = new UserData(lines[0].Trim(), lines[1].Trim());
                }
                else
                {
                    Log(txtLog, $"Serverdata.cfg 파일에 ID와 비밀번호가 올바르게 설정되지 않았습니다.\r\n");
                }
            }
            else
            {
                Log(txtLog, $"Serverdata.cfg 파일을 찾을 수 없습니다.\r\n");
            }
            return userData;
        }

        public static async void ConnectStart(TextBox txtLog, Dictionary<string, string> dicTagIp, UserData userData)
        {
            userData.SetConnect(false);
            var tasks = dicTagIp
                        .Select(pair => Task.Run(() => ConnectServersAsync(pair.Value, txtLog, true, pair.Key, userData.User, userData.Pass)))
                        .ToList();

            int taskCount = tasks.Count();
            Log(txtLog, $"서버 접속 시작 {taskCount}", 1);
            await Task.WhenAll(tasks);
            userData.SetConnect(true);

            Log(txtLog, $"서버 접속 완료 {taskCount}", 1);
        }

        static async Task ConnectServersAsync(string serverIp, TextBox txtLog, bool bFirst, string strTag, string strUser, string strPass)
        {
            try
            {
                if (!IsPingSuccess(serverIp))
                {
                    Log(txtLog, $"❌  서버 {strTag,-15} : {serverIp,-15} 연결 안됨. VDI 폐쇠망에서 접속 필요.");
                    return;
                }

                if (!m_dicServer.ContainsKey(serverIp))
                {
                    var client = new SshClient(serverIp, strUser, strPass);
                    await Task.Run(() => client.Connect());
                    m_dicServer[serverIp] = client;
                    Log(txtLog, $"✅ 서버 {strTag,-15} {serverIp,-15} 접속.");
                }
                else
                {
                    if (!m_dicServer[serverIp].IsConnected)
                    {
                        await Task.Run(() => m_dicServer[serverIp].Connect());
                        Log(txtLog, $"✅ 서버 {strTag,-15} {serverIp,-15} 재접속.");
                    }
                    else
                    {
                        if (bFirst) // 최초 연결 중복 시도시에만 로그 표기.
                        {
                            Log(txtLog, $"✅ 서버 {strTag,-15} {serverIp,-15} 이미 연결됨.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ 오류 발생 ConnectServersAsync : {strTag,-15} : {serverIp,-15} 실행 중 오류 발생: {ex.Message}");
            }
        }

        public static async Task CommandServersAsync(string strServerIp, string strCommand, string strTag, TextBox txtLog, string strUser, string strPass, EnCommandType CommandType)
        {
            try
            {
                await ConnectServersAsync(strServerIp, txtLog, false, strTag, strUser, strPass);

                var cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
                string strResult = cmd.Result;

                switch(CommandType)
                {
                    case EnCommandType.Command:
                        var lines = strResult.Split('@');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i == 0) continue;
                            if (lines[i] == "")
                            {
                                Log(txtLog, $"🔹 {strTag,-15} : 실행 중 Docker 없습니다.");
                            }
                            else
                            {
                                var Parts = lines[i].Split('/');
                                var Dockers = Parts[Parts.Length - 1].Split(':', ',');
                                Log(txtLog, $"🔹 {strTag,-15} : {Dockers[0],-13} : {Dockers[1],-11} : {Dockers[2],12}");
                            }
                        }
                        break;
                    case EnCommandType.UserCheck:
                        var UserChecklines = strResult.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
                        int nCurrentSize = int.Parse(UserChecklines[0]);
                        Log(txtLog, $"🔹 실행 결과 : {strTag,-20} 유저접근 상태 확인중 5초 소요.... {nCurrentSize}");

                        await Task.Delay(5000); // 5초 지연.
                        var cmd2 = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
                        var UserChecklines2 = cmd2.Result.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
                        int nCurrentSize2 = int.Parse(UserChecklines2[0]);

                        if (nCurrentSize < nCurrentSize2)
                        {
                            Log(txtLog, $"🔹 실행 결과 : {strTag,-20} 게임서버 유저접근 확인됩니다. {nCurrentSize} >> {nCurrentSize2}");
                        }
                        else
                        {
                            Log(txtLog, $"❌ 실행 결과 : {strTag,-20} 게임서버 유저접근 없습니다.  {nCurrentSize} == {nCurrentSize2}");
                        }
                        break;
                    case EnCommandType.Monitoring:
                        var Monitoringlines = strResult.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        for (int i = 1; i < Monitoringlines.Length; i++)
                        {
                            var Parts = Monitoringlines[i].Split(' ');
                            if (Parts.Length == 2)
                            {
                                Log(txtLog, $"🔹 {Parts[0].Trim(),-25} : {Parts[1].Trim(),-11}");
                            }
                        }
                        break;
                    case EnCommandType.Scripts:
                        if (strResult.Contains("inacrive"))
                        {
                            Log(txtLog, $"🔹시간 변경 확인 재시도", 1);
                            cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
                        }
                        else
                        {
                            if (strCommand.Contains("pullUp_") || strCommand.Contains("removeDown"))
                            {
                                Result(txtLog, strCommand, cmd.Result, strTag);
                            }
                            else if (strCommand.Contains("oneCommand_") || strCommand.Contains("allTogether_Up") || strCommand.Contains("allTogether_Restart"))
                            {
                                if (strResult.Contains("inacrive"))
                                {
                                    Log(txtLog, $"❌ 시간 변경 확인 재시도", 1);
                                    cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
                                }
                                else
                                {
                                    Result(txtLog, strCommand, strResult, strTag);
                                }
                            }
                            else
                            {
                                Log(txtLog, $"🔹 실행 결과 : {strTag,-15} {strCommand}\r\n{strResult}");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ {strTag,-15} {strServerIp,-15} 실행 중 오류 발생: {ex.Message}");
            }
        }

        public static async Task AwaitWithPeriodicLog(TextBox txtLog, Task task, string tag, string operation, int intervalMilliseconds = 20000)
        {
            using (var cts = new CancellationTokenSource())
            {
                int timeoutMilliseconds = 120000; // 타임아웃 2분
                var logTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(intervalMilliseconds, cts.Token);
                        Log(txtLog, $"[대기중] {tag} : {operation} 실행중...");
                    }
                }, cts.Token);

                var timeoutTask = Task.Delay(timeoutMilliseconds); // 타임아웃 Task
                var completedTask = await Task.WhenAny(task, timeoutTask);

                if (completedTask == timeoutTask) // 타임아웃 발생
                {
                    Log(txtLog, $"{tag} : {operation}  타임아웃 (대기 시간 {timeoutMilliseconds}ms 초과)", 1);
                }
                else
                {
                    await task; // 작업 완료된 경우, 혹은 예외 발생 시 await로 처리
                }

                cts.Cancel(); // 완료 시 타이머 취소
                await Task.Delay(1000);
            }
        }

        public static async void CountDownStart(TextBox txtLog, UserData userData, int nMinutes)
        {
            userData.SetCountdown(true);
            ChangeCountdown(true, txtLog);
            int countdown = nMinutes; // 10 minutes in seconds   // 600 > 10분
            var timer = new System.Timers.Timer(10000); // 10 seconds interval
            timer.Elapsed += (s, args) =>
            {
                countdown -= 10;
                Log(txtLog, $"Countdown: {countdown / 60} minutes and {countdown % 60} seconds remaining.", 0, true);
            };
            timer.Start();

            await Task.Delay(600000);
            timer.Stop();

            await Task.Delay(1000);
            userData.SetCountdown(false);
            ChangeCountdown(false, txtLog);
        }

        public static bool ClickCheck(TextBox txtLog, string strName, bool bConnect, bool bCountdown = false)
        {
            if (!ProsessCheck($"{strName} 기능을 실행하시겠습니까?"))
            {
                if (bConnect && !bCountdown)
                {
                    Log(txtLog, $"{strName} Start", 1);
                    return true;
                }
                else
                {
                    ProsessCheck("서버 접속중 잠시 후 다시 시도하세요.");
                }
            }
            return false;
        }

        public static bool ProsessCheck(string strName)
        {
            if (MessageBox.Show(strName, "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return true;
            }

            return false;
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static string GetCommonTag(string[] strGroup, string tag)
        {
            for (int i = 0; i < strGroup.Length; i++)
            {
                if (tag.Contains(strGroup[i]))
                {
                    return strGroup[i];
                }
            }
            return tag;
        }
        
        static void Result(TextBox txtLog, string strCommand, string strResult, string strTag)
        {
            if (strResult.Contains("Error response from daemon"))
            {
                Log(txtLog, $"❌ {strTag,-20} : Error response from daemon : Docker 오류 발생.");
            }
            else if (strResult.Contains("already exists"))
            {
                Log(txtLog, $"❌ {strTag,-20} : already exists : Docker 이미 존재합니다.");
            }
            else if (strResult.Contains("Error: No such container"))
            {
                Log(txtLog, $"❌ {strTag,-20} : No such container : Docker 이미 존재합니다.");
            }
            else if (strResult.Contains("Error: No such image"))
            {
                Log(txtLog, $"❌ {strTag,-20} : No such image : Docker 이미지가 없습니다.");
            }
            else if (strResult.Contains("Error: No such file or directory"))
            {
                Log(txtLog, $"❌ {strTag,-20} : No such file or directory : Docker 파일이 없습니다.");
            }
            else if (strResult.Contains("Error: No such service"))
            {
                Log(txtLog, $"❌ {strTag,-20} : No such service : Docker 서비스가 없습니다.");
            }
            else
            {
                var Parts = strCommand.Split('/');
                Log(txtLog, $"🔹 DockerUp 실행 완료. : {strTag,-20} : {Parts[Parts.Length - 1]}");
            }
        }

        static void ChangeCountdown(bool bCountdown, TextBox txtLog)
        {
            if (bCountdown)
                Log(txtLog, $"Countdown Start 10 minutes", 1);
            else
                Log(txtLog, $"Countdown Stop", 1);
        }

        static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        static bool IsPingSuccess(string ip, int timeoutMs = 1000)
        {
            try
            {
                using var ping = new Ping();
                PingReply reply = ping.Send(ip, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
        #endregion EP7 & OP

        #region L9_Tool
        
        public static bool IsActivated(TextBox txtLog, ComboBox comboBox, bool bSetting, bool bLoad)
        {
            if (!bSetting)
            {
                Log(txtLog, $"cfg 파일이 없어 Patch_QA 환경 실행 할 수 없습니다.");
                return false;
            }

            string selectedValue = comboBox.SelectedItem?.ToString(); // 콤보박스에서 선택한 값
            if (!bLoad && string.IsNullOrEmpty(selectedValue))
            {
                Log(txtLog, $"서버 환경을 선택해주세요....");
                MessageBox.Show("환경을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        public static bool SetPatchL9Data(string configFile, Dictionary<L9DataType, string> dicData)
        {

            if (File.Exists(configFile))
            {
                var lines = File.ReadAllLines(configFile);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) // 공백 줄 또는 주석 처리된 줄 무시
                        continue;

                    var parts = line.Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    if (Enum.TryParse(parts[0], out L9DataType key)) // Enum.TryParse를 이용한 안전한 파싱
                    {
                        if (dicData.ContainsKey(key))
                        {
                            dicData[key] = parts[1].Trim();
                        }
                        else
                        {
                            dicData.Add(key, parts[1].Trim());
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public static string UpdateImageTag(string fullImage, string newTag)
        {
            var baseImage = fullImage.Contains(":") ? fullImage.Substring(0, fullImage.LastIndexOf(":")) : fullImage;
            return $"{baseImage}:{newTag}";
        }

        public async static void UpdateECS(bool bOn, TextBox txtLog, AmazonECSClient ecsClient, TableLayoutPanel checkBoxPanel, Dictionary<EcsDataEnum, EcsData> dicecsData)
        {
            var selectedTags = checkBoxPanel.Controls.OfType<TableLayoutPanel>()
                .SelectMany(panel => panel.Controls.OfType<CheckBox>())
                .Where(cb => cb.Checked)
                .Select(cb => cb.Text)
                .ToList();

            Log(txtLog, $"UpdateECS {selectedTags.Count}개 선택됨");

            foreach (var task in dicecsData)
            {
                if (!selectedTags.Contains(task.Key.ToString())) continue;

                var taskDefs = await ecsClient.ListTaskDefinitionsAsync(new ListTaskDefinitionsRequest
                {
                    FamilyPrefix = task.Value.Task,
                    Sort = Amazon.ECS.SortOrder.DESC
                });

                var latestTaskDefArn = taskDefs.TaskDefinitionArns.FirstOrDefault();
                if (latestTaskDefArn == null)
                {
                    MessageBox.Show("❌ 최신 태스크 정의를 찾을 수 없습니다.");
                }
                else
                {
                    int desiredCount = bOn ? task.Value.ServiceCount : 0;
                    await ecsClient.UpdateServiceAsync(new UpdateServiceRequest
                    {
                        Cluster = task.Value.Cluster,
                        Service = task.Value.Service,
                        TaskDefinition = latestTaskDefArn,
                        DesiredCount = desiredCount
                    });

                    Log(txtLog, $"✅ UpdateECS {task.Value.Tag} : 태스크로 {latestTaskDefArn}변경, 서비스 {desiredCount}개 설정됨");
                }
            }           
        }

        public static bool SetPatchData(string strConfigFile, Dictionary<L9DataType, string> dicData)
        {
            if (File.Exists(strConfigFile))
            {
                var lines = File.ReadAllLines(strConfigFile);

                foreach (var line in lines)
                {
                    // 공백 줄 또는 주석 처리된 줄 무시
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    // Enum.TryParse를 이용한 안전한 파싱
                    if (Enum.TryParse(parts[0], out L9DataType key))
                    {
                        if (dicData.ContainsKey(key))
                        {
                            dicData[key] = parts[1].Trim();
                        }
                        else
                        {
                            dicData.Add(key, parts[1].Trim());
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public static async Task DownloadAsyncToS3(TextBox txtLog, TransferUtility transferUtility, string localFilePath, string s3Bucket, string key)
        {
            Log(txtLog, $"[DownloadAsyncToS3] {localFilePath}, {s3Bucket}, {key} 다운로드 시작!");
            try
            {
                // 1. FTP로 key 경로 파일을 다운로드 받는다. // project-lord
                await transferUtility.DownloadAsync(localFilePath, s3Bucket, key);
                Log(txtLog, $"[DownloadAsyncToS3] {key} 다운로드 완료!");
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ [DownloadAsyncToS3 ERROR] {ex.Message}");
            }
        }

        public static async Task UploadAsyncToS3(TextBox txtLog, TransferUtility transferUtility, string localFilePath, string s3Bucket, string key)
        {
            Log(txtLog, $"[UploadAsyncToS3] {localFilePath}, {s3Bucket}, {key} 업로드 시작!");
            try
            {
                await transferUtility.UploadAsync(localFilePath, s3Bucket, key);
                Log(txtLog, $"[UploadAsyncToS3] {key} 업로드 완료!");
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ [UploadAsyncToS3 ERROR] {ex.Message}");
            }
        }

        // dicData 저장 된 값은 cfg 에 다시 저장. >  데이터 업데이트 처리.
        public static void SaveData(TextBox txtLog, string strConfigFile, Dictionary<L9DataType, string> dicData)
        {
            if (File.Exists(strConfigFile))
            {
                var lines = File.ReadAllLines(strConfigFile);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].TrimStart().StartsWith("#"))
                        continue;

                    var parts = lines[i].Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    if (Enum.TryParse(parts[0], out L9DataType key))
                    {
                        if (key == L9DataType.S3FileBucket || key == L9DataType.S3UploadBucket)
                        {
                            Log(txtLog, $"[SaveData][INFO] {parts[0]} 업데이트 : {parts[1]} → {dicData[key]}");
                            lines[i] = $"{parts[0]} {dicData[key]}";
                        }
                    }
                }
                File.WriteAllLines(strConfigFile, lines);
            }
        }
        #endregion L9_Tool

        #region Email_Tool
        public static void SetPatchData(string strPath, Dictionary<Email_DataType, string> dicData)
        {
            if (File.Exists(strPath))
            {
                var lines = File.ReadAllLines(strPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // 공백 줄 또는 주석 처리된 줄 무시
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;

                    var parts = line.Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2) continue;

                    //Enum.TryParse를 이용한 안전한 파싱
                    if (Enum.TryParse(parts[0], out Email_DataType key))
                    {
                        string value = parts[1].Trim(); // 일반적인 경우
                        if (!dicData.ContainsKey(key))
                        {
                            // MailMain은 다음 줄 전체를 값으로 사용
                            if (key == Email_DataType.MailMain)
                            {
                                for (int j = i + 1; j < lines.Length; j++)
                                {
                                    value += $"\r\n{lines[j].Trim()}"; // 다음 줄을 값으로 사용하고 인덱스 증가
                                }

                                dicData.Add(key, value);
                                break;
                            }
                            else
                            {
                                if (parts.Length < 2) continue;
                                dicData.Add(key, value);
                            }
                        }
                    }
                }
            }
        }

        public static void SavePatchData(TextBox txtLog, string strPath, Dictionary<Email_DataType, string> dicData)
        {
            if (!File.Exists(strPath))
            {
                Log(txtLog, $"[ERROR] Login ini 파일 경로가 존재하지 않습니다: {strPath}");
                return;
            }

            var lines = File.ReadAllLines(strPath);
            Log(txtLog, $"[SavePatchData] 업데이트 시작 {lines.Length}");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#")) continue;

                if (line.StartsWith(Email_DataType.Name.ToString()))
                {
                    lines[i] = $"Name {dicData[Email_DataType.Name]}";
                    File.WriteAllLines(strPath, lines);
                }
                else if (line.StartsWith(Email_DataType.MailMain.ToString()))
                {
                    var newLines = lines.Take(i).ToList();
                    newLines.Add($"MailMain {dicData[Email_DataType.MailMain]}");
                    File.WriteAllLines(strPath, newLines);
                    Log(txtLog, $"[SavePatchData] MailMain 업데이트 완료.");
                    break;
                }
            }
        }


        public static void ReplyToLatestMail(TextBox txtLog, string subjectKeyword, string bodyToInsert)
        {
            Log(txtLog, $"[ReplyToLatestMail] Outlook 연결 시도...");
            #if NET48
            try
            {
                Outlook.Application outlookApp = new Outlook.Application();
                Outlook.NameSpace ns = outlookApp.GetNamespace("MAPI");
                ns.Logon("", "", false, false);

                List<Outlook.MAPIFolder> allFolders = GetAllFolders(ns);

                Outlook.MailItem latestMail = null;
                DateTime latestTime = DateTime.MinValue;
                Log(txtLog, $"[ReplyToLatestMail] Outlook 조회 시작.. {allFolders.Count}");

                foreach (var folder in allFolders)
                {
                    if (folder.DefaultItemType != Outlook.OlItemType.olMailItem)
                        continue;
                    Outlook.Items items = folder.Items;
                    items.Sort("[ReceivedTime]", true); // 최신순 정렬 (내림차순)

                    foreach (object obj in items)
                    {
                        if (obj is Outlook.MailItem mailItem)
                        {
                            // 조건이 있다면 추가 가능: 제목 포함 여부 등
                            if (!string.IsNullOrEmpty(mailItem.Subject) && mailItem.Subject.Contains(subjectKeyword) && mailItem.ReceivedTime > latestTime && mailItem.Sender != null)
                            {
                                latestMail = mailItem;
                                latestTime = mailItem.ReceivedTime;
                            }
                        }
                    }
                }

                if (latestMail != null)
                {
                    Log(txtLog, $"[ReplyToLatestMail] ▶ 최신 대상 메일 Outlook Open : {latestMail.Subject} ({latestMail.ReceivedTime})");
                    try
                    {
                        if (latestMail.Sender != null && latestMail.Recipients.Count > 0)
                        {
                            Outlook.MailItem reply = latestMail.ReplyAll();
                            //reply.Body = bodyToInsert + "\n\n" + reply.Body;
                            reply.Display(); // 또는 reply.Send();
                        }
                        else
                        {
                            Log(txtLog, "[ReplyToLatestMail] ▶ 회신 대상 메일 없음");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(txtLog, $"[ReplyToLatestMail] 오류2 : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(txtLog, $"[ReplyToLatestMail] 오류1 : {ex.Message}");
            }

            Log(txtLog, $"[ReplyToLatestMail] Outlook 관련 작업 완료.");
            #endif
        }

#if NET48
        static List<Outlook.MAPIFolder> GetAllFolders(Outlook.NameSpace ns)
        {
            List<Outlook.MAPIFolder> result = new List<Outlook.MAPIFolder>();
            foreach (Outlook.MAPIFolder root in ns.Folders)
            {
                result.Add(root);
                result.AddRange(GetSubFoldersRecursive(root));
            }
            return result;
        }

        static List<Outlook.MAPIFolder> GetSubFoldersRecursive(Outlook.MAPIFolder folder)
        {
            List<Outlook.MAPIFolder> folders = new List<Outlook.MAPIFolder>();
            foreach (Outlook.MAPIFolder sub in folder.Folders)
            {
                folders.Add(sub);
                folders.AddRange(GetSubFoldersRecursive(sub));
            }
            return folders;
        }
#endif
        
        #endregion Email_Tool

        public static void Log(TextBox txtLog, string message, int nType = 0, bool overwrite = false, bool bBox = false)
        {
            SystemLog_Form.LogMessage(txtLog, message, nType, overwrite);
            if (bBox)
            {
                MessageBox.Show($"{message}", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}