//
// ProxyAuthenticationHandler.cs
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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTest
{
	class ProxyAuthenticationHandler : DelegatingHandler
	{
		IWebProxy proxy;

		internal ProxyAuthenticationHandler (IWebProxy proxy)
		{
			this.proxy = proxy;
		}

		protected async override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
			bool retry = false;
			while (true) {
				try {
					var response = await base.SendAsync (request, cancellationToken);

					if (response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired) {
						return response;
					}

					if (retry) {
						Console.WriteLine ("Proxy credentials rejected for '{0}'", request.RequestUri);
						return response;
					}

					if (!AcquireProxyCredentialsAsync (request.RequestUri)) {
						return response;
					}
				} catch (Exception ex)
				  when (IsProxyAuthenticationRequired (ex)) {
					if (!AcquireProxyCredentialsAsync (request.RequestUri)) {
						throw;
					}
				}

				retry = true;
			}
		}

		static bool IsProxyAuthenticationRequired (Exception ex)
		{
			var webException = ex.InnerException as WebException;
			var response = webException?.Response as HttpWebResponse;
			return response?.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
		}

		bool AcquireProxyCredentialsAsync (Uri url)
		{
			Console.WriteLine ("ProxyAuthenticationRequired for '{0}'", url);
			var credentials = MacProxyCredentialProvider.GetCredentials (url, proxy);

			if (credentials != null) {
				Console.WriteLine ("Credentials found in KeyChain for for '{0}'", url);
				proxy.Credentials = credentials;
				return true;
			} else {
				Console.WriteLine ("No credentials found in KeyChain for for '{0}'", url);
			}
			return false;
		}
	}
}