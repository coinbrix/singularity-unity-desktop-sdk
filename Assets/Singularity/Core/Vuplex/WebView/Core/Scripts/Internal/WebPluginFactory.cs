// Copyright (c) 2023 Vuplex Inc. All rights reserved.
//
// Licensed under the Vuplex Commercial Software Library License, you may
// not use this file except in compliance with the License. You may obtain
// a copy of the License at
//
//     https://vuplex.com/commercial-library-license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    public class WebPluginFactory {

        public virtual List<IWebPlugin> GetAllPlugins() => _allPlugins.ToList();

        public virtual IWebPlugin GetDefaultPlugin(WebPluginType[] preferredPlugins = null) {

            var isServerBuild = false;
            #if UNITY_SERVER
                isServerBuild = true;
            #endif
            if (isServerBuild) {
                _logMockWarningOnce("3D WebView doesn't support the \"Server Build\" option because it uses a null graphics device (GraphicsDeviceType.Null)");
                return MockWebPlugin.Instance;
            }

            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                return _choosePlugin(_standalonePlugin, "Windows or macOS", "3D WebView for Windows and macOS", "windows-mac");
            #elif UNITY_ANDROID
                var preferChromiumAndroidPlugin = preferredPlugins != null && preferredPlugins.Contains(WebPluginType.Android);
                IWebPlugin selectedAndroidPlugin = null;
                if (_androidPlugin != null && (_androidGeckoPlugin == null || preferChromiumAndroidPlugin)) {
                    selectedAndroidPlugin = _androidPlugin;
                } else if (_androidGeckoPlugin != null) {
                    selectedAndroidPlugin = _androidGeckoPlugin;
                }
                return _choosePlugin(selectedAndroidPlugin, "Android", "3D WebView for Android", "android");
            #elif UNITY_IOS
                return _choosePlugin(_iosPlugin, "iOS", "3D WebView for iOS", "ios");
            #elif UNITY_WSA
                return _choosePlugin(_uwpPlugin, "UWP", "3D WebView for UWP", "uwp");
            #elif UNITY_WEBGL
                return _choosePlugin(_webGLPlugin, "WebGL", "2D WebView for WebGL", "webgl");
            #else
                throw new WebViewUnavailableException("3D WebView is not supported on the current build platform. For more info, please visit https://developer.vuplex.com .");
            #endif
        }

        public static void RegisterAndroidPlugin(IWebPlugin plugin) {

            _addPlugin(_androidPlugin = plugin);
        }

        public static void RegisterAndroidGeckoPlugin(IWebPlugin plugin) {

            _addPlugin(_androidGeckoPlugin = plugin);
        }

        public static void RegisterIOSPlugin(IWebPlugin plugin) {

            _addPlugin(_iosPlugin = plugin);
        }

        public static void RegisterUwpPlugin(IWebPlugin plugin) {

            _addPlugin(_uwpPlugin = plugin);
        }

        public static void RegisterWebGLPlugin(IWebPlugin plugin) {

            _addPlugin(_webGLPlugin = plugin);
        }

        public static void RegisterStandalonePlugin(IWebPlugin plugin) {

            _addPlugin(_standalonePlugin = plugin);
        }

        protected static HashSet<IWebPlugin> _allPlugins = new HashSet<IWebPlugin>();
        protected static IWebPlugin _androidPlugin;
        protected static IWebPlugin _androidGeckoPlugin;
        protected static IWebPlugin _iosPlugin;
        bool _mockWarningLogged;
        protected static IWebPlugin _standalonePlugin;
        protected static IWebPlugin _uwpPlugin;
        protected static IWebPlugin _webGLPlugin;

        static void _addPlugin(IWebPlugin plugin) {

            if (plugin != null) {
                _allPlugins.Add(plugin);
            }
        }

        IWebPlugin _choosePlugin(IWebPlugin plugin, string buildPlatform, string packageName, string storeUrlPath) {

            if (plugin == null) {
                throw new WebViewUnavailableException($"The build platform is set to {buildPlatform}, but {packageName} isn't installed in the project. {packageName} is required in order for 3D WebView to work on {buildPlatform}." + _getMoreInfoText(storeUrlPath));
            }
            if ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor) && _standalonePlugin != null) {
                return _standalonePlugin;
            }
            if (plugin is MockWebPlugin) {
                if (Application.platform == RuntimePlatform.LinuxEditor) {
                    _logMockWarningOnce("3D WebView doesn't support the Linux Unity Editor");
                } else {
                    _logMockWarningOnce("3D WebView for Windows and macOS is not currently installed");
                }
            }
            return plugin;
        }

        string _getMoreInfoText(string storeUrlPath) => $" For more info, please visit https://store.vuplex.com/webview/{storeUrlPath} .";

        /// <summary>
        /// Logs the warning once so that it doesn't spam the console.
        /// </summary>
        void _logMockWarningOnce(string reason) {

            if (!_mockWarningLogged) {
                _mockWarningLogged = true;
                WebViewLogger.LogWarning($"{reason}, so the mock webview will be used{(Application.isEditor ? " while running in the editor" : "")}. For more info, please see <em>https://support.vuplex.com/articles/mock-webview</em>.");
            }
        }
    }
}
