// using directives for .NET Framework 4.8
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace JtagConverter
{
public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    private void btnBrowseInput_Click(object sender, EventArgs e)
    {
        using (var ofd = new OpenFileDialog())
        {
            ofd.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            ofd.Title = "选择包含32字节字符串的TXT文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                Log($"已选择输入文件: {ofd.FileName}");
            }
        }
    }

    private void btnBrowseOutput_Click(object sender, EventArgs e)
    {
        using (var sfd = new SaveFileDialog())
        {
            sfd.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            sfd.Title = "选择输出文件路径";
            sfd.FileName = "converted.txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = sfd.FileName;
                Log($"已选择输出文件: {sfd.FileName}");
            }
        }
    }

    private void btnConvert_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtInputPath.Text))
            {
                MessageBox.Show("请先选择输入TXT文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show("请先选择输出文件路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rawText = File.ReadAllText(txtInputPath.Text).Trim();
            if (rawText.Length == 0)
            {
                MessageBox.Show("输入文件为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 将文本内容解析为字节数组：
            // 支持两种格式：
            // 1) 纯ASCII字符（长度为32），按UTF-8/ASCII取前32字节
            // 2) 十六进制字符串（64个hex字符代表32字节）
            byte[] bytes;
            if (IsHexString(rawText) && rawText.Length >= 64)
            {
                bytes = HexStringToBytes(rawText.Substring(0, 64));
            }
            else
            {
                var ascii = Encoding.ASCII.GetBytes(rawText);
                if (ascii.Length < 32)
                {
                    MessageBox.Show("输入内容不足32字节", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                bytes = ascii.Take(32).ToArray();
            }

            // 将32字节拆分为8个UInt32（小端或大端？默认按大端处理常见表示）
            // 如果你的数据是小端，请将下面的 Endian 组装方式调整。
            var words = new List<uint>();
            for (int i = 0; i < 32; i += 4)
            {
                uint w = ((uint)bytes[i] << 24) | ((uint)bytes[i + 1] << 16) | ((uint)bytes[i + 2] << 8) | bytes[i + 3];
                words.Add(w);
            }

            var converted = words.Select(ConvertOriginalData).ToList();

            // 输出格式：0xXXXXXXXX,0xYYYYYYYY,...
            var sb = new StringBuilder();
            for (int i = 0; i < converted.Count; i++)
            {
                sb.Append("0x");
                sb.Append(converted[i].ToString("X8"));
                if (i != converted.Count - 1)
                {
                    sb.Append(',');
                }
            }

            File.WriteAllText(txtOutputPath.Text, sb.ToString());
            Log("转换完成并已保存。");
            MessageBox.Show("转换完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log("错误: " + ex);
        }
    }

    private static uint ConvertOriginalData(uint TempPassWord)
    {
        TempPassWord ^= (TempPassWord << 3);
        TempPassWord ^= (TempPassWord >> 7);
        TempPassWord ^= (TempPassWord << 15);
        return TempPassWord;
    }

    private static bool IsHexString(string s)
    {
        foreach (char c in s)
        {
            if (!Uri.IsHexDigit(c)) return false;
        }
        return true;
    }

    private static byte[] HexStringToBytes(string hex)
    {
        int len = hex.Length;
        byte[] data = new byte[len / 2];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return data;
    }

    private void Log(string msg)
    {
        // 更新状态栏文本，保持界面整洁
        if (this.lblStatus != null)
        {
            this.lblStatus.Text = msg;
        }
    }
}
}
