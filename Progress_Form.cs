namespace SG_Tool
{
    public class Progress_Form : Form
    {
        string m_strMessage = "작업 진행 중입니다...";
        Label m_label;
        ProgressBar m_progressBar;
        System.Windows.Forms.Timer m_timer;
        int m_currentProgress = 0;
        int m_targetProgress = 100;

        public Progress_Form(string message = "작업 진행 중입니다...", int targetProgress = 100)
        {
            this.Text = "처리 중...";
            this.Size = new Size(550, 130);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false;
            this.TopMost = true;

            m_strMessage = message;
            m_label = new Label
            {
                Text = m_strMessage,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold)
            };

            m_progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Dock = DockStyle.Bottom,
                Height = 25,
                Minimum = 0,
                Maximum = 100
            };

            this.Controls.Add(m_label);
            this.Controls.Add(m_progressBar);

            m_targetProgress = targetProgress;

            m_timer = new System.Windows.Forms.Timer();
            m_timer.Interval = 50; // 부드러운 애니메이션 속도
            m_timer.Tick += Timer_Tick;
            m_timer.Start();
        }

        int m_animProgress = 0;
        int m_animDirection = 1;
        void Timer_Tick2(object? sender, EventArgs e)
        {
            if (m_targetProgress <= 0) return;

            // 애니메이션으로 0~target 범위 왕복
            m_animProgress += m_animDirection;
            if (m_animProgress >= m_targetProgress)
            {
                m_animProgress = m_targetProgress;
                m_animDirection = -1;
            }
            else if (m_animProgress <= 0)
            {
                m_animProgress = 0;
                m_animDirection = 1;
            }

            m_progressBar.Value = m_animProgress;
            m_label.Text = $"진행 중... (~{m_targetProgress}%)";
        }

        int m_nTempProgress = 0;
        void Timer_Tick(object? sender, EventArgs e)
        {
            if (m_currentProgress < m_targetProgress)
            {
                m_currentProgress++;
                m_progressBar.Value = m_currentProgress;
                m_label.Text = $"{m_strMessage} ({m_currentProgress}%)";
            }
            else
            {
                if (m_targetProgress == 100)
                {
                    m_timer.Stop(); // 목표 도달 시 멈춤
                }
                else
                {
                    if (m_nTempProgress == m_currentProgress)
                    {
                        m_timer.Interval = 200;
                        m_nTempProgress = 0;
                    }
                    m_progressBar.Value = m_nTempProgress++;
                }
            }
        }

        public void UpdateProgress(int percent, string message)
        {
            m_strMessage = message;

            if (InvokeRequired)
            {
                Invoke(() => UpdateProgress(percent, m_strMessage));
                return;
            }

            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            m_nTempProgress = 0;
            m_timer.Interval = 50;
            m_targetProgress = percent;

            if (!string.IsNullOrEmpty(m_strMessage))
                m_label.Text = $"{m_strMessage} ({m_currentProgress}%)";

            if (!m_timer.Enabled)
                m_timer.Start();
        }
    }
}