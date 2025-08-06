using Amazon.Runtime;

namespace SG_Tool.Log
{
    // public class SystemLog_Form : Form
    // {
    //     static string m_strLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
    //     static string m_strlogMessage = string.Empty;
    //     static readonly object logLock = new object();
    //     static bool m_bSaving = false;

    //     public static void StartLogSaving()
    //     {
    //         // 10초마다 로그를 저장하는 작업 시작
    //         Task.Run(async () =>
    //         {
    //             while (true)
    //             {
    //                 await Task.Delay(10000); // 10초 대기
    //                 SaveFile();
    //             }
    //         });
    //     }

    //     static void SaveFile()
    //     {
    //         lock (logLock)
    //         {
    //             if (string.IsNullOrEmpty(m_strlogMessage) || m_bSaving) return;

    //             m_bSaving = true;

    //             try
    //             {
    //                 File.AppendAllText(m_strLogPath, m_strlogMessage);
    //                 m_strlogMessage = string.Empty; // 저장 후 메시지 초기화
    //             }
    //             finally
    //             {
    //                 m_bSaving = false;
    //             }
    //         }
    //     }

    //     public static void LogMessage(TextBox txtLog, string message, int nType = 0, bool overwrite = false)
    //     {
    //         try
    //         {
    //             if (txtLog == null) return;

    //             string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
    //             string logMessage = string.Empty;

    //             switch (nType)
    //             {
    //                 case 0:
    //                 default:
    //                     logMessage = $"{timestamp,-15} - {message,-80}\r\n";
    //                     break;
    //                 case 1:
    //                     logMessage = $"{message}\r\n";
    //                     break;
    //             }

    //             if (overwrite)
    //             {
    //                 var lines = txtLog.Lines.ToList();
    //                 if (lines.Count > 0)
    //                 {
    //                     lines[lines.Count - 1] = logMessage.TrimEnd(); // 마지막 줄 업데이트
    //                     txtLog.Lines = lines.ToArray();
    //                 }
    //                 else
    //                 {
    //                     txtLog.AppendText(logMessage); // 텍스트가 없으면 추가
    //                 }
    //             }
    //             else
    //             {
    //                 logMessage = logMessage.Replace("\r\n", Environment.NewLine);
    //                 if (txtLog.InvokeRequired)
    //                 {
    //                     txtLog.Invoke(() => txtLog.AppendText(logMessage));
    //                 }
    //                 else
    //                 {
    //                     txtLog.AppendText(logMessage);
    //                 }
    //             }

    //             lock (logLock)
    //             {
    //                 m_strlogMessage += logMessage; // 로그 메시지 추가
    //                 SaveFile();
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             lock (logLock)
    //             {
    //                 m_strlogMessage += $"[LogMessage ERROR] 예외 발생: {ex.Message}"; // 로그 메시지 추가
    //                 SaveFile();
    //             }
    //         }        
    //     }

    //     public static void AppendRtfText(RichTextBox rtb, string rtfContent)
    //     {
    //         // 현재 위치에 커서 설정
    //         rtb.SelectionStart = rtb.TextLength;
    //         rtb.SelectionLength = 0;

    //         // RTF 서식을 임시 RichTextBox로 로드 후 Append
    //         using (RichTextBox tempRtb = new RichTextBox())
    //         {
    //             tempRtb.Rtf = rtfContent;
    //             rtb.SelectedRtf = tempRtb.Rtf;
    //         }
    //     }

    //     public static void LogMessage(RichTextBox txtLog, string message, int nType = 0, bool overwrite = false)
    //     {
    //         if (txtLog == null) return;

    //         string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
    //         string logMessage = string.Empty;

    //         switch (nType)
    //         {
    //             case 0:
    //             default:
    //                 logMessage = $"{timestamp,-15} - {message,-80}\r\n";
    //                 break;
    //             case 1:
    //                 logMessage = $"{message}\r\n";
    //                 break;
    //         }

    //         if (overwrite)
    //         {
    //             var lines = txtLog.Lines.ToList();
    //             if (lines.Count > 0)
    //             {
    //                 lines[lines.Count - 1] = logMessage.TrimEnd(); // 마지막 줄 업데이트
    //                 txtLog.Lines = lines.ToArray();
    //             }
    //             else
    //             {
    //                 txtLog.AppendText(logMessage); // 텍스트가 없으면 추가
    //             }
    //         }
    //         else
    //         {
    //             logMessage = logMessage.Replace("\r\n", Environment.NewLine);
    //             txtLog.AppendText(logMessage);
    //         }

    //         lock (logLock)
    //         {
    //             m_strlogMessage += logMessage; // 로그 메시지 추가
    //         }
    //     }
    // }

    public class SystemLog_Form : Form
    {
        static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        static string m_strLogPath = GetLogFilePath();
        static string m_strlogMessage = string.Empty;
        static readonly object logLock = new object();
        static bool m_bSaving = false;

