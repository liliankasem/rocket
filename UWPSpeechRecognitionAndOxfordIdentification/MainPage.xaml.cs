//#define GRAMMAR
using System;

#if GRAMMAR
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
#endif

using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using com.mtaulty.OxfordVerify;
using Windows.UI.Popups;
using System.Collections.ObjectModel;

namespace Demo2_RocketLaunch
{
  public sealed partial class MainPage : Page
  {
    DispatcherTimer timer;
    SpeechRecognizer speechRecognizer;

    static readonly string LAUNCH_RULE = "launch";
    static readonly string MOVE_RULE = "direction";
    const string LAUNCH_COMMAND = "launch";
    const string LEFT_COMMAND = "left";
    const string RIGHT_COMMAND = "right";

    public MainPage()
    {
      this.InitializeComponent();
      this.LaunchUsers = new ObservableCollection<string>();
      this.oxfordClient = new OxfordVerificationClient(Keys.OxfordKey);
      this.Loaded += OnLoaded;
    }
    // NB: Not the best place to put this but ok for a demo.
    void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.DataContext = this;

      this.InitialiseGame();
      this.BuildLaunchUsersAsync();
      this.ListenForCommandsAsync();
    }

    async Task ListenForCommandsAsync()
    {
      this.speechRecognizer = new SpeechRecognizer();
      this.speechRecognizer.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(0);
      this.speechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(0);
      this.speechRecognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(0);

#if GRAMMAR
      var grammarFile = await StorageFile.GetFileFromApplicationUriAsync(
          new Uri("ms-appx:///grammar.xml"));

      this.speechRecognizer.Constraints.Add(
      new SpeechRecognitionGrammarFileConstraint(grammarFile));
#else
      this.speechRecognizer.Constraints.Add(
        new SpeechRecognitionListConstraint(
            new string[] { LAUNCH_COMMAND, LEFT_COMMAND, RIGHT_COMMAND }));
#endif

      var result = await speechRecognizer.CompileConstraintsAsync();

      if (result.Status == SpeechRecognitionResultStatus.Success)
      {
        while (true)
        {
          var speechResult = await speechRecognizer.RecognizeAsync();

          if ((speechResult.Confidence == SpeechRecognitionConfidence.Medium) ||
            (speechResult.Confidence == SpeechRecognitionConfidence.High))
          {
            string spokenCommand = string.Empty;

#if GRAMMAR
            // is it one of the launch options?
            var lastRulePath = speechResult.RulePath.Last();

            if (lastRulePath == LAUNCH_RULE)
            {
                spokenCommand = LAUNCH_COMMAND;
            }
            else if (lastRulePath == MOVE_RULE)
            {
                IReadOnlyList<string> values;

                if (speechResult.SemanticInterpretation.Properties.TryGetValue(
                  "action", out values))
                {
                  spokenCommand = values.FirstOrDefault();
                }
            }
#else
            spokenCommand = speechResult.Text;
#endif
            if (!string.IsNullOrEmpty(spokenCommand))
            {
              await this.HandleCommandAsync(spokenCommand);
            }
          }
        }
      }
    }

    async Task HandleCommandAsync(string command)
    {
      switch (command)
      {
        case LAUNCH_COMMAND:
          await this.LaunchAsync();
          break;
        case LEFT_COMMAND:
          this.MoveLeft();
          break;
        case RIGHT_COMMAND:
          this.MoveRight();
          break;
        default:
          break;
      }
    }
    async Task LaunchAsync()
    {
      bool launch = true;

      if (this.IsSecure)
      {
        launch = await this.VerifyUserAsync();
      }
      if (launch)
      {
        if (this.timer == null)
        {
          timer = new DispatcherTimer();
          timer.Tick += OnGameLoopTimerTick;
          timer.Interval = TimeSpan.FromSeconds(TIMER_INTERVAL_SEC);
        }
        //Start going up if not already launched                            
        if (!timer.IsEnabled)
        {
          this.OnGameLoopTimerTick(null, null);
          timer.Start();
        }
      }
      else
      {
        await this.ShowMessageDialogAsync("Sorry, security check failed", "No Launch");
      }
    }

    async void OnAddUser(object sender, RoutedEventArgs args)
    {
      var securityPhrase = await this.oxfordClient.GetVerificationPhraseForUserAsync(
        this.UserToAdd);

      await this.ShowMessageDialogAsync(
        $"Please hit 'Close' and then repeat the phrase below{Environment.NewLine}" +
        $"\"{securityPhrase}\"",
        "Verification Needed");

      var enrollmentResult = new EnrollmentResult()
      {
        EnrollmentStatus = EnrollmentStatus.None
      };

      while (enrollmentResult.EnrollmentStatus != EnrollmentStatus.Enrolled)
      {
        try
        {
          enrollmentResult = await this.oxfordClient.RecordAndEnrollUserAsync(
            this.UserToAdd, TimeSpan.FromSeconds(10));
        }
        catch
        {

        }
        if (enrollmentResult.EnrollmentStatus != EnrollmentStatus.Enrolled)
        {
          await this.ShowMessageDialogAsync(
            "Can you please close the dialog and repeat it again for me?",
            "More Training Needed");
        }
      }
      await this.ShowMessageDialogAsync(
        $"Ok {this.UserToAdd} you are registered",
        "registered");

      this.UserToAdd = string.Empty;
      this.BuildLaunchUsersAsync();
    }
    async Task<bool> VerifyUserAsync()
    {
      var securityPhrase =
        await this.oxfordClient.GetVerificationPhraseForUserAsync(this.LaunchUser);

      await ShowMessageDialogAsync(
        $"We need to verify you as {this.launchUser}.{Environment.NewLine}" +
        $"Hit close then repeat the phrase \"{securityPhrase}\".",
        "Launch Verification Required");

      VerificationResult verificationResult = 
        await this.oxfordClient.RecordAndVerifyUserAsync(
          this.LaunchUser, 
          TimeSpan.FromSeconds(5));

      return (verificationResult.Result == VerificationStatus.Accept);
    }
  }
}
