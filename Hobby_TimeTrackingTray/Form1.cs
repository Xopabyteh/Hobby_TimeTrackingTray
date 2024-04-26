using System.Diagnostics;
using Hobby_TimeTrackingTray.Properties;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace Hobby_TimeTrackingTray;

public partial class Form1 : Form
{
    private Stopwatch sw;
    private DateTime startDate;
    private DateTime stopDate;
    private bool isPaused;

    private string selectedTimeSheetFile;
    /// <summary>
    /// Persist the last selected time sheet file, so that it can be automatically selected again on next startup
    /// </summary>
    private string lastSelectedTimeSheetFile;
    public static readonly string TimeSheetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Time tracking");

    //private long discordApplicationId = 1160159654429597706;
    //private Discord.Discord discord;
    //private Discord.ActivityManager? discordActivityManager;

    public Form1()
    {
        InitializeComponent();

        MakeApplicationRunOnStartup();

        InitializeTimeTracking();
    }



    //Make the application start on startup
    private void MakeApplicationRunOnStartup()
    {
        var rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;

        rk.SetValue("Hobby_TimeTrackingTray", Application.ExecutablePath);
    }

    //Load timesheets and Initialize context menu strip
    private void InitializeTimeTracking()
    {
        notifyIcon.BalloonTipTitle = @"Time tracking";
        notifyIcon.Text = @"Time tracking ";
        notifyIcon.Visible = true;
        notifyIcon.Icon = Icons.ClockAdd;

        sw = new();
        clockLabelStripMenuItem.Enabled = false;

        pauseToolStripMenuItem.Enabled = false;

        AssureTimeSheetFolder();

        lastSelectedTimeSheetFile = Settings.Default.LastSelectedTimeSheetFile;
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

    private void startStopToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (sw.IsRunning || isPaused)
        {
            isPaused = false;
            StopClock();
        }
        else
        {
            StartClock();
        }
    }

    private void StartClock()
    {
        startDate = DateTime.Now;

        sw.Start();
        startStopToolStripMenuItem.Text = @"Stop";

        pauseToolStripMenuItem.Enabled = true;

        notifyIcon.Icon = Icons.ClockRunning;

        //if (discordActivityManager is not null)
        //{
        //    var details = string.IsNullOrEmpty(selectedTimeSheetFile) ?
        //        "Working on something" :
        //        $"Working on {Path.GetFileNameWithoutExtension(selectedTimeSheetFile)}";

        //    var activity = new Discord.Activity
        //    {
        //        ApplicationId = discordApplicationId,
        //        Name = "TimeTracTray",
        //        State = "Working",
        //        Timestamps =
        //        {
        //            Start = DateTimeOffset.Now.ToUnixTimeSeconds()
        //        },
        //        Details = details
        //    };

        //    try
        //    {
        //        discordActivityManager.UpdateActivity(activity, res =>
        //        {
        //            if (res != Result.Ok)
        //            {
        //                MessageBox.Show("Couldn't connect to discord");
        //            }
        //        });
        //    }
        //    catch
        //    {
        //        MessageBox.Show("Couldn't connect to discord");
        //    }
        //}
    }

    /// <returns>True if saved data and stopped clock, False if didn't stop (didn't have a selected timesheet)</returns>
    private bool StopClock()
    {
        if (string.IsNullOrEmpty(selectedTimeSheetFile))
        {
            //Ask user to select a time sheet file
            MessageBox.Show(@"Please select a time sheet file first", @"Save", MessageBoxButtons.OK);
            contextMenuStrip.Show();
            contextMenuStrip.Items[@"timeSheetFileSelectToolStripMenuItem"].Select();
            return false;
        }

        //Stop the clock and save the time
        var workingFor = sw.Elapsed;
        stopDate = DateTime.Now;
        sw.Reset();

        //Get additional comment from user
        var comment = Interaction.InputBox("Enter a comment for this time entry", "Comment", string.Empty);
        comment = string.IsNullOrEmpty(comment) ? "-" : comment;

        //Format the time into csv
        var timeDataFrame = $"{startDate:yyyy-MM-dd},{startDate:HH:mm:ss},{stopDate:HH:mm:ss},{workingFor.Hours:00}:{workingFor.Minutes:00}:{workingFor.Seconds:00},{comment}";
        //Save to documents
        File.AppendAllText(selectedTimeSheetFile, timeDataFrame + Environment.NewLine);

        startStopToolStripMenuItem.Text = @"Start";

        pauseToolStripMenuItem.Enabled = false;

        notifyIcon.Icon = Icons.ClockAdd;
        pauseToolStripMenuItem.Text = @"Pause";

        return true;
    }

    private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!isPaused)
        {
            //Pause

            isPaused = true;
            sw.Stop();
            pauseToolStripMenuItem.Text = @"Resume";
            notifyIcon.Icon = Icons.ClockWarning;
        }
        else
        {
            //Resume

            isPaused = false;
            sw.Start();
            pauseToolStripMenuItem.Text = @"Pause";
            notifyIcon.Icon = Icons.ClockRunning;
        }
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var canExit = Exit();
        if (canExit)
        {
            Application.Exit();
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        var canExit = Exit();
        e.Cancel = !canExit;
    }

    ///<returns>True if the application can exit, False if it can't</returns>
    private bool Exit()
    {
        if (sw.IsRunning)
        {
            var didStop = StopClock();
            if (!didStop)
            {
                return false;
            }
        }

        //Persist the selected time sheet
        Settings.Default.LastSelectedTimeSheetFile = selectedTimeSheetFile;
        Settings.Default.Save();

        return true;
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

    private void updatexlsxToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ExcelConverter.TimeSheetToExcel(selectedTimeSheetFile);
    }

    //Hide from alt+tab
    protected override CreateParams CreateParams
    {
        get
        {
            var @params = base.CreateParams;
            @params.ExStyle |= 0x00000080;
            return @params;
        }
    }
}