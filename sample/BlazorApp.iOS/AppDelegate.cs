// <copyright file="AppDelegate.cs" company="Steve Sanderson and Jan-Willem Spuij">
// Copyright 2020 Steve Sanderson and Jan-Willem Spuij
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace BlazorApp.iOS
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    using Foundation;
    using UIKit;

    /// <summary>
    /// The UIApplicationDelegate for the application. This class is responsible for launching the
    /// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : UIResponder, IUIApplicationDelegate
    {
        /// <summary>
        /// Gets or sets the window.
        /// </summary>
        [Export("window")]
        public UIWindow Window { get; set; }

        /// <summary>
        /// Override point for customization after application launch.
        /// If not required for your application you can safely delete this method.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="launchOptions">The launch options.</param>
        /// <returns>A boolean indicating success.</returns>
        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            return true;
        }

        /// <summary>
        /// Called when a new scene session is being created.
        /// Use this method to select a configuration to create the new scene with.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="connectingSceneSession">The connecting scene session.</param>
        /// <param name="options">The scene connection options.</param>
        /// <returns>A scene configuration.</returns>
        [Export("application:configurationForConnectingSceneSession:options:")]
        public UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
        {
            return UISceneConfiguration.Create("Default Configuration", connectingSceneSession.Role);
        }

        /// <summary>
        /// Called when the user discards a scene session.
        /// If any sessions were discarded while the application was not running, this will be called shortly after `FinishedLaunching`.
        /// Use this method to release any resources that were specific to the discarded scenes, as they will not return.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="sceneSessions">The set of discarded scene sessions.</param>
        [Export("application:didDiscardSceneSessions:")]
        public void DidDiscardSceneSessions(UIApplication application, NSSet<UISceneSession> sceneSessions)
        {
        }
    }
}