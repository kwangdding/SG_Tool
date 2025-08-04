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

   
            // 초기 탭 QA만 미리 로딩
            m_tabInitialized = new bool[2]; // 탭 수
            m_tabServerPatch_QA.Controls.Add(new Ep7_Patch_QA_Form { Dock = DockStyle.Fill });
            m_tabInitialized[0] = true;
            m_tabControl.SelectedIndex = 0;

            m_tabControl.Selecting += M_tabControl_Selecting;
            m_tabControl.SelectedIndexChanged += M_tabControl_SelectedIndexChanged;

            Controls.Add(m_tabControl);
        }

        void M_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // 디폴트 탭(0번)은 제외하고, 아직 로딩 안 된 탭만 확인
            if (e.TabPageIndex != m_lastSelectedIndex && !m_tabInitialized[e.TabPageIndex])
            {
                var result = MessageBox.Show(
                    $"[{e.TabPage.Text}] 탭을 활성화 하겠습니까?",
                    "탭 활성화",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (MessageBox.Show($"[{e.TabPage.Text}]탭을 활성화 하겠습니까?", "탭 활성화", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
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
                // 최초 진입 시 컨트롤 생성
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
