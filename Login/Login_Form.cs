namespace SG_Tool.Login
{
    public class Login_Form : Form
    {
        bool m_bAuthenticated = false;
        TextBox? txtUsername;
        TextBox? txtPassword;
        Button? btnLogin;


        public bool IsAuthenticated { get { return m_bAuthenticated; } }
        public string Username => txtUsername != null ? txtUsername.Text.Trim() : string.Empty;
        public string Password => txtPassword != null ? txtPassword.Text.Trim() : string.Empty;

        public Login_Form()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "SG_Tool Login"; 
            this.Size = new Size(300, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            var lblUsername = new Label { Text = "아이디:", Location = new Point(20, 20), AutoSize = true };
            txtUsername = new TextBox { Location = new Point(100, 18), Width = 150 };

            var lblPassword = new Label { Text = "비밀번호:", Location = new Point(20, 60), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(100, 58), Width = 150, UseSystemPasswordChar = true };

            btnLogin = new Button { Text = "로그인", Location = new Point(100, 100), Width = 150 };
            btnLogin.Click += btnLogin_Click;

            this.AcceptButton = btnLogin;

            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            if (Username == "admin" && Password == "admin")
            {
                m_bAuthenticated = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("ID 또는 비밀번호가 올바르지 않습니다.", "로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
