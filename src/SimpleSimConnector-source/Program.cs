// Copyright (c) 2026 Alex Nord.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// Commercial use is prohibited without written permission.
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SimpleSimConnector
{
    enum DEFINITIONS
    {
        Identity,
        Telemetry
    }

    enum REQUESTS
    {
        Identity,
        Telemetry
    }

    enum EVENTS
    {
        Frame
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct IdentityData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct Telemetry
    {
        public double latitude;
        public double longitude;
        public double altitudeMeters;
        public double groundSpeedMetersPerSecond;
        public double headingTrueDegrees;
        public double headingMagneticDegrees;
        public double onGround;
        public double verticalSpeedMetersPerSecond;
        public double pitchDegrees;
        public double bankDegrees;
        public double gForce;
        public double groundElevationMeters;
        public double landingRateMetersPerSecond;
        public double indicatedAirspeedMetersPerSecond;
        public double trueAirspeedMetersPerSecond;
        public double barberPoleAirspeedMetersPerSecond;
        public double parkingBrake;
        public double numFlapPositions;
        public double gearDown;
        public double lightNavigation;
        public double lightBeacon;
        public double lightStrobes;
        public double lightInstruments;
        public double lightLogo;
        public double lightCabin;
        public double com1Frequency;
        public double com2Frequency;
        public double nav1Frequency;
        public double nav2Frequency;
        public double transponderCode;
        public double engineType;
        public double itt1DegreesCelsius;
        public double itt2DegreesCelsius;
        public double antiIce1Enabled;
        public double antiIce2Enabled;
        public double exitOpen;
        public double apuPctRpm;
        public double apuSwitch;
        public double apuGeneratorActive;
        public double fuelWeightPerGallon;
        public double fuelTankCenterCapacityGallons;
        public double fuelTankCenterQuantityGallons;
        public double fuelTankCenter2CapacityGallons;
        public double fuelTankCenter2QuantityGallons;
        public double fuelTankCenter3CapacityGallons;
        public double fuelTankCenter3QuantityGallons;
        public double fuelTankLeftMainCapacityGallons;
        public double fuelTankLeftMainQuantityGallons;
        public double fuelTankLeftAuxCapacityGallons;
        public double fuelTankLeftAuxQuantityGallons;
        public double fuelTankLeftTipCapacityGallons;
        public double fuelTankLeftTipQuantityGallons;
        public double fuelTankRightMainCapacityGallons;
        public double fuelTankRightMainQuantityGallons;
        public double fuelTankRightAuxCapacityGallons;
        public double fuelTankRightAuxQuantityGallons;
        public double fuelTankRightTipCapacityGallons;
        public double fuelTankRightTipQuantityGallons;
        public double fuelTankExternal1CapacityGallons;
        public double fuelTankExternal1QuantityGallons;
        public double fuelTankExternal2CapacityGallons;
        public double fuelTankExternal2QuantityGallons;
        public double outsideAirTemperatureCelsius;
        public double visibilityMeters;
        public double windSpeedKnots;
        public double windDirectionDegrees;
        public double ambientPressureInchesHg;
        public double seaLevelPressurePascal;
        public double barometerSettingMillibars;
        public double cabinAltitudeMeters;
        public double yawDamperEnabled;
        public double flightDirectorEnabled;
        public double autopilotAirspeedHoldKnots;
        public double autopilotMachHoldMach;
        public double autopilotAltitudeHoldFeet;
        public double autopilotHeadingLockDegrees;
        public double autopilotPitchHoldRadians;
        public double autopilotVerticalSpeedHoldFeetPerMinute;
        public double autopilotAltitudeHoldActive;
        public double autopilotHeadingLockActive;
        public double autopilotAirspeedHoldActive;
        public double autopilotMachHoldActive;
        public double autopilotVerticalSpeedHoldActive;
    }

    class ConnectorSettings
    {
        public string BackendUrl = "http://127.0.0.1:5000/api/telemetry";
        public bool LocalApiEnabled = true;
        public int LocalApiPort = 4789;
        public bool WriteLocalTelemetryFile = true;

        public bool WaitForSim = true;
        public bool AutoExitWithSim = true;
        public int AutoExitDelaySeconds = 10;

        public string[] SimProcessNames = new string[]
        {
            "FlightSimulator2024",
            "FlightSimulator"
        };
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                string command = args[0].Trim().ToLowerInvariant();

                try
                {
                    if (command == "--install-autostart")
                    {
                        string changed = MsfsAutostartManager.Install(Application.ExecutablePath);

                        MessageBox.Show(
                            "Autostart installed." +
                            Environment.NewLine + Environment.NewLine +
                            changed,
                            "Simple Sim Connector",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return;
                    }

                    if (command == "--uninstall-autostart")
                    {
                        string changed = MsfsAutostartManager.Uninstall();

                        MessageBox.Show(
                            "Autostart removed." +
                            Environment.NewLine + Environment.NewLine +
                            changed,
                            "Simple Sim Connector",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Simple Sim Connector",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return;
                }
            }

            Application.Run(new ConnectorForm());
        }
    }

    public class ConnectorForm : Form
    {
        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect simconnect;

        private Label statusLabel;
        private Label latestLabel;
        private Label configLabel;

        private Button installAutostartButton;
        private Button removeAutostartButton;

        private ConnectorSettings settings;

        private TcpListener localApiServer;
        private CancellationTokenSource localApiCancellation;

        private System.Windows.Forms.Timer simWatcherTimer;
        private DateTime? simMissingSinceUtc;
        private bool hasEverConnectedToSim = false;

        private readonly object latestJsonLock = new object();
        private string latestJson;
        private bool backendConnected = false;
        private double latestFrameRate = double.NaN;
        private double latestSimulationRate = double.NaN;
        private string latestAircraftTitle = "";
        private readonly List<string> identityDefinitionNames = new List<string>();
        private readonly List<string> telemetryDefinitionNames = new List<string>();

        private static readonly HttpClient http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private readonly string appDataFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SimpleSimConnector"
            );

        private string ExeFolder
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private string ConfigPath
        {
            get { return Path.Combine(ExeFolder, "connector.ini"); }
        }

        private string LogPath
        {
            get { return Path.Combine(appDataFolder, "connector.log"); }
        }

        private string TelemetryPath
        {
            get { return Path.Combine(appDataFolder, "telemetry.ndjson"); }
        }

        public ConnectorForm()
        {
            Text = "Simple Sim Connector";
            Width = 760;
            Height = 230;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ShowIcon = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Keep default icon if Windows cannot extract the embedded one.
            }

            statusLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 18,
                Width = 700,
                Height = 24,
                Text = "Starting..."
            };

            latestLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 52,
                Width = 700,
                Height = 52,
                Text = "Waiting for telemetry..."
            };

            configLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 112,
                Width = 700,
                Height = 42,
                Text = ""
            };

            installAutostartButton = new Button
            {
                Left = 20,
                Top = 160,
                Width = 190,
                Height = 30,
                Text = "Install MSFS autostart"
            };

            removeAutostartButton = new Button
            {
                Left = 225,
                Top = 160,
                Width = 190,
                Height = 30,
                Text = "Remove MSFS autostart"
            };

            installAutostartButton.Click += InstallAutostartButton_Click;
            removeAutostartButton.Click += RemoveAutostartButton_Click;

            Controls.Add(statusLabel);
            Controls.Add(latestLabel);
            Controls.Add(configLabel);
            Controls.Add(installAutostartButton);
            Controls.Add(removeAutostartButton);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Directory.CreateDirectory(appDataFolder);

            settings = LoadSettings();

            Log("Simple Sim Connector started.");
            Log("Backend URL: " + settings.BackendUrl);
            Log("Local API enabled: " + settings.LocalApiEnabled);
            Log("Local API port: " + settings.LocalApiPort);
            Log("Wait for sim: " + settings.WaitForSim);
            Log("Auto exit with sim: " + settings.AutoExitWithSim);

            configLabel.Text =
                "POST: " + settings.BackendUrl + Environment.NewLine +
                "Local API: " + (settings.LocalApiEnabled
                    ? "http://0.0.0.0:" + settings.LocalApiPort + "/telemetry"
                    : "disabled");

            if (settings.LocalApiEnabled)
            {
                StartLocalApi();
            }

            if (settings.WaitForSim)
            {
                SetStatus("Waiting for Microsoft Flight Simulator 2024...");
                StartSimWatcher();
            }
            else
            {
                SetStatus("Connecting to Microsoft Flight Simulator...");
                ConnectToSim();
            }
        }

        private ConnectorSettings LoadSettings()
        {
            var loaded = new ConnectorSettings();

            if (!File.Exists(ConfigPath))
            {
                string defaultConfig =
                    "# Simple Sim Connector settings" + Environment.NewLine +
                    "backend_url=http://127.0.0.1:5000/api/telemetry" + Environment.NewLine +
                    Environment.NewLine +
                    "local_api_enabled=true" + Environment.NewLine +
                    "local_api_port=4789" + Environment.NewLine +
                    Environment.NewLine +
                    "write_local_telemetry_file=true" + Environment.NewLine +
                    Environment.NewLine +
                    "wait_for_sim=true" + Environment.NewLine +
                    "auto_exit_with_sim=true" + Environment.NewLine +
                    "auto_exit_delay_seconds=10" + Environment.NewLine +
                    "sim_process_names=FlightSimulator2024,FlightSimulator" + Environment.NewLine;

                File.WriteAllText(ConfigPath, defaultConfig);
            }

            foreach (string rawLine in File.ReadAllLines(ConfigPath))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int equalsIndex = line.IndexOf('=');

                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, equalsIndex).Trim().ToLowerInvariant();
                string value = line.Substring(equalsIndex + 1).Trim();

                if (key == "backend_url" && value.Length > 0)
                {
                    loaded.BackendUrl = value;
                }
                else if (key == "local_api_enabled")
                {
                    loaded.LocalApiEnabled = ParseBool(value, loaded.LocalApiEnabled);
                }
                else if (key == "local_api_port")
                {
                    int port;
                    if (int.TryParse(value, out port) && port > 0 && port <= 65535)
                    {
                        loaded.LocalApiPort = port;
                    }
                }
                else if (key == "write_local_telemetry_file")
                {
                    loaded.WriteLocalTelemetryFile = ParseBool(value, loaded.WriteLocalTelemetryFile);
                }
                else if (key == "wait_for_sim")
                {
                    loaded.WaitForSim = ParseBool(value, loaded.WaitForSim);
                }
                else if (key == "auto_exit_with_sim")
                {
                    loaded.AutoExitWithSim = ParseBool(value, loaded.AutoExitWithSim);
                }
                else if (key == "auto_exit_delay_seconds")
                {
                    int seconds;
                    if (int.TryParse(value, out seconds) && seconds >= 0 && seconds <= 300)
                    {
                        loaded.AutoExitDelaySeconds = seconds;
                    }
                }
                else if (key == "sim_process_names")
                {
                    string[] parts = value.Split(',');
                    var names = new System.Collections.Generic.List<string>();

                    foreach (string part in parts)
                    {
                        string cleaned = (part ?? "").Trim();

                        if (cleaned.Length > 0)
                        {
                            names.Add(cleaned);
                        }
                    }

                    if (names.Count > 0)
                    {
                        loaded.SimProcessNames = names.ToArray();
                    }
                }
            }

            return loaded;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            string normalized = (value ?? "").Trim().ToLowerInvariant();

            if (normalized == "true" || normalized == "yes" || normalized == "1" || normalized == "on")
            {
                return true;
            }

            if (normalized == "false" || normalized == "no" || normalized == "0" || normalized == "off")
            {
                return false;
            }

            return fallback;
        }

        private void StartSimWatcher()
        {
            simWatcherTimer = new System.Windows.Forms.Timer();
            simWatcherTimer.Interval = 2000;
            simWatcherTimer.Tick += SimWatcherTick;
            simWatcherTimer.Start();

            Log("Sim watcher started.");
            SimWatcherTick(null, EventArgs.Empty);
        }

        private void SimWatcherTick(object sender, EventArgs e)
        {
            bool simRunning = IsSimulatorRunning();

            if (simRunning)
            {
                simMissingSinceUtc = null;

                if (simconnect == null)
                {
                    SetStatus("MSFS detected. Connecting...");
                    ConnectToSim();
                }

                return;
            }

            if (simconnect != null)
            {
                Log("MSFS process no longer detected. Closing SimConnect.");
                SetStatus("MSFS closed. Disconnecting...");

                CloseSimConnect();
            }

            if (settings.AutoExitWithSim && hasEverConnectedToSim)
            {
                if (simMissingSinceUtc == null)
                {
                    simMissingSinceUtc = DateTime.UtcNow;
                    return;
                }

                double missingSeconds = (DateTime.UtcNow - simMissingSinceUtc.Value).TotalSeconds;

                if (missingSeconds >= settings.AutoExitDelaySeconds)
                {
                    Log("MSFS closed. Auto exiting connector.");
                    Close();
                    return;
                }

                SetStatus("MSFS closed. Exiting in " + Math.Ceiling(settings.AutoExitDelaySeconds - missingSeconds) + "s...");
            }
            else
            {
                SetStatus("Waiting for Microsoft Flight Simulator 2024...");
            }
        }

        private bool IsSimulatorRunning()
        {
            try
            {
                Process[] processes = Process.GetProcesses();

                foreach (Process process in processes)
                {
                    string processName = "";

                    try
                    {
                        processName = process.ProcessName;
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string configuredName in settings.SimProcessNames)
                    {
                        string wanted = (configuredName ?? "").Trim();

                        if (wanted.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            wanted = wanted.Substring(0, wanted.Length - 4);
                        }

                        if (string.Equals(processName, wanted, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed to check simulator process: " + ex.Message);
            }

            return false;
        }

        private void StartLocalApi()
        {
            try
            {
                localApiCancellation = new CancellationTokenSource();
                localApiServer = new TcpListener(IPAddress.Any, settings.LocalApiPort);
                localApiServer.Start();

                Log("Local API listening on 0.0.0.0:" + settings.LocalApiPort);

                Task.Run(() => LocalApiLoop(localApiCancellation.Token));
            }
            catch (Exception ex)
            {
                string message = "Failed to start local API: " + ex.Message;
                Log(message);
                SetStatus(message);
            }
        }

        private async Task LocalApiLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = await localApiServer.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleLocalApiClient(client));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log("Local API accept error: " + ex.Message);

                    try
                    {
                        if (client != null)
                        {
                            client.Close();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void HandleLocalApiClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string firstLine = "";
                    using (StringReader reader = new StringReader(request))
                    {
                        firstLine = reader.ReadLine() ?? "";
                    }

                    string body;
                    string status = "200 OK";
                    string contentType = "application/json";

                    if (firstLine.StartsWith("GET /telemetry ") || firstLine.StartsWith("GET /telemetry?"))
                    {
                        body = GetLatestJsonOrOfflineStatus();
                    }
                    else if (firstLine.StartsWith("GET /health "))
                    {
                        body =
                            "{" +
                                "\"online\":true," +
                                "\"name\":\"simple-sim-connector\"," +
                                "\"source\":\"simconnect-bridge\"" +
                            "}";
                    }
                    else if (firstLine.StartsWith("GET / "))
                    {
                        contentType = "text/plain; charset=utf-8";
                        body =
                            "Simple Sim Connector" + Environment.NewLine +
                            "GET /telemetry" + Environment.NewLine +
                            "GET /health" + Environment.NewLine;
                    }
                    else
                    {
                        status = "404 Not Found";
                        body =
                            "{" +
                                "\"error\":\"not_found\"," +
                                "\"message\":\"Use GET /telemetry or GET /health\"" +
                            "}";
                    }

                    WriteHttpResponse(stream, status, contentType, body);
                }
                catch (Exception ex)
                {
                    Log("Local API client error: " + ex.Message);
                }
            }
        }

        private void WriteHttpResponse(NetworkStream stream, string status, string contentType, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            string headers =
                "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Connection: close\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        private string GetLatestJsonOrOfflineStatus()
        {
            lock (latestJsonLock)
            {
                if (!string.IsNullOrWhiteSpace(latestJson))
                {
                    return latestJson;
                }
            }

            return BuildStatusJson(false, "No telemetry received yet");
        }

        private void ConnectToSim()
        {
            try
            {
                simconnect = new SimConnect(
                    "Simple Sim Connector",
                    Handle,
                    WM_USER_SIMCONNECT,
                    null,
                    0
                );

                simconnect.OnRecvOpen += OnSimConnected;
                simconnect.OnRecvQuit += OnSimQuit;
                simconnect.OnRecvException += OnSimException;
                simconnect.OnRecvEventFrame += OnFrameEvent;
                simconnect.OnRecvSimobjectData += OnTelemetryReceived;

                Log("SimConnect object created.");
                SetStatus("SimConnect object created. Waiting for MSFS...");
            }
            catch (COMException ex)
            {
                simconnect = null;

                string message = "Could not connect to MSFS yet. " + ex.Message;
                Log(message);
                SetStatus("Waiting for SimConnect...");
            }
            catch (Exception ex)
            {
                simconnect = null;

                string message = "Unexpected startup error: " + ex.Message;
                Log(message);
                SetStatus(message);

                SendStatusPayload(false, message);
            }
        }

        private void OnSimConnected(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            hasEverConnectedToSim = true;

            Log("Connected to MSFS.");
            SetStatus("Connected to MSFS. Requesting telemetry...");
            identityDefinitionNames.Clear();
            telemetryDefinitionNames.Clear();
            latestAircraftTitle = "";

            TelemetryBridgeCatalog.ValidateStructOrder<IdentityData>(TelemetryBridgeCatalog.IdentityDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<Telemetry>(TelemetryBridgeCatalog.NumericDefinitions);

            foreach (SimVarDefinition definition in TelemetryBridgeCatalog.IdentityDefinitions)
            {
                AddDefinition(DEFINITIONS.Identity, identityDefinitionNames, definition);
            }

            foreach (SimVarDefinition definition in TelemetryBridgeCatalog.NumericDefinitions)
            {
                AddDefinition(DEFINITIONS.Telemetry, telemetryDefinitionNames, definition);
            }

            simconnect.RegisterDataDefineStruct<IdentityData>(DEFINITIONS.Identity);
            simconnect.RegisterDataDefineStruct<Telemetry>(DEFINITIONS.Telemetry);
            simconnect.SubscribeToSystemEvent(EVENTS.Frame, "Frame");

            simconnect.RequestDataOnSimObject(
                REQUESTS.Identity,
                DEFINITIONS.Identity,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.ONCE,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );

            simconnect.RequestDataOnSimObject(
                REQUESTS.Telemetry,
                DEFINITIONS.Telemetry,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );

            Log("Telemetry request started.");
            SetStatus("Connected. Telemetry request started.");
        }

        private void AddDefinition(DEFINITIONS target, List<string> definitionNames, SimVarDefinition definition)
        {
            definitionNames.Add(definition.SimVarName);

            simconnect.AddToDataDefinition(
                target,
                definition.SimVarName,
                definition.SimConnectUnit,
                definition.IsString ? SIMCONNECT_DATATYPE.STRING256 : SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED
            );
        }

        private void OnFrameEvent(SimConnect sender, SIMCONNECT_RECV_EVENT_FRAME data)
        {
            latestFrameRate = data.fFrameRate;
            latestSimulationRate = data.fSimSpeed;
        }

        private async void OnTelemetryReceived(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            REQUESTS request = (REQUESTS)data.dwRequestID;

            if (request == REQUESTS.Identity)
            {
                try
                {
                    var identity = (IdentityData)data.dwData[0];
                    latestAircraftTitle = Clean(identity.title);
                }
                catch (Exception ex)
                {
                    Log("Identity handling error: " + ex.Message);
                }

                return;
            }

            if (request != REQUESTS.Telemetry)
            {
                return;
            }

            try
            {
                var telemetry = (Telemetry)data.dwData[0];

                string json = BuildBackendJson(
                    telemetry,
                    latestAircraftTitle,
                    connected: true,
                    lastError: null
                );

                lock (latestJsonLock)
                {
                    latestJson = json;
                }

                if (settings.WriteLocalTelemetryFile)
                {
                    AppendTelemetry(json);
                }

                UpdateLatestLabel(telemetry);

                await PostToBackend(json);
            }
            catch (Exception ex)
            {
                string message = "Telemetry handling error: " + ex.Message;
                Log(message);
                SetStatus(message);

                SendStatusPayload(false, message);
            }
        }

        private async Task PostToBackend(string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(settings.BackendUrl, content);
                backendConnected = response.IsSuccessStatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    Log("Backend returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                backendConnected = false;
                Log("Backend upload failed: " + ex.Message);
            }
        }

        private async void SendStatusPayload(bool connected, string lastError)
        {
            string json = BuildStatusJson(connected, lastError);

            lock (latestJsonLock)
            {
                latestJson = json;
            }

            if (settings != null && settings.WriteLocalTelemetryFile)
            {
                AppendTelemetry(json);
            }

            if (settings != null)
            {
                await PostToBackend(json);
            }
        }

        private void OnSimQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Log("MSFS quit.");
            SetStatus("MSFS quit. Connector disconnected.");

            SendStatusPayload(false, "MSFS quit");

            CloseSimConnect();
        }

        private void OnSimException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            string message = "SimConnect exception: " + data.dwException;
            if (data.dwIndex > 0)
            {
                int definitionIndex = (int)data.dwIndex - 1;
                if (definitionIndex >= 0 && definitionIndex < telemetryDefinitionNames.Count)
                {
                    message += " at telemetry definition #" + data.dwIndex + " (" + telemetryDefinitionNames[definitionIndex] + ")";
                }
                else
                {
                    message += " at telemetry definition #" + data.dwIndex;
                }
            }

            if (data.dwSendID > 0)
            {
                message += " sendId=" + data.dwSendID;
            }

            Log(message);
            SetStatus(message);

            SendStatusPayload(false, message);
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                try
                {
                    simconnect?.ReceiveMessage();
                }
                catch (Exception ex)
                {
                    Log("SimConnect receive error: " + ex.Message);
                    CloseSimConnect();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Log("Connector closed.");

            if (simWatcherTimer != null)
            {
                simWatcherTimer.Stop();
                simWatcherTimer.Dispose();
                simWatcherTimer = null;
            }

            StopLocalApi();
            CloseSimConnect();

            base.OnClosed(e);
        }

        private void StopLocalApi()
        {
            try
            {
                if (localApiCancellation != null)
                {
                    localApiCancellation.Cancel();
                }

                if (localApiServer != null)
                {
                    localApiServer.Stop();
                }
            }
            catch
            {
            }
        }

        private void CloseSimConnect()
        {
            try
            {
                simconnect?.Dispose();
            }
            catch
            {
            }

            simconnect = null;
        }

        private string BuildStatusJson(bool connectedToSimulator, string lastError)
        {
            var sb = new StringBuilder(2048);
            sb.Append("{");
            sb.Append("\"online\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"connected\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"latitude\":null,");
            sb.Append("\"longitude\":null,");
            sb.Append("\"altitude\":null,");
            sb.Append("\"groundspeed\":null,");
            sb.Append("\"heading\":null,");
            sb.Append("\"callsign\":\"SIMCONNECT\",");
            sb.Append("\"flight_plan\":{\"departure\":null,\"arrival\":null,\"aircraft_short\":\"UNKNOWN\"},");
            sb.Append("\"source\":\"simconnect-bridge\",");
            sb.Append("\"last_error\":").Append(JsonStringOrNull(lastError)).Append(",");
            sb.Append("\"Connected to Simulator\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"Connected to Backend\":").Append(Bool(backendConnected)).Append(",");
            sb.Append("\"simulator\":\"Microsoft Flight Simulator\",");
            sb.Append("\"aircraftType\":\"UNKNOWN\",");
            sb.Append("\"aircraftPath\":null,");
            sb.Append("\"fps\":").Append(Num(latestFrameRate)).Append(",");
            sb.Append("\"simulationRate\":").Append(Num(latestSimulationRate)).Append(",");
            sb.Append("\"position\":{\"latitude\":null,\"longitude\":null},");
            sb.Append("\"headingTrueDegrees\":null,");
            sb.Append("\"headingMagneticDegrees\":null,");
            sb.Append("\"gForce\":null,");
            sb.Append("\"altitudeMeters\":null,");
            sb.Append("\"pitchDegrees\":null,");
            sb.Append("\"bankDegrees\":null,");
            sb.Append("\"groundElevationMeters\":null,");
            sb.Append("\"landingRateMetersPerSecond\":null,");
            sb.Append("\"onGround\":null,");
            sb.Append("\"indicatedAirspeedMetersPerSecond\":null,");
            sb.Append("\"trueAirspeedMetersPerSecond\":null,");
            sb.Append("\"barberPoleAirspeedMetersPerSecond\":null,");
            sb.Append("\"groundSpeedMetersPerSecond\":null,");
            sb.Append("\"verticalSpeedMetersPerSecond\":null,");
            sb.Append("\"parkingBrake\":null,");
            sb.Append("\"numFlapPositions\":null,");
            sb.Append("\"gearDown\":null,");
            sb.Append("\"doorsOpen\":null,");
            sb.Append("\"lights\":{\"navigation\":null,\"beacon\":null,\"strobes\":null,\"instruments\":null,\"logo\":null,\"cabin\":null},");
            sb.Append("\"com1\":null,");
            sb.Append("\"com2\":null,");
            sb.Append("\"nav1\":null,");
            sb.Append("\"nav2\":null,");
            sb.Append("\"transponder\":null,");
            sb.Append("\"engineType\":null,");
            sb.Append("\"engines\":[{\"ittDegreesCelsius\":null,\"antiIce\":{\"antiIceEnabled\":null}},{\"ittDegreesCelsius\":null,\"antiIce\":{\"antiIceEnabled\":null}}],");
            sb.Append("\"fuelTanks\":[");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER\",\"capacityKgs\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER_2\",\"capacityKgs\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER_3\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_MAIN\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_AUX\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_TIP\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_MAIN\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_AUX\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_TIP\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_EXTERNAL_1\"},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_EXTERNAL_2\"}");
            sb.Append("],");
            sb.Append("\"outsideAirTemperatureCelsius\":null,");
            sb.Append("\"visibilityKm\":null,");
            sb.Append("\"windSpeedMetersPerSecond\":null,");
            sb.Append("\"windDirectionDegrees\":null,");
            sb.Append("\"ambientPressurePascal\":null,");
            sb.Append("\"seaLevelPressurePascal\":null,");
            sb.Append("\"barometerSettingPascal\":null,");
            sb.Append("\"apu\":{\"status\":\"\"},");
            sb.Append("\"pressurization\":{\"cabinAltitudeMeters\":null},");
            sb.Append("\"flightControls\":{\"yawDamperEnabled\":[null]},");
            sb.Append("\"autopilot\":{\"flightDirectorEnabled\":null,\"modes\":[],\"airspeedHoldMetersPerSecond\":null,\"machHoldMach\":null,\"altitudeHoldMeters\":null,\"altitudeArmMeters\":null,\"headingLockDegrees\":null,\"pitchHoldDegrees\":null,\"verticalSpeedHoldMetersPerSecond\":null},");
            sb.Append("\"aircraftInformation\":{\"simulatorPath\":\"\",\"packagePath\":\"\",\"version\":\"1.1.0\"},");
            sb.Append("\"diagnostics\":{\"rejected\":[]}");
            sb.Append("}");
            return sb.ToString();
        }

        private string BuildBackendJson(Telemetry t, string aircraftTitle, bool connected, string lastError)
        {
            var rejected = new List<TelemetryRejectedValue>();
            string callsign = BuildCallsign();
            string aircraftShort = BuildAircraftShort(aircraftTitle);
            double? latitude = TelemetryMath.ValidateNumeric("latitude", t.latitude, t.latitude, rejected);
            double? longitude = TelemetryMath.ValidateNumeric("longitude", t.longitude, t.longitude, rejected);
            double? altitudeMeters = TelemetryMath.ValidateNumeric("altitudeMeters", t.altitudeMeters, t.altitudeMeters, rejected);
            double convertedGroundspeed = TelemetryMath.KnotsToMetersPerSecond(t.groundSpeedMetersPerSecond);
            double? groundspeed = TelemetryMath.ValidateNumeric("groundSpeedMetersPerSecond", t.groundSpeedMetersPerSecond, ZeroNoise(convertedGroundspeed, 0.01), rejected);
            double? heading = TelemetryMath.ValidateNumeric("headingTrueDegrees", t.headingTrueDegrees, NormalizeHeading(t.headingTrueDegrees), rejected);
            double? headingMagnetic = TelemetryMath.ValidateNumeric("headingMagneticDegrees", t.headingMagneticDegrees, NormalizeHeading(t.headingMagneticDegrees), rejected);
            double convertedVerticalSpeed = TelemetryMath.FeetPerSecondToMetersPerSecond(t.verticalSpeedMetersPerSecond);
            double? verticalSpeed = TelemetryMath.ValidateNumeric("verticalSpeedMetersPerSecond", t.verticalSpeedMetersPerSecond, ZeroNoise(convertedVerticalSpeed, 0.001), rejected);
            string engineType = MapEngineType(t.engineType, aircraftShort);
            string simulator = "Microsoft Flight Simulator";
            List<string> autopilotModes = BuildAutopilotModes(t);
            string apuStatus = BuildApuStatus(t);
            string flightDirectorEnabled = JsonBoolOrNull(t.flightDirectorEnabled);
            double? visibilityKm = TelemetryMath.ValidateNumeric("visibilityKm", t.visibilityMeters, TelemetryMath.MetersToKilometers(t.visibilityMeters), rejected);
            double? windSpeedMetersPerSecond = TelemetryMath.ValidateNumeric("windSpeedMetersPerSecond", t.windSpeedKnots, TelemetryMath.KnotsToMetersPerSecond(t.windSpeedKnots), rejected);
            double? windDirectionDegrees = TelemetryMath.ValidateNumeric("windDirectionDegrees", t.windDirectionDegrees, NormalizeHeading(t.windDirectionDegrees), rejected);
            double? ambientPressurePascal = TelemetryMath.ValidateNumeric("ambientPressurePascal", t.ambientPressureInchesHg, TelemetryMath.InchesHgToPascals(t.ambientPressureInchesHg), rejected);
            double? seaLevelPressurePascal = TelemetryMath.ValidateNumeric("seaLevelPressurePascal", t.seaLevelPressurePascal, TelemetryMath.MillibarsToPascals(t.seaLevelPressurePascal), rejected);
            double? barometerSettingPascal = TelemetryMath.ValidateNumeric("barometerSettingPascal", t.barometerSettingMillibars, TelemetryMath.MillibarsToPascals(t.barometerSettingMillibars), rejected);
            double? outsideAirTemperatureCelsius = TelemetryMath.ValidateNumeric("outsideAirTemperatureCelsius", t.outsideAirTemperatureCelsius, t.outsideAirTemperatureCelsius, rejected);
            double? autopilotAirspeedHoldMetersPerSecond = TelemetryMath.ValidateNumeric("autopilot.airspeedHoldMetersPerSecond", t.autopilotAirspeedHoldKnots, TelemetryMath.KnotsToMetersPerSecond(t.autopilotAirspeedHoldKnots), rejected);
            double? autopilotMachHoldMach = TelemetryMath.ValidateNumeric("autopilot.machHoldMach", t.autopilotMachHoldMach, t.autopilotMachHoldMach, rejected);
            double? autopilotAltitudeHoldMeters = TelemetryMath.ValidateNumeric("autopilot.altitudeHoldMeters", t.autopilotAltitudeHoldFeet, TelemetryMath.FeetToMeters(t.autopilotAltitudeHoldFeet), rejected);
            double? autopilotHeadingLockDegrees = TelemetryMath.ValidateNumeric("autopilot.headingLockDegrees", t.autopilotHeadingLockDegrees, NormalizeHeading(t.autopilotHeadingLockDegrees), rejected);
            double? autopilotPitchHoldDegrees = TelemetryMath.ValidateNumeric("autopilot.pitchHoldDegrees", t.autopilotPitchHoldRadians, TelemetryMath.RadiansToDegrees(t.autopilotPitchHoldRadians), rejected);
            double? autopilotVerticalSpeedHoldMetersPerSecond = TelemetryMath.ValidateNumeric("autopilot.verticalSpeedHoldMetersPerSecond", t.autopilotVerticalSpeedHoldFeetPerMinute, TelemetryMath.FeetPerMinuteToMetersPerSecond(t.autopilotVerticalSpeedHoldFeetPerMinute), rejected);
            double? indicatedAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("indicatedAirspeedMetersPerSecond", t.indicatedAirspeedMetersPerSecond, TelemetryMath.KnotsToMetersPerSecond(t.indicatedAirspeedMetersPerSecond), rejected);
            double? trueAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("trueAirspeedMetersPerSecond", t.trueAirspeedMetersPerSecond, TelemetryMath.KnotsToMetersPerSecond(t.trueAirspeedMetersPerSecond), rejected);
            double? barberPoleAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("barberPoleAirspeedMetersPerSecond", t.barberPoleAirspeedMetersPerSecond, TelemetryMath.KnotsToMetersPerSecond(t.barberPoleAirspeedMetersPerSecond), rejected);
            string com1 = TelemetryMath.ValidateFrequencyString("com1", t.com1Frequency, TelemetryMath.FormatComFrequencyBcd16(t.com1Frequency), rejected);
            string com2 = TelemetryMath.ValidateFrequencyString("com2", t.com2Frequency, TelemetryMath.FormatComFrequencyBcd16(t.com2Frequency), rejected);
            string nav1 = TelemetryMath.ValidateFrequencyString("nav1", t.nav1Frequency, TelemetryMath.FormatFrequency(t.nav1Frequency), rejected);
            string nav2 = TelemetryMath.ValidateFrequencyString("nav2", t.nav2Frequency, TelemetryMath.FormatFrequency(t.nav2Frequency), rejected);

            var sb = new StringBuilder(4096);
            sb.Append("{");
            sb.Append("\"online\":").Append(Bool(connected)).Append(",");
            sb.Append("\"connected\":").Append(Bool(connected)).Append(",");
            sb.Append("\"latitude\":").Append(Num(latitude)).Append(",");
            sb.Append("\"longitude\":").Append(Num(longitude)).Append(",");
            sb.Append("\"altitude\":").Append(Num(altitudeMeters)).Append(",");
            sb.Append("\"groundspeed\":").Append(Num(groundspeed)).Append(",");
            sb.Append("\"heading\":").Append(Num(heading)).Append(",");
            sb.Append("\"callsign\":\"").Append(Escape(callsign)).Append("\",");
            sb.Append("\"flight_plan\":{");
            sb.Append("\"departure\":null,");
            sb.Append("\"arrival\":null,");
            sb.Append("\"aircraft_short\":\"").Append(Escape(aircraftShort)).Append("\"");
            sb.Append("},");
            sb.Append("\"source\":\"simconnect-bridge\",");
            sb.Append("\"last_error\":").Append(JsonStringOrNull(lastError)).Append(",");

            sb.Append("\"Connected to Simulator\":").Append(Bool(connected)).Append(",");
            sb.Append("\"Connected to Backend\":").Append(Bool(backendConnected)).Append(",");
            sb.Append("\"simulator\":\"").Append(Escape(simulator)).Append("\",");
            sb.Append("\"aircraftType\":\"").Append(Escape(aircraftShort)).Append("\",");
            sb.Append("\"aircraftPath\":null,");
            sb.Append("\"fps\":").Append(Num(latestFrameRate)).Append(",");
            sb.Append("\"simulationRate\":").Append(Num(latestSimulationRate)).Append(",");
            sb.Append("\"position\":{");
            sb.Append("\"latitude\":").Append(Num(latitude)).Append(",");
            sb.Append("\"longitude\":").Append(Num(longitude));
            sb.Append("},");
            sb.Append("\"headingTrueDegrees\":").Append(Num(heading)).Append(",");
            sb.Append("\"headingMagneticDegrees\":").Append(Num(headingMagnetic)).Append(",");
            sb.Append("\"gForce\":").Append(Num(t.gForce)).Append(",");
            sb.Append("\"altitudeMeters\":").Append(Num(altitudeMeters)).Append(",");
            sb.Append("\"pitchDegrees\":").Append(Num(t.pitchDegrees)).Append(",");
            sb.Append("\"bankDegrees\":").Append(Num(t.bankDegrees)).Append(",");
            sb.Append("\"groundElevationMeters\":").Append(Num(t.groundElevationMeters)).Append(",");
            sb.Append("\"landingRateMetersPerSecond\":").Append(Num(t.landingRateMetersPerSecond)).Append(",");
            sb.Append("\"onGround\":").Append(JsonBoolOrNull(t.onGround)).Append(",");
            sb.Append("\"indicatedAirspeedMetersPerSecond\":").Append(Num(indicatedAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"trueAirspeedMetersPerSecond\":").Append(Num(trueAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"barberPoleAirspeedMetersPerSecond\":").Append(Num(barberPoleAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"groundSpeedMetersPerSecond\":").Append(Num(groundspeed)).Append(",");
            sb.Append("\"verticalSpeedMetersPerSecond\":").Append(Num(verticalSpeed)).Append(",");
            sb.Append("\"parkingBrake\":").Append(JsonBoolOrNull(t.parkingBrake)).Append(",");
            sb.Append("\"numFlapPositions\":").Append(Num(t.numFlapPositions)).Append(",");
            sb.Append("\"gearDown\":").Append(JsonBoolOrNull(t.gearDown)).Append(",");
            sb.Append("\"doorsOpen\":").Append(JsonBoolOrNull(t.exitOpen)).Append(",");
            sb.Append("\"lights\":{");
            sb.Append("\"navigation\":").Append(JsonBoolOrNull(t.lightNavigation)).Append(",");
            sb.Append("\"beacon\":").Append(JsonBoolOrNull(t.lightBeacon)).Append(",");
            sb.Append("\"strobes\":").Append(JsonBoolOrNull(t.lightStrobes)).Append(",");
            sb.Append("\"instruments\":").Append(JsonBoolOrNull(t.lightInstruments)).Append(",");
            sb.Append("\"logo\":").Append(JsonBoolOrNull(t.lightLogo)).Append(",");
            sb.Append("\"cabin\":").Append(JsonBoolOrNull(t.lightCabin));
            sb.Append("},");
            sb.Append("\"com1\":").Append(JsonStringOrNull(com1)).Append(",");
            sb.Append("\"com2\":").Append(JsonStringOrNull(com2)).Append(",");
            sb.Append("\"nav1\":").Append(JsonStringOrNull(nav1)).Append(",");
            sb.Append("\"nav2\":").Append(JsonStringOrNull(nav2)).Append(",");
            sb.Append("\"transponder\":").Append(JsonStringOrNull(FormatTransponder(t.transponderCode))).Append(",");
            sb.Append("\"engineType\":").Append(JsonStringOrNull(engineType)).Append(",");
            sb.Append("\"engines\":[");
            sb.Append("{\"ittDegreesCelsius\":").Append(Num(t.itt1DegreesCelsius)).Append(",\"antiIce\":{\"antiIceEnabled\":").Append(JsonBoolOrNull(t.antiIce1Enabled)).Append("}},");
            sb.Append("{\"ittDegreesCelsius\":").Append(Num(t.itt2DegreesCelsius)).Append(",\"antiIce\":{\"antiIceEnabled\":").Append(JsonBoolOrNull(t.antiIce2Enabled)).Append("}}");
            sb.Append("],");
            sb.Append("\"fuelTanks\":[");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER", t.fuelTankCenterCapacityGallons, t.fuelTankCenterQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER_2", t.fuelTankCenter2CapacityGallons, t.fuelTankCenter2QuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER_3", t.fuelTankCenter3CapacityGallons, t.fuelTankCenter3QuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_MAIN", t.fuelTankLeftMainCapacityGallons, t.fuelTankLeftMainQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_AUX", t.fuelTankLeftAuxCapacityGallons, t.fuelTankLeftAuxQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_TIP", t.fuelTankLeftTipCapacityGallons, t.fuelTankLeftTipQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_MAIN", t.fuelTankRightMainCapacityGallons, t.fuelTankRightMainQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_AUX", t.fuelTankRightAuxCapacityGallons, t.fuelTankRightAuxQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_TIP", t.fuelTankRightTipCapacityGallons, t.fuelTankRightTipQuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_EXTERNAL_1", t.fuelTankExternal1CapacityGallons, t.fuelTankExternal1QuantityGallons, t.fuelWeightPerGallon, rejected)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_EXTERNAL_2", t.fuelTankExternal2CapacityGallons, t.fuelTankExternal2QuantityGallons, t.fuelWeightPerGallon, rejected));
            sb.Append("],");
            sb.Append("\"outsideAirTemperatureCelsius\":").Append(Num(outsideAirTemperatureCelsius)).Append(",");
            sb.Append("\"visibilityKm\":").Append(Num(visibilityKm)).Append(",");
            sb.Append("\"windSpeedMetersPerSecond\":").Append(Num(windSpeedMetersPerSecond)).Append(",");
            sb.Append("\"windDirectionDegrees\":").Append(Num(windDirectionDegrees)).Append(",");
            sb.Append("\"ambientPressurePascal\":").Append(Num(ambientPressurePascal)).Append(",");
            sb.Append("\"seaLevelPressurePascal\":").Append(Num(seaLevelPressurePascal)).Append(",");
            sb.Append("\"barometerSettingPascal\":").Append(Num(barometerSettingPascal)).Append(",");
            sb.Append("\"apu\":{\"status\":").Append(JsonStringOrNull(apuStatus)).Append("},");
            sb.Append("\"pressurization\":{\"cabinAltitudeMeters\":").Append(Num(t.cabinAltitudeMeters)).Append("},");
            sb.Append("\"flightControls\":{\"yawDamperEnabled\":[").Append(JsonBoolOrNull(t.yawDamperEnabled)).Append("]},");
            sb.Append("\"autopilot\":{");
            sb.Append("\"flightDirectorEnabled\":").Append(flightDirectorEnabled).Append(",");
            sb.Append("\"modes\":").Append(JsonStringArray(autopilotModes)).Append(",");
            sb.Append("\"airspeedHoldMetersPerSecond\":").Append(Num(autopilotAirspeedHoldMetersPerSecond)).Append(",");
            sb.Append("\"machHoldMach\":").Append(Num(autopilotMachHoldMach)).Append(",");
            sb.Append("\"altitudeHoldMeters\":").Append(Num(autopilotAltitudeHoldMeters)).Append(",");
            sb.Append("\"altitudeArmMeters\":").Append(Num(autopilotAltitudeHoldMeters)).Append(",");
            sb.Append("\"headingLockDegrees\":").Append(Num(autopilotHeadingLockDegrees)).Append(",");
            sb.Append("\"pitchHoldDegrees\":").Append(Num(autopilotPitchHoldDegrees)).Append(",");
            sb.Append("\"verticalSpeedHoldMetersPerSecond\":").Append(Num(autopilotVerticalSpeedHoldMetersPerSecond));
            sb.Append("},");
            sb.Append("\"aircraftInformation\":{");
            sb.Append("\"simulatorPath\":\"\",");
            sb.Append("\"packagePath\":\"\",");
            sb.Append("\"version\":\"1.1.0\"");
            sb.Append("},");
            sb.Append("\"diagnostics\":").Append(BuildDiagnosticsJson(rejected));
            sb.Append("}");
            LogRejectedTelemetry(rejected);
            return sb.ToString();
        }

        private string BuildCallsign()
        {
            return "SIMCONNECT";
        }

        private string BuildAircraftShort(string aircraftTitle)
        {
            string title = Clean(aircraftTitle);
            string combined = title.ToUpperInvariant();

            if (combined.Contains("A20N")) return "A20N";
            if (combined.Contains("A320")) return "A320";
            if (combined.Contains("A319")) return "A319";
            if (combined.Contains("A321")) return "A321";
            if (combined.Contains("A339") || combined.Contains("A330")) return "A339";
            if (combined.Contains("A359") || combined.Contains("A350")) return "A359";

            if (combined.Contains("B738") || combined.Contains("737-800") || combined.Contains("737")) return "B738";
            if (combined.Contains("B739") || combined.Contains("737-900")) return "B739";
            if (combined.Contains("B789") || combined.Contains("787-9")) return "B789";
            if (combined.Contains("B788") || combined.Contains("787-8")) return "B788";
            if (combined.Contains("B77W") || combined.Contains("777-300")) return "B77W";
            if (combined.Contains("B772") || combined.Contains("777-200")) return "B772";
            if (combined.Contains("B748") || combined.Contains("747-8")) return "B748";
            if (combined.Contains("B744") || combined.Contains("747-400")) return "B744";

            if (combined.Contains("C172") || combined.Contains("172")) return "C172";
            if (combined.Contains("TBM")) return "TBM9";
            if (combined.Contains("CJ4")) return "C25C";
            if (combined.Contains("DA40")) return "DA40";
            if (combined.Contains("DA62")) return "DA62";

            return "UNKNOWN";
        }

        private void AppendTelemetry(string json)
        {
            File.AppendAllText(TelemetryPath, json + Environment.NewLine);
        }

        private void Log(string message)
        {
            Directory.CreateDirectory(appDataFolder);

            File.AppendAllText(
                LogPath,
                DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine
            );
        }

        private void SetStatus(string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.BeginInvoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        private void UpdateLatestLabel(Telemetry t)
        {
            string callsign = BuildCallsign();
            string aircraftShort = BuildAircraftShort(latestAircraftTitle);
            double altitudeFeet = TelemetryMath.MetersToFeet(t.altitudeMeters);
            double groundspeedKnots = ZeroNoise(t.groundSpeedMetersPerSecond, 0.01);

            string text =
                callsign + " / " + aircraftShort + Environment.NewLine +
                "Lat " + Num(t.latitude) +
                " Lon " + Num(t.longitude) +
                " Alt " + Math.Round(altitudeFeet).ToString(CultureInfo.InvariantCulture) + " ft" +
                " GS " + Math.Round(groundspeedKnots).ToString(CultureInfo.InvariantCulture) + " kt" +
                " HDG " + Math.Round(NormalizeHeading(t.headingTrueDegrees)).ToString(CultureInfo.InvariantCulture);

            if (latestLabel.InvokeRequired)
            {
                latestLabel.BeginInvoke(new Action(() => latestLabel.Text = text));
            }
            else
            {
                latestLabel.Text = text;
            }
        }

        private void InstallAutostartButton_Click(object sender, EventArgs e)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string changedFiles = MsfsAutostartManager.Install(exePath);

                MessageBox.Show(
                    "Simple Sim Connector autostart has been installed." +
                    Environment.NewLine + Environment.NewLine +
                    "Updated:" +
                    Environment.NewLine +
                    changedFiles +
                    Environment.NewLine + Environment.NewLine +
                    "MSFS 2024 should now launch the connector automatically.",
                    "Autostart installed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Log("Autostart installed: " + changedFiles.Replace(Environment.NewLine, " | "));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Autostart install failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Log("Autostart install failed: " + ex.Message);
            }
        }

        private void RemoveAutostartButton_Click(object sender, EventArgs e)
        {
            try
            {
                string changedFiles = MsfsAutostartManager.Uninstall();

                MessageBox.Show(
                    "Autostart removal complete." +
                    Environment.NewLine + Environment.NewLine +
                    changedFiles,
                    "Autostart removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Log("Autostart removed: " + changedFiles.Replace(Environment.NewLine, " | "));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Autostart removal failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Log("Autostart removal failed: " + ex.Message);
            }
        }

        private static string Clean(string value)
        {
            return (value ?? "").Trim();
        }

        private static double ZeroNoise(double value, double threshold)
        {
            return Math.Abs(value) < threshold ? 0 : value;
        }

        private static double NormalizeHeading(double heading)
        {
            if (double.IsNaN(heading) || double.IsInfinity(heading))
            {
                return heading;
            }

            heading = heading % 360.0;

            if (heading < 0)
            {
                heading += 360.0;
            }

            return heading;
        }

        private static string Num(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "null";
            }

            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static string Num(double? value)
        {
            return value.HasValue ? Num(value.Value) : "null";
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static bool IsTruthy(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.5;
        }

        private static string JsonBoolOrNull(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "null";
            }

            return IsTruthy(value) ? "true" : "false";
        }

        private static double CombineAnyTrue(params double[] values)
        {
            bool sawFinite = false;

            foreach (double value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    continue;
                }

                sawFinite = true;
                if (IsTruthy(value))
                {
                    return 1.0;
                }
            }

            return sawFinite ? 0.0 : double.NaN;
        }

        private string BuildDiagnosticsJson(IList<TelemetryRejectedValue> rejected)
        {
            var sb = new StringBuilder();
            sb.Append("{\"rejected\":[");

            for (int i = 0; i < rejected.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                TelemetryRejectedValue item = rejected[i];
                sb.Append("{");
                sb.Append("\"jsonField\":").Append(JsonStringOrNull(item.JsonField)).Append(",");
                sb.Append("\"simVarName\":").Append(JsonStringOrNull(item.SimVarName)).Append(",");
                sb.Append("\"requestedUnit\":").Append(JsonStringOrNull(item.RequestedUnit)).Append(",");
                sb.Append("\"rawValue\":").Append(JsonStringOrNull(item.RawValue)).Append(",");
                sb.Append("\"convertedValue\":").Append(JsonStringOrNull(item.ConvertedValue)).Append(",");
                sb.Append("\"sanityRange\":").Append(JsonStringOrNull(item.SanityRange)).Append(",");
                sb.Append("\"reason\":").Append(JsonStringOrNull(item.Reason)).Append(",");
                sb.Append("\"action\":").Append(JsonStringOrNull(item.Action));
                sb.Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private void LogRejectedTelemetry(IList<TelemetryRejectedValue> rejected)
        {
            foreach (TelemetryRejectedValue item in rejected)
            {
                Log(
                    "Rejected telemetry field " +
                    (item.JsonField ?? "unknown") +
                    " simvar=" + (item.SimVarName ?? "") +
                    " requestedUnit=" + (item.RequestedUnit ?? "") +
                    " raw=" + (item.RawValue ?? "null") +
                    " converted=" + (item.ConvertedValue ?? "null") +
                    " sanity=" + (item.SanityRange ?? "") +
                    " reason=" + (item.Reason ?? "") +
                    " action=" + (item.Action ?? ""));
            }
        }

        private static string FormatFrequency(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return null;
            }

            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatComFrequencyBcd16(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return null;
            }

            int bcd = (int)Math.Round(value);
            int nibble1 = (bcd >> 12) & 0xF;
            int nibble2 = (bcd >> 8) & 0xF;
            int nibble3 = (bcd >> 4) & 0xF;
            int nibble4 = bcd & 0xF;

            double mhz = 100.0 + (nibble1 * 10.0) + nibble2 + (nibble3 / 10.0) + (nibble4 / 100.0);
            return mhz.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatTransponder(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                return null;
            }

            int rounded = (int)Math.Round(value);
            return rounded.ToString("0000", CultureInfo.InvariantCulture);
        }

        private static string MapEngineType(double value, string aircraftShort)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return InferEngineTypeFromAircraft(aircraftShort);
            }

            switch ((int)Math.Round(value))
            {
                case 0: return "ENGINE_TYPE_PISTON";
                case 1: return "ENGINE_TYPE_JET";
                case 2: return "ENGINE_TYPE_NONE";
                case 3: return "ENGINE_TYPE_HELO_TURBINE";
                case 4: return "ENGINE_TYPE_UNSUPPORTED";
                case 5: return "ENGINE_TYPE_TURBOPROP";
                case 6: return "ENGINE_TYPE_ELECTRIC";
                default: return InferEngineTypeFromAircraft(aircraftShort) ?? "ENGINE_TYPE_UNKNOWN";
            }
        }

        private static string InferEngineTypeFromAircraft(string aircraftShort)
        {
            string upper = Clean(aircraftShort).ToUpperInvariant();

            if (upper.StartsWith("A3") || upper.StartsWith("B7") || upper.StartsWith("B78") || upper.StartsWith("B77") || upper.StartsWith("B74"))
            {
                return "ENGINE_TYPE_JET";
            }

            if (upper.StartsWith("C172") || upper.StartsWith("DA40") || upper.StartsWith("DA62"))
            {
                return "ENGINE_TYPE_PISTON";
            }

            if (upper.StartsWith("TBM"))
            {
                return "ENGINE_TYPE_TURBOPROP";
            }

            return null;
        }

        private static string BuildApuStatus(Telemetry t)
        {
            if (double.IsNaN(t.apuPctRpm) || double.IsInfinity(t.apuPctRpm))
            {
                return null;
            }

            if (IsTruthy(t.apuGeneratorActive) || t.apuPctRpm >= 95.0)
            {
                return "running";
            }

            if (IsTruthy(t.apuSwitch) || t.apuPctRpm > 1.0)
            {
                return "starting";
            }

            return "off";
        }

        private string BuildFuelTankJson(
            string position,
            double capacityGallons,
            double quantityGallons,
            double fuelWeightPerGallon,
            IList<TelemetryRejectedValue> rejected)
        {
            var sb = new StringBuilder();
            sb.Append("{\"position\":\"").Append(Escape(position)).Append("\"");

            double? capacityKgs = TelemetryMath.ValidateNumeric(
                "fuelTanks[*].capacityKgs",
                capacityGallons,
                TelemetryMath.GallonsToKilograms(capacityGallons, fuelWeightPerGallon),
                rejected);
            if (capacityKgs.HasValue)
            {
                sb.Append(",\"capacityKgs\":").Append(Num(capacityKgs));
            }

            double? percentageFilled = TelemetryMath.ValidateNumeric(
                "fuelTanks[*].percentageFilled",
                quantityGallons,
                TelemetryMath.QuantityToPercent(quantityGallons, capacityGallons),
                rejected);
            if (percentageFilled.HasValue)
            {
                sb.Append(",\"percentageFilled\":").Append(Num(percentageFilled));
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static double GallonsToKilograms(double gallons, double poundsPerGallon)
        {
            if (double.IsNaN(gallons) || double.IsInfinity(gallons) || gallons < 0)
            {
                return double.NaN;
            }

            if (double.IsNaN(poundsPerGallon) || double.IsInfinity(poundsPerGallon) || poundsPerGallon <= 0)
            {
                return double.NaN;
            }

            return gallons * poundsPerGallon * 0.45359237;
        }

        private static double QuantityToPercent(double quantityGallons, double capacityGallons)
        {
            if (double.IsNaN(quantityGallons) || double.IsInfinity(quantityGallons) || quantityGallons < 0)
            {
                return double.NaN;
            }

            if (double.IsNaN(capacityGallons) || double.IsInfinity(capacityGallons) || capacityGallons <= 0)
            {
                return double.NaN;
            }

            return (quantityGallons / capacityGallons) * 100.0;
        }

        private static double MetersToFeet(double meters)
        {
            if (double.IsNaN(meters) || double.IsInfinity(meters))
            {
                return meters;
            }

            return meters * 3.280839895;
        }

        private static double FeetToMeters(double feet)
        {
            if (double.IsNaN(feet) || double.IsInfinity(feet))
            {
                return feet;
            }

            return feet / 3.280839895;
        }

        private static double MetersPerSecondToKnots(double metersPerSecond)
        {
            if (double.IsNaN(metersPerSecond) || double.IsInfinity(metersPerSecond))
            {
                return metersPerSecond;
            }

            return metersPerSecond * 1.94384449;
        }

        private static double KnotsToMetersPerSecond(double knots)
        {
            if (double.IsNaN(knots) || double.IsInfinity(knots))
            {
                return knots;
            }

            return knots / 1.94384449;
        }

        private static double FeetPerMinuteToMetersPerSecond(double feetPerMinute)
        {
            if (double.IsNaN(feetPerMinute) || double.IsInfinity(feetPerMinute))
            {
                return feetPerMinute;
            }

            return feetPerMinute * 0.00508;
        }

        private static double RadiansToDegrees(double radians)
        {
            if (double.IsNaN(radians) || double.IsInfinity(radians))
            {
                return radians;
            }

            return radians * (180.0 / Math.PI);
        }

        private static double MetersToKilometers(double meters)
        {
            if (double.IsNaN(meters) || double.IsInfinity(meters))
            {
                return meters;
            }

            return meters / 1000.0;
        }

        private static double InchesHgToPascals(double inchesHg)
        {
            if (double.IsNaN(inchesHg) || double.IsInfinity(inchesHg))
            {
                return inchesHg;
            }

            return inchesHg * 3386.389;
        }

        private static double MillibarsToPascals(double millibars)
        {
            if (double.IsNaN(millibars) || double.IsInfinity(millibars))
            {
                return millibars;
            }

            return millibars * 100.0;
        }

        private static List<string> BuildAutopilotModes(Telemetry t)
        {
            var modes = new List<string>();

            if (IsTruthy(t.autopilotVerticalSpeedHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_VERTICAL_SPEED_HOLD");
            }

            if (IsTruthy(t.autopilotAltitudeHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_ALTITUDE_HOLD");
            }

            if (IsTruthy(t.autopilotHeadingLockActive))
            {
                modes.Add("AUTOPILOT_MODE_HEADING_HOLD");
            }

            if (IsTruthy(t.autopilotAirspeedHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_AIRSPEED_HOLD");
            }

            if (IsTruthy(t.autopilotMachHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_MACH_HOLD");
            }

            return modes;
        }

        private static string JsonStringArray(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "[]";
            }

            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append("\"").Append(Escape(values[i])).Append("\"");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string JsonStringOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "null";
            }

            return "\"" + Escape(value) + "\"";
        }

        private static string Escape(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    class MsfsAutostartManager
    {
        private const string AppName = "Simple Sim Connector";

        public static string Install(string exePath)
        {
            string[] baseFolders = GetCandidateBaseFolders();
            int changedCount = 0;
            string result = "";

            foreach (string baseFolder in baseFolders)
            {
                if (!Directory.Exists(baseFolder))
                {
                    continue;
                }

                string exeXmlPath = Path.Combine(baseFolder, "EXE.xml");
                InstallIntoExeXml(exeXmlPath, exePath);
                changedCount++;

                result += exeXmlPath + Environment.NewLine;
            }

            if (changedCount == 0)
            {
                throw new Exception(
                    "Could not find the MSFS 2024 LocalCache folder. Start MSFS 2024 once, then try again."
                );
            }

            return result.Trim();
        }

        public static string Uninstall()
        {
            string[] baseFolders = GetCandidateBaseFolders();
            int changedCount = 0;
            string result = "";

            foreach (string baseFolder in baseFolders)
            {
                if (!Directory.Exists(baseFolder))
                {
                    continue;
                }

                string exeXmlPath = Path.Combine(baseFolder, "EXE.xml");

                if (!File.Exists(exeXmlPath))
                {
                    continue;
                }

                bool removed = RemoveFromExeXml(exeXmlPath);

                if (removed)
                {
                    changedCount++;
                    result += exeXmlPath + Environment.NewLine;
                }
            }

            if (changedCount == 0)
            {
                return "No Simple Sim Connector autostart entry was found.";
            }

            return result.Trim();
        }

        private static string[] GetCandidateBaseFolders()
        {
            return new string[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache"
                ),

                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft Flight Simulator 2024"
                )
            };
        }

        private static void InstallIntoExeXml(string exeXmlPath, string exePath)
        {
            string directory = Path.GetDirectoryName(exeXmlPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlDocument doc = LoadOrCreateExeXml(exeXmlPath);
            XmlElement root = doc.DocumentElement;

            if (File.Exists(exeXmlPath))
            {
                string backupPath = exeXmlPath + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(exeXmlPath, backupPath, true);
            }

            XmlElement addon = FindAddonByName(root, AppName);

            if (addon == null)
            {
                addon = doc.CreateElement("Launch.Addon");
                root.AppendChild(addon);
            }
            else
            {
                addon.RemoveAll();
            }

            AppendElement(doc, addon, "Name", AppName);
            AppendElement(doc, addon, "Disabled", "False");
            AppendElement(doc, addon, "ManualLoad", "False");
            AppendElement(doc, addon, "Path", exePath);
            AppendElement(doc, addon, "CommandLine", "");
            AppendElement(doc, addon, "NewConsole", "False");

            SaveExeXml(doc, exeXmlPath);
        }

        private static bool RemoveFromExeXml(string exeXmlPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(exeXmlPath);

            XmlElement root = doc.DocumentElement;
            XmlElement addon = FindAddonByName(root, AppName);

            if (addon == null)
            {
                return false;
            }

            string backupPath = exeXmlPath + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(exeXmlPath, backupPath, true);

            root.RemoveChild(addon);
            SaveExeXml(doc, exeXmlPath);

            return true;
        }

        private static XmlDocument LoadOrCreateExeXml(string exeXmlPath)
        {
            XmlDocument doc = new XmlDocument();

            if (File.Exists(exeXmlPath))
            {
                doc.Load(exeXmlPath);
                return doc;
            }

            string xml =
                "<?xml version=\"1.0\" encoding=\"Windows-1252\"?>" +
                "<SimBase.Document Type=\"Launch\" version=\"1,0\">" +
                "<Descr>Auto launch external applications on MSFS start</Descr>" +
                "<Filename>EXE.xml</Filename>" +
                "<Disabled>False</Disabled>" +
                "<Launch.ManualLoad>False</Launch.ManualLoad>" +
                "</SimBase.Document>";

            doc.LoadXml(xml);
            return doc;
        }

        private static XmlElement FindAddonByName(XmlElement root, string name)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (node.Name != "Launch.Addon")
                {
                    continue;
                }

                XmlElement element = (XmlElement)node;
                string addonName = GetChildText(element, "Name");

                if (string.Equals(addonName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            return null;
        }

        private static string GetChildText(XmlElement parent, string childName)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name == childName)
                {
                    return node.InnerText;
                }
            }

            return "";
        }

        private static void AppendElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value ?? "";
            parent.AppendChild(element);
        }

        private static void SaveExeXml(XmlDocument doc, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.GetEncoding("Windows-1252");

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                doc.Save(writer);
            }
        }
    }
}
