using SG_Tool.Log;
using System.Diagnostics;

namespace SG_Tool.EP7_Tool.CDN
{
    public class EP7_CDN_Purge : UserControl
    {
        ComboBox m_comboBox = null!;
        Button m_btRunButton = null!;
        TextBox m_txtLog = null!;

        public EP7_CDN_Purge()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // 중앙 정렬을 위한 패널 생성
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(mainPanel);

            Label label = new Label
            {
                Text = "서버 선택 :",
                AutoSize = true,
                Width = 100,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            m_comboBox = new ComboBox
            {
                Items = {
                    "QA",
                    "Live"
                },
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };


            m_btRunButton = new Button
            {
                Text = "실행",
                Width = 100,
                Height = 50,
                BackColor = Color.LightYellow,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            m_btRunButton.Click += new EventHandler(RunAkamaiPurge);

            // 로그 영역
            m_txtLog = SG_Common.GetLogBox(null, "EP7_CDN");
            //m_txtLog = new TextBox
            //{
            //    Name = "EP7_CDN",
            //    Multiline = true,
            //    Width = 950,
            //    Height = 750,
            //    Location = new Point(20, 170),
            //    ScrollBars = ScrollBars.Vertical,
            //    Font = new Font("Consolas", 8), // 폰트 사이즈 줄이기
            //    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            //};

            // UI 중앙 배치
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Label
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // m_comboBox

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5)); // Label + m_comboBox
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5)); // 실행 버튼
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // 로그 박스
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5)); // 여백

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(m_comboBox, 1, 0);
            layout.Controls.Add(m_btRunButton, 0, 1);
            layout.SetColumnSpan(m_btRunButton, 2); // 실행 버튼을 두 열에 걸쳐 배치
            layout.Controls.Add(m_txtLog, 0, 2);
            layout.SetColumnSpan(m_txtLog, 2); // 로그 박스를 두 열에 걸쳐 배치

            mainPanel.Controls.Add(layout);
        }

        async void RunAkamaiPurge(object sender, EventArgs e)
        {
            string selectedValue = m_comboBox.SelectedItem?.ToString(); // 콤보박스에서 선택한 값
            if (string.IsNullOrEmpty(selectedValue))
            {
                MessageBox.Show("환경을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LogMessage($"========== CDN Purge Start ==========");
                LogMessage($"실행 명령어: {selectedValue}");
                ProcessStartInfo psi_Android = Getpsi(GetCommnad(0, selectedValue));

                using (Process process = new Process())
                {
                    process.StartInfo = psi_Android;
                    process.OutputDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            m_txtLog.Invoke((MethodInvoker)(() =>
                            {
                                LogMessage(ev.Data);
                            }));
                        }
                    };

                    process.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            m_txtLog.Invoke((MethodInvoker)(() =>
                            {
                                LogMessage(ev.Data);
                            }));
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await Task.Run(() => process.WaitForExit());

                    LogMessage($"실행 명령어: {selectedValue} Adnroid : {process.ExitCode == 0} ");
                }

                ProcessStartInfo psi_iOS = Getpsi(GetCommnad(1, selectedValue));
                using (Process process = new Process())
                {
                    process.StartInfo = psi_iOS;
                    process.OutputDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            m_txtLog.Invoke((MethodInvoker)(() =>
                            {
                                LogMessage(ev.Data);
                            }));
                        }
                    };

                    process.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            m_txtLog.Invoke((MethodInvoker)(() =>
                            {
                                LogMessage(ev.Data);
                            }));
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await Task.Run(() => process.WaitForExit());

                    LogMessage($"실행 명령어: {selectedValue} iOS : {process.ExitCode == 0} ");

                    if (process.ExitCode == 0)
                    {
                        //MessageBox.Show("Akamai Purge 성공!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LogMessage($"========== Akamai Purge 성공 ==========");
                    }
                    else
                    {
                        MessageBox.Show($"작업이 실패했습니다. 종료 코드: {process.ExitCode}", "실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Akamai Purge 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 프로세스 종료
            LogMessage($"========== CDN Purge Finish ==========");
        }

        ProcessStartInfo Getpsi(string strAkamaiCommand)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {strAkamaiCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            return psi;
        }

        string GetCommnad(int nType, string selectedValue)
        {
            return GetCommand_Platform(selectedValue);
        }

        string GetCommand_Platform(string selectedValue)
        {
            string edgercPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EP7", "mgs.edgerc");
            if (selectedValue.Contains("Live"))
            {
                return $"akamai purge --edgerc \"{edgercPath}\" --section default delete --cpcode 1110090";
            }
            else
            {
                return $"akamai purge --edgerc \"{edgercPath}\" --section default delete --cpcode 1110790";
            }
        }

        void LogMessage(string message)
        {
            SystemLog_Form.LogMessage(m_txtLog, message);
        }
    }
}

