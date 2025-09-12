using SG_Tool.L9_Tool.AWS;
using SG_Tool.L9_Tool.FTP;

namespace SG_Tool.L9_Tool
{
    public class L9_Tool_Form : UserControl, IActivatableTool
    {
        TabControl m_tabControl;
        TabPage m_tabServerPatch_QA;
        TabPage m_tabServerPatch_Live;
        TabPage m_tabJsonUpdate;
        TabPage m_tabDBUpload;

        int m_lastSelectedIndex = 0;
        bool[] m_tabInitialized;

        public L9_Tool_Form()
        {
            Init();
        }

        void Init()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 각 탭 페이지 생성
            m_tabDBUpload = new TabPage("DB Upload");
            m_tabJsonUpdate = new TabPage("Json Update");
            m_tabServerPatch_QA = new TabPage("ECS QA");
            m_tabServerPatch_Live = new TabPage("ECS Live");

            m_tabControl.TabPages.Add(m_tabDBUpload);
            m_tabControl.TabPages.Add(m_tabJsonUpdate);
            m_tabControl.TabPages.Add(m_tabServerPatch_QA);
            m_tabControl.TabPages.Add(m_tabServerPatch_Live);
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
            foreach (Control ctrl in m_tabDBUpload.Controls)
                ctrl.Dispose();
            foreach (Control ctrl in m_tabJsonUpdate.Controls)
                ctrl.Dispose();
            foreach (Control ctrl in m_tabServerPatch_QA.Controls)
                ctrl.Dispose();
            foreach (Control ctrl in m_tabServerPatch_Live.Controls)
                ctrl.Dispose();


            m_tabDBUpload.Controls.Clear();
            m_tabJsonUpdate.Controls.Clear();
            m_tabServerPatch_QA.Controls.Clear();
            m_tabServerPatch_Live.Controls.Clear();
            m_tabInitialized[m_lastSelectedIndex] = false;
            Controls.Clear();
        }

        void InitializeComponent()
        {
            // 각 탭에 대응되는 사용자 컨트롤 추가
            m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9) { Dock = DockStyle.Fill });

            m_tabInitialized = new bool[4]; // 탭 수
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
                    case 0:
                        m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
                        break;
                    case 1:
                        m_tabJsonUpdate.Controls.Add(new JsonUpdate(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
                        break;
                    case 2:
                        m_tabServerPatch_QA.Controls.Add(new EcsPatch_QA_Form(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
                        break;
                    case 3:
                        m_tabServerPatch_Live.Controls.Add(new EcsPatch_Live_Form(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
                        break;
                }

                m_tabInitialized[index] = true;
            }

            m_lastSelectedIndex = index;
        }
    }
    
    public class L9_Asia_Tool_Form : UserControl, IActivatableTool
    {
        TabControl m_tabControl;
        //TabPage m_tabServerPatch_QA;
        //TabPage m_tabServerPatch_Live;
        TabPage m_tabJsonUpdate;
        TabPage m_tabDBUpload;

        int m_lastSelectedIndex = 0;
        bool[] m_tabInitialized = new bool[4]; // 탭 수

        public L9_Asia_Tool_Form()
        {
            Init();
        }

        void Init()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 각 탭 페이지 생성
            m_tabDBUpload = new TabPage("DB Upload");
            m_tabJsonUpdate = new TabPage("Json Update");
            //m_tabServerPatch_QA = new TabPage("ECS QA");
            //m_tabServerPatch_Live = new TabPage("ECS Live");

            m_tabControl.TabPages.Add(m_tabDBUpload);
            m_tabControl.TabPages.Add(m_tabJsonUpdate);
            //m_tabControl.TabPages.Add(m_tabServerPatch_QA);
            //m_tabControl.TabPages.Add(m_tabServerPatch_Live);
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
            foreach (Control ctrl in m_tabDBUpload.Controls)
                ctrl.Dispose();
            foreach (Control ctrl in m_tabJsonUpdate.Controls)
                ctrl.Dispose();
            //foreach (Control ctrl in m_tabServerPatch_QA.Controls)
            //    ctrl.Dispose();
            //foreach (Control ctrl in m_tabServerPatch_Live.Controls)
            //    ctrl.Dispose();


            m_tabDBUpload.Controls.Clear();
            m_tabJsonUpdate.Controls.Clear();
            //m_tabServerPatch_QA.Controls.Clear();
            //m_tabServerPatch_Live.Controls.Clear();
            m_tabInitialized[m_lastSelectedIndex] = false;
            Controls.Clear();
        }

        void InitializeComponent()
        {
            // 각 탭에 대응되는 사용자 컨트롤 추가
            m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
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
                    case 0:
                        m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
                        break;
                    case 1:
                        m_tabJsonUpdate.Controls.Add(new JsonUpdate(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
                        break;
                    //case 2:
                    //    m_tabServerPatch_QA.Controls.Add(new EcsPatch_QA_Form(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
                    //    break;
                    //case 3:
                    //    m_tabServerPatch_Live.Controls.Add(new EcsPatch_Live_Form(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
                    //    break;
                }

                m_tabInitialized[index] = true;
            }

            m_lastSelectedIndex = index;
        }
    }
}
