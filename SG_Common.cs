
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.S3.Transfer;
using OfficeOpenXml.Utils;
using Renci.SshNet;
using SG_Tool.Log;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Outlook = Microsoft.Office.Interop.Outlook;
using Task = System.Threading.Tasks.Task;

namespace SG_Tool
{
    public enum EP7_CommandType { Verinfo, Game, Battle, Log, Chan }
    public enum EP7_EnRegion { Asia, Europ, Global, Japan, Korea }
    public enum EcsDataEnum { front, auth, noti, op_api, op_front, log }
    public enum L9DataType { AwsS3, S3FileBucket, S3Bucket, AwsAccessKey, AwsSecretKey, JsonPath }
    public enum L9FTP_DataType { S3FileBucket, S3UploadBucket, AwsAccessKey, AwsSecretKey, JsonUpdate, DBUpload, NX3URL }
    public enum Email_DataType { Name, MailMain }
    public enum CF_DataType { URL, ID, PW, Akamai, Akamai_ID, Akamai_Key, LocalPath, TargetPath, Version, PatchLocalPath, PatchPath, Login_ServerInfo, WinPath, Patcher, Purge, RemotePath, SVNPath, VersionPath }

    public enum EnCommandType { Command, UserCheck, Monitoring, Scripts }

    public class EcsData
    {
        string m_strTag = string.Empty;
        string m_strTask = string.Empty;
        string m_strCluster = string.Empty;
        string m_strService = string.Empty;
        int m_nServicecount = 1;

        public string Tag => m_strTag;
        public string Task => m_strTask;
        public string Cluster => m_strCluster;
        public string Service => m_strService;
        public int ServiceCount => m_nServicecount;

        public EcsData(string tag, string task, string cluster, string service, int servicecount = -1)
        {
            m_strTag = tag;
            m_strTask = task;
            m_strCluster = cluster;
            m_strService = service;
            m_nServicecount = servicecount;
        }
    }

    internal class SG_Common
    {
        static Dictionary<string, SshClient> m_dicServer = new Dictionary<string, SshClient>();

        public static Dictionary<string, SshClient> Servers { get { return m_dicServer; } }

