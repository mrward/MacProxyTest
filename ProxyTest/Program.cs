//
// Program.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AppKit;

namespace ProxyTest
{
	class MainClass
	{
		static readonly string defaultUrl = "https://www.nuget.org";

		public static void Main (string[] args)
		{
			try {
				NSApplication.Init ();
				Task task = Run (args);
				task.Wait ();
			} catch (Exception ex) {
				Console.WriteLine (ex.GetBaseException ());
			}
		}

		static Task Run (string[] args)
		{
			Uri url = GetUrl (args);
			IWebProxy proxy = GetProxy (url);

			Console.WriteLine ();
			return Task.Run (() => Connect (url, proxy));
		}

		static IWebProxy GetProxy (Uri uri)
		{
			Console.WriteLine ("# Xamarin.Mac");

			// Check Proxy types
			var proxies = CoreFoundation.CFNetwork.GetProxiesForUri (uri, null);
			if (proxies?.Any () == true) {
				Console.WriteLine ("Proxy information found:");
				foreach (var proxy in proxies) {
					Console.WriteLine ("  ProxyType: {0}", proxy.ProxyType);
					Console.WriteLine ("  ProxyAddress: '{0}'", proxy.HostName);
					Console.WriteLine ("  ProxyPort: {0}", proxy.Port);
					if (proxy.AutoConfigurationUrl != null) {
						Console.WriteLine ("  ProxyAutoConfigurationUrl: '{0}'", proxy.AutoConfigurationUrl);
					}
					if (!string.IsNullOrEmpty (proxy.AutoConfigurationJavaScript)) {
						Console.WriteLine ("  Proxy has AutoConfigurationJavaScript");
					}
					Console.WriteLine ();
				}
			}

			// Get the proxy that Mono finds.
			return ProxyProvider.GetProxy (uri);
		}

		static Uri GetUrl (string[] args)
		{
			if (args.Length == 0)
				return new Uri (defaultUrl);

			return new Uri (args [0]);
		}

		static async Task Connect (Uri uri, IWebProxy proxy)
		{
			Console.WriteLine ("# HttpClient connection test:");
			var handler = GetMessageHandler (uri, proxy);
			var client = new HttpClient (handler);
			var request = new HttpRequestMessage (HttpMethod.Get, uri);

			Console.WriteLine ("Sending HTTP get to '{0}'", uri);
			var response = await client.SendAsync (request).ConfigureAwait (false);

			Console.WriteLine ("Response.StatusCode: {0}", response.StatusCode);
		}

		static HttpMessageHandler GetMessageHandler (Uri uri, IWebProxy proxy)
		{
			var handler = new HttpClientHandler ();
			HttpMessageHandler messageHandler = handler;

			if (proxy != null) {
				messageHandler = new ProxyAuthenticationHandler (proxy) {
					InnerHandler = handler
				};
				handler.Proxy = proxy;
				proxy.Credentials = CredentialCache.DefaultCredentials;
			}

			return messageHandler;
		}
	}
}
