using SG_Tool.Email_Tool.Target;

namespace SG_Tool.Email_Tool
{
    public class Email_Tool_Form : UserControl, IActivatableTool
    {
        TabControl m_tabControl = null!;
        TabPage m_tabEpic = null!;
        TabPage m_tabLoad = null!;
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
            m_tabOuter = new TabPage("Outer");

            m_tabControl.TabPages.Add(m_tabEpic);
            m_tabControl.TabPages.Add(m_tabLoad);
            m_tabControl.TabPages.Add(m_tabOuter);
        }

        public void ActivateTool()
        {
            // 활성화 로직
            InitializeComponent();
        }

        public void DeactivateTool()
        {
            m_tabControl.Selecting -= M_tabControl_Selecting;
            m_tabControl.SelectedIndexChanged -= M_tabControl_SelectedIndexChanged;

            // 비활성화 로직
            foreach (Control ctrl in m_tabEpic.Controls)
                ctrl.Dispose();

            foreach (Control ctrl in m_tabLoad.Controls)
                ctrl.Dispose();

            foreach (Control ctrl in m_tabOuter.Controls)
                ctrl.Dispose();

            m_tabEpic.Controls.Clear();
            m_tabLoad.Controls.Clear();
            m_tabOuter.Controls.Clear();
            m_tabInitialized[m_lastSelectedIndex] = false;
            Controls.Clear();
        }

        void InitializeComponent()
        {
            // 초기 탭 CDN_Purge만 미리 로딩
            m_tabEpic.Controls.Add(new Epic_Form { Dock = DockStyle.Fill });

            m_tabInitialized = new bool[3]; // 탭 수
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
            //MessageBox.Show($"M_tabControl_SelectedIndexChanged {index} 탭을 활성화 하겠습니까?", "탭 활성화", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (!m_tabInitialized[index])
            {
                // 최초 진입 시 컨트롤 생성
                switch (index)
                {
                    case 0:
                        m_tabEpic.Controls.Add(new Epic_Form { Dock = DockStyle.Fill });
                        break;
                    case 1:
                        m_tabLoad.Controls.Add(new Load_Form { Dock = DockStyle.Fill });
                        break;
                    case 2:
                        m_tabOuter.Controls.Add(new Outer_Form { Dock = DockStyle.Fill });
                        break;
                }

                m_tabInitialized[index] = true;
            }

            m_lastSelectedIndex = index;
        }
    }
}