        /// <summary>
        /// 로그 저장 주기 작업 시작 (10초마다 저장)
        /// </summary>
        public static void StartLogSaving()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(10000); // 10초 대기
                    SaveBufferedLogs();
                }
            });
        }

        /// <summary>
        /// 오늘 날짜 기준 로그 파일 경로 반환
        /// </summary>
        static string GetLogFilePath()
        {
            string todayFileName = $"{DateTime.Now:yyyyMMdd}_log.txt";
            return Path.Combine(LogDirectory, todayFileName);
        }

        /// <summary>
        /// 버퍼에 쌓인 로그 저장
        /// </summary>
        static void SaveBufferedLogs()
        {
            lock (logLock)
            {
                if (string.IsNullOrEmpty(m_strlogMessage) || m_bSaving) return;
                m_bSaving = true;

                try
                {
                    EnsureLogDirectory();
                    m_strLogPath = GetLogFilePath();

                    File.AppendAllText(m_strLogPath, m_strlogMessage);
                    m_strlogMessage = string.Empty;
                }
                finally
                {
                    m_bSaving = false;
                }
            }
        }

        /// <summary>
        /// 즉시 로그 저장
        /// </summary>
        static void SaveImmediate(string logMessage)
        {
            try
            {
                EnsureLogDirectory();
                m_strLogPath = GetLogFilePath();
                File.AppendAllText(m_strLogPath, logMessage);
            }
            catch
            {
                // 즉시 저장 실패해도 버퍼 저장이 나중에 처리
            }
        }

        /// <summary>
        /// 로그 디렉토리 생성
        /// </summary>
        static void EnsureLogDirectory()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        /// <summary>
        /// 로그 메시지 기록 (즉시 저장 + 버퍼 저장)
        /// </summary>
        public static void LogMessage(TextBox txtLog, string message, int nType = 0, bool overwrite = false)
        {
            try
            {
                if (txtLog == null) return;

                string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
                string logMessage = string.Empty;

                //switch (nType)
                //{
                //    case 0:
                //    default:
                //        logMessage = $"{timestamp,-15} - {message,-80}\r\n";
                //        break;
                //    case 1:
                //        logMessage = $"{message}\r\n";
                //        break;
                //}

                switch (nType)
                {
                    case 0:
                    default:
                        logMessage = $"{timestamp,-15} - {message,-80}\r\n";
                        break;
                    case 1:
                        int totalWidth = 40;
                        int padding = (totalWidth - message.Length) / 2;
                        string centeredMessage = message.PadLeft(message.Length + padding).PadRight(totalWidth);
                        message = $"============= {centeredMessage,-40} =============";
                        //logMessage = $"{timestamp,-15} - {message,-80}\r\n";
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
                        if (txtLog.InvokeRequired)
                        {
                            txtLog.Invoke(() => txtLog.AppendText(logMessage));
                        }
                        else
                        {
                            txtLog.AppendText(logMessage);
                        }
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

                // 버퍼 저장
                lock (logLock)
                {
                    m_strlogMessage += logMessage;
                }

                // 즉시 저장
                SaveImmediate(logMessage);
            }
            catch (Exception ex)
            {
                lock (logLock)
                {
                    m_strlogMessage += $"[LogMessage ERROR] 예외 발생: {ex.Message}"; // 로그 메시지 추가
                    SaveImmediate(m_strlogMessage);
                }
            }
        }

        //public static void LogMessage(TextBox txtLog, string message, int nType = 0, bool overwrite = false)
        //{
        //    if (txtLog == null) return;

        //    string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
        //    string logMessage = string.Empty;

        //    switch (nType)
        //    {
        //        case 0:
        //        default:
        //            logMessage = $"{timestamp,-15} - {message,-80}\r\n";
        //            break;
        //        case 1:
        //            int totalWidth = 40;
        //            int padding = (totalWidth - message.Length) / 2;
        //            string centeredMessage = message.PadLeft(message.Length + padding).PadRight(totalWidth);
        //            message = $"============= {centeredMessage,-40} =============";
        //            logMessage = $"{timestamp,-15} - {message,-80}\r\n";
        //            break;
        //    }

        //    // UI 로그 표시
        //    if (overwrite)
        //    {
        //        var lines = txtLog.Lines.ToList();
        //        if (lines.Count > 0)
        //        {
        //            lines[lines.Count - 1] = logMessage.TrimEnd();
        //            txtLog.Lines = lines.ToArray();
        //        }
        //        else
        //        {
        //            txtLog.AppendText(logMessage);
        //        }
        //    }
        //    else
        //    {
        //        logMessage = logMessage.Replace("\r\n", Environment.NewLine);
        //        txtLog.AppendText(logMessage);
        //    }

        //    // 버퍼 저장
        //    lock (logLock)
        //    {
        //        m_strlogMessage += logMessage;
        //    }

        //    // 즉시 저장
        //    SaveImmediate(logMessage);
        //}
    }
}