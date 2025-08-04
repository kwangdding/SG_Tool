namespace SG_Tool.Log
{
    public class SystemLog_Form : Form
    {
        static string m_strLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        static string m_strlogMessage = string.Empty;
        static readonly object logLock = new object();
        static bool m_bSaving = false;

        public static void StartLogSaving()
        {
            // 10초마다 로그를 저장하는 작업 시작
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(10000); // 10초 대기
                    SaveFile();
                }
            });
        }

        static void SaveFile()
        {
            lock (logLock)
            {
                if (string.IsNullOrEmpty(m_strlogMessage) || m_bSaving) return;

                m_bSaving = true;

                try
                {
                    File.AppendAllText(m_strLogPath, m_strlogMessage);
                    m_strlogMessage = string.Empty; // 저장 후 메시지 초기화
                }
                finally
                {
                    m_bSaving = false;
                }
            }
        }

        public static void LogMessage(TextBox txtLog, string message, int nType = 0, bool overwrite = false)
        {
            try
            {
                if (txtLog == null) return;

                string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
                string logMessage = string.Empty;

                switch (nType)
                {
                    case 0:
                    default:
                        logMessage = $"{timestamp,-15} - {message,-80}\r\n";
                        break;
                    case 1:
                        logMessage = $"{message}\r\n";
                        break;
                }

                if (overwrite)
                {
                    var lines = txtLog.Lines.ToList();
                    if (lines.Count > 0)
                    {
                        lines[lines.Count - 1] = logMessage.TrimEnd(); // 마지막 줄 업데이트
                        txtLog.Lines = lines.ToArray();
                    }
                    else
                    {
                        txtLog.AppendText(logMessage); // 텍스트가 없으면 추가
                    }
                }
                else
                {
                    logMessage = logMessage.Replace("\r\n", Environment.NewLine);
                    if (txtLog.InvokeRequired)
                    {
                        txtLog.Invoke(() => txtLog.AppendText(logMessage));
                    }
                    else
                    {
                        txtLog.AppendText(logMessage);
                    }
                }

                lock (logLock)
                {
                    m_strlogMessage += logMessage; // 로그 메시지 추가
                    SaveFile();
                }
            }
            catch (Exception ex)
            {
                lock (logLock)
                {
                    m_strlogMessage += $"[LogMessage ERROR] 예외 발생: {ex.Message}"; // 로그 메시지 추가
                    SaveFile();
                }
            }        
        }

        public static void AppendRtfText(RichTextBox rtb, string rtfContent)
        {
            // 현재 위치에 커서 설정
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;

            // RTF 서식을 임시 RichTextBox로 로드 후 Append
            using (RichTextBox tempRtb = new RichTextBox())
            {
                tempRtb.Rtf = rtfContent;
                rtb.SelectedRtf = tempRtb.Rtf;
            }
        }

        public static void LogMessage(RichTextBox txtLog, string message, int nType = 0, bool overwrite = false)
        {
            if (txtLog == null) return;

            string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            string logMessage = string.Empty;

            switch (nType)
            {
                case 0:
                default:
                    logMessage = $"{timestamp,-15} - {message,-80}\r\n";
                    break;
                case 1:
                    logMessage = $"{message}\r\n";
                    break;
            }

            if (overwrite)
            {
                var lines = txtLog.Lines.ToList();
                if (lines.Count > 0)
                {
                    lines[lines.Count - 1] = logMessage.TrimEnd(); // 마지막 줄 업데이트
                    txtLog.Lines = lines.ToArray();
                }
                else
                {
                    txtLog.AppendText(logMessage); // 텍스트가 없으면 추가
                }
            }
            else
            {
                logMessage = logMessage.Replace("\r\n", Environment.NewLine);
                txtLog.AppendText(logMessage);
            }

            lock (logLock)
            {
                m_strlogMessage += logMessage; // 로그 메시지 추가
            }
        }
    }
}