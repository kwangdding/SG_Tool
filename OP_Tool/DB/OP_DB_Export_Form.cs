using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml;

namespace SG_Tool.OP_Tool.DB
{
    public class OP_DB_Export_Form : UserControl
    {
        string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        TextBox m_txtQuery; // 쿼리 입력 텍스트 박스
        TextBox m_txtAdditionalQuery; // 추가 쿼리 입력 텍스트 박스
        TextBox m_txtTableName; // DB명 입력 텍스트 박스
        TextBox m_txtExcelFileName; // 엑셀 파일명 입력 텍스트 박스
        TextBox m_txtLog; // 로그 메시지 표시 텍스트 박스
        FlowLayoutPanel m_checkBoxPanel; // 체크박스 패널

        public OP_DB_Export_Form()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // 폼 설정
            Text = "DB 조회 및 엑셀 추출";
            Size = new Size(800, 700);

            // 체크박스 패널 추가
            m_checkBoxPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 20),
                Width = 750,
                Height = 100,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 서버 체크박스 추가
            string[] servers = { "QA", "글로벌", "한국", "아시아", "일본" };
            foreach (var server in servers)
            {
                CheckBox checkBox = new CheckBox
                {
                    Text = server,
                    AutoSize = true
                };
                m_checkBoxPanel.Controls.Add(checkBox);
            }

