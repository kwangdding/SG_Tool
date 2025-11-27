using SG_Tool.Email_Tool;
using SG_Tool.EP7_Tool;
using SG_Tool.L9_Tool;
using SG_Tool.Log;
using System.Drawing.Drawing2D;

namespace SG_Tool
{
    public class MainForm : Form
    {
        public MainForm()
        {
            SystemLog_Form.StartLogSaving();
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        void InitializeComponent()
        {
            this.Text = "SG_Tool - v1.0.15";
            this.Width = 1000;
            this.Height = 900;
            this.MinimumSize = new Size(600, 400);
            this.Icon = Properties.Resources.icon;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Left,
                SizeMode = TabSizeMode.Fixed,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(60, 120) // 세로 길이 확보
            };
           

            // ImageList 생성 및 고품질 아이콘 등록
            var imageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            imageList.Images.Add("email", ResizeIcon(Properties.Resources.icon_email.ToBitmap()));
            imageList.Images.Add("l9", ResizeIcon(Properties.Resources.icon_l9.ToBitmap()));
            imageList.Images.Add("l9_Asia", ResizeIcon(Properties.Resources.icon_l9.ToBitmap()));
            imageList.Images.Add("ep7", ResizeIcon(Properties.Resources.icon_ep7.ToBitmap()));
            //imageList.Images.Add("op", ResizeIcon(Properties.Resources.icon_op.ToBitmap()));
            tabControl.ImageList = imageList;

            // 탭 생성
            tabControl.TabPages.Add(new TabPage(" Email") { ImageIndex = 0, Controls = { new Email_Tool_Form { Dock = DockStyle.Fill } } });
            tabControl.TabPages.Add(new TabPage(" 로드나인") { ImageIndex = 1, Controls = { new L9_Tool_Form { Dock = DockStyle.Fill } } });
            tabControl.TabPages.Add(new TabPage(" 로드나인 아시아") { ImageIndex = 2, Controls = { new L9_Asia_Tool_Form { Dock = DockStyle.Fill } } });
            tabControl.TabPages.Add(new TabPage(" 에픽세븐") { ImageIndex = 3, Controls = { new EP7_Tool_Form { Dock = DockStyle.Fill } } });
            //tabControl.TabPages.Add(new TabPage(" 아우터플레인") { ImageIndex = 3, Controls = { new OP_Tool_Form { Dock = DockStyle.Fill } } });

            // 탭 선택/해제 이벤트 처리
            tabControl.Selected += (s, e) =>
            {
                if (e.TabPage.Controls.Count > 0)
                {
                    var tool = e.TabPage.Controls[0] as IActivatableTool;
                    tool?.ActivateTool();
                }
            };

            tabControl.Deselected += (s, e) =>
            {
                if (e.TabPage.Controls.Count > 0)
                {
                    var tool = e.TabPage.Controls[0] as IActivatableTool;
                    tool?.DeactivateTool();
                }
            };

            // 탭 커스텀 그리기
            tabControl.DrawItem += (s, e) =>
            {
                var g = e.Graphics;
                var tabPage = tabControl.TabPages[e.Index];
                var tabBounds = tabControl.GetTabRect(e.Index);
                bool isSelected = (e.Index == tabControl.SelectedIndex);

                // 배경 색상
                using var backBrush = new SolidBrush(isSelected ? Color.SkyBlue : SystemColors.Control);
                g.FillRectangle(backBrush, tabBounds);

                // 아이콘
                var image = tabControl.ImageList?.Images[tabPage.ImageIndex];
                int iconSize = 16;
                int iconX = tabBounds.X + (tabBounds.Width - iconSize) / 2;
                int iconY = tabBounds.Y + 12;

                if (image != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawImage(image, new Rectangle(iconX, iconY, iconSize, iconSize));
                }

                // 텍스트 렌더링 힌트 설정 (더 선명하게)
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 텍스트 폰트 및 색상
                using var font = new Font(e.Font.FontFamily, 9, isSelected ? FontStyle.Bold : FontStyle.Regular);
                using var textBrush = new SolidBrush(isSelected ? Color.White : Color.Black);

                // 텍스트 위치 계산
                var text = tabPage.Text.Trim();
                var textSize = g.MeasureString(text, font);
                float textX = tabBounds.X + (tabBounds.Width - textSize.Width) / 2;
                float textY = iconY + iconSize + 6;

                g.DrawString(text, font, textBrush, new PointF(textX, textY));
            };

            this.Controls.Add(tabControl);
        }

        // 아이콘 리사이즈 함수 (고품질 축소)
        Image ResizeIcon(Image original, int size = 16)
        {
            Bitmap resized = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(original, 0, 0, size, size);
            }
            return resized;
        }
        // Bitmap → Icon 변환 함수 (앞서 제공한 것과 동일)
    }

    // 탭에서 활성/비활성 처리를 위한 인터페이스
    public interface IActivatableTool
    {
        void ActivateTool();
        void DeactivateTool();
    }
}
