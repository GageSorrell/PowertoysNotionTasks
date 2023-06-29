// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.NotionTask
{
    public partial class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable
    {
        // Should only be set in Init()
        private Action onPluginError;

        private PluginInitContext _context;

        private string _iconPath;

        private bool _disposed;

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
        // new PluginAdditionalOption()
        // {
        //     Key = NotGlobalIfUri,
        //     DisplayLabel = Properties.Resources.plugin_global_if_uri,
        //     Value = true,
        // },
        };

        public Main()
        {
            // GetPackages();
            // LoadInstalledList();
        }

        public List<Result> Query(Query query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var results = new List<Result>();

            bool queryIsTask = query.Search.StartsWith(Properties.Resources.plugin_silent_create, StringComparison.Ordinal) || query.Search.StartsWith(Properties.Resources.plugin_create_and_open, StringComparison.Ordinal);

            if (queryIsTask)
            {
                bool browserOpensNewTask = query.Search.StartsWith(Properties.Resources.plugin_create_and_open, StringComparison.Ordinal);

                string taskNameAbbreviated = query.Search.Length <= 22
                    ? query.Search.Length > 2

                        // Assumes that the create_and_open sequence is longer than the silent_create sequence
                        ? query.Search.StartsWith(Properties.Resources.plugin_create_and_open, StringComparison.Ordinal)
                            ? string.Concat("\"", query.Search.AsSpan(Properties.Resources.plugin_create_and_open.Length).ToString(), "\" ")
                            : string.Concat("\"", query.Search.AsSpan(Properties.Resources.plugin_silent_create.Length).ToString(), "\" ")
                        : string.Empty
                    : query.Search.StartsWith(Properties.Resources.plugin_create_and_open, StringComparison.Ordinal)
                        ? string.Concat("\"", query.Search.AsSpan(Properties.Resources.plugin_create_and_open.Length, 22).ToString(), "…\" ")
                        : string.Concat("\"", query.Search.AsSpan(Properties.Resources.plugin_silent_create.Length, 22).ToString(), "…\" ");
                results.Add(new Result
                {
                    Title = Properties.Resources.plugin_description,
                    SubTitle = browserOpensNewTask ? $"Create Task {taskNameAbbreviated}in Notion and View in the Browser." : $"Create Task {taskNameAbbreviated}in Notion.",
                    QueryTextDisplay = string.Empty,
                    IcoPath = _iconPath,
                    ProgramArguments = string.Empty,
                    Action = action =>
                    {
                        HttpClient httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Properties.Resources.plugin_notion_integration_secret}");
                        httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

                        string jsonPayload = @$"
                        {{";

                        if (Properties.Resources.plugin_icon_url != string.Empty)
                        {
                            jsonPayload += @$"
                                ""icon"": {{ ""type"": ""external"", ""external"": {{ ""url"": ""{Properties.Resources.plugin_icon_url}"" }} }},
                            ";
                        }

                        if (Properties.Resources.plugin_banner_url != string.Empty)
                        {
                            jsonPayload += @$"
                                ""cover"": {{
                                    ""external"": {{
                                        ""url"": ""{Properties.Resources.plugin_banner_url}""
                                    }}
                                }},
                            ";
                        }

                        jsonPayload += @$"
                            ""parent"": {{ ""database_id"": ""{Properties.Resources.plugin_notion_database_id}"" }},
                            ""properties"": {{
                                ""Name"": {{
                                    ""title"": [
                                        {{
                                            ""text"": {{
                                                ""content"": ""{query.Search.Substring(browserOpensNewTask ? Properties.Resources.plugin_create_and_open.Length : Properties.Resources.plugin_silent_create.Length)}""
                                            }}
                                        }}
                                    ]
                                }}
                            }}
                        }}";

                        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var task = httpClient.PostAsync("https://api.notion.com/v1/pages", httpContent);
                        task.Wait();
                        var response = task.Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var contentTask = response.Content.ReadAsStringAsync();
                            contentTask.Wait();
                            var responseContent = contentTask.Result;

                            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PowertoysTemp.txt");
                            File.WriteAllText(tempFilePath, responseContent);

                            if (browserOpensNewTask)
                            {
                                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                                {
                                    // Get the URL property
                                    if (doc.RootElement.TryGetProperty("url", out var urlElement))
                                    {
                                        // Get the URL as a string
                                        string extractedUrl = urlElement.GetString();

                                        // Open the URL in a browser
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = extractedUrl,
                                            UseShellExecute = true,
                                        });
                                    }
                                    else
                                    {
                                        string tempFilePathInner = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PowertoysTempInner.txt");
                                        File.WriteAllText(tempFilePathInner, responseContent);

                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = tempFilePathInner,
                                            UseShellExecute = true,
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            var errorContentTask = response.Content.ReadAsStringAsync();
                            errorContentTask.Wait();
                            var errorContent = errorContentTask.Result;

                            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PowertoysTemp.txt");
                            File.WriteAllText(tempFilePath, errorContent);

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = tempFilePath,
                                UseShellExecute = true,
                            });
                        }

                        return true;
                    },
                });
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();

            onPluginError = () =>
            {
                string errorMsgString = string.Format(CultureInfo.CurrentCulture, Properties.Resources.plugin_search_failed, BrowserInfo.Name ?? BrowserInfo.MSEdgeName);

                Log.Error(errorMsgString, GetType());
                _context.API.ShowMsg(
                    $"Plugin: {Properties.Resources.plugin_name}",
                    errorMsgString);
            };
        }

        private static List<ContextMenuResult> GetContextMenu(in Result result, in string assemblyName)
        {
            return new List<ContextMenuResult>(0);
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return GetContextMenu(selectedResult, "someassemblyname");
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/NotionTask.light.png";
            }
            else
            {
                _iconPath = "Images/NotionTask.dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            // _notGlobalIfUri = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == NotGlobalIfUri)?.Value ?? false;
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
