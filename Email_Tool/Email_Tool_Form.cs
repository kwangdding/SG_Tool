using SG_Tool.Email_Tool.Target;

namespace SG_Tool.Email_Tool
{
    public class Email_Tool_Form : UserControl, IActivatableTool
    {
        TabControl m_tabControl = null!;
        TabPage m_tabEpic = null!;
        TabPage m_tabLoad = null!;
        TabPage m_tabLoadAsia = null!;
        TabPage m_tabOuter = null!;

        int m_lastSelectedIndex = 0;
        bool[] m_tabInitialized;

        public Email_Tool_Form()
        {
            Init();
            InitializeComponent();
        }

        void Init()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            m_tabEpic = new TabPage("Epic");
            m_tabLoad = new TabPage("Load");
            m_tabLoadAsia = new TabPage("LoadAsia");
            m_tabOuter = new TabPage("Outer");

            m_tabControl.TabPages.Add(m_tabEpic);
            m_tabControl.TabPages.Add(m_tabLoad);
            m_tabControl.TabPages.Add(m_tabLoadAsia);
            m_tabControl.TabPages.Add(m_tabOuter);
        }

        public void ActivateTool()
        {
            // Ȱ��ȭ ����
            InitializeComponent();
        }

        public void DeactivateTool()
        {
            m_tabControl.Selecting -= M_tabControl_Selecting;
            m_tabControl.SelectedIndexChanged -= M_tabControl_SelectedIndexChanged;

            // ��Ȱ��ȭ ����
            foreach (Control ctrl in m_tabEpic.Controls)
                ctrl.Dispose();

            foreach (Control ctrl in m_tabLoad.Controls)
                ctrl.Dispose();

            foreach (Control ctrl in m_tabLoadAsia.Controls)
                ctrl.Dispose();

            foreach (Control ctrl in m_tabOuter.Controls)
                ctrl.Dispose();

            m_tabEpic.Controls.Clear();
            m_tabLoad.Controls.Clear();
            m_tabLoadAsia.Controls.Clear();
            m_tabOuter.Controls.Clear();
            m_tabInitialized[m_lastSelectedIndex] = false;
            Controls.Clear();
        }

        void InitializeComponent()
        {
            // �ʱ� �� CDN_Purge�� �̸� �ε�
            m_tabEpic.Controls.Add(new Epic_Form { Dock = DockStyle.Fill });

            m_tabInitialized = new bool[3]; // �� ��
            m_tabInitialized[0] = true;
            m_tabControl.SelectedIndex = 0;

            m_tabControl.Selecting += M_tabControl_Selecting;
            m_tabControl.SelectedIndexChanged += M_tabControl_SelectedIndexChanged;

            Controls.Add(m_tabControl);
        }

        void M_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // ����Ʈ ��(0��)�� �����ϰ�, ���� �ε� �� �� �Ǹ� Ȯ��
            if (e.TabPageIndex != m_lastSelectedIndex && !m_tabInitialized[e.TabPageIndex])
            {
                var result = MessageBox.Show(
                    $"[{e.TabPage.Text}] ���� Ȱ��ȭ �ϰڽ��ϱ�?",
                    "�� Ȱ��ȭ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (MessageBox.Show($"[{e.TabPage.Text}]���� Ȱ��ȭ �ϰڽ��ϱ�?", "�� Ȱ��ȭ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        void M_tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = m_tabControl.SelectedIndex;
            // MessageBox.Show($"M_tabControl_SelectedIndexChanged {index} ���� Ȱ��ȭ �ϰڽ��ϱ�?", "�� Ȱ��ȭ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (!m_tabInitialized[index])
            {
                // ���� ���� �� ��Ʈ�� ����
                switch (index)
                {
                    case 0:
                        m_tabEpic.Controls.Add(new Epic_Form { Dock = DockStyle.Fill });
                        break;
                    case 1:
                        m_tabLoad.Controls.Add(new Load_Form { Dock = DockStyle.Fill });
                        break;
                    case 2:
                        m_tabLoadAsia.Controls.Add(new LoadAsia_Form { Dock = DockStyle.Fill });
                        break;
                    case 3:
                        m_tabOuter.Controls.Add(new Outer_Form { Dock = DockStyle.Fill });
                        break;
                }

                m_tabInitialized[index] = true;
            }

            m_lastSelectedIndex = index;
        }

        public class SQLData
        {
            string strAccount = string.Empty;
            string strShard = string.Empty;
            string strWorld = string.Empty;

            public string Account => strAccount;
            public string Shard => strShard;
            public string World => strWorld;

            public SQLData()
            {
            }

            public void SetSQLData(string strText)
            {
                // ���� �� �Է��̶�� �� ������ �и��Ͽ� ó��
                var lines = strText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.Contains("DBAccount"))
                        strAccount += $"\t\t    {line}\r\n";
                    else if (line.Contains("DBShard"))
                        strShard += $"\t\t    {line}\r\n";
                    else if (line.Contains("DBWorld"))
                        strWorld += $"\t\t    {line}\r\n";
                }
            }
        }
    }
}