            // DB명 입력 텍스트 박스 추가
            Label lblDBName = new Label
            {
                Text = "DB명 입력",
                Location = new Point(20, 130),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            m_txtTableName = new TextBox
            {
                Width = 750,
                Location = new Point(20, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "OP_Game" // 기본값 설정
            };

            // 쿼리 입력 텍스트 박스 추가
            Label lblQuery = new Label
            {
                Text = "쿼리",
                Location = new Point(20, 180),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            m_txtQuery = new TextBox
            {
                Multiline = true,
                Width = 750,
                Height = 100,
                Location = new Point(20, 200),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 추가 쿼리 입력 텍스트 박스 추가
            Label lblAdditionalQuery = new Label
            {
                Text = "추가 쿼리",
                Location = new Point(20, 310),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            m_txtAdditionalQuery = new TextBox
            {
                Multiline = true,
                Width = 750,
                Height = 100,
                Location = new Point(20, 330),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 엑셀 파일명 입력 텍스트 박스 추가
            Label lblExcelFileName = new Label
            {
                Text = "엑셀 파일명 입력 (확장자 제외)",
                Location = new Point(20, 440),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            m_txtExcelFileName = new TextBox
            {
                Width = 750,
                Location = new Point(20, 460),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.Gray,
                Text = "output" // 플레이스홀더 텍스트 설정
            };
            m_txtExcelFileName.GotFocus += RemovePlaceholderText;
            m_txtExcelFileName.LostFocus += SetPlaceholderText;

            // 엑셀 추출 버튼 추가
            Button btnExportToExcel = new Button
            {
                Text = "엑셀로 추출",
                Location = new Point(20, 490),
                Width = 150,
                Height = 40,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnExportToExcel.Click += ExportToExcel_Click;

            // 로그 메시지 표시 텍스트 박스 추가
            m_txtLog = new TextBox
            {
                Multiline = true,
                Width = 750,
                Height = 150,
                Location = new Point(20, 540),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(m_checkBoxPanel);
            Controls.Add(lblDBName);
            Controls.Add(m_txtTableName);
            Controls.Add(lblQuery);
            Controls.Add(m_txtQuery);
            Controls.Add(lblAdditionalQuery);
            Controls.Add(m_txtAdditionalQuery);
            Controls.Add(lblExcelFileName);
            Controls.Add(m_txtExcelFileName);
            Controls.Add(btnExportToExcel);
            Controls.Add(m_txtLog);
        }

        private void RemovePlaceholderText(object sender, EventArgs e)
        {
            if (m_txtExcelFileName.Text == "output")
            {
                m_txtExcelFileName.Text = "";
                m_txtExcelFileName.ForeColor = Color.Black;
            }
        }

        private void SetPlaceholderText(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(m_txtExcelFileName.Text))
            {
                m_txtExcelFileName.Text = "output";
                m_txtExcelFileName.ForeColor = Color.Gray;
            }
        }

        void LoadCredentials(string serverType, out string serverIp, out string port, out string username, out string password)
        {
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OP", $"OP_dbconfig_{serverType}.cfg");
            if (File.Exists(credentialsPath))
            {
                var lines = File.ReadAllLines(credentialsPath);
                if (lines.Length >= 4)
                {
                    serverIp = lines[0].Trim();
                    port = lines[1].Trim();
                    username = lines[2].Trim();
                    password = lines[3].Trim();
                }
                else
                {
                    LogMessage($"{credentialsPath} 파일에 DB 연결 정보가 올바르게 설정되지 않았습니다.\r\n");
                    throw new Exception("Invalid dbconfig file");
                }
            }
            else
            {
                LogMessage($"{credentialsPath} 파일을 찾을 수 없습니다.\r\n");
                throw new Exception("dbconfig file not found");
            }
        }

        async void ExportToExcel_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedServers = m_checkBoxPanel.Controls.OfType<CheckBox>().Where(cb => cb.Checked).Select(cb => cb.Text.ToLower()).ToList();
                if (!selectedServers.Any())
                {
                    MessageBox.Show("적어도 하나의 서버를 선택해야 합니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tableName = m_txtTableName.Text.Trim();
                if (string.IsNullOrEmpty(tableName))
                {
                    MessageBox.Show("DB명 입력해야 합니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = m_txtQuery.Text.Trim();
                string additionalQuery = m_txtAdditionalQuery.Text.Trim();

                // 쿼리와 추가 쿼리가 SELECT로 시작하지 않으면 에러 메시지 표시
                if (!query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || 
                    !string.IsNullOrEmpty(additionalQuery) && !additionalQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("쿼리와 추가 쿼리는 SELECT로 시작해야 합니다.", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                query = $"USE {tableName};\n" + query; // 쿼리 앞에 USE DB명 추가
                additionalQuery = string.IsNullOrEmpty(additionalQuery) ? null : $"USE {tableName};\n" + additionalQuery; // 추가 쿼리 앞에 USE DB명 추가
                var dataTables = new Dictionary<string, DataTable>();

                foreach (var serverType in selectedServers)
                {
                    LoadCredentials(serverType, out string serverIp, out string port, out string username, out string password);

                    string connectionString = $"Server={serverIp},{port};User Id={username};Password={password};";
                    LogMessage($"서버 {serverType} ({serverIp})에 접속 시도 중...\r\n");

                    if (serverType == "아시아")
                    {
                        // 아시아1 쿼리 실행
                        DataTable dataTable1 = await GetDataTableAsync(connectionString, query);
                        dataTables["아시아1"] = dataTable1;

                        // 아시아2 쿼리 실행 (DB 이름에 _SG가 붙음)
                        string query2 = query.Replace(tableName, tableName + "_SG");
                        DataTable dataTable2 = await GetDataTableAsync(connectionString, query2);
                        dataTables["아시아2"] = dataTable2;
                    }
                    else
                    {
                        DataTable dataTable = await GetDataTableAsync(connectionString, query);
                        dataTables[serverType] = dataTable;
                    }

                    if (!string.IsNullOrEmpty(additionalQuery))
                    {
                        DataTable additionalDataTable = await GetDataTableAsync(connectionString, additionalQuery);
                        dataTables[$"{serverType}_additional"] = additionalDataTable;
                    }
                }

                string excelFileName = m_txtExcelFileName.Text.Trim();
                if (string.IsNullOrEmpty(excelFileName) || excelFileName == "output")
                {
                    excelFileName = "output";
                }
                excelFileName += ".xlsx";

                await SaveDataTablesToExcel(dataTables, excelFileName);

                LogMessage("엑셀 파일로 데이터 추출 완료.\r\n");
                MessageBox.Show("엑셀 파일로 데이터 추출 완료.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"오류 발생: {ex.Message}\r\n");
                MessageBox.Show($"오류 발생: {ex.Message}", "실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        async Task<DataTable> GetDataTableAsync(string connectionString, string query)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    LogMessage("DB 접속 성공.\r\n");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 180; // 3분 타임아웃 설정
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Load(reader);
                            return dataTable;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"DB 접속 실패: {ex.Message}\r\n");
                    throw;
                }
            }
        }

        async Task SaveDataTablesToExcel(Dictionary<string, DataTable> dataTables, string filePath)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    foreach (var serverType in dataTables.Keys.Select(k => k.Split('_')[0]).Distinct())
                    {
                        var worksheet = package.Workbook.Worksheets.Add(serverType);
                        int startRow = 1;

                        foreach (var kvp in dataTables.Where(kvp => kvp.Key.StartsWith(serverType)))
                        {
                            DataTable dataTable = kvp.Value;

                            // 컬럼 헤더 추가
                            for (int col = 0; col < dataTable.Columns.Count; col++)
                            {
                                worksheet.Cells[startRow, col + 1].Value = dataTable.Columns[col].ColumnName;
                            }

                            // 데이터 추가
                            for (int row = 0; row < dataTable.Rows.Count; row++)
                            {
                                for (int col = 0; col < dataTable.Columns.Count; col++)
                                {
                                    worksheet.Cells[startRow + row + 1, col + 1].Value = dataTable.Rows[row][col];
                                }
                            }

                            startRow += dataTable.Rows.Count + 2; // 기존 데이터 아래에 2줄 띄우고 추가
                        }
                    }

                    // 엑셀 파일 저장
                    File.WriteAllBytes(filePath, package.GetAsByteArray());
                }

                LogMessage($"엑셀 파일 저장 성공: {filePath}\r\n");
                MessageBox.Show($"엑셀 파일 저장 성공: {filePath}", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"엑셀 파일 저장 실패: {ex.Message}\r\n");
                MessageBox.Show($"엑셀 파일 저장 실패: {filePath}", "실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            string logMessage = $"{timestamp} - {message}\r\n";

            // 로그 파일에 로그 메시지 추가
            File.AppendAllText(logFilePath, logMessage);

            // 로그 메시지를 텍스트 박스에 추가
            if (m_txtLog.InvokeRequired)
            {
                m_txtLog.Invoke(new Action(() => m_txtLog.AppendText(logMessage)));
            }
            else
            {
                m_txtLog.AppendText(logMessage);
            }
        }
    }
}