#nullable enable

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace CustomNameList
{
    class AddNameRequestInput
    { 
        public string? UserName { get; set; }
    }
    
    class RemoveNameRequestInput
    { 
        public string? UserName { get; set; }
        public bool Banned { get; set; }
    }
    
    class UnbanRequestInput
    { 
        public string? UserName { get; set; }
    }

    class AdminListResponse
    {
        public List<string> QueuedNames { get; set; } = new List<string>();
        public List<string> ActiveNames { get; set; } = new List<string>();
        public List<string> BannedNames { get; set; } = new List<string>();
    }
    
    class CustomNameService {
        private static string _adminHtmlFile = $"{Path.GetDirectoryName(Paths.ExecutablePath)}/BepInEx/plugins/ChatbotNames/admin.html";
        private static string _saveFile = $"{Path.GetDirectoryName(Paths.ExecutablePath)}/BepInEx/plugins/ChatbotNames/saved_names.json";
        
        private List<string> _namesQueue = new();
        private HashSet<string> _activeNames = new();
        private HashSet<string> _bannedNames = new();
        private ManualLogSource _logger;
        private static Timer _saveTimer;

        internal bool IsInitialized { get; private set; }

        internal void Init(ManualLogSource log) {
            _logger = log;

            if (File.Exists(_saveFile))
            {
                var saveData = File.ReadAllText(_saveFile);
                var loadedNames = JsonConvert.DeserializeObject<AdminListResponse>(saveData);
                if (loadedNames != null)
                {
                    _activeNames = loadedNames.ActiveNames.ToHashSet();
                    _bannedNames = loadedNames.BannedNames.ToHashSet();
                    _namesQueue = loadedNames.QueuedNames;
                }
            }
            
            Task.Run(StartHttpServer);

            _saveTimer = new Timer(5000);
            _saveTimer.Elapsed += SaveTimerTick;
            _saveTimer.Start();

            IsInitialized = true;
        }

        private void SaveTimerTick(object sender, ElapsedEventArgs e)
        {
            AdminListResponse savedNames = new()
            {
                QueuedNames = _namesQueue,
                ActiveNames = _activeNames.ToList(),
                BannedNames = _bannedNames.ToList()
            };
            
            var serializedNames = JsonConvert.SerializeObject(savedNames);
            File.WriteAllText(_saveFile, serializedNames);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool IsActive(string name)
        {
            return _activeNames.Contains(name);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void AddToQueue(string name)
        {
            if (!_namesQueue.Contains(name) && !_bannedNames.Contains(name))
            {
                Plugin.Log.LogInfo($"Requeueing character {name} Killed.");
                _namesQueue.Add(name);
                _activeNames.Remove(name);
            }
        }
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void RemoveFromQueue(string userName, bool banned)
        {
            if (banned)
            {
                _bannedNames.Add(userName);
            }
            
            _activeNames.Remove(userName);
            _namesQueue.Remove(userName);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal string? NextName()
        {
            if (_namesQueue.Count == 0)
            {
                _logger.LogInfo("No names available");
                return null;
            }
            
            var name = _namesQueue[0];
            _namesQueue.RemoveAt(0);

            _activeNames.Add(name);
            _logger.LogInfo($"Dequeued next name {name}");
            
            return name;
        }

        private void StartHttpServer()
        {
            var Port = 9001;
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");
            listener.Start();

            _logger.LogInfo($"Listening on port {Port}");

            var listenTask = HandleIncomingConnections(listener);
            listenTask.GetAwaiter().GetResult();
            listener.Close();
        }

        private async Task HandleIncomingConnections(HttpListener listener)
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;
                string? responseText = null;
                string contentType = "text/plain";
                int statusCode = 200;
                
                try
                {
                    _logger.LogInfo($"Incoming Request: {request.HttpMethod} {request.Url.PathAndQuery}");

                    var bodyReader = new StreamReader(request.InputStream);
                    var requestBody = await bodyReader.ReadToEndAsync();

                    switch (request.Url.PathAndQuery)
                    {
                        case "/add" when request.HttpMethod == "POST":
                        {
                            var requestInput = JsonConvert.DeserializeObject<AddNameRequestInput>(requestBody);
                            var userName = requestInput?.UserName;
                            if (userName != null)
                            {
                                _logger.LogInfo($"Attempting to add new beaver {userName}");
                                if (_bannedNames.Contains(userName))
                                {
                                    _logger.LogInfo($"{userName} is banned");
                                    responseText = "banned";
                                }
                                else if (_namesQueue.Contains(userName) || _activeNames.Contains(userName))
                                {
                                    _logger.LogInfo($"{userName} is already in the queue");
                                    responseText = "already_registered";
                                }
                                else
                                {
                                    AddToQueue(userName);
                                    _logger.LogInfo($"{userName} has been added to the queue");
                                    responseText = "success";
                                }
                            }

                            break;
                        }
                        case "/remove" when request.HttpMethod == "POST":
                        {
                            var requestInput = JsonConvert.DeserializeObject<RemoveNameRequestInput>(requestBody);
                            if (requestInput?.UserName != null)
                            {
                                RemoveFromQueue(requestInput.UserName, requestInput.Banned);
                            }
                            break;
                        }
                        case "/unban" when request.HttpMethod == "POST":
                        {
                            var requestInput = JsonConvert.DeserializeObject<UnbanRequestInput>(requestBody);
                            if (requestInput?.UserName != null)
                            {
                                _bannedNames.Remove(requestInput.UserName);
                            }
                            break;
                        }
                        case "/reset" when request.HttpMethod == "POST":
                        {
                            _activeNames.Clear();
                            _namesQueue.Clear();
                            break;
                        }
                        case "/list" when request.HttpMethod == "GET":
                        {
                            var namesQueueCopy = _namesQueue.Take(5);
                            responseText = string.Join(", ", namesQueueCopy);
                            break;
                        }
                        case "/adminlist" when request.HttpMethod == "GET":
                        {
                            AdminListResponse adminlistResponse = new()
                            {
                                QueuedNames = _namesQueue,
                                ActiveNames = _activeNames.ToList(),
                                BannedNames = _bannedNames.ToList()
                            };
                            responseText = JsonConvert.SerializeObject(adminlistResponse);
                            break;
                        }
                        case "/admin" when request.HttpMethod == "GET":
                        {
                            responseText = await File.ReadAllTextAsync(_adminHtmlFile);
                            contentType = "text/html";
                            break;
                        }
                        case "/" when request.HttpMethod == "GET":
                        {
                            responseText = "Beaver Name Service is Online";
                            break;
                        }
                        default:
                        {
                            responseText = "Not Found";
                            statusCode = 404;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error executing request: {e.Message}");
                    statusCode = 500;
                    responseText = e.Message;
                }
                
                response.StatusCode = statusCode;
                response.ContentType = contentType;
                    
                if (responseText != null)
                {
                    var responseData = Encoding.UTF8.GetBytes(responseText);
                    response.ContentLength64 = responseData.Length;
                    await response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
                }
                else
                {
                    response.ContentLength64 = 0;
                }

                response.Close();
            }
        }
    }
}