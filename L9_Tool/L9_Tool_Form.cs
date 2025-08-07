using SG_Tool.L9_Tool.AWS;
using SG_Tool.L9_Tool.FTP;

namespace SG_Tool.L9_Tool
{
    public class L9_Tool_Form : UserControl
    {
        TabControl m_tabControl;
        TabPage m_tabServerPatch_QA;
        TabPage m_tabServerPatch_Live;
        TabPage m_tabJsonUpdate;
        TabPage m_tabDBUpload;

        public L9_Tool_Form()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };


            // 각 탭 페이지 생성
            m_tabServerPatch_QA = new TabPage("ECS QA");
            m_tabJsonUpdate = new TabPage("Json Update");
            m_tabDBUpload = new TabPage("DB Upload");
            m_tabServerPatch_Live = new TabPage("ECS Live");

            // 각 탭에 대응되는 사용자 컨트롤 추가

            m_tabServerPatch_QA.Controls.Add(new EcsPatch_QA_Form(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
            m_tabJsonUpdate.Controls.Add(new JsonUpdate(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
            m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9) { Dock = DockStyle.Fill });
            m_tabServerPatch_Live.Controls.Add(new EcsPatch_Live_Form(EnLoad9_Type.L9) { Dock = DockStyle.Fill });

            // 탭 추가
            m_tabControl.TabPages.AddRange(new[] {
                m_tabServerPatch_QA,
                m_tabJsonUpdate,
                m_tabDBUpload,
                m_tabServerPatch_Live
            });

            m_tabControl.Selecting += M_tabControl_Selecting;
            Controls.Add(m_tabControl);
        }

        void M_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Text.Contains("Patch_Live"))
            {
                if (MessageBox.Show($"[{e.TabPage.Text}]탭을 활성화 하겠습니까?", "탭 활성화", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
    
    public class L9_Asia_Tool_Form : UserControl
    {
        TabControl m_tabControl;
        TabPage m_tabServerPatch_QA;
        TabPage m_tabServerPatch_Live;
        TabPage m_tabJsonUpdate;
        TabPage m_tabDBUpload;

        public L9_Asia_Tool_Form()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

           
            // 각 탭 페이지 생성
            m_tabServerPatch_QA = new TabPage("ECS QA");
            m_tabJsonUpdate = new TabPage("Json Update");
            m_tabDBUpload = new TabPage("DB Upload");
            m_tabServerPatch_Live = new TabPage("ECS Live");

            // 각 탭에 대응되는 사용자 컨트롤 추가

            m_tabServerPatch_QA.Controls.Add(new EcsPatch_QA_Form(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
            m_tabJsonUpdate.Controls.Add(new JsonUpdate(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
            m_tabDBUpload.Controls.Add(new DBUpload(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });
            m_tabServerPatch_Live.Controls.Add(new EcsPatch_Live_Form(EnLoad9_Type.L9_Asia) { Dock = DockStyle.Fill });

            // 탭 추가
            m_tabControl.TabPages.AddRange(new[] {
                m_tabServerPatch_QA,
                m_tabJsonUpdate,
                m_tabDBUpload,
                m_tabServerPatch_Live
            });

            m_tabControl.Selecting += M_tabControl_Selecting;
            Controls.Add(m_tabControl);
        }

        void M_tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Text.Contains("Patch_Live"))
            {
                if (MessageBox.Show($"[{e.TabPage.Text}]탭을 활성화 하겠습니까?", "탭 활성화", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
