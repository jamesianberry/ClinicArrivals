﻿using ClinicArrivals.Models;
using System;
using System.Windows;

namespace ClinicArrivals
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Threading.Timer poll;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (DataContext == null)
                DataContext = new Model();
            var model = DataContext as Model;
            model.Waiting.Clear();
            model.Expecting.Clear();
            Dispatcher.Invoke(async () =>
            {
                // read the settings from storage
                model.Settings.CopyFrom(await model.Storage.LoadSettings());
                if (model.Settings.SystemIdentifier == Guid.Empty)
                {
                    // this only occurs when the system hasn't had one allocated
                    // so we can create a new one, then save the settings.
                    // (this will force an empty setting file with the System Identifier if needed)
                    model.Settings.AllocateNewSystemIdentifier();
                    await model.Storage.SaveSettings(model.Settings);
                }

                // read the room mappings from storage
                model.RoomMappings.Clear();
                foreach (var map in await model.Storage.LoadRoomMappings())
                    model.RoomMappings.Add(map);

                // reload any unmatched messages
                var messages = await model.Storage.LoadUnprocessableMessages(model.DisplayingDate);

                // Start the FHIR server
                MessageProcessing.OnStarted += MessageProcessing_OnStarted;
                MessageProcessing.OnStopped += MessageProcessing_OnStopped;
                MessageProcessing.OnVisitStarted += MessageProcessing_OnVisitStarted;
                MessageProcessing.StartServer(model.Settings.ExamplesServer);

                model.serverStatuses.Oridashi.CurrentStatus = "starting...";
                model.serverStatuses.Oridashi.Start = new ServerStatusCommand(model.serverStatuses.Oridashi, "stopped", () =>
                {
                    model.serverStatuses.Oridashi.CurrentStatus = "starting...";
                    MessageProcessing.StartServer(model.Settings.ExamplesServer);
                });
                model.serverStatuses.Oridashi.Stop = new ServerStatusCommand(model.serverStatuses.Oridashi, "running", async () =>
                {
                    model.serverStatuses.Oridashi.CurrentStatus = "stopping...";
                    await MessageProcessing.StopServer();
                });
                model.ReadSmsMessage = new BackgroundProcess(model.Settings, model.serverStatuses.IncomingSmsReader, Dispatcher, async () => 
                {
                    // Logic to run on this process
                    // (called every settings.interval)
                    model.StatusBarMessage = $"Last read SMS messages at {DateTime.Now.ToLongTimeString()}";
                    var processor = new MessageProcessing();
                    await processor.CheckForMessages(model);
                    // return System.Threading.Tasks.Task.CompletedTask;
                });
            });
        }

        private void MessageProcessing_OnVisitStarted(PmsAppointment appt)
        {
            System.Windows.MessageBox.Show("SMS TO " + appt.PatientName, "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
        }

        private void MessageProcessing_OnStopped()
        {
            poll.Dispose();
            poll = null;

            Dispatcher.Invoke(() =>
            {
                var model = DataContext as Model;
                model.serverStatuses.Oridashi.CurrentStatus = "stopped";
            });
        }

        private void MessageProcessing_OnStarted()
        {
            Dispatcher.Invoke(() =>
            {
                var model = DataContext as Model;
                model.serverStatuses.Oridashi.CurrentStatus = "started";
                poll = new System.Threading.Timer((o) =>
                {
                    Dispatcher.Invoke(async () =>
                    {
                        // check for any appointments
                        // var model = DataContext as Model;
                        try
                        {
                            model.serverStatuses.Oridashi.CurrentStatus = "now scanning";
                            await MessageProcessing.CheckAppointments(model);
                            model.serverStatuses.Oridashi.CurrentStatus = "running";
                        }
                        catch (Exception ex)
                        {
                            model.serverStatuses.Oridashi.CurrentStatus = $"Error: {ex.Message}";
                        }
                    });
                }, null, 0, model.Settings.PollIntervalSeconds * 1000);
            });
        }

        private async void buttonSmsOut_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as Model;
            var processor = new MessageProcessing();
            await processor.CheckForMessages(model);
        }

        private async void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as Model;
            await MessageProcessing.CheckAppointments(model);
        }
    }
}
