using SG_Tool.EP7_Tool.ServerPatch;

namespace SG_Tool.EP7_Tool
{
    public class EP7_Tool_Form : UserControl
    {
        TabControl m_tabControl = null!;
        TabPage m_tabServerPatch_QA = null!;
        TabPage m_tabServerPatch_Live = null!;

        int m_lastSelectedIndex = 0;
        bool[] m_tabInitialized;

        public EP7_Tool_Form()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            m_tabServerPatch_QA = new TabPage("Patch QA");
            m_tabServerPatch_Live = new TabPage("Patch Live");

            m_tabControl.TabPages.Add(m_tabServerPatch_QA);
            m_tabControl.TabPages.Add(m_tabServerPatch_Live);

   
            // �ʱ� �� QA�� �̸� �ε�
            m_tabInitialized = new bool[2]; // �� ��
            m_tabServerPatch_QA.Controls.Add(new Ep7_Patch_QA_Form { Dock = DockStyle.Fill });
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

            if (!m_tabInitialized[index])
            {
                // ���� ���� �� ��Ʈ�� ����
                switch (index)
                {
                    case 1:
                        m_tabServerPatch_Live.Controls.Add(new ServerPatch_Live_Form { Dock = DockStyle.Fill });
                        break;
                }

                m_tabInitialized[index] = true;
            }

            m_lastSelectedIndex = index;
        }
    }
}
