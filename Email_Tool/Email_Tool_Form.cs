using SG_Tool.Email_Tool.Target;

namespace SG_Tool.Email_Tool
{
    public class Email_Tool_Form : UserControl
    {
        TabControl m_tabControl = null!;
        TabPage m_tabEpic = null!;
        TabPage m_tabLoad = null!;
        TabPage m_tabOuter = null!;

        public Email_Tool_Form()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            m_tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            m_tabEpic = new TabPage("Epic");
            m_tabLoad = new TabPage("Load");
            m_tabOuter = new TabPage("Outer");

            m_tabEpic.Controls.Add(new Epic_Form { Dock = DockStyle.Fill });
            m_tabLoad.Controls.Add(new Load_Form { Dock = DockStyle.Fill });
            m_tabOuter.Controls.Add(new Outer_Form { Dock = DockStyle.Fill });

            m_tabControl.TabPages.Add(m_tabEpic);
            m_tabControl.TabPages.Add(m_tabLoad);
            m_tabControl.TabPages.Add(m_tabOuter);

            Controls.Add(m_tabControl);
        }
    }
}
