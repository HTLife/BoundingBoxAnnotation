using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace BoundingBoxTool
{
    public partial class Form1 : Form
    {
        private static readonly string FolderSelectDetaultTip = "Plase select the image directory...";
        private List<string> imgPathList = new List<string>();

        private int curImageIndex = -1;
        private List<Rectangle> curBBX = new List<Rectangle>(); //记录原始的boundingbox
        private double curScale = 1.0; 
        private Rectangle curRect = new Rectangle(); //pictureBox控件中 图像的显示区域
        private Image curImg = null;  //缩放之后，即实际显示的图像
        private Image curBbxImg = null;  //
        private Point? leftUp = null; //当前标记的左上角，相对于控件左上角

        private bool isProcessed = false;
        private Color bbxColor = Color.Red;

        public Form1()
        {
            InitializeComponent();
            txt_folder.Text = FolderSelectDetaultTip;
            lbl_folder_desc.Text = "";
            pcb_img.Anchor = AnchorStyles.Bottom;
        }

        private bool ValidImageIndex()
        {
            return imgPathList.Count > 0 && curImageIndex >= 0 && curImageIndex < imgPathList.Count;
        }

        private bool ValidImageIndex(int tmpIndex)
        {
            return imgPathList.Count > 0 && tmpIndex >= 0 && tmpIndex < imgPathList.Count;
        }

        private bool MoreImagesAfter()
        {
            return imgPathList.Count > 0 && curImageIndex >= 0 && curImageIndex < imgPathList.Count-1;
        }

        private bool MoreImagesBefore()
        {
            return imgPathList.Count > 0 && curImageIndex > 0 && curImageIndex < imgPathList.Count;
        }

        private bool ReadBoundingBox()
        {
            if (!ValidImageIndex())
            {
                return false;
            }
            string bbxFile = Path.Combine(Path.GetDirectoryName(imgPathList[curImageIndex]),
                    Path.GetFileNameWithoutExtension(imgPathList[curImageIndex]) + ".txt");
            curBBX.Clear();
            if (File.Exists(bbxFile))
            {
                using (StreamReader reader = new StreamReader(bbxFile))
                {
                    while (!reader.EndOfStream)
                    {
                        string lineStr = reader.ReadLine();
                        string[] values = lineStr.Split(' ');
                        if (values != null && values.Length == 4)
                        {
                            curBBX.Add(new Rectangle(Convert.ToInt32(values[0]),
                                Convert.ToInt32(values[1]),
                                Convert.ToInt32(values[2]),
                                Convert.ToInt32(values[3])));
                        }
                    }
                }
            }
            return true;
        }

        private bool SaveBoundingBox()
        {
            if (isProcessed)
            {
                if (!ValidImageIndex())
                {
                    return false;
                }

                string bbxFile = Path.Combine(Path.GetDirectoryName(imgPathList[curImageIndex]),
                    Path.GetFileNameWithoutExtension(imgPathList[curImageIndex]) + ".txt");
                using (StreamWriter writer = new StreamWriter(bbxFile, false))
                {
                    foreach (Rectangle rect in curBBX)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", rect.Left, rect.Top, rect.Width, rect.Height);
                        writer.Flush();
                    }
                }
                return File.Exists(bbxFile);
            }
            

            return true;
        }

        private void DisplayBoundingBox()
        {
            if (ValidImageIndex() && curImg != null)
            {

                curBbxImg = curImg.Clone() as Image;
                Graphics g = Graphics.FromImage(curBbxImg);
                Brush brush = new SolidBrush(bbxColor);
                Pen pen = new Pen(brush, 10);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;

                foreach (Rectangle rect in curBBX)
                {
                    int projectedLeft = Convert.ToInt32(rect.Left / curScale);
                    int projectedTop = Convert.ToInt32(rect.Top / curScale);
                    int projectedWidth = Convert.ToInt32(rect.Width / curScale);
                    int projectedHeight = Convert.ToInt32(rect.Height / curScale);

                    g.DrawLine(pen, projectedLeft, projectedTop, projectedLeft, projectedTop + projectedHeight);//左
                    g.DrawLine(pen, projectedLeft + projectedWidth, projectedTop, projectedLeft + projectedWidth, projectedTop + projectedHeight);//右
                    g.DrawLine(pen, projectedLeft, projectedTop, projectedLeft + projectedWidth, projectedTop);//上
                    g.DrawLine(pen, projectedLeft, projectedTop + projectedHeight, projectedLeft + projectedWidth, projectedTop + projectedHeight);//下

                }

                g.Dispose();
                pcb_img.Image = curBbxImg;
                
                pcb_img.Update();
            }
        }

        private void load(string imageRoot)
        {
            ReadImgPathList(imageRoot);
            lbl_folder_desc.Text = string.Format("【{0}】images found", imgPathList.Count);
            curImageIndex = 0;
            pcb_img.Focus();
            DisplayNewImage();
        }

        private void ResizeImage(ref Image srcImage, ref Image destImg, double scale)
        {
            int destWidth = Convert.ToInt32(srcImage.Width / scale);
            int destHeight = Convert.ToInt32(srcImage.Height / scale);

            destImg = new Bitmap(destWidth, destHeight);

            Graphics g = Graphics.FromImage(destImg);
            g.InterpolationMode = InterpolationMode.High;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(srcImage, new Rectangle(0, 0, destWidth, destHeight),
                    new Rectangle(0, 0, srcImage.Width, srcImage.Height), GraphicsUnit.Pixel);

            g.Dispose();
        }

        private bool DisplayNewImage()
        {
            if (!ValidImageIndex())
            {
                return false;
            }

            leftUp = null;
            isProcessed = false;
            lbl_cur_image.Text = string.Format("Image name：{0}", Path.GetFileName(imgPathList[curImageIndex]));
            lbl_progress.Text = string.Format("Progress：{0}/{1}", curImageIndex + 1, imgPathList.Count);
            Image tmpImg = Image.FromFile(imgPathList[curImageIndex]);
            int curWidth = tmpImg.Width;
            int curHeight = tmpImg.Height;
            int ctlWidth = pcb_img.Width;
            int ctlHeight = pcb_img.Height;
            double widthScales = curWidth * 1.0 / ctlWidth;
            double heightScale = curHeight * 1.0 / ctlHeight;
            curScale = Math.Max(Math.Max(widthScales, heightScale), 1.0);
            curRect = new Rectangle(Convert.ToInt32((ctlWidth - curWidth / curScale) / 2),
                Convert.ToInt32((ctlHeight - curHeight / curScale) / 2),
                Convert.ToInt32(curWidth / curScale),
                Convert.ToInt32(curHeight / curScale));
            ResizeImage(ref tmpImg, ref curImg, curScale);

            ReadBoundingBox();
            DisplayBoundingBox();

            tmpImg.Dispose();
            pcb_img.Focus();
            return true;
        }

        private void ReadImgPathList(string folder)
        {
            if (!Directory.Exists(folder))
            {
                MessageBox.Show("The directory does not exist！Please double check it.");
                return;
            }
            string[] imgPaths = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            imgPathList.Clear();
            if (imgPaths != null && imgPaths.Length > 0)  
            {
                imgPathList.Clear();
                imgPathList = imgPaths.Where(x =>
                    {
                        string imgName = x.ToLower();
                        return imgName.EndsWith(".jpg") || imgName.EndsWith(".jpeg") || imgName.EndsWith(".bmp") || imgName.EndsWith(".png");
                    }).ToList();
                
            }
        }

        private void btn_folder_select_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Please select the image directory";
        
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txt_folder.Text = dialog.SelectedPath;
                load(dialog.SelectedPath);
            }

            pcb_img.Focus();
        }

        private int NextUnLabeled()
        {
            int tmpIndex = curImageIndex + 1;
            while (ValidImageIndex(tmpIndex) && File.Exists(Path.ChangeExtension(imgPathList[tmpIndex],".txt")))
            {
                tmpIndex++;
            }
            return tmpIndex;
        }

        private int PreUnLabeled()
        {
            int tmpIndex = curImageIndex - 1;
            while (ValidImageIndex(tmpIndex) && File.Exists(Path.ChangeExtension(imgPathList[tmpIndex], ".txt")))
            {
                tmpIndex--;
            }
            return tmpIndex;
        }

        private void pcb_img_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                if (ValidImageIndex())
                {
                    if (!SaveBoundingBox())
                    {
                        MessageBox.Show("Fail to save, please annotate once more");
                        return;
                    }
                }

                if (MoreImagesAfter())
                {
                    curImageIndex++;
                    DisplayNewImage();
                }
                else
                {
                    MessageBox.Show("all finished！");
                }
            }
            else if(e.KeyCode == Keys.Q)
            {
                if (ValidImageIndex())
                {
                    if (!SaveBoundingBox())
                    {
                        MessageBox.Show("Fail to save, please annotate once more");
                        return;
                    }
                }

                if (MoreImagesBefore())
                {
                    curImageIndex--;
                    DisplayNewImage();
                }
                else
                {
                    MessageBox.Show("This is the very first image！");
                }
            }
            else if (e.KeyCode == Keys.A)
            {
                if (ValidImageIndex())
                {
                    if (!SaveBoundingBox())
                    {
                        MessageBox.Show("Fail to save, please annotate once more");
                        return;
                    }
                }
                int nextUnLabeledIndex = PreUnLabeled();
                if (ValidImageIndex(nextUnLabeledIndex))
                {
                    curImageIndex = nextUnLabeledIndex;
                    DisplayNewImage();
                }
                else
                {
                    MessageBox.Show("Images prior to this one have all been annotated！");
                }
            }
            else if (e.KeyCode == Keys.S)
            {
                if (ValidImageIndex())
                {
                    if (!SaveBoundingBox())
                    {
                        MessageBox.Show("Fail to save, please annotate once more");
                        return;
                    }
                }
                int nextUnLabeledIndex = NextUnLabeled();
                if (ValidImageIndex(nextUnLabeledIndex))
                {
                    curImageIndex = nextUnLabeledIndex;
                    DisplayNewImage();
                }
                else
                {
                    MessageBox.Show("Images after this one have all been annotated！");
                }
            }
            else if (e.KeyCode == Keys.H)
            {
                if (ValidImageIndex())
                {
                    if (!SaveBoundingBox())
                    {
                        MessageBox.Show("Fail to save, please annotate once more");
                        return;
                    }
                }
                curImageIndex = 0;
                DisplayNewImage();
            }
        }

        private void pcb_img_MouseDown(object sender, MouseEventArgs e)
        {
            if (curRect.Contains(e.Location))
            {
                leftUp = new Point(e.X,e.Y);
            }
            else
            {
                leftUp = null;
            }
        }

        private void pcb_img_MouseUp(object sender, MouseEventArgs e)
        {
            if (leftUp.HasValue && curRect.Contains(e.Location))
            {
                
                int left = Math.Min(leftUp.Value.X, e.X);
                int top = Math.Min(leftUp.Value.Y, e.Y);
                int right = Math.Max(leftUp.Value.X, e.X);
                int bottom = Math.Max(leftUp.Value.Y, e.Y);
                if (right - left >= 20 && bottom - top >= 20)
                {
                    isProcessed = true;
                    curBBX.Add(new Rectangle(Convert.ToInt32((left - curRect.Left) * curScale),
                    Convert.ToInt32((top - curRect.Top) * curScale),
                    Convert.ToInt32((right - left) * curScale),
                    Convert.ToInt32((bottom - top) * curScale)));
                    DisplayBoundingBox();
                }
            }
            leftUp = null;
        }

        private bool inside = false;

        private void pcb_img_MouseMove(object sender, MouseEventArgs e)
        {
            if (curRect.Contains(e.Location))
            {
                if (leftUp.HasValue)
                {
                    Image tmpImg = curBbxImg.Clone() as Image;
                    Graphics g = Graphics.FromImage(tmpImg);
                    Brush brush = new SolidBrush(bbxColor);
                    Pen pen = new Pen(brush, 3);
                    pen.DashStyle = DashStyle.Custom;

                    int left = Convert.ToInt32(leftUp.Value.X - curRect.Left);
                    int top = Convert.ToInt32(leftUp.Value.Y - curRect.Top);
                    int right = Convert.ToInt32(e.X - curRect.Left);
                    int bottom = Convert.ToInt32(e.Y - curRect.Top);
                    g.DrawLine(pen, left, top, left, bottom);//左
                    g.DrawLine(pen, right, top, right, bottom);//右
                    g.DrawLine(pen, left, top, right, top);//上
                    g.DrawLine(pen, left, bottom, right, bottom);//下
                    g.Dispose();
                    pcb_img.Image = tmpImg;
                    pcb_img.Update();
                }
                else
                {
                    Image tmpImg = curBbxImg.Clone() as Image;
                    Graphics g = Graphics.FromImage(tmpImg);
                    Brush brush = new SolidBrush(bbxColor);
                    Pen pen = new Pen(brush, 3);
                    pen.DashStyle = DashStyle.Dot;

                    g.DrawLine(pen, 0, e.Y - curRect.Top, curRect.Width, e.Y - curRect.Top);//水平
                    g.DrawLine(pen, e.X - curRect.Left, 0, e.X - curRect.Left, curRect.Height);//垂直
                    g.Dispose();
                    pcb_img.Image = tmpImg;
                    pcb_img.Update();
                }
                inside = true;
            }
            else
            {
                if (inside)
                {
                    DisplayBoundingBox();
                }
                inside = false;
                leftUp = null;
            }
        }

        private void pcb_img_MouseLeave(object sender, EventArgs e)
        {
            if (inside)
            {
                DisplayBoundingBox();
            }
            inside = false;
            leftUp = null;
        }

        private void pcb_img_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (curRect.Contains(e.Location))
            {
                for (int i = 0; i < curBBX.Count; ++i)
                {
                    int projectedLeft = Convert.ToInt32(curBBX[i].Left / curScale);
                    int projectedTop = Convert.ToInt32(curBBX[i].Top / curScale);
                    int projectedWidth = Convert.ToInt32(curBBX[i].Width / curScale);
                    int projectedHeight = Convert.ToInt32(curBBX[i].Height / curScale);
                    Rectangle tmpBBX = new Rectangle(curRect.Left + projectedLeft,
                        curRect.Top + projectedTop,
                        projectedWidth, projectedHeight);
                    if (tmpBBX.Contains(e.Location))
                    {
                        isProcessed = true;
                        curBBX.RemoveAt(i);
                        --i;
                    }
                }
                DisplayBoundingBox();
            }
           
        }

        private void btn_update_Click(object sender, EventArgs e)
        {
            string path = txt_folder.Text;
            if (Directory.Exists(path))
            {
                load(path);
            }
            else
            {
                MessageBox.Show("Please select the directory first！");
            }
            pcb_img.Focus();
        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            if (ValidImageIndex())
            {
                File.Delete(imgPathList[curImageIndex]);
                File.Delete(Path.ChangeExtension(imgPathList[curImageIndex], ".txt"));
                imgPathList.RemoveAt(curImageIndex);
                lbl_folder_desc.Text = string.Format("【{0}】 images found", imgPathList.Count);
                DisplayNewImage();
            }
            pcb_img.Focus();
        }

        private void btn_openpath_Click(object sender, EventArgs e)
        {
            if (ValidImageIndex())
            {
                System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(imgPathList[curImageIndex])); 
            }
            pcb_img.Focus();

        }

        private void rbn_red_CheckedChanged(object sender, EventArgs e)
        {
            if (rbn_red.Checked)
            {
                bbxColor = Color.Red;
                DisplayBoundingBox();
            }
            pcb_img.Focus();
        }

        private void rbn_green_CheckedChanged(object sender, EventArgs e)
        {
            if (rbn_green.Checked)
            {
                bbxColor = Color.Green;
                DisplayBoundingBox();
            }
            pcb_img.Focus();
        }

        private void rbn_yellow_CheckedChanged(object sender, EventArgs e)
        {
            if (rbn_yellow.Checked)
            {
                bbxColor = Color.Yellow;
                DisplayBoundingBox();
            }
            pcb_img.Focus();
        }

        private void rbn_black_CheckedChanged(object sender, EventArgs e)
        {
            if (rbn_black.Checked)
            {
                bbxColor = Color.Black;
                DisplayBoundingBox();
            }
            pcb_img.Focus();
        }

        private void pcb_img_Click(object sender, EventArgs e)
        {

        }
    }
}
