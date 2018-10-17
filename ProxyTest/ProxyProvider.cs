﻿//
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
using System.Net;

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
	}
}