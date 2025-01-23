using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AForge.Video;
using AForge.Video.DirectShow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace yeristasyonu
{

    public partial class Form1 : Form
    {
        private MJPEGStream stream;
        private string esp32CamUrl = "http://192.168.42.134:81/stream";

        public Form1()
        {
            InitializeComponent();
            pictureBox57.SizeMode = PictureBoxSizeMode.StretchImage;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lblComPort_Click(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            serialPort1.PortName = cbComPort.Text;
            serialPort1.BaudRate = 9600;

            try
            {
                serialPort1.Open();
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                cbComPort.Enabled = false;
                lblStatus.Text = "CONNECTED";
                lblStatus.ForeColor = Color.Green;
                progressBar1.Value = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            } // Kapanış düzeltildi
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            // SaveFileDialog'ı başlat
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.Title = "Save Data As";

            String[] ports = SerialPort.GetPortNames();
            cbComPort.Items.AddRange(ports);

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            cbComPort.Enabled = true;
            progressBar1.Value = 0;

            Series xSeries = new Series("XSeries");
            xSeries.ChartType = SeriesChartType.Line; // Çizgi grafik türü
            xSeries.Color = Color.Black;
            chart1.Series.Add(xSeries);

            Series ySeries = new Series("YSeries");
            ySeries.ChartType = SeriesChartType.Line;
            ySeries.Color = Color.Black;
            chart2.Series.Add(ySeries);

            Series zSeries = new Series("ZSeries");
            zSeries.ChartType = SeriesChartType.Line;
            zSeries.Color = Color.Black;
            chart3.Series.Add(zSeries);

            Series wSeries = new Series("WSeries");
            wSeries.ChartType = SeriesChartType.Line;
            wSeries.Color = Color.Black;
            chart4.Series.Add(wSeries);

            stream = new MJPEGStream(esp32CamUrl);
            stream.NewFrame += video_NewFrame;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                cbComPort.Enabled = true;
                lblStatus.Text = "DISCONNECTED";
                lblStatus.ForeColor = Color.Red;
                progressBar1.Value = 0;
            }
        }

        private List<double> xValues = new List<double>(); // X verilerini tutan liste
        private List<double> yValues = new List<double>(); // Y verilerini tutan liste
        private List<double> zValues = new List<double>(); // Z verilerini tutan liste
        private List<double> wValues = new List<double>(); // W verilerini tutan liste

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string receivedData = serialPort1.ReadExisting();

            Invoke(new Action(() => {
                txtReceivedData.AppendText(receivedData + Environment.NewLine);

                string[] dataParts = receivedData.Split('*');

                if (dataParts.Length >= 4)
                {
                    lblXValue.Text = "X: " + dataParts[0];
                    lblYValue.Text = "Y: " + dataParts[1];
                    lblZValue.Text = "Z: " + dataParts[2];
                    lblWValue.Text = "Altitude: " + dataParts[3] + " m";

                    if (double.TryParse(dataParts[0], out double xVal) &&
                        double.TryParse(dataParts[1], out double yVal) &&
                        double.TryParse(dataParts[2], out double zVal) &&
                        double.TryParse(dataParts[3], out double wVal))
                    {
                        xValues.Add(xVal);
                        yValues.Add(yVal);
                        zValues.Add(zVal);
                        wValues.Add(wVal);

                        UpdateChart();
                    }

                    // Eğer veri kaydetme başlatılmışsa, veriyi kaydedelim
                    if (isSavingData)
                    {
                        // Veriyi txt dosyasına kaydedin
                        if (saveFileDialog1.FileName != "")
                        {
                            using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName, true)) // 'true' parametresi ile ekleme yapıyoruz
                            {
                                writer.WriteLine(receivedData); // Alınan veriyi dosyaya yazdırıyoruz
                            }
                        }
                    }
                }
            }));
        }

        // Grafiklerin güncellenmesi
        private int maxDataPoints = 100; // Grafiklerde tutulacak maksimum veri noktası

        private void UpdateChart()
        {
            // Eğer veriler çok fazla oldugunda, ilk verileri çıkarıyoruz
            if (xValues.Count > maxDataPoints)
            {
                xValues.RemoveAt(0); // İlk veriyi çıkar
                yValues.RemoveAt(0); // İlk veriyi çıkar
                zValues.RemoveAt(0); // İlk veriyi çıkar
                wValues.RemoveAt(0); // İlk veriyi çıkar
            }

            // X, Y, Z ve W serilerinin veri noktalarını sırasıyla ekleyelim
            chart1.Series["XSeries"].Points.Clear();
            chart2.Series["YSeries"].Points.Clear();
            chart3.Series["ZSeries"].Points.Clear();
            chart4.Series["WSeries"].Points.Clear();

            // X verileri
            for (int i = 0; i < xValues.Count; i++)
            {
                chart1.Series["XSeries"].Points.AddXY(i, xValues[i]);
            }

            // Y verileri
            for (int i = 0; i < yValues.Count; i++)
            {
                chart2.Series["YSeries"].Points.AddXY(i, yValues[i]);
            }

            // Z verileri
            for (int i = 0; i < zValues.Count; i++)
            {
                chart3.Series["ZSeries"].Points.AddXY(i, zValues[i]);
            }

            // W verileri
            for (int i = 0; i < wValues.Count; i++)
            {
                chart4.Series["WSeries"].Points.AddXY(i, wValues[i]);
            }

            // Verinin akıp geçmesini sağlamak için X ekseninin sınırlamalarını ayarlayalım
            chart1.ChartAreas[0].AxisX.Minimum = xValues.Count > 0 ? xValues.Count - maxDataPoints : 0;
            chart1.ChartAreas[0].AxisX.Maximum = xValues.Count;

            chart2.ChartAreas[0].AxisX.Minimum = yValues.Count > 0 ? yValues.Count - maxDataPoints : 0;
            chart2.ChartAreas[0].AxisX.Maximum = yValues.Count;

            chart3.ChartAreas[0].AxisX.Minimum = zValues.Count > 0 ? zValues.Count - maxDataPoints : 0;
            chart3.ChartAreas[0].AxisX.Maximum = zValues.Count;

            chart4.ChartAreas[0].AxisX.Minimum = wValues.Count > 0 ? wValues.Count - maxDataPoints : 0;
            chart4.ChartAreas[0].AxisX.Maximum = wValues.Count;
        }

        private void btnSaveData_Click_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName))
                    {
                        // Seri port üzerinden alınan veriyi olduğu gibi kaydediyoruz.
                        writer.Write(txtReceivedData.Text); // Seri monitörde görünen veriyi yazdırıyoruz
                    }
                
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void btnStartSave_Click(object sender, EventArgs e)
        {
            isSavingData = true;
            btnStopSave.Enabled = true;
            btnStartSave.Enabled = false;
            datalog.Text = "RECORDING";
            datalog.ForeColor = Color.Green;
        }

        private void btnStopSave_Click(object sender, EventArgs e)
        {
            isSavingData = false;
            btnStartSave.Enabled = true;
            btnStopSave.Enabled = false;
            datalog.Text = "NOTRECORDING";
            datalog.ForeColor = Color.Red;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            {
                string esp32CamUrl = "http://192.168.42.134:81/stream";
                stream = new MJPEGStream(esp32CamUrl);
                stream.NewFrame += video_NewFrame;
                stream.Start();
                button23.Enabled = true;
                button1.Enabled = false;
                camlabel.Text = "CONNECTED";
                camlabel.ForeColor = Color.Green;
                camprogress.Value = 100;
            }
        }
 

        private void button23_Click(object sender, EventArgs e)
        {
            if (stream != null && stream.IsRunning)
            {
                stream.Stop();
                stream = null;
                button23.Enabled = false;
                button1.Enabled = true;
                camlabel.Text = "DISCONNECTED";
                camlabel.ForeColor = Color.Red;
                camprogress.Value = 0;
            }
        }
        void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            {
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
                pictureBox57.Image = frame;
            }
        }
        private void pictureBox57_Click(object sender, EventArgs e)
        {

        }
    }
    }

    
    



