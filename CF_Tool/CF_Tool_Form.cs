using SG_Tool.CF_Tool.ServerPatch;

namespace SG_Tool.CF_Tool
{
    public class CF_Tool_Form : UserControl
    {
        public enum ViewType { QA, Live, CDN, All }
        TabControl m_tabControl = null!;
        TabPage m_tabServerPatch_QA = null!;
        TabPage m_tabServerPatch_Live = null!;
        static ViewType m_eViewType = ViewType.All;
        public static ViewType CurrentViewType { get { return m_eViewType; } }
        
        public CF_Tool_Form()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            switch(m_eViewType)
            {
                case ViewType.QA:
                    m_tabServerPatch_QA = new TabPage("Patch QA");
                    m_tabServerPatch_QA.Controls.Add(new CF_Patch_QA_Form { Dock = DockStyle.Fill });
                    m_tabControl.TabPages.Add(m_tabServerPatch_QA);
                    break;
                case ViewType.Live:
                    m_tabServerPatch_Live = new TabPage("Patch Live");
                    m_tabServerPatch_Live.Controls.Add(new CF_Patch_Live_Form { Dock = DockStyle.Fill });
                    m_tabControl.TabPages.Add(m_tabServerPatch_Live);
                    break;
                default:
                case ViewType.All:
                case ViewType.CDN:
                    m_tabServerPatch_QA = new TabPage("Patch QA");
                    m_tabServerPatch_Live = new TabPage("Patch Live");
                    m_tabServerPatch_QA.Controls.Add(new CF_Patch_QA_Form { Dock = DockStyle.Fill });
                    m_tabServerPatch_Live.Controls.Add(new CF_Patch_Live_Form { Dock = DockStyle.Fill });
                    m_tabControl.TabPages.Add(m_tabServerPatch_QA);
                    m_tabControl.TabPages.Add(m_tabServerPatch_Live);
                    break;
            }

            this.Controls.Add(m_tabControl);
        }
    }
}
