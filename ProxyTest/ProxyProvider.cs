//
// From NuGet src/Core
// Based on ProxyCache
//
// Copyright (c) 2010-2014 Outercurve Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Configuration;
using System.Net;
using System.Net.Configuration;

namespace ProxyTest
{
	static class ProxyProvider
	{
		/// <summary>
		/// Capture the default System Proxy so that it can be re-used by the IProxyFinder
		/// because we can't rely on WebRequest.DefaultWebProxy since someone can modify the DefaultWebProxy
		/// property and we can't tell if it was modified and if we are still using System Proxy Settings or not.
		/// </summary>
		static readonly IWebProxy originalSystemProxy = WebRequest.GetSystemWebProxy ();

		public static IWebProxy GetProxy (Uri uri)
		{
			if (!IsSystemProxySet (uri)) {
				Console.WriteLine ("No proxy found for '{0}'", uri);
				return null;
			}

			return GetSystemProxy (uri);
		}

		static WebProxy GetSystemProxy (Uri uri)
		{
			// WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
			// proxy settings instead of the WebRequest.GetSystemProxy()
			var proxyUri = originalSystemProxy.GetProxy (uri);
			Console.WriteLine ("Proxy uri '{0}'", uri);

			return new WebProxy (proxyUri);
		}

		/// <summary>
		/// Return true or false if connecting through a proxy server
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		static bool IsSystemProxySet (Uri uri)
		{
			CheckProxyConfigSettings ();
			CheckMacProxy (uri);

			Console.WriteLine ("# Mono's WebRequest");
			Console.WriteLine ("WebRequest.GetSystemWebProxy().GetType: {0}", originalSystemProxy.GetType ().Name);
			var systemProxy = GetSystemProxy (uri);
			Console.WriteLine ("WebRequest.GetSystemWebProxy().GetProxy() returned proxy: Uri: '{0}'", uri);

			// The reason for not calling the GetSystemProxy is because the object
			// that will be returned is no longer going to be the proxy that is set by the settings
			// on the users machine only the Address is going to be the same.
			// Not sure why the .NET team did not want to expose all of the useful settings like
			// ByPass list and other settings that we can't get because of it.
			// Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
			// getting the proxy for to should be bypassed or not. If it should be bypassed then
			// return that we don't need a proxy and we should try to connect directly.
			IWebProxy proxy = WebRequest.DefaultWebProxy;
			if (proxy != null) {
				Console.WriteLine ("WebRequest.DefaultWebProxy.GetType: {0}", proxy.GetType ().Name);
				Uri proxyAddress = new Uri (proxy.GetProxy (uri).AbsoluteUri);
				if (string.Equals (proxyAddress.AbsoluteUri, uri.AbsoluteUri)) {
					Console.WriteLine ("ProxyAddress matches request uri. Ignoring proxy uri: '{0}'", proxyAddress);
					return false;
				}
				if (proxy.IsBypassed (uri)) {
					Console.WriteLine ("Proxy IsByPassed for '{0}'", uri);
					return false;
				}
			} else {
				Console.WriteLine ("WebRequest.DefaultWebProxy is null. Trying WebRequest.GetSystemWebProxy");
				proxy = GetSystemProxy (uri);
				if (proxy == null) {
					Console.WriteLine ("WebRequest.GetSystemWebProxy returned null");
				}
			}

			return proxy != null;
		}

		static void CheckProxyConfigSettings ()
		{
			Console.WriteLine ("# ConfigurationSection");
			var section = ConfigurationManager.GetSection ("system.net/defaultProxy") as DefaultProxySection;
			if (section != null) {
				Console.WriteLine ("Found 'system.net/defaultProxy' config section. Enabled={0}", section.Enabled);
				if (section.Enabled) {
					if (section.Proxy != null) {
						var proxy = section.Proxy;
						Console.WriteLine ("  'system.net/defaultProxy': Proxy.ProxyAddress: '{0}'", proxy.ProxyAddress);
						Console.WriteLine ("  'system.net/defaultProxy': Proxy.ScriptLocation: '{0}'", proxy.ScriptLocation);
						Console.WriteLine ("  'system.net/defaultProxy': Proxy.AutoDetect: {0}", proxy.AutoDetect);
						Console.WriteLine ("  'system.net/defaultProxy': Proxy.BypassOnLocal: {0}", proxy.BypassOnLocal);
						Console.WriteLine ("  'system.net/defaultProxy': Proxy.UseSystemDefault: {0}", proxy.UseSystemDefault);
						if (section.BypassList != null) {
							foreach (var bypass in section.BypassList) {
								var bypassElement = bypass as BypassElement;
								if (bypassElement != null) {
									Console.WriteLine ("  'system.net/defaultProxy' config section: ByPass: {0}", bypassElement.Address);
								}
							}
						}
					} else {
						Console.WriteLine ("No proxy element in 'system.net/defaultProxy'");
					}
				}
			} else {
				Console.WriteLine ("No 'system.net/defaultProxy' config section");
			}
			Console.WriteLine ();
		}

		static void CheckMacProxy (Uri uri)
		{
			Console.WriteLine ("# Mono's CFNetwork");
			var defaultProxy = CFNetwork.GetDefaultProxy ();
			if (defaultProxy != null) {
				Console.WriteLine ("Got default proxy from CFNetwork.GetDefaultProxy");
				var proxyAddress = new Uri (defaultProxy.GetProxy (uri).AbsoluteUri);
				if (string.Equals (proxyAddress.AbsoluteUri, uri.AbsoluteUri)) {
					Console.WriteLine ("CFNetwork.GetDefaultProxy. ProxyAddress matches request uri. Ignoring proxy uri: '{0}'", proxyAddress);
				} else {
					Console.WriteLine ("Found proxy from CFNetwork.GetDefaultProxy. Url '{0}'", proxyAddress.AbsoluteUri);
				}
			}
			Console.WriteLine ();
		}
	}
}