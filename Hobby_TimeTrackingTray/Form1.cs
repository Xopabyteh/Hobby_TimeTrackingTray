using System.Diagnostics;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace Hobby_TimeTrackingTray;

public partial class Form1 : Form
{
    private Stopwatch sw;
    private DateTime startDate;
    private DateTime stopDate;

    private string selectedTimeSheetFile;
    /// <summary>
    /// Persist the last selected time sheet file, so that it can be automatically selected again on next startup
    /// </summary>
    private string lastSelectedTimeSheetFile;
    private static readonly string TimeSheetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Time tracking");

    public Form1()
    {
        InitializeComponent();

        notifyIcon.BalloonTipTitle = @"Time tracking";
        notifyIcon.Text = @"Time tracking ";
        notifyIcon.Visible = true;

        sw = new();
        clockLabelStripMenuItem.Enabled = false;

        pauseToolStripMenuItem.Enabled = false;

        SetStartup();
        AssureTimeSheetFolder();

        lastSelectedTimeSheetFile = Settings.Default.lastSelectedTimeSheetFile;
        if (string.IsNullOrEmpty(lastSelectedTimeSheetFile))
        {
            timeSheetFileSelectToolStripMenuItem.Text = @"Select time sheet file";
        }
        else
        {
            selectedTimeSheetFile = lastSelectedTimeSheetFile;
            timeSheetFileSelectToolStripMenuItem.Text = Path.GetFileName(selectedTimeSheetFile);
        }

        var availableTimeSheets = GetTimeSheetFiles();
        for (var i = 0; i < availableTimeSheets.Length; i++)
        {
            var timeSheet = availableTimeSheets[i];
            var fileName = Path.GetFileName(timeSheet);
            timeSheetFileSelectToolStripMenuItem.DropDownItems.Add(fileName);
            timeSheetFileSelectToolStripMenuItem.DropDownItems[i].Click += (sender, args) =>
            {
                selectedTimeSheetFile = timeSheet;
                timeSheetFileSelectToolStripMenuItem.Text = fileName;
            };
        }

        //Last in the dropdown: create new file
        timeSheetFileSelectToolStripMenuItem.DropDownItems.Add("Create new file");
        timeSheetFileSelectToolStripMenuItem.DropDownItems[^1].Click += (sender, args) =>
        {
            //Open message box with input field
            var newTimeSheetName = Interaction.InputBox(
                "Enter the name of the new time sheet file",
                "New time sheet file",
                "Time sheet");


            if (newTimeSheetName == string.Empty || availableTimeSheets.Any(x => Path.GetFileName(x) == newTimeSheetName))
                return;

            //Verify that the file doesn't have any invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            if (newTimeSheetName.Any(x => invalidChars.Contains(x)))
            {
                MessageBox.Show(@"The file name contains invalid characters");
                return;
            }

            var newFilePath = File.Create(Path.Combine(TimeSheetFolder, newTimeSheetName + ".csv"));
            newFilePath.Close();
            selectedTimeSheetFile = Path.Combine(TimeSheetFolder, newTimeSheetName + ".csv");
            timeSheetFileSelectToolStripMenuItem.Text = newTimeSheetName + ".csv";
        };
    }

    //Makes sure the time sheet folder exists (creates it)
    private void AssureTimeSheetFolder()
    {
        if (!Directory.Exists(TimeSheetFolder))
        {
            Directory.CreateDirectory(TimeSheetFolder);
        }
    }

    private string[] GetTimeSheetFiles()
    {
        return Directory.GetFiles(TimeSheetFolder, "*.csv");
    }

    //Make the application start on startup
    private void SetStartup()
    {
        var rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;

        rk.SetValue("Hobby_TimeTrackingTray", Application.ExecutablePath);
    }

    private void startStopToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (sw.IsRunning)
        {
            //Stop the clock and save the time
            var workingFor = sw.Elapsed;
            stopDate = DateTime.Now;
            sw.Reset();

            //Format the time into csv
            var timeDataFrame = $"{startDate:yyyy-MM-dd HH:mm:ss},{stopDate:yyyy-MM-dd HH:mm:ss},{workingFor.Hours:00}:{workingFor.Minutes:00}:{workingFor.Seconds:00}";

            //Save to documents
            File.AppendAllText(selectedTimeSheetFile, timeDataFrame + Environment.NewLine);

            startStopToolStripMenuItem.Text = @"Start";

            pauseToolStripMenuItem.Enabled = false;
        }
        else
        {
            //Start the clock
            startDate = DateTime.Now;

            sw.Start();
            startStopToolStripMenuItem.Text = @"Stop";

            pauseToolStripMenuItem.Enabled = true;
        }
    }

    private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (sw.IsRunning)
        {
            sw.Stop();
            pauseToolStripMenuItem.Text = @"Resume";
        }
        else
        {
            sw.Start();
            pauseToolStripMenuItem.Text = @"Pause";
        }
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        => Exit();

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        => Exit();

    private void Exit()
    {
        //Persist the selected time sheet
        Settings.Default.lastSelectedTimeSheetFile = selectedTimeSheetFile;

        Application.Exit();
    }

    private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var workingFor = sw.Elapsed;
        clockLabelStripMenuItem.Text = $@"Working for: {workingFor.Hours:00}:{workingFor.Minutes:00}:{workingFor.Seconds:00}";
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        //Hide the form so the window doesn't show up
        Hide();
    }
}