        public static Button GetButton(string strName)
        {
            Button button = new Button
            {
                Text = strName,
                Width = 100,
                Height = 30,
                Margin = new Padding(5, 6, 5, 0),
                BackColor = Color.AliceBlue,
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

        public static void LoadCredentials(TextBox txtLog, string strUser, string strPass)
        {
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EP7", "EP7_Serverdata.cfg");
            if (File.Exists(credentialsPath))
            {
                var lines = File.ReadAllLines(credentialsPath);
                if (lines.Length >= 2)
                {
                    strUser = lines[0].Trim();
                    strPass = lines[1].Trim();
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
        }

        public static async void ConnectStart(TextBox txtLog, Dictionary<string, string> dicTagIp, string strUser, string strPass, bool bConnect)
        {
            bConnect = false;
            var tasks = dicTagIp
                        .Select(pair => Task.Run(() => ConnectServersAsync(pair.Value, txtLog, true, pair.Key, strUser, strPass)))
                        .ToList();

            int taskCount = tasks.Count();
            Log(txtLog, $"서버 접속 시작 {taskCount}", 1);
            await Task.WhenAll(tasks);
            bConnect = true;

            Log(txtLog, $"서버 접속 완료 {taskCount}", 1);
        }

        public static async Task ConnectServersAsync(string serverIp, TextBox txtLog, bool bFirst, string strTag, string strUser, string strPass)
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

        //public static async Task CommandAsync(string strServerIp, string strCommand, string strTag, TextBox txtLog, bool bCommand, string strUser, string strPass)
        //{
        //    try
        //    {
        //        //Log(txtLog, $"🔹 CommandAsync : {strTag,-15} : {strCommand}");
        //        //return;
        //        await ConnectServersAsync(strServerIp, txtLog, false, strTag, strUser, strPass);

        //        var cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
        //        string strResult = cmd.Result;

        //        if (bCommand)
        //        {
        //            var lines = strResult.Split('@');
        //            for (int i = 0; i < lines.Length; i++)
        //            {
        //                if (i == 0) continue;
        //                if (lines[i] == "")
        //                {
        //                    Log(txtLog, $"🔹 {strTag,-15} : 실행 중 Docker 없습니다.");
        //                }
        //                else
        //                {
        //                    var Parts = lines[i].Split('/');
        //                    var Dockers = Parts[Parts.Length - 1].Split(':', ',');
        //                    Log(txtLog, $"🔹 {strTag,-15} : {Dockers[0],-13} : {Dockers[1],-11} : {Dockers[2],12}");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (strCommand.Contains("allTogether_Tool_Monitoring.sh"))
        //            {
        //                var lines = strResult.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        //                for (int i = 1; i < lines.Length; i++)
        //                {
        //                    var Parts = lines[i].Split(' ');
        //                    if (Parts.Length == 2)
        //                    {
        //                        Log(txtLog, $"🔹 {Parts[0].Trim(),-25} : {Parts[1].Trim(),-11}");
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if (strResult.Contains("inacrive"))
        //                {
        //                    Log(txtLog, $"🔹시간 변경 확인 재시도", 1);
        //                    cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
        //                }
        //                else
        //                {
        //                                   // 도커 생성 명령어
        //                    if (strCommand.Contains("oneCommand_") || strCommand.Contains("allTogether_Up") || strCommand.Contains("allTogether_Restart"))
        //                    {
        //                        if (strResult.Contains("inacrive"))
        //                        {
        //                            Log(txtLog, $"❌ 시간 변경 확인 재시도", 1);
        //                            cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
        //                        }
        //                        else
        //                        {
        //                            Result(txtLog, strCommand, strResult, strTag);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Log(txtLog, $"🔹 실행 결과 : {strTag,-15} {strCommand}\r\n{strResult}");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(txtLog, $"❌ {strTag,-15} {strServerIp,-15} 실행 중 오류 발생: {ex.Message}");
        //    }
        //}

        
        //public static async Task CommandServersAsync(string strServerIp, string strCommand, string strTag, TextBox txtLog, bool isUserCheck = false)
        //{
        //    try
        //    {
        //        // Log(txtLog, $"🔹Docker {strTag, -15} : {strCommand}");
        //        // return;
        //        var cmd = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));

        //        if (isUserCheck)
        //        {
        //            string strResult = cmd.Result;
        //            var lines = strResult.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
        //            int nCurrentSize = int.Parse(lines[0]);
        //            Log(txtLog, $"🔹 실행 결과 : {strTag,-20} 유저접근 상태 확인중 5초 소요.... {nCurrentSize}");

        //            await Task.Delay(5000); // 5초 지연.
        //            var cmd2 = await Task.Run(() => m_dicServer[strServerIp].RunCommand(strCommand));
        //            var lines2 = cmd2.Result.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
        //            int nCurrentSize2 = int.Parse(lines2[0]);

        //            if (nCurrentSize < nCurrentSize2)
        //            {
        //                Log(txtLog, $"🔹 실행 결과 : {strTag,-20} 게임서버 유저접근 확인됩니다. {nCurrentSize} >> {nCurrentSize2}");
        //            }
        //            else
        //            {
        //                Log(txtLog, $"❌ 실행 결과 : {strTag,-20} 게임서버 유저접근 없습니다.  {nCurrentSize} == {nCurrentSize2}");
        //            }
        //        }
        //        else if (strCommand.Contains("docker"))
        //        {
        //            var lines = cmd.Result.Split('@');
        //            for (int i = 0; i < lines.Length; i++)
        //            {
        //                if (lines.Length == 1)
        //                {
        //                    Log(txtLog, $"🔹Docker {strTag,-20} : 실행 중 Docker 없습니다.");
        //                }
        //                else
        //                {
        //                    if (i == 0) continue;

        //                    var Parts = lines[i].Split('/');
        //                    var Dockers = Parts[Parts.Length - 1].Split(':', ',');
        //                    Log(txtLog, $"🔹Docker {strTag,-20} : {Dockers[0],-13} : {Dockers[1],-11} : {Dockers[2],12}");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (strCommand.Contains("pullUp_") || strCommand.Contains("removeDown"))
        //            {
        //                Result(txtLog, strCommand, cmd.Result, strTag);
        //            }
        //            else
        //            {
        //                Log(txtLog, $"🔹 실행 결과 : {strTag,-20} {strCommand}\r\n{cmd.Result}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(txtLog, $"❌ {strTag,-20} {strServerIp} 실행 중 오류 발생: {ex.Message}");
        //    }
        //}

        
        public static async Task CommandServersAsync(string strServerIp, string strCommand, string strTag, TextBox txtLog, string strUser, string strPass, EnCommandType CommandType)
        {
            try
            {
                //Log(txtLog, $"🔹 CommandAsync : {strTag,-15} : {strCommand}");
                //return;
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

        public static async void CountDownStart(TextBox txtLog, bool bCountdown, int nMinutes)
        {
            bCountdown = true;
            ChangeCountdown(bCountdown, txtLog);
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
            bCountdown = false;
            ChangeCountdown(bCountdown, txtLog);
        }

        static void ChangeCountdown(bool bCountdown, TextBox txtLog)
        {
            if (bCountdown)
            {
                Log(txtLog, $"Countdown Start 10 minutes", 1);
            }
            else
            {
                Log(txtLog, $"Countdown Stop", 1);
            }
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
        #region L9_Tool
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

        public static bool SetPatchData(string strConfigFile, Dictionary<L9FTP_DataType, string> dicData)
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
                    if (Enum.TryParse(parts[0], out L9FTP_DataType key))
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
        public static void SaveData(TextBox txtLog, string strConfigFile, Dictionary<L9FTP_DataType, string> dicData)
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

                    if (Enum.TryParse(parts[0], out L9FTP_DataType key))
                    {
                        if (key == L9FTP_DataType.S3FileBucket || key == L9FTP_DataType.S3UploadBucket)
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
        }

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
        #endregion Email_Tool

        #region CF_Tool
        public static bool SetPatchData(string strPath, Dictionary<CF_DataType, string> dicData)
        {
            if (File.Exists(strPath))
            {
                var lines = File.ReadAllLines(strPath);

                foreach (var line in lines)
                {
                    // 공백 줄 또는 주석 처리된 줄 무시
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    // Enum.TryParse를 이용한 안전한 파싱
                    if (Enum.TryParse(parts[0], out CF_DataType key))
                    {
                        dicData[key] = parts[1].Trim();
                    }
                }
                return true;
            }

            return false;
        }


        public static async void FileMove(TextBox txtLog, Dictionary<CF_DataType, string> dicData, string strParam, int nType, string strDate, string strCfg) //QA : 0, Live : 1
        {
            using var progressForm = new Progress_Form("CDN패치 파일 설정");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "패치 다운로드 시작..");
                var parts = strParam.Split('_');  //"CF_PH_Patch_2505_QA01"
                Log(txtLog, $"FileMove(01)    parameter : {strParam} : {parts.Length}");

                string strTarget = parts[3];    //2505
                string strQA = parts[4];        //QA01

                // 1. FTP 접속정보 및 다운로드 파일 경로 설정.
                string ftpUrl = $"{dicData[CF_DataType.URL]}/{parts[0]}_{parts[1]}_{parts[2]}_{strTarget}/{strQA}";
                string ftpUsername = $"{dicData[CF_DataType.ID]}";
                string ftpPassword = $"{dicData[CF_DataType.PW]}";

                if (nType == 0) // QA
                {
                    // 2. 로컬 다운로드 받을 경로 생성.
                    string localDownloadPath = $@"{dicData[CF_DataType.LocalPath]}\CF_PH_Patch_{strTarget}\{strQA}";
                    SetDirectory(localDownloadPath);   // 로컬 경로 대상 디렉토리 생성

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(30, "FTP 파일 다운로드 중..");

                    // 3. FTP에서 로컬 경로에 파일 다운로드 및 압축 해제
                    await DownloadFromFtp(txtLog, ftpUrl, ftpUsername, ftpPassword, localDownloadPath, 1, strParam, strCfg, progressForm);
                    Log(txtLog, $"FileMove(02) localDownloadPath : {localDownloadPath}");

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(60, "패치파일 파일 복사 중..");

                    // 4. 다운로드 받은 파일 복사할 경로 생성.
                    string targetPath = $@"{dicData[CF_DataType.TargetPath]}\v{strTarget}_{strQA}";
                    SetDirectory(targetPath);   // 파일복사 대상 디렉토리 생성

                    // 5. targetPath 경로에 CF_PH_CLIENT_Patch 파일 복사.
                    string[] clientDirs = Directory.GetDirectories(localDownloadPath, "*CF_PH_CLIENT_Patch*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in clientDirs)
                    {
                        //string sourceClientPath = clientDirs[0]; // 첫 번째 매칭 폴더 사용
                        Log(txtLog, $"FileMove(03) [COPY] {dir} → {targetPath}");
                        CopyAllFiles(txtLog, dir, targetPath); // 재귀 복사 수행
                    }
                }
                else // Live
                {
                    // 4. 복사할 파일 리스트 구성.
                    string baseLocalPath = $@"{dicData[CF_DataType.LocalPath]}\CF_PH_Patch_{strTarget}";
                    int currentQAVersion = int.Parse(strQA.Replace("QA", "")); // 현재 QA 버전 숫자 추출 (예: QA09 -> 9)
                    int latestAppliedQA = GetLatestAppliedQAVersion(dicData[CF_DataType.TargetPath], strTarget); // 예: 3

                    // 5. QA 버전 순서대로 정렬
                    var qaFolders = Directory.GetDirectories(baseLocalPath, "QA*", SearchOption.TopDirectoryOnly)
                        .Select(path => new
                        {
                            FullPath = path,
                            QAName = Path.GetFileName(path),
                            QANumber = ExtractQANumber(Path.GetFileName(path))
                        })
                        .Where(x => x.QANumber > latestAppliedQA && x.QANumber <= currentQAVersion)
                        .OrderBy(x => x.QANumber)
                        .ToList();

                    if (!qaFolders.Any())
                    {
                        Log(txtLog, $"[Live] 복사할 대상 QA 폴더가 없습니다.", 0, false, true);
                        return;
                    }

                    // 6. 라이브 diff_live 경로 버전 폴더 생성.
                    string targetPath = $@"{dicData[CF_DataType.TargetPath]}\client_{strDate}_{strTarget}_{strQA}";
                    SetDirectory(targetPath);
                    Log(txtLog, $"FileMove Live(02)    targetPath : {targetPath}");

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(60, "패치파일 파일 복사 중..");

                    // 7. targetPath 경로에 CF_PH_CLIENT_Patch 파일 복사.
                    foreach (var qaFolder in qaFolders)
                    {
                        var patchDirs = Directory.GetDirectories(qaFolder.FullPath, "CF_PH_CLIENT_Patch_*", SearchOption.TopDirectoryOnly)
                            .OrderBy(x => ExtractVersionNumber(Path.GetFileName(x)))
                            .ToList();

                        foreach (var patchDir in patchDirs)
                        {
                            Log(txtLog, $"FileMove Live(03) [COPY] \r\n {patchDir} → \r\n {targetPath}");
                            CopyAllFiles(txtLog, patchDir, targetPath);
                        }
                    }

                    await Task.Delay(5000);
                    progressForm.UpdateProgress(80, "Version 수정 중..");
                    // 8. cfg 파일 Version 수정
                    VersionChange(txtLog, strParam, strCfg);
                }

                await Task.Delay(5000);
                progressForm.UpdateProgress(100, "CDN 빌드 준비 완료");
                await Task.Delay(10000);

                Log(txtLog, $"Patch process completed 복사가 완료되었습니다!", 0, false, false);

                // 3. PatchExpMgr.exe 실행
                if (File.Exists(dicData[CF_DataType.Patcher]))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = dicData[CF_DataType.Patcher],
                        UseShellExecute = true,
                        Verb = "runas", // 관리자 권한 요청
                        WorkingDirectory = Path.GetDirectoryName(dicData[CF_DataType.Patcher])
                    };
                    Process.Start(psi);
                }
                else
                {
                    throw new FileNotFoundException($"Patcher Manager not found: {dicData[CF_DataType.Patcher]}");
                }
            }
            catch (Exception ex)
            {
                Log(txtLog, $"An error occurred: {ex.Message}", 0, false, true);
                throw new Exception($"An error occurred: {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }

        static int ExtractQANumber(string qaName)
        {
            var match = Regex.Match(qaName, @"QA(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        static int ExtractVersionNumber(string folderName)
        {
            var match = Regex.Match(folderName, @"_(\d{4,})$");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        static int GetLatestAppliedQAVersion(string baseTargetPath, string strTarget)
        {
            if (!Directory.Exists(baseTargetPath))
                return 0;

            var dirs = Directory.GetDirectories(baseTargetPath, $"client_*_{strTarget}_QA*", SearchOption.TopDirectoryOnly);

            var maxQA = dirs
                .Select(d => Regex.Match(d, @"QA(\d+)$"))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max();

            return maxQA;
        }

        public static void SetDirectory(string strPath)
        {
            if (!Directory.Exists(strPath))
                Directory.CreateDirectory(strPath);
        }

        public static void CopyAllFiles(TextBox txtLog, string sourceDir, string targetDir)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    Log(txtLog, $"⚠️ [CopyAllFiles] 소스 경로가 존재하지 않습니다: {sourceDir}");
                    return; // 경로 없으면 종료
                }

                Log(txtLog, $"[CopyAllFiles] {sourceDir} → {targetDir}");

                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = GetRelativePath(sourceDir, dirPath);
                    SetDirectory(Path.Combine(targetDir, relativePath));
                }

                foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = GetRelativePath(sourceDir, filePath);
                    string destFilePath = Path.Combine(targetDir, relativePath);
                    File.Copy(filePath, destFilePath, true);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log(txtLog, $"❌ [CopyAllFiles] 권한 오류: 관리자 권한이 필요합니다. → {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 권한 오류: 관리자 권한이 필요합니다. → {ex.Message}");
            }
            catch (IOException ex)
            {
                Log(txtLog, $"❌ [CopyAllFiles] 파일 입출력 오류: {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 파일 입출력 오류: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ [CopyAllFiles] 알 수 없는 오류 발생: {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 알 수 없는 오류 발생: {ex.Message}");
            }
        }


        public static void CopyFile(TextBox txtLog, string sourceDir, string targetDir)
        {
            try
            {
                Log(txtLog, $"[CopyAllFile] {sourceDir} → {targetDir}");
                if (!File.Exists(sourceDir))
                {
                    Log(txtLog, $"❌ 원본 파일이 존재하지 않음: {sourceDir}");
                    return;
                }

                File.Copy(sourceDir, targetDir, true);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log(txtLog, $"❌ [CopyAllFile] 권한 오류: 관리자 권한이 필요합니다. → {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 권한 오류: 관리자 권한이 필요합니다. → {ex.Message}");
            }
            catch (IOException ex)
            {
                Log(txtLog, $"❌ [CopyAllFile] 파일 입출력 오류: {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 파일 입출력 오류: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ [CopyAllFile] 알 수 없는 오류 발생: {ex.Message}");
                throw new Exception($"❌ [CopyAllFiles] 알 수 없는 오류 발생: {ex.Message}");
            }
        }

        public static void UploadDirectory(TextBox txtLog, SftpClient client, string localPath, string remotePath)
        {
            foreach (var file in Directory.GetFiles(localPath))
            {
                using (var stream = File.OpenRead(file))
                {
                    string remoteFilePath = Path.Combine(remotePath, Path.GetFileName(file)).Replace("\\", "/");
                    client.UploadFile(stream, remoteFilePath);
                    Log(txtLog, $"[UPLOAD] {remoteFilePath}");
                }
            }

            foreach (var dir in Directory.GetDirectories(localPath))
            {
                string remoteSubDir = Path.Combine(remotePath, Path.GetFileName(dir)).Replace("\\", "/");
                if (!client.Exists(remoteSubDir))
                {
                    client.CreateDirectory(remoteSubDir);
                    Log(txtLog, $"[MKDIR] {remoteSubDir}");
                }

                UploadDirectory(txtLog, client, dir, remoteSubDir);
            }
        }

        public static async Task UploadFTP(TextBox txtLog, Dictionary<CF_DataType, string> dicData)
        {
            Log(txtLog, $"============ [UploadFTP] Start ============");

            using var progressForm = new Progress_Form("FTP 업로드");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            try
            {
                progressForm.UpdateProgress(10, "SVN 프로세스 시작..");

                string versionIniPath = Path.Combine(dicData[CF_DataType.VersionPath], "version.ini");

                if (!File.Exists(versionIniPath))
                    throw new FileNotFoundException($"version.ini not found at {versionIniPath}");

                // 1. 가장 최신 폴더명 추출
                var directories = Directory.GetDirectories(dicData[CF_DataType.VersionPath]);
                string? latestBuildFolder = directories
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name) && name.All(char.IsDigit))
                    .OrderByDescending(name => name)
                    .FirstOrDefault();

                if (latestBuildFolder == null)
                    throw new Exception("빌드 폴더를 찾을 수 없습니다.");

                int buildVersion = int.Parse(latestBuildFolder);
                Log(txtLog, $"[INFO] 폴더에서 가져온 빌드 버전: {buildVersion}");

                // 2. version.ini의 LatestVersion 읽기
                string[] lines = File.ReadAllLines(versionIniPath);
                int currentVersion = 0;
                int versionLineIndex = -1;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("LatestVersion"))
                    {
                        versionLineIndex = i;
                        var parts = lines[i].Split('=');
                        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int parsedVersion))
                        {
                            currentVersion = parsedVersion;
                        }
                        break;
                    }
                }

                Log(txtLog, $"[INFO] 현재 LatestVersion: {currentVersion}");

                // 3. 빌드 버전이 더 크면 업데이트
                if (buildVersion > currentVersion && versionLineIndex != -1)
                {
                    lines[versionLineIndex] = $"LatestVersion = {buildVersion}";
                    File.WriteAllLines(versionIniPath, lines);
                    Log(txtLog, $"[INFO] version.ini 업데이트 완료: {buildVersion}");
                }
                else
                {
                    Log(txtLog, $"[INFO] 업데이트 불필요: 기존 버전이 최신 또는 같음 프로세스 계속 진행");
                }

                // 4. SFTP 업로드
                using (var sftp = new SftpClient(dicData[CF_DataType.Akamai], dicData[CF_DataType.Akamai_ID], new PrivateKeyFile(dicData[CF_DataType.Akamai_Key])))
                {
                    sftp.Connect();
                    Log(txtLog, "[INFO] Akamai sftp 접근 완료");

                    // version.ini 업로드
                    using (var stream = new FileStream(versionIniPath, FileMode.Open))
                    {
                        sftp.UploadFile(stream, Path.Combine(dicData[CF_DataType.RemotePath], "version.ini"));
                        Log(txtLog, "[INFO] version.ini 업로드 완료");
                    }

                    string folderPath = Path.Combine(dicData[CF_DataType.VersionPath], latestBuildFolder);
                    string remoteFolderPath = Path.Combine(dicData[CF_DataType.RemotePath], latestBuildFolder).Replace("\\", "/");

                    if (!sftp.Exists(remoteFolderPath))
                        sftp.CreateDirectory(remoteFolderPath);

                    UploadDirectory(txtLog, sftp, folderPath, remoteFolderPath);
                    sftp.Disconnect();
                }

                // 5. Akamai Purge: 배치 파일 실행
                var purgeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = dicData[CF_DataType.Purge],
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                purgeProcess.Start();
                string output = purgeProcess.StandardOutput.ReadToEnd();
                string error = purgeProcess.StandardError.ReadToEnd();
                purgeProcess.WaitForExit();

                Log(txtLog, $"[INFO] QA_Purge.bat 실행 결과:\r\n{output}");

                if (purgeProcess.ExitCode != 0)
                {
                    throw new Exception($"QA_Purge.bat 실행 실패: {error}");
                }

                Log(txtLog, $"✅ 업로드 및 Purge 완료!", 0, false, true);
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ 오류 발생 : {ex.Message}", 0, false, true);
                throw new Exception($"❌ 오류 발생 : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }

        public static async Task DownloadFromFtp(TextBox txtLog, string ftpUrl, string strID, string strPw, string strlocal, int nType, string strCurVer, string strCfg, Progress_Form progressForm)
        {
            Log(txtLog, $"============ [DownloadFromFtp START] {ftpUrl} → {strlocal} ============");
            try
            {
                SetDirectory(strlocal);
                List<string> zipFiles = new();

                // 1. FTP 파일 목록 가져오기
                var listRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                listRequest.Credentials = new NetworkCredential(strID, strPw);

                // FTP 디렉토리 목록을 읽어 다운로드할 리스트 생성.
                using (var listResponse = (FtpWebResponse)await listRequest.GetResponseAsync())
                using (var reader = new StreamReader(listResponse.GetResponseStream()))
                {
                    while (!reader.EndOfStream)
                    {
                        string? file = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(file)) continue;

                        if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileNameOnly = Path.GetFileName(file); // "CF_PH_HOST_Patch.zip" 파일 이름만 추출

                            if (nType == 1 && fileNameOnly.Contains("CF_PH_CLIENT_Patch"))
                            {
                                Log(txtLog, $"✅ [INFO] 다운로드할 zip 파일 {nType} : {file} : {fileNameOnly}");
                                zipFiles.Add(fileNameOnly);
                            }
                            else if (nType == 2 && (fileNameOnly.Contains("CF_PH_HOST_Patch") || fileNameOnly.Contains("CF_PH_Server_Patch")))
                            {
                                Log(txtLog, $"✅ [INFO] 다운로드할 zip 파일 {nType} : {file} : {fileNameOnly}");
                                zipFiles.Add(fileNameOnly);
                            }
                        }
                    }
                }

                if (zipFiles.Count == 0)
                {
                    Log(txtLog, "[DownloadFromFtp][INFO] 다운로드할 zip 파일이 없습니다.");
                    throw new Exception($"[DownloadFromFtp][INFO] 다운로드할 zip 파일이 없습니다.");
                }

                // 2. FTP 다운로드 및 압축 해제
                foreach (var file in zipFiles)
                {
                    string fileUrl = $"{ftpUrl.TrimEnd('/')}/{file}";
                    string localFilePath = Path.Combine(strlocal, file);

                    Log(txtLog, $"✅ [ZIP DOWNLOAD] {fileUrl} → {localFilePath}");
                    long totalBytes = 0;
                    var sizeRequest = (FtpWebRequest)WebRequest.Create(fileUrl);
                    sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                    sizeRequest.Credentials = new NetworkCredential(strID, strPw);

                    using (var sizeResponse = (FtpWebResponse)await sizeRequest.GetResponseAsync())
                    {
                        totalBytes = sizeResponse.ContentLength;
                    }

                    var downloadRequest = (FtpWebRequest)WebRequest.Create(fileUrl);
                    downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    downloadRequest.Credentials = new NetworkCredential(strID, strPw);

                    using (var response = (FtpWebResponse)await downloadRequest.GetResponseAsync())
                    using (var ftpStream = response.GetResponseStream())
                    using (var fileStream = new FileStream(localFilePath, FileMode.Create))
                    {
                        // -------------------------------------------------------------------------------
                        long downloadedBytes = 0;
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        int currnetPercent = 0;

                        while ((bytesRead = await ftpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            int progressPercent = totalBytes > 0 ? (int)((downloadedBytes * 100L) / totalBytes) : 0;
                            if (progressPercent != currnetPercent)
                            {
                                string ftplog = $"📥 {file}... {progressPercent}% ({downloadedBytes:N0}/{totalBytes:N0} bytes)";
                                progressForm.UpdateProgress(progressPercent, ftplog);
                                currnetPercent = progressPercent;
                            }
                        }
                        // -------------------------------------------------------------------------------
                    }

                    Log(txtLog, $"✅ 다운로드 완료: {file}");

                    string extractTarget = Path.Combine(strlocal, Path.GetFileNameWithoutExtension(file));
                    SetDirectory(extractTarget);
                    ZipFile.ExtractToDirectory(localFilePath, extractTarget);
                    Log(txtLog, $"✅ 압축 해제 완료: {extractTarget}");

                    File.Delete(localFilePath);
                    Log(txtLog, $"🗑️ ZIP 삭제 완료: {localFilePath}");
                }

                // 3. cfg 파일 수정
                VersionChange(txtLog, strCurVer, strCfg);
            }
            catch (WebException webEx)
            {
                if (webEx.Response is FtpWebResponse ftpResponse)
                {
                    Log(txtLog, $"❌ FTP 오류: {ftpResponse.StatusDescription}");
                    throw new Exception($"❌ FTP 오류: {ftpResponse.StatusDescription}");
                }
                else
                {
                    Log(txtLog, $"❌ 일반 FTP 예외: {webEx.Message}");
                    throw new Exception($"❌ 일반 FTP 예외: {webEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌ 일반 오류: {ex.Message}");
                throw new Exception($"❌ 일반 오류: {ex.Message}");
            }
        }

        public static async Task UpdateLoginIni(TextBox txtLog, string newClientVersion, string strLogin_ServerInfo)
        {
            string iniPath = strLogin_ServerInfo;

            if (!File.Exists(iniPath))
            {
                Log(txtLog, $"[ERROR] Login ini 파일 경로가 존재하지 않습니다: {iniPath}");
                return;
            }

            var lines = File.ReadAllLines(iniPath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#")) continue;

                if (line.Contains("ClientVersion"))
                {
                    var verParts = line.Split('=', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                    string oldVer = verParts[1].Trim();
                    if (verParts.Length == 2 && oldVer != newClientVersion)
                    {
                        lines[i] = $"    ClientVersion={newClientVersion}";
                        Log(txtLog, $"[INFO] ClientVersion 업데이트: {oldVer} → {newClientVersion}");
                        File.WriteAllLines(iniPath, lines);
                    }
                    break;
                }
            }
        }

        static async void VersionChange(TextBox txtLog, string strCurVer, string strCfg)
        {
            // cfg Version 파일 수정
            string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strCfg);
            if (File.Exists(cfgPath))
            {
                var lines = File.ReadAllLines(cfgPath);
                bool updated = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("Version "))
                    {
                        string[] parts = lines[i].Split(' ', (char)2, (char)StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 && parts[1].Trim() != strCurVer)
                        {
                            Log(txtLog, $"[INFO] VersionChange .cfg 업데이트: {lines[i]} → Version {strCurVer}");
                            lines[i] = $"Version {strCurVer}";
                            updated = true;
                        }
                        break;
                    }
                }

                if (updated)
                    File.WriteAllLines(cfgPath, lines);
            }
        }

        public static async Task SVNUpdateAsync(TextBox txtLog, string svnPath)
        {
            Log(txtLog, $"============ [SVN Update] 시작: {svnPath} ============");

            using var progressForm = new Progress_Form("SVN Update");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 대기

            

            try
            {
                await RunSvnCommandAsync(txtLog, "update", svnPath, progressForm);
                //    //progressForm.UpdateProgress(10, "SVN 프로세스 시작..");

                //    var psi = new ProcessStartInfo
                //    {
                //        FileName = "svn",
                //        Arguments = $"update \"{svnPath}\"",
                //        RedirectStandardOutput = true,
                //        RedirectStandardError = true,
                //        UseShellExecute = false,
                //        CreateNoWindow = true
                //    };

                //    using var process = Process.Start(psi);
                //    if (process == null)
                //    {
                //        Log(txtLog, "[SVN Update ERROR] 프로세스를 시작할 수 없습니다.");
                //        return;
                //    }

                //    await Task.Delay(1000);
                //    progressForm.UpdateProgress(50, "SVN 데이터 수신 중...");

                //    string stdout = await process.StandardOutput.ReadToEndAsync();
                //    string stderr = await process.StandardError.ReadToEndAsync();

                //    process.WaitForExit();


                //    progressForm.UpdateProgress(80, "SVN 결과 분석 중...");
                //    await Task.Delay(5000);

                //    if (process.ExitCode != 0)
                //    {
                //        Log(txtLog, $"[SVN Update ERROR] 종료코드: {process.ExitCode}\n{stderr}");
                //        return;
                //    }

                //    if (!string.IsNullOrWhiteSpace(stdout))
                //    {
                //        Log(txtLog, $"[SVN Update] 완료됨 : {stdout.Trim()}");
                //    }
                //    else
                //    {
                //        Log(txtLog, "[SVN Update] 변경 사항 없음.");
                //    }

                //    progressForm.UpdateProgress(100, "SVN Update 완료");
                //    await Task.Delay(5000); // 사용자에게 완료 화면 잠깐 표시
            }
            catch (Exception ex)
            {
                Log(txtLog, $"[SVN Update EXCEPTION] {ex.Message}");
                throw new Exception($"❌ [SVN Update EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }




        public static async Task CDNSVNCommit(TextBox txtLog, string strSVNPath, Dictionary<CF_DataType, string> dicData, string strParam, bool bCopy = true, int nType = 0, string strDate = "0000")
        {
            Log(txtLog, $"============ [CDNSVNCommit] Start Commit ============");

            using var progressForm = new Progress_Form("SVN 커밋");
            progressForm.Show();
            await Task.Delay(5000); // UI 렌더링 여유
            progressForm.UpdateProgress(10, "SVN 변경 사항 확인 중...");

            try
            {
                var parts = strParam.Split('_');
                string commitMessage = $"v{parts[3]}_{parts[4]}";

                // 복사 (덮어쓰기)
                if (bCopy)
                {
                    string sourcePath = nType == 0
                        ? $@"{dicData[CF_DataType.TargetPath]}\{commitMessage}"
                        : $@"{dicData[CF_DataType.TargetPath]}\client_{strDate}_{parts[3]}_{parts[4]}";

                    CopyAllFiles(txtLog, sourcePath, strSVNPath);
                    Log(txtLog, $"[INFO] SVN {sourcePath} -> {strSVNPath} 복사 완료");
                }

                //var statusProcess = new ProcessStartInfo
                //{
                //    FileName = "svn",
                //    Arguments = $"status \"{strSVNPath}\"",
                //    RedirectStandardOutput = true,
                //    RedirectStandardError = true,
                //    UseShellExecute = false,
                //    CreateNoWindow = true
                //};

                string output = string.Empty;
                bool hasChanges = false;

                using (var proc = new Process { StartInfo = GetProcessStartInfo("status", strSVNPath) })
                {
                    proc.Start();
                    output = await proc.StandardOutput.ReadToEndAsync();
                    proc.WaitForExit();
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    Log(txtLog, "[AutoSvnCommit INFO] 변경 사항이 없습니다. 커밋 생략.");
                    return;
                }

                Log(txtLog, $"[AutoSvnCommit INFO] 변경 감지됨:\n{output}");
                var statusLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                int total = statusLines.Length;
                int processed = 0;

                foreach (string line in statusLines)
                {
                    if (line.Length < 9) continue;

                    char status = line[0];
                    string filePath = line.Substring(8).Trim();

                    switch (status)
                    {
                        case '?':
                            await RunSvnCommandAsync(txtLog, $"add" ,$"{filePath}", progressForm);
                            hasChanges = true;
                            break;
                        case '!':
                            await RunSvnCommandAsync(txtLog, $"delete" ,$"{filePath}", progressForm);
                            hasChanges = true;
                            break;
                        case 'M':
                        case 'A':
                        case 'D':
                            hasChanges = true;
                            break;
                    }

                    processed++;
                    int percent = 10 + (int)((processed / (float)total) * 60); // 10%~70% 진행 표시
                    progressForm.UpdateProgress(percent, $"SVN 상태 처리 중... ({processed}/{total})");
                }

                if (hasChanges)
                {
                    progressForm.UpdateProgress(80, "SVN 커밋 실행 중...");
                    await RunSvnCommandAsync(txtLog, $"commit" ,$"{strSVNPath}\" -m \"{commitMessage}\"", progressForm);
                    Log(txtLog, $"[AutoSvnCommit INFO] SVN 자동 커밋 완료: {commitMessage}");
                }
                else
                {
                    Log(txtLog, "[AutoSvnCommit INFO] 최종적으로 커밋할 변경 사항이 없습니다.");
                }

                progressForm.UpdateProgress(100, "SVN 커밋 작업 완료");
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                Log(txtLog, $"❌[CDNSVNCommit] 커밋 중 오류 발생: {ex.Message}");
                throw new Exception($"❌ [CDNSVNCommit EXCEPTION] : {ex.Message}");
            }
            finally
            {
                progressForm.Close();
            }
        }

        static async Task RunSvnCommandAsync(TextBox txtLog, string args, string strSVNPath, Progress_Form progressForm)
        {
            using var proc = new Process { StartInfo = GetProcessStartInfo(args, strSVNPath) };
            proc.Start();

            progressForm.UpdateProgress(50, $"[RunSvnCommand SVN] {args} 데이터 수신 중...");
            var outputTask = proc.StandardOutput.ReadToEndAsync();
            var errorTask = proc.StandardError.ReadToEndAsync();

            proc.WaitForExit();
            progressForm.UpdateProgress(80, $"[RunSvnCommand SVN] {args} 결과 분석 중...");
            await Task.Delay(5000);

            string output = await outputTask;
            string error = await errorTask;

            if (!string.IsNullOrWhiteSpace(output))
                Log(txtLog, $"[RunSvnCommand SVN] 완료됨 \r\n {output.Trim()}");

            if (!string.IsNullOrWhiteSpace(error))
                Log(txtLog, $"[RunSvnCommand SVN-ERR] \r\n {error.Trim()}");

            if (proc.ExitCode != 0)
                Log(txtLog, $"[RunSvnCommand ERROR] SVN 명령 실패 \r\n (ExitCode: {proc.ExitCode})");
        }
        static ProcessStartInfo GetProcessStartInfo(string arguments, string strSVNPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "svn",
                Arguments = $"{arguments} \"{strSVNPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return psi;
        }

        #endregion CF_Tool

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
