using SG_Tool.Login;

namespace SG_Tool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool isNewInstance;
            using (Mutex mutex = new Mutex(true, "SEND_EMAIL_UniqueAppId", out isNewInstance))
            {
                if (!isNewInstance)
                {
                    MessageBox.Show("⚠️ 프로그램이 이미 실행 중입니다.", "중복 실행 방지", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
                Application.Run(new MainForm());
#else
                using (var loginForm = new Login_Form())
                {
                    var result = loginForm.ShowDialog();

                    if (result == DialogResult.OK && loginForm.IsAuthenticated)
                    {
                        Application.Run(new MainForm());
                    }
                    else
                    {
                        // 로그인 실패 시 종료
                        Application.Exit();
                    }
                }
#endif
            }
        }
    }
}
